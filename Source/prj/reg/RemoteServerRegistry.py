
from prj.ioc.Inject import Inject
from prj.ioc.Inject import InjectMany
import prj.ioc.IocAssertions as Assertions

from prj.util.Assert import *

class RemoteServerRegistry:
    _log = Inject('Logger')

    def __init__(self):
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

