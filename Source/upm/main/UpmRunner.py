
import configparser
import sys
import os
import webbrowser

from upm.util.Assert import *
import upm.util.MiscUtil as MiscUtil
import upm.util.PlatformUtil as PlatformUtil

from upm.util.PlatformUtil import Platforms

import upm.ioc.Container as Container
from upm.ioc.Inject import Inject
import upm.ioc.IocAssertions as Assertions

class UpmRunner:
    _scriptRunner = Inject('ScriptRunner')
    _config = Inject('Config')
    _packageMgr = Inject('PackageManager')
    _unityHelper = Inject('UnityHelper')
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')
    _vsSolutionHelper = Inject('VisualStudioHelper')

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

        if self._args.initAll:
            self._packageMgr.initAllProjects()

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
        if self._args.listProjects:
            self._packageMgr.listAllProjects()

        if self._args.openUnity:
            self._packageMgr.checkProjectInitialized(self._project, self._platform)
            self._unityHelper.openUnity(self._project, self._platform)

        if self._args.openCustomSolution:
            self._vsSolutionHelper.openCustomSolution(self._project, self._platform)

    def _runInternal(self):
        self._log.debug("Started UPM with arguments: {0}".format(" ".join(sys.argv[1:])))

        self.processArgs()
        self._validateArgs()
        self._runPreBuild()
        self._runBuild()
        self._runPostBuild()

    def processArgs(self):

        self._project = self._args.project

        if not self._project:
            self._project = self._config.tryGetString(None, 'Projeny', 'DefaultProject')

        if self._project and not self._packageMgr.projectExists(self._project):
            self._project = self._packageMgr.getProjectFromAlias(self._project)

        self._platform = PlatformUtil.fromPlatformArgName(self._args.platform)

    def _validateArgs(self):
        requiresProject = self._args.updateLinks or self._args.updateUnitySolution \
           or self._args.updateCustomSolution or self._args.buildCustomSolution \
           or self._args.clearProjectGeneratedFiles or self._args.buildFull \
           or self._args.openUnity or self._args.openCustomSolution

        if requiresProject and not self._project:
            assertThat(False, "Cannot execute the given arguments without a project specified, or a default project defined in the upm.yaml file")

