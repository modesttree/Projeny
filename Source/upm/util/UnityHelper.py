import os
import sys
import time

from upm.log.LogWatcher import LogWatcher

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.Assertions as Assertions

import upm.util.MiscUtil as MiscUtil
import upm.util.PlatformUtil as PlatformUtil
from upm.util.PlatformUtil import Platforms

from upm.util.SystemHelper import ProcessErrorCodeException

UnityLogFileLocation = os.getenv('localappdata') + '\\Unity\\Editor\\Editor.log'
#UnityLogFileLocation = '{Modest3dDir}/Modest3DLog.txt'

class UnityReturnedErrorCodeException(Exception):
    pass

class UnityUnknownErrorException(Exception):
    pass

class UnityHelper:
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')
    _varMgr = Inject('VarManager')
    _commonSettings = Inject('CommonSettings')

    def __init__(self):
        pass

    def onUnityLog(self, logStr):
        self._log.debug(logStr)

    def runEditorFunction(self, projectName, platform, editorCommand, batchMode = True, quitAfter = True, extraExtraArgs = ''):
        extraArgs = ''

        if quitAfter:
            extraArgs += ' -quit'

        if batchMode:
            extraArgs += ' -batchmode -nographics'

        extraArgs += ' ' + extraExtraArgs

        self.runEditorFunctionRaw(projectName, platform, editorCommand, extraArgs)

    def openUnity(self, projectName, platform):
        self._log.heading('Opening Unity')
        self._sys.executeNoWait('"[UnityExePath]" -buildTarget {0} -projectPath "[UnityProjectsDir]/{1}/{2}-{3}"'.format(self._getBuildTargetArg(platform), projectName, self._commonSettings.getShortProjectName(projectName), PlatformUtil.toPlatformFolderName(platform)))

    def _getBuildTargetArg(self, platform):

        if platform == Platforms.Windows:
            return 'win32'

        if platform == Platforms.WebPlayer:
            return 'web'

        if platform == Platforms.Android:
            return 'android'

        if platform == Platforms.WebGl:
            return 'WebGl'

        if platform == Platforms.OsX:
            return 'osx'

        if platform == Platforms.Linux:
            return 'linux'

        if platform == Platforms.Ios:
            return 'ios'

        assert False

    def runEditorFunctionRaw(self, projectName, platform, editorCommand, extraArgs):

        logPath = self._varMgr.expandPath(UnityLogFileLocation)

        logWatcher = LogWatcher(logPath, self.onUnityLog)
        logWatcher.start()

        os.environ['ModestTreeBuildConfigOverride'] = "FromBuildScript"

        assert self._varMgr.hasKey('UnityExePath'), "Could not find path variable 'UnityExePath'"

        try:
            command = '"[UnityExePath]" -buildTarget {0} -projectPath "[UnityProjectsDir]/{1}/{2}-{3}"'.format(self._getBuildTargetArg(platform), projectName, self._commonSettings.getShortProjectName(projectName), PlatformUtil.toPlatformFolderName(platform))

            if editorCommand:
                command += ' -executeMethod ' + editorCommand

            command += ' ' + extraArgs

            self._sys.executeAndWait(command)
        except ProcessErrorCodeException as e:
            raise UnityReturnedErrorCodeException("Error while running Unity!  Command returned with error code.")

        except:
            raise UnityUnknownErrorException("Unknown error occurred while running Unity!")
            # Forward stack trace info as well
            raise
        finally:
            logWatcher.stop()

            while not logWatcher.isDone:
                time.sleep(0.1)

            os.environ['ModestTreeBuildConfigOverride'] = ""

if __name__ == '__main__':
    pass



