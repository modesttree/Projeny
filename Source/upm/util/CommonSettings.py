
from upm.ioc.Inject import Inject

ConfigFileName = 'upm.yaml'

class CommonSettings:
    _config = Inject('Config')

    def __init__(self):
        self.maxProjectNameLength = self._config.getInt('MaxProjectNameLength')

    def getShortProjectName(self, val):
        return val[0:self.maxProjectNameLength]

