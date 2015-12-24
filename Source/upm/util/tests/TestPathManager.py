
import sys
import os
import unittest

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.Assertions as Assertions

from upm.util.VarManager import VarManager
from upm.config.ConfigXml import ConfigXml

class TestVarManager(unittest.TestCase):
    def setUp(self):
        Container.clear()

    def test1(self):
        Container.bind('Config').toSingle(ConfigXml)
        Container.bind('VarManager').toSingle(VarManager, {'foo': 'yep [bar]', 'bar': 'result2'})

        pathMgr = Container.resolve('VarManager')

        assert pathMgr.hasKey('foo')
        assert not pathMgr.hasKey('asdf')
        assert pathMgr.tryGet('bobsdf') == None
        assert pathMgr.expand('before [bar] after') == 'before result2 after'
        assert pathMgr.expand('before [foo] after') == 'before yep result2 after'

        assert not pathMgr.hasKey('qux')
        pathMgr.add('qux', 'sadf')
        assert pathMgr.hasKey('qux')
        assert pathMgr.expand('[qux]') == 'sadf'

        print('Done')

if __name__ == '__main__':
    unittest.main()

