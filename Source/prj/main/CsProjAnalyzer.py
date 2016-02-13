import xml.etree.ElementTree as ET
import os
from mtm.util.Assert import *

CsProjXmlNs = 'http://schemas.microsoft.com/developer/msbuild/2003'
NsPrefix = '{' + CsProjXmlNs + '}'

class CsProjAnalyzer:
    def __init__(self, path):
        assertThat(os.path.isfile(path), "Expected to find file at '{0}'", path)

        self._path = path
        self._root = ET.parse(path).getroot()

    @property
    def root(self):
        return self._root

    def getAssemblyName(self):
        return self._root.findall('./{0}PropertyGroup/{0}AssemblyName'.format(NsPrefix))[0].text

    def getProjectReferences(self):
        result = []
        for projRef in self._root.findall('./{0}ItemGroup/{0}ProjectReference/{0}Name'.format(NsPrefix)):
            result.append(projRef.text)
        return result

