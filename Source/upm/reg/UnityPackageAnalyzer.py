
import os
from upm.util.Assert import *
from upm.reg.ReleaseInfo import ReleaseInfo, AssetStoreInfo
from pprint import pprint
import binascii

from datetime import datetime
import json

import time
import calendar

from upm.ioc.Inject import Inject
from upm.ioc.Inject import InjectMany

class UnityPackageAnalyzer:
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')

    def getReleaseInfoFromUnityPackage(self, unityPackagePath):
        fileName = os.path.basename(unityPackagePath)

        self._log.heading("Analyzing unity package '{0}'", fileName)

        assertThat(self._sys.fileExists(unityPackagePath))

        info = ReleaseInfo()
        info.localPath = unityPackagePath

        headerInfo = self._tryGetAssetStoreInfoFromHeader(unityPackagePath)

        if headerInfo:
            info.name = headerInfo['title']
            info.versionCode = headerInfo['version_id']
            info.version = headerInfo['version']
            info.assetStoreInfo = self._getAssetStoreInfo(headerInfo)
        else:
            info.name = os.path.splitext(fileName)[0]

        return info

    def _getAssetStoreInfo(self, allInfo):
        info = AssetStoreInfo()
        info.publisherId = allInfo['publisher']['id']
        info.publisherLabel = allInfo['publisher']['label']
        info.packageId = allInfo['id']
        info.publishNotes = allInfo.get('publishnotes', '')
        info.categoryId = allInfo['category']['id']
        info.categoryLabel = allInfo['category']['label']
        info.uploadId = allInfo.get('upload_id', None)
        info.description = allInfo.get('description', '')

        pubDate = allInfo['pubdate']
        info.publishDate = datetime.strptime(pubDate, "%d %b %Y")

        info.unityVersion = allInfo.get('unity_version', None)
        info.linkId = allInfo['link']['id']
        info.linkType = allInfo['link']['type']
        return info

    def _tryGetAssetStoreInfoFromHeader(self, unityPackagePath):

        with open(unityPackagePath, 'rb') as f:
            headerBytes = f.read(16)
            headerHexValues = binascii.hexlify(headerBytes)

            headerString = headerHexValues.decode('utf8')

            packageId = headerString[0:4]

            unixTimeStamp = (headerBytes[7] << 24) + (headerBytes[6] << 16) + (headerBytes[5] << 8) + headerBytes[4]
            timeStamp = datetime.utcfromtimestamp(unixTimeStamp)

            flag1 = headerString[6:8]
            flag2 = headerString[24:28]

            numBytes = int(headerString[22:24] + headerString[20:22], 16)
            numJsonBytes = int(headerString[30:32] + headerString[28:30], 16)

            assertThat(packageId == "1f8b", "Invalid .unitypackage file")

            # These flags indicate that it is an asset store package
            if flag1 == "04" and flag2 == "4124":
                assertThat(numBytes == numJsonBytes + 4)

                infoBytes = f.read(numJsonBytes)
                infoStr = infoBytes.decode('utf8')

                return json.loads(infoStr)

        return None

if __name__ == '__main__':

    import upm.ioc.Container as Container
    from upm.config.Config import Config
    from upm.util.ScriptRunner import ScriptRunner
    from upm.util.VarManager import VarManager
    from upm.log.Logger import Logger
    from upm.util.SystemHelper import SystemHelper
    from upm.util.ProcessRunner import ProcessRunner
    from upm.log.LogStreamConsole import LogStreamConsole

    Container.bind('Config').toSingle(Config, [])
    Container.bind('Logger').toSingle(Logger)
    Container.bind('VarManager').toSingle(VarManager, { 'UnityExePath': "C:/Program Files/Unity/Editor/Unity.exe" })
    #Container.bind('LogStream').toSingle(LogStreamConsole, True, True)
    Container.bind('SystemHelper').toSingle(SystemHelper)
    Container.bind('ProcessRunner').toSingle(ProcessRunner)

    path = "C:/Users/Steve/AppData/Roaming/Unity/Asset Store/TripleBrick/3D ModelsEnvironmentsLandscapes/Free Rocks.unitypackage"

    info = UnityPackageAnalyzer().getReleaseInfoFromUnityPackage(path)

    print('Result: ' + str(info.__dict__))

