import sys
import argparse
import os

import upm.ioc.Container as Container
from upm.ioc.Inject import Inject, InjectOptional
import upm.ioc.IocAssertions as Assertions

from upm.util.CommonSettings import ConfigFileName
import upm.util.MiscUtil as MiscUtil

import upm.main.Upm as Upm

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

        if self._sys.directoryExists('[OutDir]'):
            if not self._args.suppressPrompts and not MiscUtil.confirmChoice('Override directory "{0}"? (y/n)'.format(self._varMgr.expand('[OutDir]'))):
                self._log.warn('User aborted\n')
                sys.exit(1)

            self._log.heading('Clearing output directory')

            self._junctionHelper.removeJunctionsInDirectory('[OutDir]', True)
            self._sys.clearDirectoryContents('[OutDir]')
        else:
            self._sys.createDirectory('[OutDir]')

        self._log.heading('Clearing all generated files in UnityProjects folder')
        self._packageMgr.clearAllProjectGeneratedFiles(False)

        self._log.heading('Copying UnityPackages directory')
        self._sys.copyDirectory('[RootDir]/UnityPackages', '[OutDir]/UnityPackages')

        self._log.heading('Copying UnityProjects directory')
        self._sys.copyDirectory('[RootDir]/UnityProjects', '[OutDir]/UnityProjects')

        self._log.heading('Updating exe')
        self._sys.executeAndWait('[PythonDir]/BuildExe.bat')

        self._sys.copyDirectory('[RootDir]/Projeny/Bin', '[OutDir]/Projeny/Bin')
        self._sys.copyDirectory('[RootDir]/Projeny/Source', '[OutDir]/Projeny/Source')
        self._sys.copyDirectory('[RootDir]/Projeny/Templates', '[OutDir]/Projeny/Templates')

        self._sys.copyFile('[RootDir]/Projeny/{0}'.format(ConfigFileName), '[OutDir]/Projeny/{0}'.format(ConfigFileName))

        self._sys.removeByRegex('[OutDir]/Projeny/Bin/ProjenyLog*')
        self._sys.removeFile('[OutDir]/Projeny/Bin/.gitignore')
        self._sys.removeFile('[OutDir]/UnityProjects/.gitignore')

        self._sys.copyFile('[RootDir]/Upm.bat', '[OutDir]/Upm.bat')

        versionStr = self._sys.readFileAsText('[ProjenyDir]/Version.txt').strip()
        self._zipHelper.createZipFile('[OutDir]', '[OutRootDir]/Projeny-WithSamples-{0}.zip'.format(versionStr))

        for packageName in self._sys.walkDir('[OutDir]/UnityPackages'):
            if packageName != "Projeny":
                self._sys.deleteDirectory('[OutDir]/UnityPackages/{0}'.format(packageName))

        self._sys.clearDirectoryContents('[OutDir]/UnityProjects')
        self._zipHelper.createZipFile('[OutDir]', '[OutRootDir]/Projeny-{0}.zip'.format(versionStr))

def addArguments(parser):
    parser.add_argument('-s', '--includeSamples', action='store_true', help='Set if you want to include sample projects')
    parser.add_argument('-sp', '--suppressPrompts', action='store_true', help='If unset, confirmation prompts will be displayed for important operations.')
    parser.add_argument('-o', '--outDirectory', metavar='OUT_DIRECTORY', type=str, help="")

def installBindings(args):
    Upm.installBindings(False, False)

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

