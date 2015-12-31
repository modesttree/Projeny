
import os
from upm.util.Assert import *

class PackageInfo:
    def __init__(self):
        self.name = None
        self.path = None
        # Might be null
        self.releaseInfo = None

