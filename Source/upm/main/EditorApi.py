
from upm.log.LogStreamFile import LogStreamFile
import upm.main.Upm as Upm

from upm.log.LogStreamConsoleErrorsOnly import LogStreamConsoleErrorsOnly
import os
import upm.ioc.Container as Container
from upm.ioc.Inject import Inject
import upm.ioc.IocAssertions as Assertions
import sys
import upm.util.MiscUtil as MiscUtil

from upm.util.PlatformUtil import Platforms
import upm.util.PlatformUtil as PlatformUtil
from upm.util.Assert import *

class Runner:
    _scriptRunner = Inject('ScriptRunner')
    _log = Inject('Logger')
    _packageMgr = Inject('PackageManager')
    _unityHelper = Inject('UnityHelper')
    _vsSolutionHelper = Inject('VisualStudioHelper')
    _releaseRegistryManager = Inject('ReleaseRegistryManager')

    def run(self, project, platform, requestId):
        self._project = project
        self._platform = platform
        self._requestId = requestId

        success = self._scriptRunner.runWrapper(self._runInternal)

        if not success:
            sys.exit(1)

    def _runInternal(self):
        self._log.debug("Started EditorApi for project '{0}' and platform '{1}' with request ID: {2}".format(self._project, self._platform, self._requestId))

        if self._requestId == 'updateLinks':
            self._packageMgr.updateProjectJunctions(self._project, self._platform)

        elif self._requestId == 'openUnity':
            self._packageMgr.checkProjectInitialized(self._project, self._platform)
            self._unityHelper.openUnity(self._project, self._platform)

        elif self._requestId == 'updateCustomSolution':
            self._vsSolutionHelper.updateCustomSolution(self._project, self._platform)

        elif self._requestId == 'openCustomSolution':
            self._vsSolutionHelper.openCustomSolution(self._project, self._platform)

        elif self._requestId == 'listPackages':
            packagesNames = self._packageMgr.getAllPackageNames()
            for packageName in packagesNames:
                print(packageName)

        elif self._requestId == 'listProjects':
            projectNames = self._packageMgr.getAllProjectNames()
            for projName in projectNames:
                print(projName)

        elif self._requestId == 'listReleases':
            for release in self._releaseRegistryManager.lookupAllReleases():
                print("{0} ({1})".format(release.Title, release.Version))

        else:
            assertThat(False, "Invalid request id '{0}'", self._requestId)

def installBindings(configPath):
    Container.bind('LogStream').toSingle(LogStreamConsoleErrorsOnly)
    Container.bind('LogStream').toSingle(LogStreamFile)
    Upm.installBindings(configPath)

def main():
    import argparse

    parser = argparse.ArgumentParser(description='Projeny Editor API')
    parser.add_argument("configPath", help="")
    parser.add_argument("project", help="")
    parser.add_argument('platform', type=str, choices=[x.lower() for x in Platforms.All], help='')
    parser.add_argument('requestId', type=str, choices=['listReleases', 'listProjects', 'listPackages', 'updateLinks', 'updateCustomSolution', 'openCustomSolution', 'openUnity'], help='')

    args = parser.parse_args(sys.argv[1:])

    installBindings(args.configPath)

    Runner().run(args.project, PlatformUtil.fromPlatformFolderName(args.platform), args.requestId)

if __name__ == '__main__':
    if (sys.version_info < (3, 0)):
        print('Wrong version of python!  Install python 3 and try again')
        sys.exit(2)

    succeeded = True
    try:
        main()

    except Exception as e:
        sys.stderr.write(str(e))

        if not MiscUtil.isRunningAsExe():
            sys.stderr.write('\n' + traceback.format_exc())

        succeeded = False

    if not succeeded:
        sys.exit(1)



