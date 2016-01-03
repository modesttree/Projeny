
import os
import sys
import unittest
import yaml
from prj.util.Assert import *

import prj.ioc.Container as Container
from prj.ioc.Inject import Inject
import prj.ioc.IocAssertions as Assertions

from prj.util.VarManager import VarManager
from prj.config.Config import Config
from prj.config.YamlConfigLoader import loadYamlFilesThatExist

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

