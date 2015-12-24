
import os
import sys
import unittest

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.Assertions as Assertions

from upm.util.VarManager import VarManager
from upm.config.ConfigXml import ConfigXml

ScriptDir = os.path.dirname(os.path.realpath(__file__))

class TestConfigXml(unittest.TestCase):
    def setUp(self):
        Container.clear()

    def test1(self):
        Container.bind('ConfigXml').toSingle(ConfigXml, ScriptDir + '/TestConfig.xml')

        self._testConfig(Container.resolve('ConfigXml'))

    def _testConfig(self, config):
        '''
        @type config: ConfigXml
        '''
        listFoo = config.getList('Heading', 'listFoo')

        self.assertEqual(listFoo[0], 'var1')
        self.assertEqual(listFoo[1], 'var2')

        intFoo = config.getInt('Heading', 'intFoo')

        self.assertIs(type(intFoo), int)
        self.assertEqual(intFoo, 5)

        stringFoo = config.getString('Heading', 'stringFoo')
        self.assertEqual(stringFoo, 'bar')

        fooDict = config.getDictionary('Paths')
        assert 'SourceDir' in fooDict.keys()

        self.assertRaises(Exception, lambda: config.getString('Heading', 'nonexistentitem'))
        self.assertEqual(config.getString('Heading', 'nonexistentitem', 'default1'), 'default1')

        self.assertRaises(Exception, lambda: config.getList('Heading', 'nonexistentitem'))
        self.assertEqual(config.getList('Heading', 'nonexistentitem', [5]), [5])

        self.assertRaises(Exception, lambda: config.getDictionary('nonexistentitem'))
        self.assertEqual(config.getDictionary('nonexistentitem', {5:1}), {5:1})

        self.assertRaises(Exception, lambda: config.getInt('Heading', 'nonexistentitem'))
        self.assertEqual(config.getInt('Heading', 'nonexistentitem', 2), 2)

        self.assertRaises(Exception, lambda: config.getBool('Heading', 'nonexistentitem'))
        self.assertEqual(config.getBool('Heading', 'nonexistentitem', True), True)

        tuples = config.getTuples('Heading')
        self.assertEqual(tuples[0][0], 'listFoo')

if __name__ == '__main__':
    unittest.main()
