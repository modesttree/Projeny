
from prj.util.Assert import *
from prj.log.Logger import LogType
import sys

class LogStreamConsoleErrorsOnly:
    def log(self, logType, message):
        if logType == LogType.Error:
            sys.stderr.write(message + "\n")


