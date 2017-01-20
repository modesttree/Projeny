
import yaml

import mtm.util.Util as Util

from mtm.util.Assert import *
import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
from mtm.ioc.Inject import InjectOptional
import mtm.ioc.IocAssertions as Assertions

from collections import OrderedDict

class Config:
    ''' Build config info  (eg. path info, etc.) '''

    def __init__(self, configs):
        assertThat(all(x != None for x in configs))
        # Reverse so that later config settings act as overrides
        self.configs = list(reversed(configs))

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
        matches = self.getAll(*args)

        if len(matches) == 0:
            return None

        # First one always overrides the other ones
        return matches[0]

    def getAll(self, *args):
        assertThat(len(args) > 0)

        # When ! is appended to the end of the key, this is treated as an override
        newArgs = list(args)
        newArgs[len(newArgs)-1] += '!'
        result = self._getAllInternal(*newArgs)

        if len(result) > 0:
            return result

        result = self._getAllInternal(*args)

        if len(result) == 0:
            # When ? is appended to the end of the key, this is treated as a fallback
            newArgs = list(args)
            newArgs[len(newArgs)-1] += '?'
            result = self._getAllInternal(*newArgs)

        return result

    def _getAllInternal(self, *args):
        result = []
        for config in self.configs:
            currentDict = config
            for i in range(len(args)):
                name = args[i]

                if name not in currentDict:
                    break

                if i == len(args) - 1:
                    result.append(currentDict[name])
                    break

                currentDict = currentDict[name]
                assertThat(type(currentDict) is dict, "Unexpected type '{0}' found for '{1}': {2}", type(currentDict), '.'.join(args[0:i+1]), currentDict)

        return result

    def getList(self, *args):
        result = self.tryGetList(None, *args)
        assertThat(result is not None, "Could not find match for YAML element {0}", self._propNameToString(args))
        return result

    def tryGetList(self, fallback, *args):
        matches = self.getAll(*args)

        if len(matches) == 0:
            return fallback

        result = []

        for match in reversed(matches):
            assertIsType(match, list, "Unexpected type for yaml property '{0}'", self._propNameToString(args))
            result += match

        return result

    def getOrderedDictionary(self, *args):
        result = self.tryGetOrderedDictionary(None, *args)
        assertThat(result is not None, "Could not find match for YAML element {0}", self._propNameToString(args))
        return result

    def tryGetOrderedDictionary(self, fallback, *args):
        dictionaries = self.tryGetList(None, *args)

        if dictionaries == None:
            return fallback

        result = OrderedDict()
        for dictionary in dictionaries:
            assertThat(type(dictionary) is dict)
            assertThat(len(dictionary) == 1)
            key, value = next(iter(dictionary.items()))
            result[key] = value
        return result

    def getDictionary(self, *args):
        result = self.tryGetDictionary(None, *args)
        assertThat(result is not None, "Could not find match for YAML element {0}", self._propNameToString(args))
        return result

    def tryGetDictionary(self, fallback, *args):
        matches = self.getAll(*args)

        if len(matches) == 0:
            return fallback

        result = {}

        for match in matches:
            assertIsType(match, dict, "Unexpected type for yaml property '{0}'", self._propNameToString(args))
            result = Util.mergeDictionaries(result, match)

        return result

#if __name__ == '__main__':

    #from mtm.config.YamlConfigLoader import loadYamlFile

    #config = Config([loadYamlFile('C:/M3d/1/Src/UnityProjects/ProjenyProject.yaml')])

    #result = config.getOrderedDictionary('SolutionFolders')

    #for k, v in result.items():
        #print("-----")
        #print("{0} = {1}".format(k, v))
