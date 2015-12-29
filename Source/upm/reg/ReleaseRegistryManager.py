
from datetime import datetime
from upm.ioc.Inject import Inject
from upm.ioc.Inject import InjectMany
import upm.ioc.IocAssertions as Assertions

from upm.util.Assert import *

from upm.reg.LocalFolderRegistry import LocalFolderRegistry
from upm.reg.AssetStoreCacheRegistry import AssetStoreCacheRegistry
from upm.reg.RemoteServerRegistry import RemoteServerRegistry

import os
import yaml

ReleaseInfoFileName = 'Release.yaml'

class ReleaseRegistryManager:
    _log = Inject('Logger')
    _config = Inject('Config')
    _sys = Inject('SystemHelper')

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
            return LocalFolderRegistry(settings)

        if regType == 'AssetStoreCache':
            return AssetStoreCacheRegistry(settings)

        if regType == 'Remote':
            return RemoteServerRegistry(settings)

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

    def installRelease(self, releaseName):
        self._lazyInit()
        self._log.heading("Attempting to install release '{0}'", releaseName)

        assertThat(len(self._releaseRegistries) > 0, "Could not find any registries to search for the given release name")

        destDir = '[UnityPackagesDir]/{0}'.format(releaseName)

        for registry in self._releaseRegistries:
            for release in registry.releases:
                if release.Title == releaseName:
                    registry.installRelease(release, destDir)

                    outInfo = release.__dict__
                    outInfo['InstallDate'] = datetime.now()
                    yamlStr = yaml.dump(outInfo, default_flow_style=False)
                    self._sys.writeFileAsText(os.path.join(destDir, ReleaseInfoFileName), yamlStr)
                    return

        assertThat(False, "Failed to install release '{0}' - could not find it in any of the release registries.\nRegistries checked: \n  {1}\nTry listing all available release with the -lr command"
           .format(releaseName, "\n  ".join([x.getName() for x in self._releaseRegistries])))

