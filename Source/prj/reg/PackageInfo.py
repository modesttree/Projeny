
from mtm.util.Assert import *

class PackageFolderInfo:
    def __init__(self):
        self.path = None
        self.packages = []

class PackageInfo:
    def __init__(self):
        self.name = None
        # Might be null
        self.installInfo = None

class PackageInstallInfo:
    def __init__(self):
        self.installDate = None
        self.releaseInfo = None
