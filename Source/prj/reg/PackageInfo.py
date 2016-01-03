
import os
from prj.util.Assert import *

class PackageInfo:
    def __init__(self):
        self.name = None
        self.path = None
        # Might be null
        self.installInfo = None

class PackageInstallInfo:
    def __init__(self):
        self.installDate = None
        self.releaseInfo = None
