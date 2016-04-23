import sys
import argparse

from mtm.log.LogStreamFile import LogStreamFile
import os
import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
from mtm.ioc.Inject import InjectOptional
import mtm.ioc.IocAssertions as Assertions

from mtm.config.Config import Config
from mtm.log.LogStreamConsole import LogStreamConsole
from mtm.util.CommonSettings import ConfigFileName
import mtm.util.MiscUtil as MiscUtil

from mtm.util.Assert import *
import prj.main.Prj as Prj

ScriptDir = os.path.dirname(os.path.realpath(__file__))
PythonDir = os.path.realpath(os.path.join(ScriptDir, '../..'))
ProjenyDir = os.path.realpath(os.path.join(PythonDir, '..'))

NsisPath = "C:/Utils/NSIS/makensis.exe"

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
        self._sys.copyDirectory('[ProjenyDir]/' + relativePath, '[TempDir]/' + relativePath)

    def _copyFile(self, relativePath):
        self._sys.copyFile('[ProjenyDir]/' + relativePath, '[TempDir]/' + relativePath)

    def _runInternal(self):
        self._varMgr.add('PythonDir', PythonDir)
        self._varMgr.add('ProjenyDir', ProjenyDir)
        self._varMgr.add('SourceDir', '[ProjenyDir]/Source')
        self._varMgr.add('InstallerDir', '[ProjenyDir]/Installer')
        self._varMgr.add('TempDir', '[InstallerDir]/Build')
        self._varMgr.add('DistDir', '[InstallerDir]/Dist')

        self._sys.deleteAndReCreateDirectory('[DistDir]')
        self._sys.deleteAndReCreateDirectory('[TempDir]')

        try:
            self._updateBuildDirectory()

            versionStr = self._sys.readFileAsText('[SourceDir]/Version.txt').strip()
            installerOutputPath = '[DistDir]/ProjenyInstaller-v{0}.exe'.format(versionStr)

            self._createInstaller(installerOutputPath)

            self._createSamplesZip(versionStr)

            if self._args.addTag:
                self._log.info('Adding git tag for version number')
                self._sys.executeAndWait("git tag -a v{0} -m 'Version {0}'".format(versionStr))

            if self._args.runInstallerAfter:
                self._sys.deleteDirectoryIfExists('C:/Program Files (x86)/Projeny')
                self._sys.executeNoWait(installerOutputPath)
        finally:
            self._sys.deleteDirectoryIfExists('[TempDir]')

    def _createSamplesZip(self, versionStr):
        with self._log.heading('Clearing all generated files in Demo/UnityProjects folder'):
            self._packageMgr.clearAllProjectGeneratedFiles()

            self._sys.deleteDirectoryIfExists('[TempDir]')

            self._sys.copyDirectory('[ProjenyDir]/Demo', '[TempDir]')

            self._sys.removeFileIfExists('[TempDir]/.gitignore')
            self._sys.removeFileIfExists('[TempDir]/PrjLog.txt')

        with self._log.heading('Zipping up demo project'):
            self._zipHelper.createZipFile('[TempDir]', '[DistDir]/ProjenySamples-v{0}.zip'.format(versionStr))

    def _createInstaller(self, installerOutputPath):
        with self._log.heading('Creating installer exe'):
            assertThat(self._sys.directoryExists(NsisPath))
            self._sys.createDirectory('[DistDir]')
            self._sys.executeAndWait('"{0}" "[InstallerDir]/CreateInstaller.nsi"'.format(NsisPath))

            self._sys.renameFile('[DistDir]/ProjenyInstaller.exe', installerOutputPath)

    def _updateBuildDirectory(self):

        self._sys.deleteAndReCreateDirectory('[TempDir]')

        with self._log.heading('Building exes'):
            self._sys.executeAndWait('[PythonDir]/BuildAllExes.bat')

        with self._log.heading('Building unity plugin dlls'):
            self._vsSolutionHelper.buildVisualStudioProject('[ProjenyDir]/UnityPlugin/Projeny.sln', 'Release')

            self._copyDir('UnityPlugin/Projeny/Assets')
            self._copyDir('Templates')
            self._copyFile(ConfigFileName)
            self._copyDir('Bin')

            for fileName in self._sys.getAllFilesInDirectory('[InstallerDir]/BinFiles'):
                self._sys.copyFile('[InstallerDir]/BinFiles/' + fileName, '[TempDir]/Bin/' + fileName)

            self._sys.removeByRegex('[TempDir]/Bin/UnityPlugin/Release/*.pdb')
            self._sys.deleteDirectoryIfExists('[TempDir]/Bin/UnityPlugin/Debug')

def installBindings():

    Container.bind('LogStream').toSingle(LogStreamFile)
    Container.bind('LogStream').toSingle(LogStreamConsole, True, False)

    demoConfig = os.path.realpath(os.path.join(ProjenyDir, 'Demo/Projeny.yaml'))
    Prj.installBindings(demoConfig)

if __name__ == '__main__':
    if (sys.version_info < (3, 0)):
        print('Wrong version of python!  Install python 3 and try again')
        sys.exit(2)

    parser = argparse.ArgumentParser(description='Projeny build packager')
    parser.add_argument('-r', '--runInstallerAfter', action='store_true', help='')
    parser.add_argument('-t', '--addTag', action='store_true', help='')
    args = parser.parse_args(sys.argv[1:])

    installBindings()
    Runner().run(args)

