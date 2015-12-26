
from upm.ioc.Inject import Inject
from upm.ioc.Inject import InjectMany
import upm.ioc.IocAssertions as Assertions

from upm.util.Assert import *

class RemoteServerRegistry:
    _log = Inject('Logger')

    def __init__(self, settings):
        pass

    @property
    def releases(self):
        return []

    def init(self):
        pass

    def getName(self):
        return "Remote Server"

    def installRelease(self, releaseName):
        assertThat(False, 'TODO')


