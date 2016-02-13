
import os
import re
import sys
from mtm.ioc.Inject import Inject
import mtm.util.Util as Util
from mtm.log.Logger import LogType
import shutil

from mtm.util.Assert import *
import mtm.log.ColorConsole as ColorConsole

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
        self._defaultColors = ColorConsole.get_text_attr()
        self._defaultBg = self._defaultColors & 0x0070
        self._defaultFg = self._defaultColors & 0x0007

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

        stream.write('\n')

        stream.write(self._getHeadingIndent())

        if not useColors or logType == LogType.Info:
            stream.write(message)
            stream.flush()
        else:
            ColorConsole.set_text_attr(self._getColorAttrs(logType))
            stream.write(message)
            stream.flush()
            ColorConsole.set_text_attr(self._defaultColors)

    def _getColorAttrs(self, logType):
        if logType == LogType.HeadingStart:
            return ColorConsole.FOREGROUND_CYAN | self._defaultBg | ColorConsole.FOREGROUND_INTENSITY

        if logType == LogType.HeadingEnd:
            return ColorConsole.FOREGROUND_BLACK | self._defaultBg | ColorConsole.FOREGROUND_INTENSITY

        if logType == LogType.Good:
            return ColorConsole.FOREGROUND_GREEN | self._defaultBg | ColorConsole.FOREGROUND_INTENSITY

        if logType == LogType.Warn:
            return ColorConsole.FOREGROUND_YELLOW | self._defaultBg | ColorConsole.FOREGROUND_INTENSITY

        if logType == LogType.Error:
            return ColorConsole.FOREGROUND_RED | self._defaultBg | ColorConsole.FOREGROUND_INTENSITY

        assertThat(logType == LogType.Debug or logType == LogType.Noise)
        return ColorConsole.FOREGROUND_BLACK | self._defaultBg | ColorConsole.FOREGROUND_INTENSITY




