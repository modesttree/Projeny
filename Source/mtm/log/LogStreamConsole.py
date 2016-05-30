
import os
import re
import sys
from mtm.ioc.Inject import Inject
import mtm.util.Util as Util
from mtm.log.Logger import LogType
import shutil

from mtm.util.Assert import *

class AnsiiCodes:
    BLACK = "\033[1;30m"
    DARKBLACK = "\033[0;30m"
    RED = "\033[1;31m"
    DARKRED = "\033[0;31m"
    GREEN = "\033[1;32m"
    DARKGREEN = "\033[0;32m"
    YELLOW = "\033[1;33m"
    DARKYELLOW = "\033[0;33m"
    BLUE = "\033[1;34m"
    DARKBLUE = "\033[0;34m"
    MAGENTA = "\033[1;35m"
    DARKMAGENTA = "\033[0;35m"
    CYAN = "\033[1;36m"
    DARKCYAN = "\033[0;36m"
    WHITE = "\033[1;37m"
    DARKWHITE = "\033[0;37m"
    END = "\033[0;0m"

class LogStreamConsole:
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')
    _varManager = Inject('VarManager')
    _config = Inject('Config')

    def __init__(self, verbose, veryVerbose):
        self._verbose = verbose or veryVerbose
        self._veryVerbose = veryVerbose

        self._useColors = self._config.tryGetBool(False, 'LogStreamConsole', 'UseColors')

        self._fileStream = None
        if self._config.tryGetBool(False, 'LogStreamConsole', 'OutputToFilteredLog'):
            self._fileStream = self._getFileStream()

        if self._useColors:
            self._initColors()

    def _initColors(self):
        print("Colors are not working at the moment on mac.")

    def log(self, logType, message):

        assertIsNotNone(logType)

        if logType == LogType.Noise and not self._veryVerbose:
            return

        if logType == LogType.Debug and not self._verbose:
            return

        if logType == LogType.Error:
            self._output(logType, message, sys.stderr, self._useColors)
        else:
            self._output(logType, message, sys.stdout, self._useColors)

        if self._fileStream:
            self._output(logType, message, self._fileStream, False)

    def _getFileStream(self):

        primaryPath = self._varManager.expand('[LogFilteredPath]')

        if not primaryPath:
            raise Exception("Could not find path for log file")

        previousPath = None
        if self._varManager.hasKey('LogFilteredPreviousPath'):
            previousPath = self._varManager.expand('[LogFilteredPreviousPath]')

        # Keep one old build log
        if os.path.isfile(primaryPath) and previousPath:
            shutil.copy2(primaryPath, previousPath)

        return open(primaryPath, 'w', encoding='utf-8', errors='ignore')

    def _getHeadingIndent(self):
        return self._log.getCurrentNumHeadings() * "   "

    def _output(self, logType, message, stream, useColors):
        stream.write(self._getHeadingIndent())
        stream.write(message)
        stream.write('\n')
        stream.flush()




