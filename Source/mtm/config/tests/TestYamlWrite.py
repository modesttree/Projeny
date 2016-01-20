
import os
import unittest
import yaml
from mtm.util.Assert import *

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.IocAssertions as Assertions

from mtm.util.VarManager import VarManager
from mtm.config.Config import Config
from mtm.config.YamlConfigLoader import loadYamlFilesThatExist

ScriptDir = os.path.dirname(os.path.realpath(__file__))

class TestConfigYaml(unittest.TestCase):
    def test1(self):
        data = {
            'bob': 'jim',
            'joe': 'adsf'
        }
        result = yaml.dump(data, default_flow_style=False)
        print(result)

if __name__ == '__main__':
    unittest.main()

