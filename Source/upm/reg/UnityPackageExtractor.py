
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

import shutil

import tarfile
import os

from upm.util.Assert import *

class UnityPackageExtractor:
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')

    def extractUnityPackage(self, unityPackagePath, outputDir):

        fileName = os.path.basename(unityPackagePath)
        packageName = os.path.splitext(fileName)[0]

        self._log.heading("Extracting '{0}' to '{1}'", fileName, outputDir)

        tempDir = tempfile.mkdtemp()

        try:
            self._log.info("Extracting tar file for '{0}'...", fileName)
            self._extractTar(unityPackagePath, tempDir)

            self._log.info("Processing contents of '{0}'...", packageName)
            self._processExtractedDir(tempDir, outputDir)
        finally:
            self._log.debug("Deleting temp directory '{0}'...", tempDir)
            shutil.rmtree(tempDir)

    def _extractTar(self, unityPackagePath, tempDir):
        tar = tarfile.open(unityPackagePath, 'r:gz')
        tar.extractall(tempDir)
        tar.close()
        self._log.info("Finished extracting '{0}'", os.path.basename(unityPackagePath))

    def _processExtractedDir(self, tempDir, outputDir):

        self._sys.makeMissingDirectoriesInPath(outputDir)

        for assetId in os.listdir(tempDir):
            assetDirPath = os.path.join(tempDir, assetId)

            if not os.path.isdir(assetDirPath):
                continue

            assetSourcePath = os.path.join(assetDirPath, 'asset')

            if not self._sys.fileExists(assetSourcePath):
                continue

            pathNameFilePath = os.path.join(assetDirPath, 'pathname')

            assertThat(self._sys.fileExists(pathNameFilePath))

            with self._sys.openInputFile(pathNameFilePath) as f:
                outputRelativePath = f.readline().strip()

            assertThat(outputRelativePath.startswith("Assets/"))

            outputRelativePath = outputRelativePath[len("Assets/"):]

            self._log.debug("Processing asset '{0}'", outputRelativePath)

            destPath = os.path.join(outputDir, outputRelativePath)
            metaDestPath = destPath + ".meta"

            self._sys.copyFile(assetSourcePath, destPath)

            assetMetaFilePath = os.path.join(assetDirPath, 'asset.meta')

            if self._sys.fileExists(assetMetaFilePath):
                self._sys.copyFile(assetMetaFilePath, metaDestPath)

if __name__ == '__main__':
    Container.bind('Logger').toSingle(Logger)
    Container.bind('VarManager').toSingle(VarManager)
    Container.bind('LogStream').toSingle(LogStreamConsole, True, True)
    Container.bind('SystemHelper').toSingle(SystemHelper)
    assertThat(False, "TODO")
    #Container.bind('Config').toSingle(ConfigYaml)

    def main():
        runner = UnityPackageExtractor()

        outDir = "F:/Temp/UnityPackages/ExtractedFiles"

        if os.path.exists(outDir):
            shutil.rmtree(outDir)

        runner.extractUnityPackage("F:/Temp/UnityPackages/BobTest.unitypackage", outDir)

    ScriptRunner().runWrapper(main)

