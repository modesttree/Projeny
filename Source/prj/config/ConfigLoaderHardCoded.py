
from prj.util.Assert import *

from prj.ioc.Inject import Inject, InjectOptional

class ConfigLoaderHardCoded:
    def __init__(self, config):
        self._config = config

    def LoadConfigs(self, configPaths = None):
        return [self._config]
