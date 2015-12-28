
import configparser
import sys
import os
import webbrowser

from upm.util.Assert import *
import upm.util.MiscUtil as MiscUtil
import upm.util.PlatformUtil as PlatformUtil

from upm.util.PlatformUtil import Platforms
from upm.util.CommonSettings import ConfigFileName

import upm.ioc.Container as Container
from upm.ioc.Inject import Inject, InjectOptional, InjectMany
import upm.ioc.IocAssertions as Assertions

from upm.main.ProjectSchemaLoader import ProjectConfigFileName

class SourceControlTypes:
    Git = 'Git'
    Subversion = 'Subversion'
    # TODO - how to detect?
    #Perforce = 'Perforce'

class UpmRunner:
    _scriptRunner = Inject('ScriptRunner')
    _config = Inject('Config')
    _packageMgr = Inject('PackageManager')
    _unityHelper = Inject('UnityHelper')
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')
    _mainConfig = InjectOptional('MainConfigPath', None)
    _sys = Inject('SystemHelper')
    _vsSolutionHelper = Inject('VisualStudioHelper')
    _releaseRegistryManager = Inject('ReleaseRegistryManager')

    def run(self, args):
        self._args = args
        success = self._scriptRunner.runWrapper(self._runInternal)

        if not success:
            sys.exit(1)

    def _runPreBuild(self):
        if self._args.openDocumentation:
            self._openDocumentation()

        if self._args.clearProjectGeneratedFiles:
            self._packageMgr.clearProjectGeneratedFiles(self._project)

        if self._args.clearAllProjectGeneratedFiles:
            self._packageMgr.clearAllProjectGeneratedFiles()

        if self._args.deleteAllLinks:
            self._packageMgr.deleteAllLinks()

        if self._args.installRelease:
            self._releaseRegistryManager.installRelease(self._args.installRelease)

        if self._args.updateLinksAllProjects:
            self._packageMgr.updateLinksForAllProjects()

        if self._args.updateLinks:
            self._packageMgr.updateProjectJunctions(self._project, self._platform)

        if self._args.updateUnitySolution:
            self._vsSolutionHelper.updateUnitySolution(self._project, self._platform)

        if self._args.updateCustomSolution:
            self._vsSolutionHelper.updateCustomSolution(self._project, self._platform)

    def _openDocumentation(self):
        webbrowser.open('https://github.com/modesttree/ModestUnityPackageManager')

    def _runBuild(self):
        if self._args.buildCustomSolution:
            self._vsSolutionHelper.buildCustomSolution(self._project, self._platform)

    def _runPostBuild(self):

        if self._args.listReleases:
            self._releaseRegistryManager.listAllReleases()

        if self._args.listProjects:
            self._packageMgr.listAllProjects()

        if self._args.listPackages:
            self._packageMgr.listAllPackages()

        if self._args.openUnity:
            self._packageMgr.checkProjectInitialized(self._project, self._platform)
            self._unityHelper.openUnity(self._project, self._platform)

        if self._args.openCustomSolution:
            self._vsSolutionHelper.openCustomSolution(self._project, self._platform)

        if self._args.editProjectYaml:
            self._editProjectYaml()

    def _editProjectYaml(self):
        assertThat(self._project)
        schemaPath = self._varMgr.expandPath('[UnityProjectsDir]/{0}/{1}'.format(self._project, ProjectConfigFileName))
        os.startfile(schemaPath)

    def _runInternal(self):
        self._log.debug("Started UPM with arguments: {0}".format(" ".join(sys.argv[1:])))

        self.processArgs()
        self._validateArgs()

        if self._args.createConfig:
            self._createConfig()

        if self._args.createProject:
            self._createProject(self._args.createProject)

        if self._args.createPackage:
            self._createPackage(self._args.createPackage)

        self._runPreBuild()
        self._runBuild()
        self._runPostBuild()

    def _createPackage(self, packageName):
        self._log.heading('Creating new package "{0}"', packageName)
        self._sys.createDirectory('[UnityPackagesDir]/{0}'.format(packageName))

    def _createProject(self, projName):
        self._log.heading('Initializing new project "{0}"', projName)

        sourceControlType = self._findSourceControl()

        projDirPath = self._varMgr.expand('[UnityProjectsDir]/{0}'.format(projName))
        assertThat(not self._sys.directoryExists(projDirPath), "Cannot initialize new project '{0}', found existing project at '{1}'", projName, projDirPath)

        self._sys.createDirectory(projDirPath)

        if sourceControlType == SourceControlTypes.Git:
            self._sys.copyFile('[ProjectRootGitIgnoreTemplate]', os.path.join(projDirPath, '.gitignore'))
        elif sourceControlType == SourceControlTypes.Subversion:
            self._sys.copyFile('[ProjectRootSvnIgnoreTemplate]', os.path.join(projDirPath, '.svnignore'))
        else:
            self._log.warn('Could not determine source control in use!  An ignore file will not be added for your project.  If you add this project to source control later be careful to create an ignore file - the ganerated project directory should _never_ be stored in source control.')

        with self._sys.openOutputFile(os.path.join(projDirPath, ProjectConfigFileName)) as outFile:
            outFile.write(
"""
#packages:
    # Uncomment and Add package names here
""")

    def _findSourceControl(self):
        for dirPath in self._sys.getParentDirectoriesWithSelf('[ConfigDir]'):
            if self._sys.directoryExists(os.path.join(dirPath, '.git')):
                return SourceControlTypes.Git

            if self._sys.directoryExists(os.path.join(dirPath, '.svn')):
                return SourceControlTypes.Subversion

        return None

    def _createConfig(self):
        self._log.heading('Initializing new projeny config')

        assertThat(not self._mainConfig,
           "Cannot initialize new projeny project, found existing config at '{0}'".format(self._mainConfig))

        curDir = os.getcwd()
        configPath = os.path.join(curDir, ConfigFileName)

        assertThat(not os.path.isfile(configPath))

        self._sys.createDirectory(os.path.join(curDir, 'UnityPackages'))
        self._sys.createDirectory(os.path.join(curDir, 'UnityProjects'))

        with self._sys.openOutputFile(configPath) as outFile:
            outFile.write(
"""
PathVars:
    UnityPackagesDir: '[ConfigDir]/UnityPackages'
    UnityProjectsDir: '[ConfigDir]/UnityProjects'
    LogPath: '[ConfigDir]/UpmLog.txt'
""")

    def processArgs(self):

        self._project = self._args.project

        if not self._project:
            self._project = self._config.tryGetString(None, 'Projeny', 'DefaultProject')

        if self._project and not self._packageMgr.projectExists(self._project):
            self._project = self._packageMgr.getProjectFromAlias(self._project)

        if not self._project and self._varMgr.hasKey('UnityProjectsDir'):
            allProjects = self._packageMgr.getAllProjectNames()

            # If there's only one project, then just always assume they are operating on that
            if len(allProjects) == 1:
                self._project = allProjects[0]

        self._platform = PlatformUtil.fromPlatformArgName(self._args.platform)

    def _validateArgs(self):
        requiresProject = self._args.updateLinks or self._args.updateUnitySolution \
           or self._args.updateCustomSolution or self._args.buildCustomSolution \
           or self._args.clearProjectGeneratedFiles or self._args.buildFull \
           or self._args.openUnity or self._args.openCustomSolution \
           or self._args.editProjectYaml

        if requiresProject and not self._project:
            assertThat(False, "Cannot execute the given arguments without a project specified, or a default project defined in the {0} file", ConfigFileName)

