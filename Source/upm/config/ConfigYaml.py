
import os
import configparser
import yaml

import upm.util.Util as Util

import datetime
from upm.util.Assert import *
import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject, InjectOptional
import mtm.ioc.Assertions as Assertions

class ConfigYaml:
    ''' Build config info  (eg. path info, etc.) '''

    def __init__(self, configPaths = None):

        if not configPaths:
            configPaths = []

        if len(configPaths) == 0:
            print('Warning: No config paths provided')

        self.configs = []

        for configPath in reversed(configPaths):
            self.configs.append(yaml.load(self.readAllTextFromFile(configPath)))

    def readAllTextFromFile(self, filePath):
        assertThat(os.path.isfile(filePath), "Could not find YAML config file at path '{0}'", filePath)
        with open(filePath, 'r', encoding='utf-8') as f:
            return f.read()

    def getString(self, *args):
        result = self.get(*args)
        assertThat(isinstance(result, str) or isinstance(result, datetime.date), "Unexpected type '{0}' for property '{1}'", result.__class__, self._propNameToString(args))
        return str(result)

    def _propNameToString(self, args):
        return '.'.join(args)

    def get(self, *args):
        match = self.tryGet(*args)
        assertThat(match, "Could not find match for YAML element {0}", self._propNameToString(args))
        return match

    def tryGet(self, *args):
        matches = list(self.getAll(*args))

        if len(matches) == 0:
            return None

        # First one always overrides the other ones
        return matches[0]

    def getAll(self, *args):
        for config in self.configs:
            currentDict = config
            for i in range(len(args)):
                name = args[i]

                if name not in currentDict:
                    break

                if i == len(args) - 1:
                    yield currentDict[name]
                    break

                currentDict = currentDict[name]

    def getList(self, *args):
        result = self.get(*args)

        assertIsType(result, list, "Unexpected type for yaml property '{0}'", self._propNameToString(args))

        # When a + is added at the end we interpret this to mean concatenate to the existing list
        extraArgs = list(args)
        extraArgs[len(extraArgs)-1] += '+'
        for extra in self.getAll(*extraArgs):
            result += extra

        return result

    def getDictionary(self, *args):
        result = self.get(*args)

        assertIsType(result, dict, "Unexpected type for yaml property '{0}'", self._propNameToString(args))

        # When a + is added at the end we interpret this to mean concatenate to the existing list
        extraArgs = list(args)
        extraArgs[len(extraArgs)-1] += '+'
        for extra in self.getAll(*extraArgs):
            result = Util.mergeDictionaries(result, extra)

        return result

