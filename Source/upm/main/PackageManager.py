
import re
import os
import sys
import argparse

from upm.util.VarManager import VarManager
from upm.log.Logger import Logger
from upm.util.SystemHelper import SystemHelper
import upm.util.JunctionUtil as JunctionUtil
import upm.util.Util as Util

from upm.main.ProjectSchemaLoader import FolderTypes
from upm.util.PlatformUtil import Platforms

import shutil
import traceback

from upm.util.CommonSettings import ConfigFileName
import upm.util.MiscUtil as MiscUtil
import upm.util.PlatformUtil as PlatformUtil

from upm.util.Assert import *
from upm.reg.PackageInfo import PackageInfo

import upm.util.YamlSerializer as YamlSerializer
import upm.ioc.Container as Container
from upm.ioc.Inject import Inject
from upm.ioc.Inject import InjectMany
import upm.ioc.IocAssertions as Assertions

InstallInfoFileName = 'ProjenyInstall.yaml'

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
        return self._sys.directoryExists('[UnityProjectsDir]/{0}'.format(projectName)) or self._sys.fileExists('[UnityProjectsDir]/{0}.ini'.format(projectName))

    def listAllProjects(self):
        projectNames = self.getAllProjectNames()
        self._log.info("Found {0} Projects:".format(len(projectNames)))
        for proj in projectNames:
            alias = self.tryGetAliasFromFullName(proj)
            if alias:
                proj = "{0} ({1})".format(proj, alias)
            self._log.info("  " + proj)

    def listAllPackages(self):
        packagesNames = self.getAllPackageNames()
        self._log.info("Found {0} Packages:".format(len(packagesNames)))
        for packageName in packagesNames:
            self._log.info("  " + packageName)

    def getProjectFromAlias(self, alias):
        aliasMap = self._config.tryGetDictionary({}, 'ProjectAliases')

        assertThat(alias in aliasMap.keys(), "Unrecognized project '{0}' and could not find an alias with that name either".format(alias))
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

        self._log.heading('Updating package directories for project {0}'.format(projectName))

        self.checkProjectInitialized(projectName, platform)

        self.setPathsForProject(projectName, platform)
        schema = self._schemaLoader.loadSchema(projectName, platform)
        self._updateDirLinksForSchema(schema)

        self._log.good('Finished updating packages for project "{0}"'.format(schema.name))

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
        self._log.heading("Deleting project '{0}'", projName)
        assertThat(self._varMgr.hasKey('UnityProjectsDir'), "Could not find 'UnityProjectsDir' in PathVars.  Have you set up your {0} file?", ConfigFileName)

        fullPath = '[UnityProjectsDir]/{0}'.format(projName)

        assertThat(self._sys.directoryExists(fullPath), "Could not find project with name '{0}' - delete failed", projName)

        self.clearProjectGeneratedFiles(projName, True)
        self._sys.deleteDirectory(fullPath)

    def createPackage(self, packageName):
        assertThat(self._varMgr.hasKey('UnityPackagesDir'), "Could not find 'UnityPackagesDir' in PathVars.  Have you set up your {0} file?", ConfigFileName)

        self._log.heading('Creating new package "{0}"', packageName)
        newPath = '[UnityPackagesDir]/{0}'.format(packageName)

        assertThat(not self._sys.directoryExists(newPath), "Found existing package at path '{0}'", newPath)
        self._sys.createDirectory(newPath)

    def deletePackage(self, name):
        self._log.heading("Deleting package '{0}'", name)
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
            self._log.heading('Initializing project "{0}"'.format(projectName))

            try:
                #for platform in Platforms.All:
                for platform in [Platforms.Windows]:
                    self.updateProjectJunctions(projectName, platform)

                self._log.good('Successfully initialized project "{0}"'.format(projectName))
            except Exception as e:
                self._log.warn('Failed to initialize project "{0}": {1}'.format(projectName, e))

    def _updateDirLinksForSchema(self, schema):
        self._removePackageJunctions()

        self._sys.deleteDirectoryIfExists('[PluginsDir]/Projeny')

        if self._config.getBool('LinkToProjenyEditorDir'):
            self._junctionHelper.makeJunction('[ProjenyDir]/UnityPlugin/Projeny-editor', '[PluginsDir]/Projeny/Editor')
        else:
            self._sys.copyFile('[ProjenyUnityEditorDllPath]', '[PluginsDir]/Projeny/Editor/Projeny.dll')

        with self._sys.openOutputFile('[PluginsDir]/Projeny/Placeholder.cs') as outFile:
            outFile.write(
"""
    // This file exists purely as a way to force unity to generate the MonoDevelop csproj files so that Projeny can read the settings from it
""")

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

        self._log.heading('Deleting all junctions for all projects')

        projectNames = []

        projectsDir = self._varMgr.expandPath('[UnityProjectsDir]')

        for itemName in os.listdir(projectsDir):
            fullPath = os.path.join(projectsDir, itemName)
            if os.path.isdir(fullPath):
                projectNames.append(itemName)

        for projectName in projectNames:
            for platform in Platforms.All:
                self.setPathsForProject(projectName, platform)
                self._removeJunctionsForProjectPlatform()

    def _removePackageJunctions(self):
        self._junctionHelper.removeJunctionsInDirectory('[ProjectAssetsDir]', True)

    def _removeJunctionsForProjectPlatform(self):
        self._junctionHelper.removeJunction('[ProjectPlatformRoot]/ProjectSettings')
        self._removePackageJunctions()

    def clearAllProjectGeneratedFiles(self, addHeadings = True):
        for projName in self.getAllProjectNames():
            self.clearProjectGeneratedFiles(projName, addHeadings)

    def clearProjectGeneratedFiles(self, projectName, addHeading = True):

        if addHeading:
            self._log.heading('Clearing generated files for project {0}'.format(projectName))

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

        self._log.heading('Initializing new project {0} ({1})'.format(projectName, platform))

        schema = self._schemaLoader.loadSchema(projectName, platform)

        self.setPathsForProject(projectName, platform)

        if not self._sys.directoryExists('[ProjectRoot]/ProjectSettings'):
            self._sys.createDirectory('[ProjectRoot]/ProjectSettings')

        if self._sys.directoryExists('[ProjectPlatformRoot]'):
            raise Exception('Unable to create project "{0}". Directory already exists at path "{1}".'.format(projectName, self._varMgr.expandPath('[ProjectPlatformRoot]')))

        try:
            self._sys.createDirectory('[ProjectPlatformRoot]')

            self._log.debug('Created directory "{0}"'.format(self._varMgr.expandPath('[ProjectPlatformRoot]')))

            self._junctionHelper.makeJunction('[ProjectRoot]/ProjectSettings', '[ProjectPlatformRoot]/ProjectSettings')

            self._updateDirLinksForSchema(schema)

            for handler in self._projectInitHandlers:
                handler.onProjectInit(projectName, platform)

        except:
            self._log.endHeading()
            self._log.error("Failed to initialize project '{0}' for platform '{1}'.".format(schema.name, platform))
            raise

        self._log.good('Finished creating new project "{0}" ({1})'.format(schema.name, platform))

