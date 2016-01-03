
import os
from upm.util.Assert import *

class ReleaseInfo:
    def __init__(self):
        # Should always be valid
        self.name = None
        self.id = None
        # This is an int and should always increase with subsequent versions
        # Might be null if package is not versioned
        self.versionCode = None
        # User-readable version
        self.version = None
        # This will be null if the package was not pulled from a unity package on the local machine
        self.localPath = None
        # This will be null if the package was not pulled from the asset store cache
        self.assetStoreInfo = None
        self.fileModificationDate = None

        # This is null if not known
        self.compressedSize = None

class AssetStoreInfo:
    def __init__(self):
        self.publisherId = None
        self.publisherLabel = None
        self.publishNotes = None
        self.categoryId = None
        self.categoryLabel = None
        self.uploadId = None
        self.description = None
        self.publishDate = None
        self.unityVersion = None
        self.linkId = None
        self.linkType = None

