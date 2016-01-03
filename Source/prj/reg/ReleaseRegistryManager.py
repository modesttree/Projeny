
from datetime import datetime
from prj.ioc.Inject import Inject
from prj.ioc.Inject import InjectMany
import prj.ioc.IocAssertions as Assertions

from prj.util.Assert import *

from prj.reg.LocalFolderRegistry import LocalFolderRegistry
from prj.reg.AssetStoreCacheRegistry import AssetStoreCacheRegistry
from prj.reg.RemoteServerRegistry import RemoteServerRegistry

import prj.util.MiscUtil as MiscUtil

from prj.reg.PackageInfo import PackageInstallInfo

import os
import prj.util.YamlSerializer as YamlSerializer

from prj.main.PackageManager import InstallInfoFileName

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

        if regType == 'FileServer':
            return RemoteServerRegistry(settings['ManifestUrl'])

        assertThat(False, "Could not find registry with type '{0}'", regType)

    def listAllReleases(self):
        self._lazyInit()
        self._log.heading('Found {0} Releases', self._getTotalReleaseCount())

        for release in self.lookupAllReleases():
            self._log.info("{0} ({1}) ({2})", release.name, release.version, release.versionCode)

    def lookupAllReleases(self):
        self._lazyInit()

        result = []
        for registry in self._releaseRegistries:
            for release in registry.releases:
                result.append(release)
        result.sort(key = lambda x: x.name.lower())
        return result

    def _findReleaseInfoAndRegistryByIdAndVersionCode(self, releaseId, releaseVersionCode):
        assertIsType(releaseVersionCode, int)
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
        self._log.heading("Attempting to install release '{0}' (version '{1}')", releaseName, releaseVersion)

        assertThat(len(self._releaseRegistries) > 0, "Could not find any registries to search for the given release")

        releaseInfo, registry = self._findReleaseInfoAndRegistryByNameAndVersion(releaseName, releaseVersion)

        assertThat(releaseInfo, "Failed to install release '{0}' (version {1}) - could not find it in any of the release registries.\nRegistries checked: \n  {2}\nTry listing all available release with the -lr command"
           .format(releaseName, releaseVersion, "\n  ".join([x.getName() for x in self._releaseRegistries])))

        self._installReleaseInternal(releaseInfo, registry, suppressPrompts)

    def installReleaseById(self, releaseId, releaseVersionCode, suppressPrompts = False):

        self._log.info("Attempting to install release with ID '{0}' and version code '{1}'", releaseId, releaseVersionCode)

        try:
            releaseVersionCode = int(releaseVersionCode)
        except ValueError:
            assertThat(False, "Invalid version code '{0}' - must be convertable to an integer", releaseVersionCode)

        assertThat(releaseVersionCode, 'Invalid release version code supplied')
        assertThat(releaseId)

        self._lazyInit()

        assertThat(len(self._releaseRegistries) > 0, "Could not find any registries to search for the given release")

        releaseInfo, registry = self._findReleaseInfoAndRegistryByIdAndVersionCode(releaseId, releaseVersionCode)

        assertThat(releaseInfo, "Failed to install release '{0}' - could not find it in any of the release registries.\nRegistries checked: \n  {1}\nTry listing all available release with the -lr command"
           .format(releaseId, "\n  ".join([x.getName() for x in self._releaseRegistries])))

        self._installReleaseInternal(releaseInfo, registry, suppressPrompts)

    def _installReleaseInternal(self, releaseInfo, registry, suppressPrompts = False):

        self._log.heading("Installing release '{0}' (version {1})", releaseInfo.name, releaseInfo.version)

        installDirName = None

        for packageInfo in self._packageManager.getAllPackageInfos():
            installInfo = packageInfo.installInfo

            if installInfo and installInfo.releaseInfo and installInfo.releaseInfo.id == releaseInfo.id:
                if installInfo.releaseInfo.versionCode == releaseInfo.versionCode:
                    if not suppressPrompts:
                        shouldContinue = MiscUtil.confirmChoice(
                            "Release '{0}' (version {1}) is already installed.  Would you like to re-install anyway?  Note that this will overwrite any local changes you've made to it.".format(releaseInfo.name, releaseInfo.version))

                        assertThat(shouldContinue, 'User aborted')
                else:
                    print("\nFound release '{0}' already installed with version '{1}'".format(releaseInfo.name, releaseInfo.version), end='')

                    installDirection = 'UPGRADE' if releaseInfo.versionCode > installInfo.releaseInfo.versionCode else 'DOWNGRADE'

                    if not suppressPrompts:
                        shouldContinue = MiscUtil.confirmChoice("Are you sure you want to {0} '{1}' from version '{2}' to version '{3}'? (y/n)".format(installDirection, releaseInfo.name, installInfo.releaseInfo.version, releaseInfo.version))
                        assertThat(shouldContinue, 'User aborted')

                self._packageManager.deletePackage(packageInfo.name)
                # Retain original directory name in case it is referenced by other packages
                installDirName = packageInfo.name

        installDirName = registry.installRelease(releaseInfo, installDirName)

        destDir = self._varMgr.expand('[UnityPackagesDir]/{0}'.format(installDirName))

        assertThat(self._sys.directoryExists(destDir), 'Expected dir "{0}" to exist', destDir)

        newInstallInfo = PackageInstallInfo()
        newInstallInfo.releaseInfo = releaseInfo
        newInstallInfo.installDate = datetime.utcnow()

        yamlStr = YamlSerializer.serialize(newInstallInfo)
        self._sys.writeFileAsText(os.path.join(destDir, InstallInfoFileName), yamlStr)

        self._log.info("Successfully installed '{0}' (version {1})", releaseInfo.name, releaseInfo.version)
