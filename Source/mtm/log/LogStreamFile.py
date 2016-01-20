
import os
from mtm.ioc.Inject import Inject
from mtm.log.Logger import LogType

import shutil

class LogStreamFile:
    _varManager = Inject('VarManager')

    def __init__(self):
        self._fileStream = self._tryGetFileStream()

    def log(self, logType, message):

        if logType == LogType.Heading:
            self._printSeperator()
            self._writeLine(message)
            self._printSeperator()
        else:
            self._writeLine(message)

    def dispose(self):
        if self._fileStream:
            self._fileStream.close()

    def _printSeperator(self):
        self._writeLine('--------------------------')

    def _writeLine(self, value):
        self._write('\n' + value)

    def _write(self, value):
        if self._fileStream:
            self._fileStream.write(value)
            self._fileStream.flush()

    def _tryGetFileStream(self):

        if not self._varManager.hasKey('LogPath'):
            return None

        primaryPath = self._varManager.expand('[LogPath]')

        previousPath = None
        if self._varManager.hasKey('LogPreviousPath'):
            previousPath = self._varManager.expand('[LogPreviousPath]')

        # Keep one old build log
        if os.path.isfile(primaryPath) and previousPath:
            shutil.copy2(primaryPath, previousPath)

        self._outputFilePath = primaryPath
        return open(primaryPath, 'w', encoding='utf-8', errors='ignore')

#if __name__ == '__main__':
    #import mtm.ioc.Container as Container
    #from mtm.log.Logger import Logger
    #from mtm.util.VarManager import VarManager
    #from mtm.config.ConfigXml import ConfigXml

    #Container.bind('Config').toSingle(ConfigXml)
    #Container.bind('VarManager').toSingle(VarManager)
    #Container.bind('LogStream').toSingle(LogStreamFile)
    #Container.bind('Logger').toSingle(Logger)

    #pathMgr = Container.resolve('VarManager')
    #pathMgr.add('LogPath', 'C:/Temp/log.txt')

    #logger = Container.resolve('Logger')

    #logger.info('test info')
    #logger.error('test error')
