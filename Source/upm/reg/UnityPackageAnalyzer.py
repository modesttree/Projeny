
import os
from upm.util.Assert import *
from upm.reg.ReleaseInfo import ReleaseInfo

def calculateReleaseInfoForUnityPackage(path):
    info = ReleaseInfo()
    info.Title = os.path.splitext(os.path.basename(path))[0]
    info.LocalPath = path
    info.Version = '1.1'
    return info

if __name__ == '__main__':
    pass
