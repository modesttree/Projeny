
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
from upm.config.YamlLoader import loadYamlFilesThatExist

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

