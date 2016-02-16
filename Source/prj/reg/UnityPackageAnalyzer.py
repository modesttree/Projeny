
import os
from mtm.util.Assert import *
from prj.reg.ReleaseInfo import ReleaseInfo, AssetStoreInfo
import binascii
import re

from datetime import datetime
import json


from mtm.ioc.Inject import Inject
from mtm.ioc.Inject import InjectMany

class UnityPackageAnalyzer:
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')

    def getReleaseInfoFromUnityPackage(self, unityPackagePath):
        fileName = os.path.basename(unityPackagePath)

        #self._log.debug("Analyzing unity package '{0}'", fileName)

        assertThat(self._sys.fileExists(unityPackagePath))

        info = ReleaseInfo()
        info.localPath = unityPackagePath

        headerInfo = self._tryGetAssetStoreInfoFromHeader(unityPackagePath)

        info.compressedSize = os.path.getsize(unityPackagePath)
        info.fileModificationDate = datetime.utcfromtimestamp(os.path.getmtime(unityPackagePath))

        if headerInfo:
            info.name = headerInfo['title']
            info.versionCode = int(headerInfo['version_id'])
            info.version = headerInfo['version']
            info.id = headerInfo['id']
            info.assetStoreInfo = self._getAssetStoreInfo(headerInfo)
        else:
            # If there are no headers, then we have to derive the info from the file name
            info.id, info.name, info.versionCode, info.version = self._getInfoFromFileName(fileName)

        return info

    def _getInfoFromFileName(self, fileName):
        parts = os.path.splitext(fileName)
        assertThat(parts[1].lower() == '.unitypackage')
        baseName = parts[0].strip()

        match = re.match('^(.*)@\s*(\d+\.?\d*)\s*$', baseName)

        if match:
            groups = match.groups()

            name = groups[0]
            versionStr = groups[1]

            numDecimals = len(re.match('^\d+\.(\d*)$', versionStr).groups()[0])

            assertThat(numDecimals <= 7, 'Projeny only supports up to 7 decimal points in the version number!')

            # Need a flat int so we can do greater than/less than comparisons
            versionCode = int(10000000 * float(groups[1]))

            return (name, name, versionCode, versionStr)

        return (baseName, baseName, 0, '')

    def _getAssetStoreInfo(self, allInfo):
        info = AssetStoreInfo()
        info.publisherId = allInfo['publisher']['id']
        info.publisherLabel = allInfo['publisher']['label']
        info.publishNotes = allInfo.get('publishnotes', '')
        info.categoryId = allInfo['category']['id']
        info.categoryLabel = allInfo['category']['label']
        info.uploadId = allInfo.get('upload_id', None)

        # This often causes issues when deserialized with YamlDotNet for reasons unknown so just leave it blank for now
        #info.description = allInfo.get('description', '')
        info.description = ''

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

            flag1 = headerString[0:4]

            unixTimeStamp = (headerBytes[7] << 24) + (headerBytes[6] << 16) + (headerBytes[5] << 8) + headerBytes[4]
            datetime.utcfromtimestamp(unixTimeStamp)

            flag2 = headerString[6:8]
            flag3 = headerString[24:28]

            numBytes = int(headerString[22:24] + headerString[20:22], 16)
            numJsonBytes = int(headerString[30:32] + headerString[28:30], 16)

            assertThat(flag1 == "1f8b", "Invalid .unitypackage file")

            # These flags indicate that it is an asset store package
            if flag2 == "04" and flag3 == "4124":
                assertThat(numBytes == numJsonBytes + 4)

                infoBytes = f.read(numJsonBytes)
                infoStr = infoBytes.decode('utf8')

                return json.loads(infoStr)

        return None

if __name__ == '__main__':

    import mtm.ioc.Container as Container
    from mtm.config.Config import Config
    from mtm.util.ScriptRunner import ScriptRunner
    from mtm.util.VarManager import VarManager
    from mtm.log.Logger import Logger
    from mtm.util.SystemHelper import SystemHelper
    from mtm.util.ProcessRunner import ProcessRunner
    from mtm.log.LogStreamConsole import LogStreamConsole

    Container.bind('Config').toSingle(Config, [])
    Container.bind('Logger').toSingle(Logger)
    Container.bind('VarManager').toSingle(VarManager, { 'UnityExePath': "C:/Program Files/Unity/Editor/Unity.exe" })
    #Container.bind('LogStream').toSingle(LogStreamConsole, True, True)
    Container.bind('SystemHelper').toSingle(SystemHelper)
    Container.bind('ProcessRunner').toSingle(ProcessRunner)

    path = "C:/Users/Steve/AppData/Roaming/Unity/Asset Store/TripleBrick/3D ModelsEnvironmentsLandscapes/Free Rocks.unitypackage"

    info = UnityPackageAnalyzer().getReleaseInfoFromUnityPackage(path)

    print('Result: ' + str(info.__dict__))

