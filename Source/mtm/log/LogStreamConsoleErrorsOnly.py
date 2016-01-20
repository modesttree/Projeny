
from mtm.util.Assert import *
from mtm.log.Logger import LogType
import sys

class LogStreamConsoleErrorsOnly:
    def log(self, logType, message):
        if logType == LogType.Error:
            sys.stderr.write(message + "\n")


