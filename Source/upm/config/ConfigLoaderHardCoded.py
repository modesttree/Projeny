
from upm.util.Assert import *

from upm.ioc.Inject import Inject, InjectOptional

class ConfigLoaderHardCoded:
    def __init__(self, config):
        self._config = config

    def LoadConfigs(self, configPaths = None):
        return [self._config]
