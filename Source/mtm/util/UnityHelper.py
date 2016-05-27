import os
import time

from mtm.log.LogWatcher import LogWatcher

from mtm.ioc.Inject import Inject

from mtm.util.Assert import *
import mtm.util.PlatformUtil as PlatformUtil
from mtm.util.Platforms import Platforms

from mtm.util.SystemHelper import ProcessErrorCodeException

UnityLogFileLocation = os.getenv('HOME') + '/Unity/Editor/Editor.log'
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
        self._log.noise(logStr)

    def runEditorFunction(self, projectName, platform, editorCommand, batchMode = True, quitAfter = True, extraArgs = ''):
        allArgs = ''

        if quitAfter:
            allArgs += ' -quit'

        if batchMode:
            allArgs += ' -batchmode -nographics'

        allArgs += ' ' + extraArgs

        self.runEditorFunctionRaw(projectName, platform, editorCommand, allArgs)

    def openUnity(self, projectName, platform):
        with self._log.heading('Opening Unity'):
            projectPath = self._sys.canonicalizePath("[UnityProjectsDir]/{0}/{1}-{2}".format(projectName, self._commonSettings.getShortProjectName(projectName), PlatformUtil.toPlatformFolderName(platform)))
            self._sys.executeNoWait('"[UnityExePath]" -buildTarget {0} -projectPath "{1}"'.format(self._getBuildTargetArg(platform), projectPath))

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

        assertThat(False)

    def runEditorFunctionRaw(self, projectName, platform, editorCommand, extraArgs):

        logPath = self._varMgr.expandPath(UnityLogFileLocation)

        logWatcher = LogWatcher(logPath, self.onUnityLog)
        logWatcher.start()

        os.environ['ModestTreeBuildConfigOverride'] = "FromBuildScript"

        assertThat(self._varMgr.hasKey('UnityExePath'), "Could not find path variable 'UnityExePath'")

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
        finally:
            logWatcher.stop()

            while not logWatcher.isDone:
                time.sleep(0.1)

            os.environ['ModestTreeBuildConfigOverride'] = ""

if __name__ == '__main__':
    pass



