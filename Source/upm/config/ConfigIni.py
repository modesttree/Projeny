
import os
import configparser

import upm.util.Util as Util
import upm.ioc.Container as Container
from upm.ioc.Inject import Inject, InjectOptional
import upm.ioc.IocAssertions as Assertions
from upm.util.Assert import *

class ConfigIni:
    ''' Build config info  (eg. path info, etc.) '''

    def __init__(self, configPaths):

        self.configs = []

        if len(configPaths) > 0:
            assertThat(os.path.isfile(configPaths[0]), 'Could not find config file at path "{0}"'.format(configPaths[0]))

        for path in reversed(configPaths):
            if not os.path.isfile(path):
                continue

            config = configparser.ConfigParser()
            config.optionxform = str

            config.read(path)

            self.configs.append(config)

    def getList(self, sectionName, optionName):
        result = []
        for config in self.configs:
            result = result + self._getListInternal(config, sectionName, optionName)
        return result

    def _getListInternal(self, config, sectionName, optionName):
        if not config.has_option(sectionName, optionName):
            return []

        val = config.get(sectionName, optionName)

        return list([_f for _f in (x.strip() for x in val.splitlines()) if _f])

    def getTuples(self, sectionName, *args):
        result = []
        for config in self.configs:
            result = result + self._getTuplesInternal(config, sectionName)
        return result

    def _getTuplesInternal(self, config, sectionName):
        if not config.has_section(sectionName):
            return []

        return config.items(sectionName)

    def getString(self, sectionName, optionName, *args):
        assertThat(len(args) <= 1)

        val = self._Get(sectionName, optionName)

        if val == None:
            assertThat(len(args) > 0, "Could not find option '{0}.{1}'".format(sectionName, optionName))
            assertThat(not args[0] or type(args[0]) is str)
            return args[0]

        return val

    def getInt(self, sectionName, optionName, *args):
        val = self._Get(sectionName, optionName)

        if val == None:
            assertThat(len(args) > 0, "Could not find option '{0}.{1}'".format(sectionName, optionName))
            assertThat(not args[0] or type(args[0]) is int)
            return args[0]

        return int(val)

    def getBool(self, sectionName, optionName, *args):
        val = self._Get(sectionName, optionName)

        if val == None:
            assertThat(len(args) > 0, "Could not find option '{0}.{1}'".format(sectionName, optionName))
            assertThat(not args[0] or type(args[0]) is bool)
            return args[0]

        return val == 'True'

    def _Get(self, sectionName, optionName):

        for config in self.configs:
            if config.has_option(sectionName, optionName):
                return config.get(sectionName, optionName)

        return None

