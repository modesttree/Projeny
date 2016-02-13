
import zipfile
import os

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
from mtm.ioc.Inject import InjectOptional
import mtm.ioc.IocAssertions as Assertions
from mtm.util.Assert import *

class ZipHelper:
    _sys = Inject('SystemHelper')
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')

    def createZipFile(self, dirPath, zipFilePath):
        assertThat(zipFilePath.endswith('.zip'))

        dirPath = self._varMgr.expandPath(dirPath)
        zipFilePath = self._varMgr.expandPath(zipFilePath)

        self._sys.makeMissingDirectoriesInPath(zipFilePath)
        self._sys.removeFileIfExists(zipFilePath)

        self._log.debug("Writing directory '{0}' to zip at '{1}'", dirPath, zipFilePath)
        self._writeDirectoryToZipFile(zipFilePath, dirPath)

    def _writeDirectoryToZipFile(self, zipFilePath, dirPath):
        with zipfile.ZipFile(zipFilePath, 'w', zipfile.ZIP_DEFLATED) as zipf:
            self._zipAddDir(zipf, dirPath, '')

    def _zipAddDir(self, zipf, dirPath, zipPathPrefix = None):
        dirPath = self._varMgr.expandPath(dirPath)

        assertThat(os.path.isdir(dirPath), 'Invalid directory given at "{0}"'.format(dirPath))

        if zipPathPrefix is None:
            zipPathPrefix = os.path.basename(dirPath)

        for root, dirs, files in os.walk(dirPath):
            for file in files:
                filePath = os.path.join(root, file)
                zipf.write(filePath, os.path.join(zipPathPrefix, os.path.relpath(filePath, dirPath)))


