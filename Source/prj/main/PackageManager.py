
import os

from mtm.util.VarManager import VarManager
from mtm.log.Logger import Logger
from mtm.util.SystemHelper import SystemHelper
import mtm.util.JunctionUtil as JunctionUtil
import mtm.util.Util as Util

from prj.main.ProjectSchemaLoader import FolderTypes
from mtm.util.Platforms import Platforms

import shutil

from mtm.util.CommonSettings import ConfigFileName
import mtm.util.MiscUtil as MiscUtil
import mtm.util.PlatformUtil as PlatformUtil

from prj.reg.PackageInfo import PackageInstallInfo
from mtm.util.Assert import *
from prj.reg.PackageInfo import PackageInfo

from datetime import datetime
import mtm.util.YamlSerializer as YamlSerializer
import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
from mtm.ioc.Inject import InjectMany
import mtm.ioc.IocAssertions as Assertions

InstallInfoFileName = 'ProjenyInstall.yaml'

from prj.main.ProjenyConstants import ProjectConfigFileName

class SourceControlTypes:
    Git = 'Git'
    Subversion = 'Subversion'
    # TODO - how to detect?
    #Perforce = 'Perforce'

class PackageManager:
    """
    Main interface for Modest Package Manager
    """
    _config = Inject('Config')
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')
    _unityHelper = Inject('UnityHelper')
    _junctionHelper = Inject('JunctionHelper')
    _projectInitHandlers = InjectMany('ProjectInitHandlers')
    _schemaLoader = Inject('ProjectSchemaLoader')
    _commonSettings = Inject('CommonSettings')

    def __init__(self):
        self._packageInfos = None

    def projectExists(self, projectName):
        return self._sys.directoryExists('[UnityProjectsDir]/{0}'.format(projectName))

    def listAllProjects(self):
        projectNames = self.getAllProjectNames()

        defaultProj = self._config.tryGetString(None, 'DefaultProject')

        self._log.info("Found {0} Projects:".format(len(projectNames)))
        for proj in projectNames:
            alias = self.tryGetAliasFromFullName(proj)
            output = proj
            if alias:
                output = "{0} ({1})".format(output, alias)

            if defaultProj == proj:
                output += " (default)"

            self._log.info("  " + output)

    def listUnusedPackages(self):

        usedPackages = set()
        for projName in self.getAllProjectNames():
            with self._log.heading("Looking up packages that are used by '{0}'", projName):
                for platform in Platforms.All:
                    schema = self._schemaLoader.loadSchema(projName, platform)

                    for info in schema.packages.values():
                        usedPackages.add(info.name)

        unusedPackages = [x for x in self.getAllPackageNames() if x not in usedPackages]

        self._log.info("Found {0} unused packages:", len(unusedPackages))

        for packageName in unusedPackages:
            self._log.info("   " + packageName)

    def listAllPackages(self):
        packagesNames = self.getAllPackageNames()
        self._log.info("Found {0} Packages:".format(len(packagesNames)))
        for packageName in packagesNames:
            self._log.info("  " + packageName)

    def _findSourceControl(self):
        for dirPath in self._sys.getParentDirectoriesWithSelf('[ConfigDir]'):
            if self._sys.directoryExists(os.path.join(dirPath, '.git')):
                return SourceControlTypes.Git

            if self._sys.directoryExists(os.path.join(dirPath, '.svn')):
                return SourceControlTypes.Subversion

        return None

    def _createProject(self, projName):
        with self._log.heading('Initializing new project "{0}"', projName):
            projDirPath = self._varMgr.expand('[UnityProjectsDir]/{0}'.format(projName))
            assertThat(not self._sys.directoryExists(projDirPath), "Cannot initialize new project '{0}', found existing project at '{1}'", projName, projDirPath)

            self._sys.createDirectory(projDirPath)

            with self._sys.openOutputFile(os.path.join(projDirPath, ProjectConfigFileName)) as outFile:
                outFile.write(
"""
ProjectSettingsPath: '[ProjectRoot]/ProjectSettings'
#AssetsFolder:
    # Uncomment and Add package names here
""")

            self._sys.createDirectory('{0}/ProjectSettings'.format(projDirPath))

            self.updateProjectJunctions(projName, Platforms.Windows)

    def getProjectFromAlias(self, alias):
        result = self.tryGetProjectFromAlias(alias)
        assertThat(result, "Unrecognized project '{0}' and could not find an alias with that name either".format(alias))
        return result

    def tryGetProjectFromAlias(self, alias):
        aliasMap = self._config.tryGetDictionary({}, 'ProjectAliases')

        if alias not in aliasMap.keys():
            return None

        return aliasMap[alias]

    def tryGetAliasFromFullName(self, name):
        aliasMap = self._config.tryGetDictionary({}, 'ProjectAliases')

        for pair in aliasMap.items():
            if pair[1] == name:
                return pair[0]

        return None

    def _validateDirForFolderType(self, packageInfo, sourceDir):
        if packageInfo.folderType == FolderTypes.AndroidProject:
            assertThat(os.path.exists(os.path.join(sourceDir, "project.properties")), "Project '{0}' is marked with foldertype AndroidProject and therefore must contain a project.properties file".format(packageInfo.name))

    def updateProjectJunctions(self, projectName, platform):
        """
        Initialize all the folder links for the given project
        """

        with self._log.heading('Updating package directories for project {0}'.format(projectName)):
            self.checkProjectInitialized(projectName, platform)
            self.setPathsForProject(projectName, platform)
            schema = self._schemaLoader.loadSchema(projectName, platform)
            self._updateDirLinksForSchema(schema)

            self._checkForVersionControlIgnore()

            self._log.good('Finished updating packages for project "{0}"'.format(schema.name))

    def _checkForVersionControlIgnore(self):
        sourceControlType = self._findSourceControl()

        if sourceControlType == SourceControlTypes.Git:
            self._log.info('Detected git repository.  Making sure generated project folders are ignored by git...')
            if not self._sys.fileExists('[ProjectRoot]/.gitignore'):
                self._sys.copyFile('[ProjectRootGitIgnoreTemplate]', '[ProjectRoot]/.gitignore')
                self._log.warn('Added new git ignore file to project root')
        elif sourceControlType == SourceControlTypes.Subversion:
            self._log.info('Detected subversion repository. Making sure generated project folders are ignored by SVN...')
            try:
                self._sys.executeAndWait('svn propset svn:ignore -F [ProjectRootSvnIgnoreTemplate] .', '[ProjectRoot]')
            except Exception as e:
                self._log.warn("Warning: Failed to add generated project directories to SVN ignore!  This may be caused by 'svn' not being available on the command line.  Details: " + str(e))
        #else:
            #self._log.warn('Warning: Could not determine source control in use!  An ignore file will not be added for your project.')

    def getAllPackageInfos(self):
        if not self._packageInfos:
            self._packageInfos = []

            for name in self.getAllPackageNames():
                path = self._varMgr.expandPath('[UnityPackagesDir]/{0}'.format(name))

                installInfoFilePath = os.path.join(path, InstallInfoFileName)

                info = PackageInfo()
                info.name = name
                info.path = path

                if self._sys.fileExists(installInfoFilePath):
                    installInfo = YamlSerializer.deserialize(self._sys.readFileAsText(installInfoFilePath))
                    info.installInfo = installInfo

                self._packageInfos.append(info)

        return self._packageInfos

    def deleteProject(self, projName):
        with self._log.heading("Deleting project '{0}'", projName):
            assertThat(self._varMgr.hasKey('UnityProjectsDir'), "Could not find 'UnityProjectsDir' in PathVars.  Have you set up your {0} file?", ConfigFileName)
            fullPath = '[UnityProjectsDir]/{0}'.format(projName)

            assertThat(self._sys.directoryExists(fullPath), "Could not find project with name '{0}' - delete failed", projName)

            self.clearProjectGeneratedFiles(projName)
            self._sys.deleteDirectory(fullPath)

    def createPackage(self, packageName):
        assertThat(self._varMgr.hasKey('UnityPackagesDir'), "Could not find 'UnityPackagesDir' in PathVars.  Have you set up your {0} file?", ConfigFileName)

        with self._log.heading('Creating new package "{0}"', packageName):
            newPath = '[UnityPackagesDir]/{0}'.format(packageName)
            assertThat(not self._sys.directoryExists(newPath), "Found existing package at path '{0}'", newPath)
            self._sys.createDirectory(newPath)

            # This can be nice when sorting packages by install date, but it adds noise to the directory
            # Seems nicer to just leave the directory empty for custom packages

            #newInstallInfo = PackageInstallInfo()
            #newInstallInfo.releaseInfo = None
            #newInstallInfo.installDate = datetime.utcnow()

            #yamlStr = YamlSerializer.serialize(newInstallInfo)
            #self._sys.writeFileAsText(os.path.join(newPath, InstallInfoFileName), yamlStr)

    def deletePackage(self, name):
        with self._log.heading("Deleting package '{0}'", name):
            assertThat(self._varMgr.hasKey('UnityPackagesDir'), "Could not find 'UnityPackagesDir' in PathVars.  Have you set up your {0} file?", ConfigFileName)
            fullPath = '[UnityPackagesDir]/{0}'.format(name)

            assertThat(self._sys.directoryExists(fullPath), "Could not find package with name '{0}' - delete failed", name)

            self._sys.deleteDirectory(fullPath)

    def getAllPackageNames(self):
        assertThat(self._varMgr.hasKey('UnityPackagesDir'), "Could not find 'UnityPackagesDir' in PathVars.  Have you set up your {0} file?", ConfigFileName)

        results = []
        for name in self._sys.walkDir('[UnityPackagesDir]'):
            if self._sys.IsDir('[UnityPackagesDir]/' + name):
                results.append(name)
        return results

    def getAllProjectNames(self):
        assertThat(self._varMgr.hasKey('UnityProjectsDir'), "Could not find 'UnityProjectsDir' in PathVars.  Have you set up your {0} file?", ConfigFileName)

        results = []
        for name in self._sys.walkDir('[UnityProjectsDir]'):
            if self._sys.IsDir('[UnityProjectsDir]/' + name):
                results.append(name)
        return results

    # This will set up all the directory junctions for all projects for all platforms
    def updateLinksForAllProjects(self):
        for projectName in self.getAllProjectNames():
            with self._log.heading('Initializing project "{0}"'.format(projectName)):
                try:
                    #for platform in Platforms.All:
                    for platform in [Platforms.Windows]:
                        self.updateProjectJunctions(projectName, platform)

                    self._log.good('Successfully initialized project "{0}"'.format(projectName))
                except Exception as e:
                    self._log.warn('Failed to initialize project "{0}": {1}'.format(projectName, e))

    def _createPlaceholderCsFile(self, path):
        with self._sys.openOutputFile(path) as outFile:
            outFile.write(
"""
    // This file exists purely as a way to force unity to generate the MonoDevelop csproj files so that Projeny can read the settings from it
""")

    def _createSwitchProjectMenuScript(self, currentProjName, outputPath):

        foundCurrent = False
        menuFile = """
using UnityEditor;

namespace Projeny
{
    public static class ProjenyChangeProjectMenu
    {"""
        projIndex = 1
        for projName in self.getAllProjectNames():
            menuFile += """
        [MenuItem("Projeny/Change Project/{0}", false, 8)]""".format(projName)

            menuFile += """
        public static void ChangeProject{0}()""".format(projIndex)

            menuFile += """
        {"""

            menuFile += """
            PrjHelper.ChangeProject("{0}");""".format(projName)

            menuFile += """
        }
"""
            if projName == currentProjName:
                assertThat(not foundCurrent)
                foundCurrent = True
                menuFile += """
        [MenuItem("Projeny/Change Project/{0}", true, 8)]""".format(currentProjName)
                menuFile += """
        public static bool ChangeProject{0}Validate()""".format(projIndex)
                menuFile += """
        {
            return false;
        }"""

            projIndex += 1

        menuFile += """
    }
}
"""
        #assertThat(foundCurrent, "Could not find project " + currentProjName)
        self._sys.writeFileAsText(outputPath, menuFile)

    def _updateDirLinksForSchema(self, schema):
        self._removeProjectPlatformJunctions()

        self._sys.deleteDirectoryIfExists('[PluginsDir]/Projeny')

        # Define DoNotIncludeProjenyInUnityProject only if you want to include Projeny as just another prebuilt package
        # This is nice because then you can call methods on projeny without another package
        if self._config.tryGetBool(False, 'DoNotIncludeProjenyInUnityProject'):
            self._createSwitchProjectMenuScript(schema.name, '[PluginsDir]/ProjenyGenerated/Editor/ProjenyChangeProjectMenu.cs')

            self._createPlaceholderCsFile('[PluginsDir]/ProjenyGenerated/Placeholder.cs')
            self._createPlaceholderCsFile('[PluginsDir]/ProjenyGenerated/Editor/Placeholder.cs')
        else:
            if self._config.getBool('LinkToProjenyEditorDir') and not MiscUtil.isRunningAsExe():
                self._junctionHelper.makeJunction('[ProjenyDir]/UnityPlugin/Projeny', '[PluginsDir]/Projeny/Editor/Source')

                self._sys.copyFile('[YamlDotNetDllPath]', '[PluginsDir]/ProjenyGenerated/Editor/YamlDotNet.dll')
            else:
                dllOutPath = '[PluginsDir]/Projeny/Editor/Projeny.dll'
                self._sys.copyFile('[ProjenyUnityEditorDllPath]', dllOutPath)
                self._sys.copyFile('[ProjenyUnityEditorDllMetaFilePath]', dllOutPath + '.meta')

                self._sys.copyFile('[YamlDotNetDllPath]', '[PluginsDir]/Projeny/Editor/YamlDotNet.dll')

                assetsOutPath = '[PluginsDir]/Projeny/Editor/Assets'
                self._sys.copyDirectory('[ProjenyUnityEditorAssetsDirPath]', assetsOutPath)
                settingsFileOutPath = os.path.join(assetsOutPath, 'Resources/Projeny/PmSettings.asset')
                self._sys.writeFileAsText(settingsFileOutPath, self._sys.readFileAsText(settingsFileOutPath).replace(
                    'm_Script: {fileID: 11500000, guid: 6e78a2ff93d634841a8d26620c43dfca, type: 3}',
                    'm_Script: {fileID: 1582608718, guid: b7b2ba04b543d234aa4225d91c60af2b, type: 3}'))

            self._createSwitchProjectMenuScript(schema.name, '[PluginsDir]/Projeny/Editor/ProjenyChangeProjectMenu.cs')

            self._createPlaceholderCsFile('[PluginsDir]/Projeny/Placeholder.cs')
            self._createPlaceholderCsFile('[PluginsDir]/Projeny/Editor/Placeholder.cs')


        assertThat(self._sys.directoryExists(schema.projectSettingsPath),
           "Expected to find project settings directory at '{0}'", self._varMgr.expand(schema.projectSettingsPath))
        self._junctionHelper.makeJunction(schema.projectSettingsPath, '[ProjectPlatformRoot]/ProjectSettings')

        for packageInfo in schema.packages.values():

            self._log.debug('Processing package "{0}"'.format(packageInfo.name))

            sourceDir = self._varMgr.expandPath('[UnityPackagesDir]/{0}'.format(packageInfo.name))

            self._validateDirForFolderType(packageInfo, sourceDir)

            assertThat(os.path.exists(sourceDir),
               "Could not find package with name '{0}' while processing schema '{1}'.  See build log for full object graph to see where it is referenced".format(packageInfo.name, schema.name))

            outputPackageDir = self._varMgr.expandPath(packageInfo.outputDirVar)

            linkDir = os.path.join(outputPackageDir, packageInfo.name)

            assertThat(not os.path.exists(linkDir), "Did not expect this path to exist: '{0}'".format(linkDir))

            self._junctionHelper.makeJunction(sourceDir, linkDir)

    def checkProjectInitialized(self, projectName, platform):
        self.setPathsForProject(projectName, platform)

        if self._sys.directoryExists('[ProjectPlatformRoot]'):
            return

        self._log.warn('Project "{0}" is not initialized for platform "{1}".  Initializing now.'.format(projectName, platform))
        self._initNewProjectForPlatform(projectName, platform)

    def setPathsForProject(self, projectName, platform):

        self._varMgr.set('ShortProjectName', self._commonSettings.getShortProjectName(projectName))
        self._varMgr.set('ShortPlatform', PlatformUtil.toPlatformFolderName(platform))

        self._varMgr.set('Platform', platform)
        self._varMgr.set('ProjectName', projectName)

        self._varMgr.set('ProjectRoot', '[UnityProjectsDir]/[ProjectName]')
        self._varMgr.set('ProjectPlatformRoot', '[ProjectRoot]/[ShortProjectName]-[ShortPlatform]')
        self._varMgr.set('ProjectAssetsDir', '[ProjectPlatformRoot]/Assets')

        self._varMgr.set('UnityGeneratedProjectEditorPath', '[ProjectPlatformRoot]/[ShortProjectName]-[ShortPlatform].CSharp.Editor.Plugins.csproj')
        self._varMgr.set('UnityGeneratedProjectPath', '[ProjectPlatformRoot]/[ShortProjectName]-[ShortPlatform].CSharp.Plugins.csproj')

        # For reasons I don't understand, the unity generated project is named with 'Assembly' on some machines and not other
        # Problem due to unity version but for now just allow either or
        self._varMgr.set('UnityGeneratedProjectEditorPath2', '[ProjectPlatformRoot]/Assembly-CSharp-Editor-firstpass.csproj')
        self._varMgr.set('UnityGeneratedProjectPath2', '[ProjectPlatformRoot]/Assembly-CSharp-firstpass.csproj')

        self._varMgr.set('PluginsDir', '[ProjectAssetsDir]/Plugins')
        self._varMgr.set('PluginsAndroidDir', '[PluginsDir]/Android')
        self._varMgr.set('PluginsAndroidLibraryDir', '[PluginsDir]/Android/libs')
        self._varMgr.set('PluginsIosLibraryDir', '[PluginsDir]/iOS')
        self._varMgr.set('PluginsWebGlLibraryDir', '[PluginsDir]/WebGL')

        self._varMgr.set('StreamingAssetsDir', '[ProjectAssetsDir]/StreamingAssets')

        self._varMgr.set('IntermediateFilesDir', '[ProjectPlatformRoot]/obj')

        self._varMgr.set('SolutionPath', '[ProjectRoot]/[ProjectName]-[Platform].sln')

    def deleteAllLinks(self):
        with self._log.heading('Deleting all junctions for all projects'):
            projectNames = []
            projectsDir = self._varMgr.expandPath('[UnityProjectsDir]')

            for itemName in os.listdir(projectsDir):
                fullPath = os.path.join(projectsDir, itemName)
                if os.path.isdir(fullPath):
                    projectNames.append(itemName)

            for projectName in projectNames:
                for platform in Platforms.All:
                    self.setPathsForProject(projectName, platform)
                    self._removeProjectPlatformJunctions()

    def _removeProjectPlatformJunctions(self):
        self._junctionHelper.removeJunctionsInDirectory('[ProjectPlatformRoot]', True)

    def clearAllProjectGeneratedFiles(self):
        for projName in self.getAllProjectNames():
            self.clearProjectGeneratedFiles(projName)

    def clearProjectGeneratedFiles(self, projectName):
        with self._log.heading('Clearing generated files for project {0}'.format(projectName)):
            self._junctionHelper.removeJunctionsInDirectory('[UnityProjectsDir]/{0}'.format(projectName), True)
            for platform in Platforms.All:
                self.setPathsForProject(projectName, platform)

                if os.path.exists(self._varMgr.expandPath('[ProjectPlatformRoot]')):
                    platformRootPath = self._varMgr.expand('[ProjectPlatformRoot]')

                    try:
                        shutil.rmtree(platformRootPath)
                    except:
                        self._log.warn('Unable to remove path {0}.  Trying to kill adb.exe to see if that will help...'.format(platformRootPath))
                        MiscUtil.tryKillAdbExe(self._sys)

                        try:
                            shutil.rmtree(platformRootPath)
                        except:
                            self._log.error('Still unable to remove path {0}!  A running process may have one of the files locked.  Ensure you have closed down unity / visual studio / etc.'.format(platformRootPath))
                            raise

                    self._log.debug('Removed project directory {0}'.format(platformRootPath))
                    self._log.good('Successfully deleted project {0} ({1})'.format(projectName, platform))
                else:
                    self._log.debug('Project {0} ({1}) already deleted'.format(projectName, platform))

                # Remove the solution files and the suo files etc.
                self._sys.removeByRegex('[ProjectRoot]/[ProjectName]-[Platform].*')

    def _initNewProjectForPlatform(self, projectName, platform):

        with self._log.heading('Initializing new project {0} ({1})'.format(projectName, platform)):
            schema = self._schemaLoader.loadSchema(projectName, platform)
            self.setPathsForProject(projectName, platform)

            assertThat(self._sys.directoryExists(schema.projectSettingsPath),
               "Expected to find project settings directory at '{0}'", self._varMgr.expand(schema.projectSettingsPath))

            if self._sys.directoryExists('[ProjectPlatformRoot]'):
                raise Exception('Unable to create project "{0}". Directory already exists at path "{1}".'.format(projectName, self._varMgr.expandPath('[ProjectPlatformRoot]')))

            try:
                self._sys.createDirectory('[ProjectPlatformRoot]')

                self._log.debug('Created directory "{0}"'.format(self._varMgr.expandPath('[ProjectPlatformRoot]')))

                self._junctionHelper.makeJunction(schema.projectSettingsPath, '[ProjectPlatformRoot]/ProjectSettings')

                self._updateDirLinksForSchema(schema)

                for handler in self._projectInitHandlers:
                    handler.onProjectInit(projectName, platform)

            except:
                self._log.error("Failed to initialize project '{0}' for platform '{1}'.".format(schema.name, platform))
                raise

            self._log.good('Finished creating new project "{0}" ({1})'.format(schema.name, platform))

