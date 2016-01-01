
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

    # Returns the chosen name for the directory
    # If forcedName is given then this value is always forcedName
    def extractUnityPackage(self, unityPackagePath, fallbackName, forcedName):

        fileName = os.path.basename(unityPackagePath)
        packageName = os.path.splitext(fileName)[0]

        self._log.heading("Extracting '{0}'", packageName)
        tempDir = tempfile.mkdtemp()
        self._log.info("Using temp directory '{0}'", tempDir)

        try:
            self._sys.createDirectory(os.path.join(tempDir, 'ProjectSettings'))
            self._sys.createDirectory(os.path.join(tempDir, 'Assets'))

            self._sys.executeAndWait('"[UnityExePath]" -batchmode -nographics -quit -projectPath "{0}" -importPackage "{1}"'.format(tempDir, unityPackagePath))

            self._log.heading("Copying extracted results to output directory")

            assetsDir = os.path.join(tempDir, 'Assets')

            rootNames = [x for x in self._sys.walkDir(assetsDir) if not x.endswith('.meta')]

            # If the unitypackage only contains a single directory, then extract that instead
            # To avoid ending up with PackageName/PackageName directories for everything
            # Also, by default use that directory as the name for the package
            # This is nice for packages that assume some directory structure
            # Also, some packages have titles that aren't as nice as directories.  For example, Unity Test Tools uses the directory name UnityTestTools
            # so it's nice to use this as the package name instead of the full title
            # Note that for upgrading/downgrading, this doesn't matter because it uses the ID which is stored in the Install.yaml file
            if len(rootNames) == 1:
                rootName = rootNames[0]
                fullRootPath = os.path.join(assetsDir, rootName)

                if os.path.isdir(fullRootPath) and rootName != 'editor':
                    dirToCopy = fullRootPath

                    if not forcedName:
                        forcedName = rootName
                else:
                    dirToCopy = assetsDir
            else:
                dirToCopy = assetsDir

            if forcedName:
                outDirName = forcedName
            else:
                assertThat(fallbackName)
                outDirName = fallbackName

            outDirPath = '[UnityPackagesDir]/{0}'.format(outDirName)
            self._sys.copyDirectory(dirToCopy, outDirPath)

            return outDirName
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

