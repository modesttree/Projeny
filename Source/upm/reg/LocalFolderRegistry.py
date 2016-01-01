
from upm.ioc.Inject import Inject
from upm.ioc.Inject import InjectMany
import upm.ioc.IocAssertions as Assertions

import time
from upm.reg.ReleaseInfo import ReleaseInfo
import upm.reg.UnityPackageAnalyzer as UnityPackageAnalyzer

import os

from upm.util.Assert import *

class FileInfo:
    def __init__(self, path, release):
        self.path = path
        self.release = release

class LocalFolderRegistry:
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')
    _extractor = Inject('UnityPackageExtractor')
    _packageAnalyzer = Inject('UnityPackageAnalyzer')

    def __init__(self, folderPath):
        self._folderPath = folderPath
        self._files = []

    @property
    def releases(self):
        return [x.release for x in self._files]

    def init(self):
        self._log.heading('Initializing registry for local folder')
        self._log.debug('Initializing registry for local folder "{0}"', self._folderPath)

        for path in self._sys.findFilesByPattern(self._folderPath, '*.unitypackage'):
            release = self._packageAnalyzer.getReleaseInfoFromUnityPackage(path)

            self._files.append(FileInfo(path, release))

        self._log.info("Found {0} released in folder '{1}'", len(self._files), self._folderPath)

    def getName(self):
        return "Local Folder ({0})".format(self._folderPath)

    # Should return the chosen name for the package
    # If forcedName is non-null then this should always be the value of forcedName
    def installRelease(self, releaseInfo, forcedName):
        fileInfo = next(x for x in self._files if x.release == releaseInfo)
        assertIsNotNone(fileInfo)

        return self._extractor.extractUnityPackage(fileInfo.path, releaseInfo.name, forcedName)


