
import os
from upm.util.Assert import *

def createReleaseInfoFromPath(path):
    info = ReleaseInfo()
    info.Title = os.path.splitext(os.path.basename(path))[0]
    info.Version = '1.1'
    return info

class ReleaseInfo:
    def __init__(self):
        self.Title = None
        self.Version = None

