
import os
import configparser

import xml.etree.ElementTree as ET

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject, InjectOptional
import mtm.ioc.Assertions as Assertions

class ConfigXml:
    ''' Build config info  (eg. path info, etc.) '''

    def __init__(self, configPaths = None):

        if not configPaths:
            configPaths = []

        if len(configPaths) == 0:
            print('Warning: No config paths provided')

        self.secondaryPath = None
        self.mainPath = None

        if len(configPaths) > 0:
            self.mainPath = configPaths[0]

        if len(configPaths) > 1:
            self.secondaryPath = configPaths[1]

        self.root = None
        self.rootSecondary = None

        if self.mainPath:
            assert os.path.isfile(self.mainPath), 'Could not find config file at path "{0}"'.format(self.mainPath)
            self.root = ET.parse(self.mainPath).getroot()

        if self.secondaryPath and os.path.isfile(self.secondaryPath):
            self.rootSecondary = ET.parse(self.secondaryPath).getroot()

    def addList(self, sectionName, optionName, values):
        matches = self._getElements('./{0}/{1}'.format(sectionName, optionName))

        assert len(matches) == 1

        match = matches[0]

        for value in values:
            elem = ET.Element('Item')
            elem.text = value
            match.append(elem)

    def _getSingleElement(self, pattern):
        elems = self._getElements(pattern)
        assert len(elems) == 1, "Could not find unique element with pattern '{0}'".format(pattern)
        return elems[0]

    def _getElements(self, pattern):
        result = []

        if self.rootSecondary:
            result += self.rootSecondary.findall(pattern)

        if self.root:
            result += self.root.findall(pattern)

        return result

    def getList(self, sectionName, optionName):
        result = self.tryGetList(sectionName, optionName)
        assert result != None, "Could not find list with name '{0}.{1}'".format(sectionName, optionName)
        return result

    def tryGetList(self, sectionName, optionName, fallback = None):

        pattern = './{0}/{1}'.format(sectionName, optionName)
        rootMatches = self._getElements(pattern)

        if len(rootMatches) == 0:
            return fallback

        result = []

        for rootMatch in rootMatches:
            matches = rootMatch.findall('./Item')

            for match in matches:
                result.append(match.text)

        return result

    def getDictionary(self, sectionName):
        result = self.tryGetDictionary(sectionName)
        assert result != None, "Could not find dictionary with name '{0}'".format(sectionName)
        return result

    def getKeyValueDictionary(self, sectionName, optionName, fallback = None):
        result = self.tryGetKeyValueDictionary(sectionName, optionName)
        assert result != None, "Could not find key value dictionary with name '{0}'".format(sectionName)
        return result

    def tryGetKeyValueDictionary(self, sectionName, optionName, fallback = None):
        pattern = './{0}/{1}'.format(sectionName, optionName)
        rootMatches = self._getElements(pattern)

        if len(rootMatches) == 0:
            return fallback

        result = {}

        for rootMatch in reversed(rootMatches):
            matches = rootMatch.findall('./Item')

            for match in matches:
                key = match.find('./Key').text
                value = match.find('./Value').text
                result[key] = value

        return result

    def tryGetDictionary(self, sectionName, fallback = None):
        pattern = './{0}'.format(sectionName)
        rootMatches = self._getElements(pattern)

        if len(rootMatches) == 0:
            return fallback

        result = {}

        for rootMatch in reversed(rootMatches):
            matches = rootMatch.findall('./*')

            for match in matches:
                key = match.tag
                value = match.text
                result[key] = value

        return result

    def getString(self, sectionName, optionName, *args):
        assert len(args) <= 1

        val = self._getSingleOrNone(sectionName, optionName)

        if val == None:
            assert len(args) > 0, "Could not find option '{0}.{1}'".format(sectionName, optionName)
            assert not args[0] or type(args[0]) is str
            return args[0]

        return val

    def getInt(self, sectionName, optionName, *args):
        val = self._getSingleOrNone(sectionName, optionName)

        if val == None:
            assert len(args) > 0, "Could not find option '{0}.{1}'".format(sectionName, optionName)
            assert not args[0] or type(args[0]) is int
            return args[0]

        return int(val)

    def getBool(self, sectionName, optionName, *args):
        val = self._getSingleOrNone(sectionName, optionName)

        if val == None:
            assert len(args) > 0, "Could not find option '{0}.{1}'".format(sectionName, optionName)
            assert not args[0] or type(args[0]) is bool
            return args[0]

        return val == 'True'

    def _getSingleOrNone(self, sectionName, optionName):

        elems = []

        pattern = './{0}/{1}'.format(sectionName, optionName)

        if self.rootSecondary:
            elems += self.rootSecondary.findall(pattern)

        # Treat secondary file as an override
        if self.root and len(elems) == 0:
            elems += self.root.findall(pattern)

        if len(elems) > 0:
            assert len(elems) == 1, "Unexpected number of elements for tag '{0}/{1}'".format(sectionName, optionName)
            return elems[0].text

        return None

#if __name__ == '__main__':
    #config = ConfigXml('C:/M3d/1/Src/python/mtm/build/Build.xml')
    #result = config._getElements('./{0}/{1}'.format('LogStreamConsole', 'OutputToFile'))
    #result = config._getSingleOrNone('LogStreamConsole', 'OutputToFile')
    #result = config.getBool('LogStreamConsole', 'OutputToFile', False)
    #result = config._tryGetFirstElement('./{0}/{1}'.format('LogStreamConsole', 'OutputToFile'))

    #config.getDictionary('Paths')

    #print(result)
    #if result:
        #print("yep")
    #else:
        #print("no")

    #print(result)


