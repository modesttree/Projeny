
import os
import stat
from mtm.util.Assert import *

def printVisualStudioFriendlyError(msg):
    # Use a visual studio friendly error so it pops up as an error
    print("error MSB3021: " + msg)

def forceDeleteFile(filePath):

    try:
        try:
            os.remove(filePath)
        except Exception as e:
            os.chmod(filePath, stat.S_IWRITE)
            os.remove(filePath)

    except Exception as e:
        return False

    return True

def ensureNoDuplicates(items, collectionName):
    seen = set()
    duplicates = set()

    for item in items:
        if item in seen:
            duplicates.add(item)

        seen.add(item)

    assertThat(len(duplicates) == 0, "Found duplicates in collection '{0}': {1}".format(collectionName, ', '.join([str(x) for x in duplicates])))

def mergeDictionaries(x, y):
    z = x.copy()
    z.update(y)
    return z

def formatTimeDelta(seconds):

    hours = seconds // 3600

    msg = ''

    if hours > 0:
        msg += str(hours) + ' hours, '

    # remaining seconds
    seconds = seconds - (hours * 3600)
    # minutes
    minutes = seconds // 60

    if minutes > 0:
        msg += str(minutes) + ' minutes, '

    # remaining seconds
    seconds = seconds - (minutes * 60)

    msg += '{:.1f}'.format(seconds) + ' seconds'

    return msg
