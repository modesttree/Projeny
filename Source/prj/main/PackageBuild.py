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

from prj.util.Assert import *
import prj.main.Prj as Prj

ScriptDir = os.path.dirname(os.path.realpath(__file__))
PythonDir = os.path.realpath(os.path.join(ScriptDir, '../..'))
ProjenyDir = os.path.realpath(os.path.join(PythonDir, '..'))

NsisPath = "C:/Utils/NSIS/Bin/makensis.exe"

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
        self._sys.copyDirectory('[ProjenyDir]/' + relativePath, '[OutDir]/' + relativePath)

    def _copyFile(self, relativePath):
        self._sys.copyFile('[ProjenyDir]/' + relativePath, '[OutDir]/' + relativePath)

    def _runInternal(self):
        self._varMgr.add('PythonDir', PythonDir)
        self._varMgr.add('ProjenyDir', ProjenyDir)
        self._varMgr.add('InstallerDir', '[ProjenyDir]/Installer')
        self._varMgr.add('OutDir', '[InstallerDir]/Build')
        self._varMgr.add('DistDir', '[InstallerDir]/Dist')

        self._sys.clearDirectoryContents('[DistDir]')
        self._sys.clearDirectoryContents('[OutDir]')

        try:
            self._updateBuildDirectory()

            versionStr = 'v' + self._sys.readFileAsText('[InstallerDir]/Version.txt').strip()
            installerOutputPath = '[DistDir]/ProjenyInstaller-{0}.exe'.format(versionStr)

            self._createInstaller(installerOutputPath)
        finally:
            self._sys.deleteDirectoryIfExists('[OutDir]')

        self._createSamplesZip(versionStr)

        if self._args.runInstallerAfter:
            self._sys.executeNoWait(installerOutputPath)

    def _createSamplesZip(self, versionStr):
        self._log.heading('Clearing all generated files in Demo/UnityProjects folder')
        self._packageMgr.clearAllProjectGeneratedFiles(False)

        self._log.heading('Zipping up demo project')
        self._zipHelper.createZipFile('[ProjenyDir]/Demo', '[DistDir]/ProjenySamples-{0}.zip'.format(versionStr))

    def _createInstaller(self, installerOutputPath):
        self._log.heading('Creating installer exe')
        assertThat(self._sys.directoryExists(NsisPath))

        self._sys.createDirectory('[DistDir]')
        self._sys.executeAndWait('"{0}" "[InstallerDir]/CreateInstaller.nsi"'.format(NsisPath))

        self._sys.renameFile('[DistDir]/ProjenyInstaller.exe', installerOutputPath)

    def _updateBuildDirectory(self):

        self._sys.deleteDirectoryIfExists('[OutDir]')
        self._sys.createDirectory('[OutDir]')

        self._log.heading('Building exes')
        self._sys.executeAndWait('[PythonDir]/BuildAllExes.bat')

        self._log.heading('Building unity plugin dlls')
        self._vsSolutionHelper.buildVisualStudioProject('[ProjenyDir]/UnityPlugin/Projeny.sln', 'Release')

        self._copyDir('UnityPlugin/Projeny/Assets')
        self._copyDir('Templates')
        self._copyFile(ConfigFileName)
        self._copyDir('Bin')

        self._sys.removeFile('[OutDir]/Bin/.gitignore')

        self._sys.removeByRegex('[OutDir]/Bin/UnityPlugin/Release/*.pdb')
        self._sys.deleteDirectoryIfExists('[OutDir]/Bin/UnityPlugin/Debug')

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
    args = parser.parse_args(sys.argv[1:])

    installBindings()
    Runner().run(args)

