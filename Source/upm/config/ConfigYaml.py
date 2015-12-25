
import sys
import os
import configparser
import yaml

import upm.util.Util as Util

import datetime
from upm.util.Assert import *
import upm.ioc.Container as Container
from upm.ioc.Inject import Inject, InjectOptional
import upm.ioc.IocAssertions as Assertions

class ConfigYaml:
    ''' Build config info  (eg. path info, etc.) '''

    def __init__(self, configPaths = None):

        if not configPaths:
            configPaths = []

        self.configs = []
        self.configPaths = reversed(configPaths)

        for configPath in self.configPaths:
            if os.path.isfile(configPath):
                config = yaml.load(self.readAllTextFromFile(configPath))
                self.configs.append(config)

    def readAllTextFromFile(self, filePath):
        with open(filePath, 'r', encoding='utf-8') as f:
            return f.read()

    def getBool(self, *args):
        return self._getPrimitive(bool, *args)

    def tryGetBool(self, fallback, *args):
        return self._tryGetPrimitive(fallback, bool, *args)

    def getString(self, *args):
        return self._getPrimitive(str, *args)

    def tryGetString(self, fallback, *args):
        return self._tryGetPrimitive(fallback, str, *args)

    def getInt(self, *args):
        return self._getPrimitive(int, *args)

    def tryGetInt(self, fallback, *args):
        return self._tryGetPrimitive(fallback, int, *args)

    def _getPrimitive(self, propType, *args):
        result = self._tryGetPrimitive(None, propType, *args)
        assertThat(result is not None, "Could not find match for YAML element {0}", self._propNameToString(args))
        return result

    def _tryGetPrimitive(self, fallback, propType, *args):
        result = self.tryGet(*args)
        if result == None:
            return fallback
        assertIsType(result, propType, "Unexpected type for yaml property '{0}'", self._propNameToString(args))
        return result

    def _propNameToString(self, args):
        return '.'.join(args)

    def get(self, *args):
        match = self.tryGet(*args)
        assertThat(match is not None, "Could not find match for YAML element {0}", self._propNameToString(args))
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
        result = self.tryGetList(None, *args)
        assertThat(result is not None, "Could not find match for YAML element {0}", self._propNameToString(args))
        return result

    def tryGetList(self, fallback, *args):
        result = self.tryGet(*args)

        if result == None:
            return fallback

        assertIsType(result, list, "Unexpected type for yaml property '{0}'", self._propNameToString(args))

        # When a + is added at the end we interpret this to mean concatenate to the existing list
        extraArgs = list(args)
        extraArgs[len(extraArgs)-1] += '+'
        for extra in self.getAll(*extraArgs):
            result += extra

        return result

    def getDictionary(self, *args):
        result = self.tryGetDictionary(None, *args)
        assertThat(result is not None, "Could not find match for YAML element {0}", self._propNameToString(args))
        return result

    def tryGetDictionary(self, fallback, *args):
        result = self.tryGet(*args)

        if result == None:
            return fallback

        assertIsType(result, dict, "Unexpected type for yaml property '{0}'", self._propNameToString(args))

        # When a + is added at the end we interpret this to mean concatenate to the existing list
        extraArgs = list(args)
        extraArgs[len(extraArgs)-1] += '+'
        for extra in self.getAll(*extraArgs):
            result = Util.mergeDictionaries(result, extra)

        return result

