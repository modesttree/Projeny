
from mtm.util.Assert import *
from mtm.log.Logger import LogType
import sys

class LogStreamConsoleHeadingsOnly:
    def log(self, logType, message):
        if logType == LogType.Heading:
            sys.stdout.write(message + "\n")
            # This is completely necessary since in some cases we rely on this happening
            # to get real-time status info when observing prj from unity
            sys.stdout.flush()

