from upm.util.Assert import *

class Platforms:
    Windows = 'Windows'
    WebPlayer = 'WebPlayer'
    Android = 'Android'
    WebGl = 'WebGL'
    OsX = 'OSX'
    Linux = 'Linux'
    Ios = 'iOS'
    All = [Windows, WebPlayer, Android, WebGl, OsX, Linux, Ios]

def toPlatformFolderName(platform):
    # We can just directly use the full platform name for folder names
    return platform

def fromPlatformFolderName(platform):
    # We can just directly use the full platform name for folder names
    return platform

def fromPlatformArgName(platformArgStr):

    if platformArgStr == 'win':
        return Platforms.Windows

    if platformArgStr == 'webp':
        return Platforms.WebPlayer

    if platformArgStr == 'webgl':
        return Platforms.WebGl

    if platformArgStr == 'and':
        return Platforms.Android

    if platformArgStr == 'osx':
        return Platforms.OsX

    if platformArgStr == 'ios':
        return Platforms.Ios

    if platformArgStr == 'lin':
        return Platforms.Linux

    assertThat(False)
    return ''
