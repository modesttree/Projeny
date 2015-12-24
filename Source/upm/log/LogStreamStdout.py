
import sys

class LogStreamStdout:
    def log(self, logType, message):
        sys.stdout.write('\n')
        sys.stdout.write(message)
        sys.stdout.flush()

