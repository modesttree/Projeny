
import prj.main.Prj as Prj

import mtm.util.MiscUtil as MiscUtil
import os
import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.IocAssertions as Assertions
import sys
import argparse

from mtm.log.LogStreamConsole import LogStreamConsole

from mtm.util.Platforms import Platforms
import mtm.util.PlatformUtil as PlatformUtil
from mtm.util.Assert import *

class Runner:
    _scriptRunner = Inject('ScriptRunner')
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')
    _varMgr = Inject('VarManager')
    _vsSolutionHelper = Inject('VisualStudioHelper')
    _prjVsSolutionHelper = Inject('ProjenyVisualStudioHelper')

    def run(self, args):
        self._args = args
        success = self._scriptRunner.runWrapper(self._runInternal)

        if not success:
            sys.exit(1)

    def _runInternal(self):
        self._log.debug("Started OpenInVisualStudio with arguments: {0}".format(" ".join(sys.argv[1:])))

        project, platform = self._getProjectAndPlatformFromFilePath(self._args.filePath)

        self._log.debug("Determined Project = {0}, Platform = {1}", project, platform)

        lineNo = 1
        if self._args.lineNo:
            lineNo = int(self._args.lineNo)

        if platform == None:
            solutionPath = None
        else:
            solutionPath = self._prjVsSolutionHelper.getCustomSolutionPath(project, platform)

        self._vsSolutionHelper.openFile(
            self._args.filePath, lineNo, solutionPath)

    def _getProjectAndPlatformFromFilePath(self, filePath):
        unityProjectsDir = self._sys.canonicalizePath(self._varMgr.expand('[UnityProjectsDir]'))
        filePath = self._sys.canonicalizePath(filePath)

        if not filePath.startswith(unityProjectsDir):
            raise Exception("The given file path is not within the UnityProjects directory")

        relativePath = filePath[len(unityProjectsDir)+1:]
        dirs = relativePath.split(os.path.sep)

        projectName = dirs[0]

        try:
            platformProjectDirName = dirs[1]
            platformDirName = platformProjectDirName[platformProjectDirName.rfind('-')+1:]

            platform = PlatformUtil.fromPlatformFolderName(platformDirName)
        except:
            platform = None

        return projectName, platform

def addArguments(parser):
    parser.add_argument("filePath", help="")
    parser.add_argument("lineNo", nargs='?', help="")

def findConfigPath(filePath):
    lastParentDir = None
    parentDir = os.path.dirname(filePath)

    while parentDir and parentDir != lastParentDir:
        configPath = os.path.join(parentDir, Prj.ConfigFileName)

        if os.path.isfile(configPath):
            return configPath

        lastParentDir = parentDir
        parentDir = os.path.dirname(parentDir)

    assertThat(False, "Could not find Prj config path starting at path {0}", filePath)

def installBindings(args):

    Container.bind('LogStream').toSingle(LogStreamConsole, True, False)

    Prj.installBindings(findConfigPath(args.filePath))

def _main():
    parser = argparse.ArgumentParser(description='Projeny Visual Studio Opener')
    addArguments(parser)

    argv = sys.argv[1:]

    # If it's 2 then it only has the -cfg param
    if len(argv) == 0:
        parser.print_help()
        sys.exit(2)

    args = parser.parse_args(sys.argv[1:])

    installBindings(args)

    Runner().run(args)

if __name__ == '__main__':
    if (sys.version_info < (3, 0)):
        print('Wrong version of python!  Install python 3 and try again')
        sys.exit(2)

    succeeded = True

    try:
        _main()

    except KeyboardInterrupt as e:
        print('Operation aborted by user by hitting CTRL+C')
        succeeded = False

    except Exception as e:
        sys.stderr.write(str(e) + '\n')

        if not MiscUtil.isRunningAsExe():
            sys.stderr.write('\n' + traceback.format_exc())

        succeeded = False

    if not succeeded:
        sys.exit(1)


