

from datetime import datetime

from mtm.util.VarManager import VarManager

import re
import mtm.util.Util as Util

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
from mtm.ioc.Inject import InjectMany

from mtm.util.Assert import *

class LogType:
    Noise = 0
    Debug = 1
    Info = 2
    Good = 3
    Warn = 4
    Error = 5
    HeadingStart = 6
    HeadingEnd = 7

class LogMap:
    def __init__(self, regex, sub):
        self.regex = regex
        self.sub = sub

class HeadingBlock:
    def __init__(self, log, message):
        self._log = log
        self._message = message

        self._log._logInternal(self._message + "...", LogType.HeadingStart)
        self._startTime = datetime.now()

    def __enter__(self):
        pass

    def __exit__(self, type, value, traceback):
        assertThat(self._log.hasHeading)

        assertIsEqual(self._log._headingBlocks.pop(), self)

        delta = datetime.now() - self._startTime
        totalDelta = datetime.now() - self._log.totalStartTime

        errorOccurred = type != None

        message = '{0} {1} (Took {2}, time: {3}, total elapsed: {4})'.format(
            'Failed during task: ' if errorOccurred else 'Finished',
            self._message[0].lower() + self._message[1:],
            Util.formatTimeDelta(delta.total_seconds()),
            datetime.now().strftime('%H:%M:%S'),
            Util.formatTimeDelta(totalDelta.total_seconds()))

        self._log._logInternal(message, LogType.Error if errorOccurred else LogType.HeadingEnd)

class Logger:
    _streams = InjectMany('LogStream')
    _config = Inject('Config')

    ''' Simple log class to use with build scripts '''
    def __init__(self):
        self._totalStartTime = None
        self._headingBlocks = []

        self.goodPatterns = self._getPatterns('GoodPatterns')
        self.goodMaps = self._getPatternMaps('GoodPatternMaps')

        self.infoPatterns = self._getPatterns('InfoPatterns')
        self.infoMaps = self._getPatternMaps('InfoPatternMaps')

        self.errorPatterns = self._getPatterns('ErrorPatterns')
        self.errorMaps = self._getPatternMaps('ErrorPatternMaps')

        self.warningPatterns = self._getPatterns('WarningPatterns')
        self.warningMaps = self._getPatternMaps('WarningPatternMaps')
        self.warningPatternsIgnore = self._getPatterns('WarningPatternsIgnore')

        self.debugPatterns = self._getPatterns('DebugPatterns')
        self.debugMaps = self._getPatternMaps('DebugPatternMaps')

    @property
    def totalStartTime(self):
        return self._totalStartTime

    @property
    def hasHeading(self):
        return any(self._headingBlocks)

    def getCurrentNumHeadings(self):
        return len(self._headingBlocks)

    def heading(self, message, *args):

        if not self._totalStartTime:
            self._totalStartTime = datetime.now()

        # Need to format it now so that heading gets the args
        if len(args) > 0:
            message = message.format(*args)

        block = HeadingBlock(self, message)
        self._headingBlocks.append(block)
        return block

    def noise(self, message, *args):
        self._logInternal(message, LogType.Noise, *args)

    def debug(self, message, *args):
        self._logInternal(message, LogType.Debug, *args)

    def info(self, message, *args):
        self._logInternal(message, LogType.Info, *args)

    def error(self, message, *args):

        self._logInternal(message, LogType.Error, *args)

    def warn(self, message, *args):
        self._logInternal(message, LogType.Warn, *args)

    def good(self, message, *args):
        self._logInternal(message, LogType.Good, *args)

    def _logInternal(self, message, logType, *args):

        if len(args) > 0:
            message = message.format(*args)

        newLogType, newMessage = self.classifyMessage(logType, message)

        for stream in self._streams:
            stream.log(newLogType, newMessage)

    def _getPatternMaps(self, settingName):
        maps = self._config.tryGetDictionary({}, 'Log', settingName)

        result = []
        for key, value in maps.items():
            regex = re.compile(key)
            logMap = LogMap(regex, value)
            result.append(logMap)

        return result

    def _getPatterns(self, settingName):
        patternStrings = self._config.tryGetList([], 'Log', settingName)

        result = []
        for pattern in patternStrings:
            result.append(re.compile('.*' + pattern + '.*'))

        return result

    def tryMatchPattern(self, message, maps, patterns):
        for logMap in maps:
            if logMap.regex.match(message):
                return logMap.regex.sub(logMap.sub, message)

        for pattern in patterns:
            match = pattern.match(message)

            if match:
                groups = match.groups()

                if len(groups) > 0:
                    return groups[0]

                return message

        return None

    def classifyMessage(self, logType, message):

        if logType != LogType.Noise:
            # If it is explicitly logged as something by calling for eg. log.info, use info type
            return logType, message

        parsedMessage = self.tryMatchPattern(message, self.errorMaps, self.errorPatterns)
        if parsedMessage:
            return LogType.Error, parsedMessage

        if not any(p.match(message) for p in self.warningPatternsIgnore):
            parsedMessage = self.tryMatchPattern(message, self.warningMaps, self.warningPatterns)
            if parsedMessage:
                return LogType.Warn, parsedMessage

        parsedMessage = self.tryMatchPattern(message, self.goodMaps, self.goodPatterns)
        if parsedMessage:
            return LogType.Good, parsedMessage

        parsedMessage = self.tryMatchPattern(message, self.infoMaps, self.infoPatterns)
        if parsedMessage:
            return LogType.Info, parsedMessage

        parsedMessage = self.tryMatchPattern(message, self.debugMaps, self.debugPatterns)
        if parsedMessage:
            return LogType.Debug, parsedMessage

        return LogType.Noise, message

if __name__ == '__main__':
    import mtm.ioc.Container as Container

    class Log1:
        def log(self, logType, message):
            print('log 1: ' + message)

    class Log2:
        def log(self, logType, message):
            print('log 2: ' + message)

    Container.bind('LogStream').toSingle(Log1)
    Container.bind('LogStream').toSingle(Log2)

    Container.bind('Logger').toSingle(Logger)

    logger = Container.resolve('Logger')

    logger.info('test info')
    logger.error('test error')
