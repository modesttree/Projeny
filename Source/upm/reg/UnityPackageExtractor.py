
import upm.ioc.Container as Container
from upm.log.Logger import Logger
from upm.log.LogStreamConsole import LogStreamConsole
from upm.util.SystemHelper import SystemHelper

import tempfile

from upm.ioc.Inject import Inject
from upm.ioc.Inject import InjectMany
import upm.ioc.IocAssertions as Assertions

import stat
from upm.util.ScriptRunner import ScriptRunner
from upm.util.VarManager import VarManager
from upm.config.Config import Config

import shutil

import tarfile
import os

from upm.util.ProcessRunner import ProcessRunner
from upm.util.Assert import *

class UnityPackageExtractor:
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')

    def extractUnityPackage(self, unityPackagePath, outputDir):

        fileName = os.path.basename(unityPackagePath)
        packageName = os.path.splitext(fileName)[0]

        self._log.heading("Extracting '{0}' to temporary directory", fileName)
        tempDir = tempfile.mkdtemp()
        self._log.info("Using temp directory '{0}'", tempDir)

        try:
            self._sys.createDirectory(os.path.join(tempDir, 'ProjectSettings'))
            self._sys.createDirectory(os.path.join(tempDir, 'Assets'))

            self._sys.executeAndWait('"[UnityExePath]" -batchmode -nographics -quit -projectPath "{0}" -importPackage "{1}"'.format(tempDir, unityPackagePath))

            self._log.heading("Unity finished.  Copying results to output directory")

            assetsDir = os.path.join(tempDir, 'Assets')

            rootPaths = [os.path.join(assetsDir, x) for x in self._sys.walkDir(assetsDir) if not x.endswith('.meta')]

            # If the unitypackage only contains a single directory, then extract that instead
            # To avoid ending up with PackageName/PackageName directories for everything
            if len(rootPaths) == 1 and os.path.isdir(rootPaths[0]) and os.path.basename(rootPaths[0]).lower() != 'editor':
                dirToCopy = rootPaths[0]
            else:
                dirToCopy = assetsDir

            self._sys.copyDirectory(dirToCopy, outputDir)

        finally:
            self._log.heading("Deleting temporary directory", tempDir)
            shutil.rmtree(tempDir)

if __name__ == '__main__':
    Container.bind('Config').toSingle(Config, [])
    Container.bind('Logger').toSingle(Logger)
    Container.bind('VarManager').toSingle(VarManager, { 'UnityExePath': "C:/Program Files/Unity/Editor/Unity.exe" })
    Container.bind('LogStream').toSingle(LogStreamConsole, True, True)
    Container.bind('SystemHelper').toSingle(SystemHelper)
    Container.bind('ProcessRunner').toSingle(ProcessRunner)

    def main():
        runner = UnityPackageExtractor()

        outDir = "F:/Temp/outputtest/output"

        if os.path.exists(outDir):
            shutil.rmtree(outDir)

        runner.extractUnityPackage("C:/Users/Steve/AppData/Roaming/Unity/Asset Store-5.x/Modest Tree Media/Scripting/Zenject Dependency Injection IOC.unitypackage", outDir)

    ScriptRunner().runWrapper(main)

