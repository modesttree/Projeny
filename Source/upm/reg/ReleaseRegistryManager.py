
from datetime import datetime
from upm.ioc.Inject import Inject
from upm.ioc.Inject import InjectMany
import upm.ioc.IocAssertions as Assertions

from upm.util.Assert import *

from upm.reg.LocalFolderRegistry import LocalFolderRegistry
from upm.reg.AssetStoreCacheRegistry import AssetStoreCacheRegistry
from upm.reg.RemoteServerRegistry import RemoteServerRegistry

import upm.util.MiscUtil as MiscUtil

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

    def _lazyInit(self):
        if self._hasInitialized:
            return

        self._hasInitialized = True
        for regSettings in self._config.getList('Registries'):
            for pair in regSettings.items():
                reg = self._createRegistry(pair[0], pair[1])
                reg.init()
                self._releaseRegistries.append(reg)

        self._log.info("Finished initializing Release Registry Manager, found {0} releases in total", self._getTotalReleaseCount())

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

    def _findReleaseInfoAndRegistryByIdAndVersionCode(self, releaseId, releaseVersionCode):
        for registry in self._releaseRegistries:
            for release in registry.releases:
                if release.id == releaseId and release.versionCode == releaseVersionCode:
                    return (release, registry)
        return (None, None)

    def _findReleaseInfoAndRegistryByNameAndVersion(self, releaseName, releaseVersion):
        for registry in self._releaseRegistries:
            for release in registry.releases:
                if release.name == releaseName and release.version == releaseVersion:
                    return (release, registry)
        return (None, None)

    def installReleaseByName(self, releaseName, releaseVersion, suppressPrompts = False):
        assertThat(releaseName)
        assertThat(releaseVersion)

        self._lazyInit()
        self._log.heading("Attempting to install release '{0}' for version '{1}'", releaseName, releaseVersion)

        assertThat(len(self._releaseRegistries) > 0, "Could not find any registries to search for the given release")

        releaseInfo, registry = self._findReleaseInfoAndRegistryByNameAndVersion(releaseName, releaseVersion)

        assertThat(releaseInfo, "Failed to install release '{0}' (version {1}) - could not find it in any of the release registries.\nRegistries checked: \n  {2}\nTry listing all available release with the -lr command"
           .format(releaseName, releaseVersion, "\n  ".join([x.getName() for x in self._releaseRegistries])))

        self._installReleaseInternal(releaseInfo, registry)

    def installRelease(self, releaseId, releaseVersionCode, suppressPrompts = False):

        assertThat(releaseVersionCode)
        assertThat(releaseId)

        self._lazyInit()
        self._log.heading("Attempting to install release with id '{0}'", releaseId)

        assertThat(len(self._releaseRegistries) > 0, "Could not find any registries to search for the given release")

        releaseInfo, registry = self._findReleaseInfoAndRegistryByIdAndVersionCode(releaseId, releaseVersionCode)

        assertThat(releaseInfo, "Failed to install release '{0}' - could not find it in any of the release registries.\nRegistries checked: \n  {1}\nTry listing all available release with the -lr command"
           .format(releaseId, "\n  ".join([x.getName() for x in self._releaseRegistries])))

        self._installReleaseInternal(releaseInfo, registry)

    def _installReleaseInternal(self, releaseInfo, registry, suppressPrompts = False):

        for packageInfo in self._packageManager.getAllPackageInfos():
            installedInfo = packageInfo.releaseInfo

            if installedInfo and installedInfo.id == releaseInfo.id:
                if installedInfo.versionCode == releaseInfo.versionCode:
                    self._log.info("Release '{0}' (version {1}) is already installed.  Installation aborted.", releaseInfo.name, releaseInfo.version)
                    return

                print("\nFound release '{0}' already installed with version '{1}'".format(releaseInfo.name, releaseInfo.version), end='')

                installDirection = 'UPGRADE' if releaseInfo.versionCode > installedInfo.versionCode else 'DOWNGRADE'

                if not suppressPrompts:
                    shouldContinue = MiscUtil.confirmChoice("Are you sure you want to {0} '{1}' from version '{2}' to version '{3}'? (y/n)".format(installDirection, releaseInfo.name, installedInfo.version, releaseInfo.version))
                    assertThat(shouldContinue, 'User aborted')

                self._packageManager.deletePackage(packageInfo.name)

        destDir = '[UnityPackagesDir]/{0}'.format(releaseInfo.name)

        assertThat(not self._sys.directoryExists(destDir), "Found existing package with the same name '{0}'", releaseInfo.name)

        registry.installRelease(releaseInfo, destDir)

        releaseInfo.installDate = datetime.utcnow()
        yamlStr = YamlSerializer.serialize(releaseInfo)
        self._sys.writeFileAsText(os.path.join(destDir, ReleaseInfoFileName), yamlStr)

        self._log.info("Successfully installed '{0}' (version {1})", releaseInfo.name, releaseInfo.version)
