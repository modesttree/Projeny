import subprocess
import csv
import re
import os
import sys
import imp

# Taken from here
# http://www.py2exe.org/index.cgi/HowToDetermineIfRunningFromExe
def isRunningAsExe():
   return (hasattr(sys, "frozen") or # new py2exe
       hasattr(sys, "importers") # old py2exe
       or imp.is_frozen("__main__")) # tools/freeze

def getExecDirectory():
    return os.path.dirname(sys.argv[0])

def confirmChoice(msg):
    valid = {"yes": True, "y": True, "ye": True, "no": False, "n": False}

    print('/n'+msg, end="")

    choice = input().lower()

    while True:
        if choice in valid:
            return valid[choice]
        else:
            print('Invalid selection "%s".' % choice)

def tryKillAdbExe(sysManager):
    try:
        sysManager.executeAndWait('taskkill /f /IM adb.exe')
    except:
        pass

def doesProcessExist(pattern):
    p_tasklist = subprocess.Popen('tasklist.exe /fo csv', stdout=subprocess.PIPE, universal_newlines=True)

    for p in csv.DictReader(p_tasklist.stdout):
        if re.match(pattern, p['Image Name']):
            return True

    return False
