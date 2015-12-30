
import os
import sys
import unittest
import yaml
from upm.util.Assert import *

import upm.ioc.Container as Container
from upm.ioc.Inject import Inject
import upm.ioc.IocAssertions as Assertions

from upm.util.VarManager import VarManager
from upm.config.Config import Config
from upm.config.YamlConfigLoader import loadYamlFilesThatExist

ScriptDir = os.path.dirname(os.path.realpath(__file__))

class TestConfigYaml(unittest.TestCase):
    def setUp(self):
        Container.clear()

    def testSimple(self):
        yamlPath = ScriptDir + '/ExampleConfig.yaml'

        Container.bind('Config').toSingle(Config, loadYamlFilesThatExist(yamlPath))

        config = Container.resolve('Config')

        assertIsEqual(config.getString('date'), '2012-08-06')
        assertIsEqual(config.getString('receipt'), 'Oz-Ware Purchase Invoice')

        assertIsEqual(config.getList('places'), ['New Jersey', 'New York'])
        assertRaisesAny(lambda: config.getString('places'))
        assertRaisesAny(lambda: config.getDictionary('places'))

        assertIsEqual(config.getDictionary('customer'),
          {'first_name': 'Dorothy', 'family_name': 'Gale'})

        # Tests YAML references
        assertIsEqual(config.getString('foo1'), config.getString('receipt'))

    def testMultiple(self):

        Container.bind('Config').toSingle(Config, loadYamlFilesThatExist(ScriptDir + '/ExampleConfig.yaml', ScriptDir + '/ExampleConfig2.yaml'))

        config = Container.resolve('Config')

        # From 1
        assertIsEqual(config.getString('receipt'), 'Oz-Ware Purchase Invoice')

        # From 2
        assertIsEqual(config.getString('thing1'), 'Foo')

        # Second one should override
        assertIsEqual(config.getString('foo2'), 'ipsum')

        assertIsEqual(config.getString('nest1', 'firstName'), 'Dorothy')

        # Test concatenating lists together
        assertIsEqual(config.getList('list1'), ['lorem', 'ipsum', 'asdf', 'joe', 'frank'])

        # Test concatenating dictionaries together
        assertIsEqual(config.getDictionary('dict1'), {'joe': 5, 'mary': 15, 'kate': 5, 'jim': 10})

        assertIsEqual(config.tryGetDictionary(None, 'asdfasdfasdf'), None)
        assertIsEqual(config.tryGetDictionary({ 5: 1 }, 'asdfasdfasdf'), { 5: 1 })

        assertIsEqual(config.tryGetList(None, 'asdfasdfasdf'), None)
        assertIsEqual(config.tryGetList([1, 2], 'asdfasdfasdf'), [1, 2])

        assertIsEqual(config.tryGetBool(False, 'zxvzasdfasdfasdf'), False)
        assertIsEqual(config.tryGetBool(True, 'zxvzasdfasdfasdf'), True)

        assertIsEqual(config.tryGetString('asdf', 'zxvzasdfasdfasdf'), 'asdf')

        assertIsEqual(config.tryGetInt(5, 'zxvzasdfasdfasdf'), 5)

    def testSpecialChars(self):
        Container.bind('Config').toSingle(Config, loadYamlFilesThatExist(ScriptDir + '/ExampleConfig.yaml', ScriptDir + '/ExampleConfig2.yaml'))

        config = Container.resolve('Config')

        assertIsEqual(config.tryGetString(None, 'foo4'), 'asdf')

        assertIsEqual(config.tryGetString(None, 'foo5'), 'zxcv')

        assertIsEqual(config.tryGetString(None, 'foo6'), 'asdf')
        assertIsEqual(config.tryGetString(None, 'foo7'), 'zxcv')

if __name__ == '__main__':
    unittest.main()
