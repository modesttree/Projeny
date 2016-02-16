
import os
import traceback
from mtm.log.LogStreamFile import LogStreamFile
import prj.main.Prj as Prj

import mtm.util.YamlSerializer as YamlSerializer
from mtm.log.LogStreamConsoleHeadingsOnly import LogStreamConsoleHeadingsOnly
import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.IocAssertions as Assertions
import sys
import mtm.util.MiscUtil as MiscUtil

from mtm.util.Platforms import Platforms
import mtm.util.PlatformUtil as PlatformUtil
from mtm.util.Assert import *

class Runner:
    _log = Inject('Logger')
    _packageMgr = Inject('PackageManager')
    _unityHelper = Inject('UnityHelper')
    _projVsHelper = Inject('ProjenyVisualStudioHelper')
    _releaseSourceManager = Inject('ReleaseSourceManager')
    _sys = Inject('SystemHelper')
    _varMgr = Inject('VarManager')

    def run(self, project, platform, requestId, param1, param2, param3):
        self._log.debug("Started EditorApi with arguments: {0}".format(" ".join(sys.argv[1:])))

        self._project = project
        self._platform = platform
        self._requestId = requestId
        self._param1 = param1
        self._param2 = param2
        self._param3 = param3

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

    def _outputAllPathVars(self):
        self._outputContent(YamlSerializer.serialize(self._varMgr.getAllParameters()))

    def _outputContent(self, value):
        self._log.noise(value)
        sys.stderr.write(value)

    def _runInternal(self):

        self._packageMgr.setPathsForProjectPlatform(self._project, self._platform)

        if self._requestId == 'updateLinks':
            self._packageMgr.updateProjectJunctions(self._project, self._platform)

        elif self._requestId == 'openUnity':
            self._packageMgr.checkProjectInitialized(self._project, self._platform)
            self._unityHelper.openUnity(self._project, self._platform)

        elif self._requestId == 'getPathVars':
            self._outputAllPathVars()

        elif self._requestId == 'updateCustomSolution':
            self._projVsHelper.updateCustomSolution(self._project, self._platform)

        elif self._requestId == 'openCustomSolution':
            self._projVsHelper.openCustomSolution(self._project, self._platform)

        elif self._requestId == 'listPackages':
            infos = self._packageMgr.getAllPackageFolderInfos(self._project)
            for folderInfo in infos:
                self._outputContent('---\n')
                self._outputContent(YamlSerializer.serialize(folderInfo) + '\n')

        elif self._requestId == 'listProjects':
            projectNames = self._packageMgr.getAllProjectNames()
            for projName in projectNames:
                self._outputContent(projName + '\n')

        elif self._requestId == 'listReleases':
            for release in self._releaseSourceManager.lookupAllReleases():
                self._outputContent('---\n')
                self._outputContent(YamlSerializer.serialize(release) + '\n')

        elif self._requestId == 'installRelease':
            releaseName = self._param1
            packageRoot = self._param2
            versionCode = self._param3

            if versionCode == None or len(versionCode) == 0:
                versionCode = 0

            self._log.info("Installing release '{0}' into package dir '{1}' with version code '{2}'", releaseName, packageRoot, versionCode)
            self._releaseSourceManager.installReleaseById(releaseName, self._project, packageRoot, versionCode, True)

        elif self._requestId == 'createProject':
            newProjName = self._param1
            duplicateSettings = (self._param2 == 'True')
            self._log.info("Creating new project '{0}'", newProjName)
            self._packageMgr.createProject(newProjName, self._project if duplicateSettings else None)

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
    parser.add_argument('requestId', type=str, choices=['createProject', 'installRelease', 'listReleases', 'listProjects', 'listPackages', 'updateLinks', 'updateCustomSolution', 'openCustomSolution', 'openUnity', 'getPathVars'], help='')
    parser.add_argument("param1", nargs='?', help="")
    parser.add_argument("param2", nargs='?', help="")
    parser.add_argument("param3", nargs='?', help="")

    args = parser.parse_args(sys.argv[1:])

    installBindings(args.configPath)
    Prj.installPlugins()

    Runner().run(args.project, PlatformUtil.fromPlatformFolderName(args.platform), args.requestId, args.param1, args.param2, args.param3)

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

