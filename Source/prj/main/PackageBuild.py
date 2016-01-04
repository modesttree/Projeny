import sys
import argparse

from prj.log.LogStreamFile import LogStreamFile
import os
import prj.ioc.Container as Container
from prj.ioc.Inject import Inject
from prj.ioc.Inject import InjectOptional
import prj.ioc.IocAssertions as Assertions

from prj.config.Config import Config
from prj.log.LogStreamConsole import LogStreamConsole
from prj.util.CommonSettings import ConfigFileName
import prj.util.MiscUtil as MiscUtil

import prj.main.Prj as Prj

ScriptDir = os.path.dirname(os.path.realpath(__file__))
PythonDir = os.path.realpath(os.path.join(ScriptDir, '../..'))
ProjenyRootDir = os.path.realpath(os.path.join(PythonDir, '..'))

class Runner:
    _sys = Inject('SystemHelper')
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')
    _scriptRunner = Inject('ScriptRunner')
    _packageMgr = Inject('PackageManager')
    _zipHelper = Inject('ZipHelper')
    _vsSolutionHelper = Inject('VisualStudioHelper')

    def run(self, args):
        self._args = args
        success = self._scriptRunner.runWrapper(self._runInternal)

        if not success:
            sys.exit(1)

    def _copyDir(self, relativePath):
        self._sys.copyDirectory('[RootDir]/' + relativePath, '[OutDir]/' + relativePath)

    def _copyFile(self, relativePath):
        self._sys.copyFile('[RootDir]/' + relativePath, '[OutDir]/' + relativePath)

    def _runInternal(self):
        self._varMgr.add('OutRootDir', args.outDirectory)
        self._varMgr.add('OutDir', '[OutRootDir]/Contents')

        self._varMgr.add('PythonDir', PythonDir)
        self._varMgr.add('RootDir', ProjenyRootDir)

        if self._sys.directoryExists('[OutDir]'):
            if not self._args.suppressPrompts and not MiscUtil.confirmChoice('Override directory "{0}"? (y/n)'.format(self._varMgr.expand('[OutDir]'))):
                self._log.warn('User aborted\n')
                sys.exit(1)

            self._log.heading('Clearing output directory')
            self._sys.clearDirectoryContents('[OutDir]')
        else:
            self._sys.createDirectory('[OutDir]')

        self._log.heading('Building exes')
        self._sys.executeAndWait('[PythonDir]/BuildAllExes.bat')

        self._log.heading('Building unity plugin dlls')
        self._vsSolutionHelper.buildVisualStudioProject('[RootDir]/UnityPlugin/Projeny.sln', 'Release')

        self._copyDir('UnityPlugin/Projeny/Assets')
        self._copyDir('Templates')
        self._copyFile(ConfigFileName)
        self._copyDir('Bin')

        self._sys.removeFile('[OutDir]/Bin/.gitignore')

        self._sys.removeByRegex('[OutDir]/Bin/UnityPlugin/Release/*.pdb')
        self._sys.deleteDirectory('[OutDir]/Bin/UnityPlugin/Debug')

        #versionStr = self._sys.readFileAsText('[ProjenyDir]/Version.txt').strip()
        #self._zipHelper.createZipFile('[OutDir]', '[OutRootDir]/Projeny-WithSamples-{0}.zip'.format(versionStr))
        #self._zipHelper.createZipFile('[OutDir]', '[OutRootDir]/Projeny-{0}.zip'.format(versionStr))

def addArguments(parser):
    parser.add_argument('-s', '--includeSamples', action='store_true', help='Set if you want to include sample projects')
    parser.add_argument('-sp', '--suppressPrompts', action='store_true', help='If unset, confirmation prompts will be displayed for important operations.')
    parser.add_argument('-o', '--outDirectory', metavar='OUT_DIRECTORY', type=str, help="")

def installBindings(args):

    Container.bind('LogStream').toSingle(LogStreamFile)
    Container.bind('LogStream').toSingle(LogStreamConsole, True, False)

    demoConfig = os.path.realpath(os.path.join(ProjenyRootDir, 'Demo/Projeny.yaml'))
    Prj.installBindings(demoConfig)

if __name__ == '__main__':
    if (sys.version_info < (3, 0)):
        print('Wrong version of python!  Install python 3 and try again')
        sys.exit(2)

    argv = sys.argv[1:]

    parser = argparse.ArgumentParser(description='Projeny Build Packager')
    addArguments(parser)

    if len(argv) == 0:
        parser.print_help()
        sys.exit(2)

    args = parser.parse_args(sys.argv[1:])

    installBindings(args)

    Runner().run(args)

