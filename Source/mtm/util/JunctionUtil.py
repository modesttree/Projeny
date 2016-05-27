import os


def islink(path):
    try:
        os.readlink(path)
        return True
    except OSError:
        return False


def readlink(path):
    try:
        link = os.readlink(path)
        return link
    except OSError:
        raise ValueError("not a link")

if __name__ == '__main__':
    path = "C:/Temp/JunctionTest"
    if islink(path):
        print("yep")
    else:
        print("nope")

