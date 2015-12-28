
from upm.util.Assert import *
from upm.log.Logger import LogType
import sys

class LogStreamConsoleErrorsOnly:
    def log(self, logType, message):
        if logType == LogType.Error:
            sys.stderr.write(message + "\n")


