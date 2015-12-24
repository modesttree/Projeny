
import sys

import os
from datetime import datetime

from upm.config.ConfigXml import ConfigXml
from upm.util.VarManager import VarManager

import upm.util.Util as Util

import upm.ioc.Container as Container
from upm.ioc.Inject import Inject, InjectMany

import time

class LogType:
    Info = 0
    Heading = 1
    Good = 2
    Warn = 3
    Error = 4
    Debug = 5
    HeadingSucceeded = 6
    HeadingFailed = 7

class Logger:
    _streams = InjectMany('LogStream')

    ''' Simple log class to use with build scripts '''
    def __init__(self):
        self.currentHeading = ''
        self._startTime = None
        self._headerStartTime = None
        self._errorOccurred = False

    @property
    def hasHeading(self):
        return self._headerStartTime != None

    def heading(self, msg):

        self.endHeading()

        self._errorOccurred = False
        self._headerStartTime = datetime.now()

        if not self._startTime:
            self._startTime = datetime.now()

        self.currentHeading = msg
        self._logInternal(msg + '...', LogType.Heading)

    def debug(self, msg):
        self._logInternal(msg, LogType.Debug)

    def info(self, msg):
        self._logInternal(msg, LogType.Info)

    def error(self, msg):

        if self._headerStartTime:
            self._errorOccurred = True

        self._logInternal(msg, LogType.Error)

    def warn(self, msg):
        self._logInternal(msg, LogType.Warn)

    def finished(self, msg):
        ''' Call this when your script is completely finished '''

        self.endHeading()
        self._logInternal(msg, LogType.Heading)

    def good(self, msg):
        self._logInternal(msg, LogType.Good)

    def endHeading(self):

        if not self._headerStartTime:
            return

        delta = datetime.now() - self._headerStartTime
        totalDelta = datetime.now() - self._startTime

        message = ''

        if self._errorOccurred:
            message = 'Failed'
        else:
            message = 'Done'

        message += ' (Took %s, time: %s, total elapsed: %s)' % (Util.formatTimeDelta(delta.total_seconds()), datetime.now().strftime('%H:%M:%S'), Util.formatTimeDelta(totalDelta.total_seconds()))

        self._headerStartTime = None

        if self._errorOccurred:
            self._logInternal(message, LogType.HeadingFailed)
        else:
            self._logInternal(message, LogType.HeadingSucceeded)

    def _logInternal(self, msg, logType):

        for stream in self._streams:
            stream.log(logType, msg)

if __name__ == '__main__':
    import upm.ioc.Container as Container

    class Log1:
        def log(self, logType, msg):
            print('log 1: ' + msg)

    class Log2:
        def log(self, logType, msg):
            print('log 2: ' + msg)

    Container.bind('LogStream').toSingle(Log1)
    Container.bind('LogStream').toSingle(Log2)

    Container.bind('Logger').toSingle(Logger)

    logger = Container.resolve('Logger')

    logger.info('test info')
    logger.error('test error')
