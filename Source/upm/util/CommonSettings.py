
from mtm.ioc.Inject import Inject

class CommonSettings:
    _config = Inject('Config')

    def __init__(self):
        self.maxProjectNameLength = self._config.getInt('Projeny', 'MaxProjectNameLength')

    def getShortProjectName(self, val):
        return val[0:self.maxProjectNameLength]

