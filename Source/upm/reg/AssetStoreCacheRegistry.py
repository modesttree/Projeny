
import os
from upm.ioc.Inject import Inject
from upm.ioc.Inject import InjectMany
import upm.ioc.IocAssertions as Assertions
from upm.reg.LocalFolderRegistry import LocalFolderRegistry
from upm.util.Assert import *

class AssetStoreCacheRegistry:
    def __init__(self):
        unityUserRootFolder = os.path.join(os.getenv('APPDATA'), 'Unity')
        assetStoreCache1 = os.path.join(unityUserRootFolder, 'Asset Store')
        assetStoreCache2 = os.path.join(unityUserRootFolder, 'Asset Store-5.x')

        self._folderRegistries = [
            LocalFolderRegistry(assetStoreCache1), LocalFolderRegistry(assetStoreCache2)]

    @property
    def releases(self):
        result = []
        for subReg in self._folderRegistries:
            result += subReg.releases
        return result

    def init(self):
        for subReg in self._folderRegistries:
            subReg.init()

    def getName(self):
        return "Asset Store Cache"

    def installRelease(self, releaseInfo, outputDir):
        for subReg in self._folderRegistries:
            if releaseInfo in subReg.releases:
                subReg.installRelease(releaseInfo, outputDir)
                return

        assertThat(False)
