
from upm.log.LogStreamFile import LogStreamFile
import upm.main.Upm as Upm

import yaml
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
import time

class Runner:
    _scriptRunner = Inject('ScriptRunner')
    _log = Inject('Logger')
    _packageMgr = Inject('PackageManager')
    _unityHelper = Inject('UnityHelper')
    _vsSolutionHelper = Inject('VisualStudioHelper')
    _releaseRegistryManager = Inject('ReleaseRegistryManager')

    def run(self, project, platform, requestId, param1, param2):
        self._project = project
        self._platform = platform
        self._requestId = requestId
        self._param1 = param1
        self._param2 = param2

        success = self._scriptRunner.runWrapper(self._runInternal)

        if not success:
            sys.exit(1)

    def _runInternal(self):
        self._log.debug("Started EditorApi with arguments: {0}".format(" ".join(sys.argv[1:])))

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
            infos = self._packageMgr.getAllPackageInfos()
            for packageInfo in infos:
                print('---')
                print(yaml.dump(packageInfo, default_flow_style=False))

        elif self._requestId == 'listProjects':
            projectNames = self._packageMgr.getAllProjectNames()
            for projName in projectNames:
                print(projName)

        elif self._requestId == 'listReleases':
            for release in self._releaseRegistryManager.lookupAllReleases():
                print('---')
                releaseDict = release.__dict__
                if release.assetStoreInfo:
                    releaseDict['assetStoreInfo'] = release.assetStoreInfo.__dict__
                # width is necessary otherwise it inserts newlines that are picked up as part of the string by the unity side
                print(yaml.dump(releaseDict, width=9999999, default_flow_style=False))

        elif self._requestId == 'deletePackage':
            self._log.info("Deleting package '{0}'", self._param1)
            self._packageMgr.deletePackage(self._param1)

        elif self._requestId == 'createPackage':
            self._log.info("Creating package '{0}'", self._param1)
            self._packageMgr.createPackage(self._param1)

        elif self._requestId == 'installRelease':
            self._log.info("Installing release '{0}' version '{1}'", self._param1, self._param2)
            self._releaseRegistryManager.installRelease(self._param1, self._param2)

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
    parser.add_argument('requestId', type=str, choices=['createPackage', 'deletePackage', 'installRelease', 'listReleases', 'listProjects', 'listPackages', 'updateLinks', 'updateCustomSolution', 'openCustomSolution', 'openUnity'], help='')
    parser.add_argument("param1", nargs='?', help="")
    parser.add_argument("param2", nargs='?', help="")

    args = parser.parse_args(sys.argv[1:])

    installBindings(args.configPath)

    Runner().run(args.project, PlatformUtil.fromPlatformFolderName(args.platform), args.requestId, args.param1, args.param2)

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



