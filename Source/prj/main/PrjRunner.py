
import sys
import os
import webbrowser

from mtm.util.Assert import *
import mtm.util.MiscUtil as MiscUtil
import mtm.util.PlatformUtil as PlatformUtil

from mtm.util.Platforms import Platforms
from mtm.util.CommonSettings import ConfigFileName

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
from mtm.ioc.Inject import InjectMany
from mtm.ioc.Inject import InjectOptional
import mtm.ioc.IocAssertions as Assertions

from prj.main.ProjenyConstants import ProjectConfigFileName

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
    _projVsHelper = Inject('ProjenyVisualStudioHelper')
    _releaseSourceManager = Inject('ReleaseSourceManager')

    def run(self, args):
        self._args = self._processArgs(args)
        success = self._scriptRunner.runWrapper(self._runInternal)
        self._onBuildComplete(success)

    def _onBuildComplete(self, success):
        if not success:
            sys.exit(1)

    def _processArgs(self, args):
        if args.buildFullProject or args.buildFull:
            args.updateLinks = True
            args.updateUnitySolution = True
            args.updateCustomSolution = True
            args.buildCustomSolution = True

        if args.buildFull:
            args.buildPrebuild = True

        if not args.project:
            args.project = self._config.tryGetString(None, 'DefaultProject')

        if args.project and not self._packageMgr.projectExists(args.project) and not args.createProject:
            args.project = self._packageMgr.getProjectFromAlias(args.project)

        if not args.project and self._varMgr.hasKey('UnityProjectsDir'):
            allProjects = self._packageMgr.getAllProjectNames()

            # If there's only one project, then just always assume they are operating on that
            if len(allProjects) == 1:
                args.project = allProjects[0]

        return args

    def _runPreBuild(self):

        if self._args.deleteProject:
            if not self._args.suppressPrompts:
                if not MiscUtil.confirmChoice("Are you sure you want to delete project '{0}'? (y/n)  \nNote that this will only delete your unity project settings and the {1} for this project.  \nThe rest of the content for your project will remain in the UnityPackages folder  ".format(self._args.project, ProjectConfigFileName)):
                    assertThat(False, "User aborted operation")
            self._packageMgr.deleteProject(self._args.project)

        if self._args.createProject:
            self._packageMgr.createProject(self._args.project)

        if self._args.projectAddPackageAssets:
            self._projectConfigChanger.addPackage(self._args.project, self._args.projectAddPackageAssets, True)

        if self._args.projectAddPackagePlugins:
            self._projectConfigChanger.addPackage(self._args.project, self._args.projectAddPackagePlugins, False)

        if self._args.openDocumentation:
            self._openDocumentation()

        if self._args.clearProjectGeneratedFiles:
            self._packageMgr.clearProjectGeneratedFiles(self._args.project)

        if self._args.clearAllProjectGeneratedFiles:
            self._packageMgr.clearAllProjectGeneratedFiles()

        if self._args.deleteAllLinks:
            self._packageMgr.deleteAllLinks()

        if self._args.buildPrebuild:
            self.buildPrebuildProjects()

        if self._args.init:
            self._packageMgr.updateLinksForAllProjects()

        if self._args.initLinks:
            self._packageMgr.checkProjectInitialized(self._args.project, self._platform)

        if self._args.updateLinks:
            self._packageMgr.updateProjectJunctions(self._args.project, self._platform)

        if self._args.updateUnitySolution:
            self._projVsHelper.updateUnitySolution(self._args.project, self._platform)

        if self._args.updateCustomSolution:
            self._projVsHelper.updateCustomSolution(self._args.project, self._platform)

    def buildPrebuildProjects(self, config = None):
        solutionPath = self._config.tryGetString(None, 'Prebuild', 'SolutionPath')

        if solutionPath != None:
            with self._log.heading('Building {0}'.format(os.path.basename(self._varMgr.expandPath(solutionPath)))):
                if config == None:
                    config = self._config.tryGetString('Debug', 'Prebuild', 'SolutionConfig')

                self._vsSolutionHelper.buildVisualStudioProject(solutionPath, config)

    def _openDocumentation(self):
        webbrowser.open('https://github.com/modesttree/ModestUnityPackageManager')

    def _runBuild(self):
        if self._args.buildCustomSolution:
            self._projVsHelper.buildCustomSolution(self._args.project, self._platform)

    def _runPostBuild(self):

        if self._args.listReleases:
            self._releaseSourceManager.listAllReleases()

        if self._args.listProjects:
            self._packageMgr.listAllProjects()

        if self._args.listPackages:
            self._packageMgr.listAllPackages(self._args.project)

        if self._args.openUnity:
            self._packageMgr.checkProjectInitialized(self._args.project, self._platform)
            self._unityHelper.openUnity(self._args.project, self._platform)

        if self._args.openCustomSolution:
            self._projVsHelper.openCustomSolution(self._args.project, self._platform)

        if self._args.editProjectYaml:
            self._editProjectYaml()

    def _editProjectYaml(self):
        assertThat(self._args.project)
        schemaPath = self._varMgr.expandPath('[UnityProjectsDir]/{0}/{1}'.format(self._args.project, ProjectConfigFileName))
        os.startfile(schemaPath)

    def _initialize(self):
        self._platform = PlatformUtil.fromPlatformArgName(self._args.platform)

        if self._args.project and self._platform:
            self._packageMgr.setPathsForProjectPlatform(self._args.project, self._platform)

    def _runInternal(self):
        self._log.debug("Started Prj with arguments: {0}".format(" ".join(sys.argv[1:])))

        self._initialize()
        self._validateRequest()

        self._runPreBuild()
        self._runBuild()
        self._runPostBuild()

    def _argsRequiresProject(self):
        return self._args.updateLinks or self._args.updateUnitySolution \
           or self._args.updateCustomSolution or self._args.buildCustomSolution \
           or self._args.clearProjectGeneratedFiles or self._args.buildFull \
           or self._args.openUnity or self._args.openCustomSolution \
           or self._args.editProjectYaml or self._args.createProject \
           or self._args.projectAddPackageAssets or self._args.projectAddPackagePlugins \
           or self._args.deleteProject or self._args.listPackages

    def _validateRequest(self):

        if self._argsRequiresProject() and not self._args.project:
            assertThat(False, "Cannot execute the given arguments without a project specified, or a default project defined in the {0} file", ConfigFileName)

