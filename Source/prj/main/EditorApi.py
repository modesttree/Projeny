
import os
import traceback
from prj.log.LogStreamFile import LogStreamFile
import prj.main.Prj as Prj

import prj.util.YamlSerializer as YamlSerializer
from prj.log.LogStreamConsoleHeadingsOnly import LogStreamConsoleHeadingsOnly
import prj.ioc.Container as Container
from prj.ioc.Inject import Inject
import prj.ioc.IocAssertions as Assertions
import sys
import prj.util.MiscUtil as MiscUtil

from prj.util.PlatformUtil import Platforms
import prj.util.PlatformUtil as PlatformUtil
from prj.util.Assert import *

class Runner:
    _log = Inject('Logger')
    _packageMgr = Inject('PackageManager')
    _unityHelper = Inject('UnityHelper')
    _vsSolutionHelper = Inject('VisualStudioHelper')
    _releaseSourceManager = Inject('ReleaseSourceManager')
    _sys = Inject('SystemHelper')
    _varMgr = Inject('VarManager')

    def run(self, project, platform, requestId, param1, param2):
        self._log.debug("Started EditorApi with arguments: {0}".format(" ".join(sys.argv[1:])))

        self._project = project
        self._platform = platform
        self._requestId = requestId
        self._param1 = param1
        self._param2 = param2

        succeeded = True

        # This is repeated in __main__ but this is better because
        # it will properly log detailed errors to the file log instead of to the console
        try:
            self._runInternal()
        except Exception as e:
            sys.stderr.write(str(e))
            self._log.error(str(e))

            if not MiscUtil.isRunningAsExe():
                self._log.error('\n' + traceback.format_exc())

            succeeded = False

        if not succeeded:
            sys.exit(1)

    def _runInternal(self):
        if self._requestId == 'updateLinks':
            self._packageMgr.updateProjectJunctions(self._project, self._platform)

        elif self._requestId == 'openUnity':
            self._packageMgr.checkProjectInitialized(self._project, self._platform)
            self._unityHelper.openUnity(self._project, self._platform)

        elif self._requestId == 'openPackagesFolder':
            os.startfile(self._varMgr.expandPath("[UnityPackagesDir]"))

        elif self._requestId == 'updateCustomSolution':
            self._vsSolutionHelper.updateCustomSolution(self._project, self._platform)

        elif self._requestId == 'openCustomSolution':
            self._vsSolutionHelper.openCustomSolution(self._project, self._platform)

        elif self._requestId == 'listPackages':
            infos = self._packageMgr.getAllPackageInfos()
            for packageInfo in infos:
                sys.stderr.write('---\n')
                sys.stderr.write(YamlSerializer.serialize(packageInfo) + '\n')

        elif self._requestId == 'listProjects':
            projectNames = self._packageMgr.getAllProjectNames()
            for projName in projectNames:
                sys.stderr.write(projName + '\n')

        elif self._requestId == 'listReleases':
            for release in self._releaseSourceManager.lookupAllReleases():
                sys.stderr.write('---\n')
                sys.stderr.write(YamlSerializer.serialize(release) + '\n')

        elif self._requestId == 'deletePackage':
            self._log.info("Deleting package '{0}'", self._param1)
            self._packageMgr.deletePackage(self._param1)

        elif self._requestId == 'createPackage':
            self._log.info("Creating package '{0}'", self._param1)
            self._packageMgr.createPackage(self._param1)

        elif self._requestId == 'installRelease':
            self._log.info("Installing release '{0}' version code '{1}'", self._param1, self._param2)
            self._releaseSourceManager.installReleaseById(self._param1, self._param2, True)

        elif self._requestId == 'createProject':
            self._log.info("Creating new project '{0}'", self._project)
            self._packageMgr._createProject(self._project)

        else:
            assertThat(False, "Invalid request id '{0}'", self._requestId)

def installBindings(configPath):
    Container.bind('LogStream').toSingle(LogStreamConsoleHeadingsOnly)
    Container.bind('LogStream').toSingle(LogStreamFile)
    Prj.installBindings(configPath)

def main():
    import argparse

    parser = argparse.ArgumentParser(description='Projeny Editor API')
    parser.add_argument("configPath", help="")
    parser.add_argument("project", help="")
    parser.add_argument('platform', type=str, choices=[x.lower() for x in Platforms.All], help='')
    parser.add_argument('requestId', type=str, choices=['createProject', 'createPackage', 'deletePackage', 'installRelease', 'listReleases', 'listProjects', 'listPackages', 'updateLinks', 'updateCustomSolution', 'openCustomSolution', 'openUnity', 'openPackagesFolder'], help='')
    parser.add_argument("param1", nargs='?', help="")
    parser.add_argument("param2", nargs='?', help="")

    args = parser.parse_args(sys.argv[1:])

    installBindings(args.configPath)

    Runner().run(args.project, PlatformUtil.fromPlatformFolderName(args.platform), args.requestId, args.param1, args.param2)

if __name__ == '__main__':
    if (sys.version_info < (3, 0)):
        sys.stderr.write('Wrong version of python!  Install python 3 and try again')
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

