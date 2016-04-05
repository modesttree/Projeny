import subprocess
from subprocess import Popen, PIPE
import errno
import signal
import threading
import time
import shlex
from mtm.ioc.Inject import Inject
from queue import Queue, Empty
import sys

from mtm.log.LogStreamStdout import LogStreamStdout

class ResultType:
    Success = 1
    Error = 2
    TimedOut = 3

class ProcessRunner:
    _log = Inject('Logger')

    def execNoWait(self, vals, startDir):
        params = {}

        if startDir != None:
            params['cwd'] = startDir

        Popen(vals, **params)

    def waitForProcessOrTimeout(self, commandVals, seconds, startDir = None):

        params = {}
        params['stdout'] = subprocess.PIPE
        params['stderr'] = subprocess.STDOUT

        if startDir != None:
            params['cwd'] = startDir

        proc = Popen(commandVals, **params)

        # TODO - clean this up so there's only one thread, then
        # do the timeout logic on the main thread
        timeout = KillProcessThread(seconds, proc.pid)
        timeout.run()

        def enqueueOutput(out, queue):
            for line in iter(out.readline, b''):
                queue.put(line)
            out.close()

        # We use a queue here instead of just calling stdout.readline() on the main thread
        # so that we can catch the KeyboardInterrupt event, and force kill the process
        queue = Queue()
        thread = threading.Thread(target = enqueueOutput, args = (proc.stdout, queue))
        thread.daemon = True # thread dies with the program
        thread.start()

        while True:
            try:
                try:
                    line = queue.get_nowait()
                    self._log.noise(line.decode(sys.stdout.encoding).rstrip())
                except Empty:
                    if not thread.isAlive():
                        break
                    time.sleep(0.2)
            except KeyboardInterrupt as e:
                self._log.error("Detected KeyboardInterrupt - killing process...")
                timeout.forceKill()
                raise e

        resultCode = proc.wait()

        timeout.cancel()

        if timeout.timeOutOccurred:
            return ResultType.TimedOut

        if resultCode != 0:
            return ResultType.Error

        return ResultType.Success

    # Note that in this case we pass the command as a string
    # This is recommended by the python docs here when using shell = True
    # https://docs.python.org/2/library/subprocess.html#subprocess.Popen
    def execShellCommand(self, commandStr, startDir = None, wait = True):

        params = {}
        params['stdout'] = subprocess.PIPE
        params['stderr'] = subprocess.PIPE
        params['shell'] = True

        if startDir != None:
            params['cwd'] = startDir

        # Would be nice to get back output in real time but I can't figure
        # out a way to do this
        # This method should only be used for a few command-prompt specific
        # commands anyway so not a big loss
        proc = Popen(commandStr, **params)

        if not wait:
            return ResultType.Success

        (stdoutData, stderrData) = proc.communicate()

        output = stdoutData.decode(encoding=sys.stdout.encoding, errors='ignore').strip()
        errors = stderrData.decode(encoding=sys.stderr.encoding, errors='ignore').strip()

        if output:
            for line in output.split('\n'):
                self._log.noise(line)

        if errors:
            self._log.error('Error occurred during command "{0}":'.format(commandStr))
            for line in errors.split('\n'):
                self._log.error('    ' + line)

        exitStatus = proc.returncode

        if exitStatus != 0:
            return ResultType.Error

        return ResultType.Success

class KillProcessThread:

    def __init__(self, seconds, pid):
        self.pid = pid
        self.timeOutOccurred = False
        self.seconds = seconds
        self.cond = threading.Condition()
        self.cancelled = False
        self.thread = threading.Thread(target=self.wait)

        # Setting daemon to true will kill the thread if the main
        # thread aborts (eg. user hitting ctrl+c)
        self.thread.daemon = True

    def run(self):
        '''Begin the timeout.'''
        self.thread.start()

    def wait(self):
        with self.cond:
            self.cond.wait(self.seconds)

            if not self.cancelled:
                self.forceKill()

    def cancel(self):
        '''Cancel the timeout, if it hasn't yet occured.'''
        with self.cond:
            self.cancelled = True
            self.cond.notify()
        self.thread.join()

    def forceKill(self):
        self.timeOutOccurred = True
        try:
            commandVals = shlex.split('taskkill /f /pid %i' % self.pid)
            Popen(commandVals, stdout=PIPE, stderr=PIPE)
        except OSError as e:
            # If the process is already gone, ignore the error.
            if e.errno not in (errno.EPERM, errno. ESRCH):
                raise e

#if __name__ == '__main__':
    #import mtm.ioc.Container as Container

    #from mtm.config.ConfigXml import ConfigXml
    #from mtm.log.Logger import Logger
    #from mtm.util.VarManager import VarManager
    #from datetime import datetime

    #Container.bind('Config').toSingle(ConfigXml)
    #Container.bind('VarManager').toSingle(VarManager)
    #Container.bind('ProcessRunner').toSingle(ProcessRunner)
    #Container.bind('Logger').toSingle(Logger)
    #Container.bind('LogStream').toSingle(LogStreamStdout)

    #startTime = datetime.now()

    #processRunner = Container.resolve('ProcessRunner')
    #processRunner.waitForProcessOrTimeout(['sleep','5'], 2)

    #totalSeconds = (datetime.now()-startTime).total_seconds()
    #print("finished after {0}".format(totalSeconds))

