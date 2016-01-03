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
    _junctionHelper = Inject('JunctionHelper')
    _zipHelper = Inject('ZipHelper')

    def run(self, args):
        self._args = args
        success = self._scriptRunner.runWrapper(self._runInternal)

        if not success:
            sys.exit(1)

    def _runInternal(self):
        self._varMgr.add('OutRootDir', args.outDirectory)
        self._varMgr.add('OutDir', '[OutRootDir]/Contents')

        self._varMgr.add('PythonDir', PythonDir)
        self._varMgr.add('RootDir', ProjenyRootDir)
        #self._varMgr.add('DemoRootDir', '[RootDir]/Demo')

        if self._sys.directoryExists('[OutDir]'):
            if not self._args.suppressPrompts and not MiscUtil.confirmChoice('Override directory "{0}"? (y/n)'.format(self._varMgr.expand('[OutDir]'))):
                self._log.warn('User aborted\n')
                sys.exit(1)

            self._log.heading('Clearing output directory')

            self._junctionHelper.removeJunctionsInDirectory('[OutDir]', True)
            self._sys.clearDirectoryContents('[OutDir]')
        else:
            self._sys.createDirectory('[OutDir]')

        #self._log.heading('Clearing all generated files in Demo/UnityProjects folder')
        #self._packageMgr.clearAllProjectGeneratedFiles(False)

        #self._log.heading('Copying Demo Project')
        #self._sys.copyDirectory('[DemoRootDir]', '[OutDir]/Demo')

        self._log.heading('Updating exe')
        self._sys.executeAndWait('[PythonDir]/BuildAllExes.bat')

        self._sys.createDirectory('[OutDir]/Bin')

        #self._sys.copyDirectory('[RootDir]/Bin', '[OutDir]/Bin')
        #self._sys.copyDirectory('[RootDir]/Source', '[OutDir]/Source')
        self._sys.copyDirectory('[RootDir]/Templates', '[OutDir]/Templates')

        self._sys.copyFile('[RootDir]/{0}'.format(ConfigFileName), '[OutDir]/{0}'.format(ConfigFileName))

        self._sys.removeByRegex('[OutDir]/Bin/ProjenyLog*')

        versionStr = self._sys.readFileAsText('[ProjenyDir]/Version.txt').strip()

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

