
from datetime import datetime
import traceback
from mtm.ioc.Inject import Inject
import mtm.util.Util as Util
from mtm.util.SystemHelper import ProcessErrorCodeException, ProcessTimeoutException
import mtm.util.MiscUtil as MiscUtil

class ScriptRunner:
    _log = Inject('Logger')

    def runWrapper(self, runner):
        startTime = datetime.now()

        succeeded = False

        try:
            runner()
            succeeded = True

        except KeyboardInterrupt as e:
            self._log.error('Operation aborted by user by hitting CTRL+C')

        except Exception as e:
            self._log.error(str(e))
            self._log.error('See PrjLog.txt for more details')

            # Only print stack trace if it's a build-script error
            if not isinstance(e, ProcessErrorCodeException) and not isinstance(e, ProcessTimeoutException):
                if MiscUtil.isRunningAsExe():
                    self._log.noise('\n' + traceback.format_exc())
                else:
                    self._log.debug('\n' + traceback.format_exc())

        totalSeconds = (datetime.now()-startTime).total_seconds()
        totalSecondsStr = Util.formatTimeDelta(totalSeconds)

        if succeeded:
            self._log.good('Operation completed successfully.  Took ' + totalSecondsStr + '.\n')
        else:
            self._log.info('Operation completed with errors.  Took ' + totalSecondsStr + '.\n')

        return succeeded

