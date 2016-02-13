
import sys
import time
import errno
import os
import signal
import threading
import msvcrt

class LogWatcher:
    def __init__(self, logPath, logFunc):
        self.thread = threading.Thread(target=self.update)
        # Setting daemon to true will kill the thread if the main
        # thread aborts (eg. user hitting ctrl+c)
        self.thread.daemon = True
        self.logPath = logPath
        self.logFunc = logFunc
        self.killed = False
        self.isDone = False

    def start(self):
        self.thread.start()

    def stop(self):
        self.killed = True

    def update(self):

        # Wait until file exists
        while True:

            if self.killed:
                return

            if os.path.isfile(self.logPath):
                break

            time.sleep(1)

        with open(self.logPath, 'r', encoding='utf-8', errors='ignore') as logFile:

            logFile.seek(0,2)

            while not self.killed:

                try:
                    where = logFile.tell()
                except:
                    time.sleep(0.1)
                    continue

                line = logFile.readline()

                if not line:
                    time.sleep(1)
                    logFile.seek(where)
                else:
                    self.logFunc(line.strip())

            # Make sure we get the rest of the log before quitting
            while True:
                line = logFile.readline()
                if not line:
                    break

                self.logFunc(line.strip())

        self.isDone = True

def onLog(logStr):
    print(logStr)

if __name__ == '__main__':

    if len(sys.argv) != 2:
        print("Invalid # of arguments")
        exit(-1)

    path = sys.argv[1]

    log = LogWatcher(path, onLog)
    log.start()

    while 1:
        if msvcrt.kbhit():
            key = msvcrt.getch().decode('UTF-8')

            if ord(key) == 27:
                sys.exit()

            if key == 'c':
                os.system('cls')

        time.sleep(0.1)

    log.stop()
