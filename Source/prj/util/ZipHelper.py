
import zipfile
import os

import prj.ioc.Container as Container
from prj.ioc.Inject import Inject
from prj.ioc.Inject import InjectOptional
import prj.ioc.IocAssertions as Assertions
from prj.util.Assert import *

class ZipHelper:
    _sys = Inject('SystemHelper')
    _varMgr = Inject('VarManager')

    def createZipFile(self, dirPath, zipFilePath):
        assertThat(zipFilePath.endswith('.zip'))

        zipFilePath = self._varMgr.expandPath(zipFilePath)

        self._sys.makeMissingDirectoriesInPath(zipFilePath)
        self._sys.removeFileIfExists(zipFilePath)

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


