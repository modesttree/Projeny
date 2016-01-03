
import os
import prj.ioc.Container as Container
from prj.ioc.Inject import Inject
import prj.ioc.IocAssertions as Assertions
import prj.util.JunctionUtil as JunctionUtil

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

        self._sys.makeMissingDirectoriesInPath(linkPath)

        self._log.debug('Making junction with actual path ({0}) and new link path ({1})'.format(linkPath, actualPath))
        # Note: mklink is a shell command and can't be executed otherwise
        self._sys.executeShellCommand('mklink /J "{0}" "{1}"'.format(linkPath, actualPath))

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

if __name__ == '__main__':

    import mtm.build.BuildCommon as BuildCommon
    BuildCommon.installBindings(True, True)
    helper = Container.resolve('JunctionHelper')

    #helper.makeJunction('F:/Temp/JunctionTest/Source', 'F:/Temp/JunctionTest/Junction')

    helper.removeJunction('F:/Temp/JunctionTest/Junction')

