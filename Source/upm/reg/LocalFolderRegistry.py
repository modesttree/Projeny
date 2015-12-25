
from upm.ioc.Inject import Inject
from upm.ioc.Inject import InjectMany
import upm.ioc.IocAssertions as Assertions

from upm.util.Assert import *

class LocalFolderRegistry:
    _log = Inject('Logger')

    def __init__(self):
        self._folderPath = "F:/Temp/UnityPackages"

    def getName(self):
        return "Local Folder - {0}".format(self._folderPath)

    def tryInstallRelease(self, releaseName):
        print("TODO")
        return False

