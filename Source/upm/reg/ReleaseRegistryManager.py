
from datetime import datetime
from upm.ioc.Inject import Inject
from upm.ioc.Inject import InjectMany
import upm.ioc.IocAssertions as Assertions

from upm.util.Assert import *

from upm.reg.LocalFolderRegistry import LocalFolderRegistry
from upm.reg.AssetStoreCacheRegistry import AssetStoreCacheRegistry
from upm.reg.RemoteServerRegistry import RemoteServerRegistry

import os
import upm.util.YamlSerializer as YamlSerializer

ReleaseInfoFileName = 'Release.yaml'

class ReleaseRegistryManager:
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')
    _config = Inject('Config')
    _sys = Inject('SystemHelper')
    _packageManager = Inject('PackageManager')

    def __init__(self):
        self._hasInitialized = False
        self._releaseRegistries = []
        self._installedReleaseInfos = []

    def _lazyInit(self):
        if self._hasInitialized:
            return

        self._hasInitialized = True
        for regSettings in self._config.getList('Registries'):
            for pair in regSettings.items():
                reg = self._createRegistry(pair[0], pair[1])
                reg.init()
                self._releaseRegistries.append(reg)

        self._findInstalledReleases()

        self._log.info("Finished initializing Release Registry Manager, found {0} releases in total", self._getTotalReleaseCount())

    def _findInstalledReleases(self):
        for name in self._packageManager.getAllPackageNames():
            path = self._varMgr.expandPath('[UnityPackagesDir]/{0}'.format(name))

            releaseInfoPath = os.path.join(path, 'Release.yaml')

            if self._sys.fileExists(releaseInfoPath):
                releaseInfo = YamlSerializer.deserialize(self._sys.readFileAsText(releaseInfoPath))
                self._installedReleaseInfos.append(releaseInfo)

    def _getTotalReleaseCount(self):
        total = 0
        for reg in self._releaseRegistries:
            total += len(reg.releases)
        return total

    def _createRegistry(self, regType, settings):
        if regType == 'LocalFolder':
            folderPath = self._varMgr.expand(settings['Path']).replace("\\", "/")
            return LocalFolderRegistry(folderPath)

        if regType == 'AssetStoreCache':
            return AssetStoreCacheRegistry()

        if regType == 'Remote':
            return RemoteServerRegistry()

        assertThat(False, "Could not find registry with type '{0}'", regType)

    def listAllReleases(self):
        self._lazyInit()
        self._log.heading('Found {0} Releases', self._getTotalReleaseCount())

        for release in self.lookupAllReleases():
            self._log.info("{0} ({1})", release.Title, release.Version)

    def lookupAllReleases(self):
        self._lazyInit()
        for registry in self._releaseRegistries:
            for release in registry.releases:
                yield release

    def _findReleaseInfoAndRegistry(self, releaseId, releaseVersionCode):
        for registry in self._releaseRegistries:
            for release in registry.releases:
                if release.id == releaseId and release.versionCode == releaseVersionCode:
                    return (release, registry)

        return (None, None)

    def installRelease(self, releaseId, releaseVersionCode):

        # TODO: - when not provided just install the newest
        assertThat(releaseVersionCode)

        self._lazyInit()
        self._log.heading("Attempting to install release with id '{0}'", releaseId)

        assertThat(len(self._releaseRegistries) > 0, "Could not find any registries to search for the given release name")

        releaseInfo, registry = self._findReleaseInfoAndRegistry(releaseId, releaseVersionCode)

        assertThat(releaseInfo, "Failed to install release '{0}' - could not find it in any of the release registries.\nRegistries checked: \n  {1}\nTry listing all available release with the -lr command"
           .format(releaseId, "\n  ".join([x.getName() for x in self._releaseRegistries])))

        destDir = '[UnityPackagesDir]/{0}'.format(releaseInfo.name)

        assertThat(not self._sys.directoryExists(destDir), "Found existing package with the same name '{0}'", releaseInfo.name)

        registry.installRelease(releaseInfo, destDir)

        yamlDict = releaseInfo.__dict__
        yamlDict['installDate'] = datetime.utcnow()
        yamlStr = YamlSerializer.serialize(yamlDict)
        self._sys.writeFileAsText(os.path.join(destDir, ReleaseInfoFileName), yamlStr)

        self._log.info("Successfully installed '{0}' (version {1})", releaseInfo.name, releaseInfo.version)
