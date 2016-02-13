
from mtm.util.Assert import *

from mtm.ioc.Inject import Inject
from mtm.ioc.Inject import InjectOptional

class ConfigLoaderHardCoded:
    def __init__(self, config):
        self._config = config

    def LoadConfigs(self, configPaths = None):
        return [self._config]
