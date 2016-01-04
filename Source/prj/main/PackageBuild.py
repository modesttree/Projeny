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
ProjenyRootDir = os.path.realpath(os.path.join(PythonDir, '..'))

NsisPath = "C:/Utils/NSIS/Bin/makensis.exe"

class Runner:
    _sys = Inject('SystemHelper')
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')
    _scriptRunner = Inject('ScriptRunner')
    _packageMgr = Inject('PackageManager')
    _zipHelper = Inject('ZipHelper')
    _vsSolutionHelper = Inject('VisualStudioHelper')

    def run(self):
        success = self._scriptRunner.runWrapper(self._runInternal)

        if not success:
            sys.exit(1)

    def _copyDir(self, relativePath):
        self._sys.copyDirectory('[RootDir]/' + relativePath, '[OutDir]/' + relativePath)

    def _copyFile(self, relativePath):
        self._sys.copyFile('[RootDir]/' + relativePath, '[OutDir]/' + relativePath)

    def _runInternal(self):
        self._varMgr.add('OutDir', os.path.join(ProjenyRootDir, 'Installer/Build'))

        self._varMgr.add('PythonDir', PythonDir)
        self._varMgr.add('RootDir', ProjenyRootDir)

        self._updateBuildDirectory()
        self._createInstaller()

        #versionStr = self._sys.readFileAsText('[ProjenyDir]/Version.txt').strip()
        #self._zipHelper.createZipFile('[OutDir]', '[OutRootDir]/Projeny-WithSamples-{0}.zip'.format(versionStr))
        #self._zipHelper.createZipFile('[OutDir]', '[OutRootDir]/Projeny-{0}.zip'.format(versionStr))

    def _createInstaller(self):
        self._log.heading('Creating installer exe')
        assertThat(self._sys.directoryExists(NsisPath))
        self._sys.executeAndWait('"{0}" "[RootDir]/Installer/CreateInstaller.nsi"'.format(NsisPath))

    def _updateBuildDirectory(self):

        if self._sys.directoryExists('[OutDir]'):
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

def installBindings():

    Container.bind('LogStream').toSingle(LogStreamFile)
    Container.bind('LogStream').toSingle(LogStreamConsole, True, False)

    demoConfig = os.path.realpath(os.path.join(ProjenyRootDir, 'Demo/Projeny.yaml'))
    Prj.installBindings(demoConfig)

if __name__ == '__main__':
    if (sys.version_info < (3, 0)):
        print('Wrong version of python!  Install python 3 and try again')
        sys.exit(2)

    installBindings()
    Runner().run()

