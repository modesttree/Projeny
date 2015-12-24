
import sys
import os
import unittest

import upm.ioc.Container as Container
from upm.ioc.Inject import Inject
import upm.ioc.IocAssertions as Assertions

from upm.util.Assert import *
from upm.util.VarManager import VarManager

class TestVarManager(unittest.TestCase):
    def setUp(self):
        Container.clear()

    def test1(self):
        assertThat(False)
        #Container.bind('Config').toSingle(ConfigXml)
        Container.bind('VarManager').toSingle(VarManager, {'foo': 'yep [bar]', 'bar': 'result2'})

        pathMgr = Container.resolve('VarManager')

        assertThat(pathMgr.hasKey('foo'))
        assertThat(not pathMgr.hasKey('asdf'))
        assertThat(pathMgr.tryGet('bobsdf') == None)
        assertThat(pathMgr.expand('before [bar] after') == 'before result2 after')
        assertThat(pathMgr.expand('before [foo] after') == 'before yep result2 after')

        assertThat(not pathMgr.hasKey('qux'))
        pathMgr.add('qux', 'sadf')
        assertThat(pathMgr.hasKey('qux'))
        assertThat(pathMgr.expand('[qux]') == 'sadf')

        print('Done')

if __name__ == '__main__':
    unittest.main()

