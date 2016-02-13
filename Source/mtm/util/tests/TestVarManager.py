
import unittest

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.IocAssertions as Assertions

from mtm.config.ConfigLoaderHardCoded import ConfigLoaderHardCoded
from mtm.config.Config import Config

from mtm.util.Assert import *
from mtm.util.VarManager import VarManager

class TestVarManager(unittest.TestCase):
    def setUp(self):
        Container.clear()

    def test1(self):
        config = {
            'PathVars': {
                'foo': 'yep [bar]',
                'bar': 'result2',
                'nest1': 'asdf [foo]',
            }
        }
        Container.bind('Config').toSingle(Config, [config])

        Container.bind('VarManager').toSingle(VarManager)

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

        assertThat(pathMgr.expand('[nest1]') == 'asdf yep result2')

        print('Done')

if __name__ == '__main__':
    unittest.main()

