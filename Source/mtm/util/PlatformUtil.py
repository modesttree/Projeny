from mtm.util.Assert import *

from mtm.util.Platforms import Platforms

def toPlatformFolderName(platform):
    # We can just directly use the full platform name for folder names
    return platform

def fromPlatformFolderName(platformDirName):
    platformDirName = platformDirName.lower()

    for curPlatform in Platforms.All:
        if curPlatform.lower() == platformDirName:
            return curPlatform

    assertThat(False)

def fromPlatformArgName(platformArgStr):

    if platformArgStr == 'win' or platformArgStr == 'w':
        return Platforms.Windows

    if platformArgStr == 'webgl' or platformArgStr == 'g':
        return Platforms.WebGl

    if platformArgStr == 'and' or platformArgStr == 'a':
        return Platforms.Android

    if platformArgStr == 'osx' or platformArgStr == 'o':
        return Platforms.OsX

    if platformArgStr == 'ios' or platformArgStr == 'i':
        return Platforms.Ios

    if platformArgStr == 'lin' or platformArgStr == 'l':
        return Platforms.Linux

    if platformArgStr == 'uwp':
        return Platforms.UWP

    if platformArgStr == 'lumin':
        return Platforms.Lumin

    assertThat(False)
    return ''
