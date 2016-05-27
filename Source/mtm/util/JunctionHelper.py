
import os
import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
from mtm.util.SystemHelper import ProcessErrorCodeException
import mtm.ioc.IocAssertions as Assertions
import mtm.util.JunctionUtil as JunctionUtil

from mtm.util.Assert import *

class JunctionHelper:
    """
    Misc. helper functions related to windows junctions
    """
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')

    def __init__(self):
        pass

    def removeJunction(self, linkDir):
        linkDir = self._varMgr.expand(linkDir)
        if os.path.isdir(linkDir) and JunctionUtil.islink(linkDir):
            try:
                # Use rmdir not python unlink to ensure we don't delete the link source
                self._sys.executeShellCommand('rmdir "{0}"'.format(linkDir))
            except Exception as e:
                raise Exception('Failed while attempting to delete junction "{0}":\n{1}'.format(linkDir, str(e))) from e

            return True

        return False

    def makeJunction(self, actualPath, linkPath):
        actualPath = self._varMgr.expandPath(actualPath)
        linkPath = self._varMgr.expandPath(linkPath)

        if os.path.exists(actualPath):
            self._sys.executeShellCommand("rm -r {0}".format(actualPath))
        if os.path.exists(linkPath):
            self._sys.executeShellCommand("rm -r {0}".format(linkPath))

        assertThat(not self._sys.directoryExists(actualPath), "These locations should not exist: {0}, {1}".format(actualPath, linkPath))

        self._log.debug('Making symlink with actual path ({0}) and new link path ({1})'.format(linkPath, actualPath))
        # Note: mklink is a shell command and can't be executed otherwise
        self._sys.executeShellCommand('ln -s {0} {1}'.format(linkPath, actualPath))

    def removeJunctionsInDirectory(self, dirPath, recursive):
        fullDirPath = self._varMgr.expandPath(dirPath)

        if not os.path.exists(fullDirPath):
            return

        for name in os.listdir(fullDirPath):
            fullPath = os.path.join(fullDirPath, name)

            if not os.path.isdir(fullPath):
                continue

            if self.removeJunction(fullPath):
                if os.path.exists(fullPath + '.meta'):
                    os.remove(fullPath + '.meta')

                self._log.debug('Removed directory for package "{0}"'.format(name))
            else:
                if recursive:
                    self.removeJunctionsInDirectory(fullPath, True)

