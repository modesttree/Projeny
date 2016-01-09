
import sys
import os
import webbrowser

from prj.util.Assert import *
import prj.util.MiscUtil as MiscUtil
import prj.util.PlatformUtil as PlatformUtil

from prj.util.PlatformUtil import Platforms
from prj.util.CommonSettings import ConfigFileName

import prj.ioc.Container as Container
from prj.ioc.Inject import Inject
from prj.ioc.Inject import InjectMany
from prj.ioc.Inject import InjectOptional
import prj.ioc.IocAssertions as Assertions

from prj.main.ProjectSchemaLoader import ProjectConfigFileName

class PrjRunner:
    _scriptRunner = Inject('ScriptRunner')
    _config = Inject('Config')
    _packageMgr = Inject('PackageManager')
    _projectConfigChanger = Inject('ProjectConfigChanger')
    _unityHelper = Inject('UnityHelper')
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')
    _mainConfig = InjectOptional('MainConfigPath', None)
    _sys = Inject('SystemHelper')
    _vsSolutionHelper = Inject('VisualStudioHelper')
    _releaseSourceManager = Inject('ReleaseSourceManager')

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

        if self._args.deletePackage:
            if not self._args.suppressPrompts:
                if not MiscUtil.confirmChoice("Are you sure you want to delete package '{0}'? (y/n)  \nNote that this change is non-recoverable!  (unless you are using source control)  ".format(self._args.deletePackage)):
                    assertThat(False, "User aborted operation")

            self._packageMgr.deletePackage(self._args.deletePackage)

        if self._args.deleteProject:
            if not self._args.suppressPrompts:
                if not MiscUtil.confirmChoice("Are you sure you want to delete project '{0}'? (y/n)  \nNote that this will only delete your unity project settings and the {1} for this project.  \nThe rest of the content for your project will remain in the UnityPackages folder  ".format(self._args.deleteProject, ProjectConfigFileName)):
                    assertThat(False, "User aborted operation")
            self._packageMgr.deleteProject(self._args.deleteProject)

        if self._args.installRelease:
            releaseName, releaseVersion = self._args.installRelease
            self._releaseSourceManager.installReleaseByName(releaseName, releaseVersion)

        if self._args.init:
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
            self._releaseSourceManager.listAllReleases()

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
        self._log.debug("Started Prj with arguments: {0}".format(" ".join(sys.argv[1:])))

        self.processArgs()
        self._validateArgs()

        if self._args.createConfig:
            self._createConfig()

        if self._args.createProject:
            self._packageMgr._createProject(self._project)

        if self._args.createPackage:
            self._packageMgr.createPackage(self._args.createPackage)

        if self._args.projectAddPackage:
            self._projectConfigChanger.addPackage(self._project, self._args.projectAddPackage)

        self._runPreBuild()
        self._runBuild()
        self._runPostBuild()

    def _createConfig(self):
        self._log.heading('Initializing new projeny config')

        assertThat(not self._mainConfig,
           "Cannot initialize new projeny project, found existing config at '{0}'".format(self._mainConfig))

        curDir = os.getcwd()
        configPath = os.path.join(curDir, ConfigFileName)

        assertThat(not os.path.isfile(configPath), "Found existing projeny config at '{0}'.  Has the configuration already been created?", configPath)

        self._sys.createDirectory(os.path.join(curDir, 'UnityPackages'))
        self._sys.createDirectory(os.path.join(curDir, 'UnityProjects'))

        with self._sys.openOutputFile(configPath) as outFile:
            outFile.write(
"""
PathVars:
    UnityPackagesDir: '[ConfigDir]/UnityPackages'
    UnityProjectsDir: '[ConfigDir]/UnityProjects'
    LogPath: '[ConfigDir]/PrjLog.txt'
""")

    def processArgs(self):

        self._project = self._args.project

        if not self._project:
            self._project = self._config.tryGetString(None, 'DefaultProject')

        if self._project and not self._packageMgr.projectExists(self._project) and not self._args.createProject:
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
           or self._args.editProjectYaml or self._args.createProject \
            or self._args.projectAddPackage

        if requiresProject and not self._project:
            assertThat(False, "Cannot execute the given arguments without a project specified, or a default project defined in the {0} file", ConfigFileName)

