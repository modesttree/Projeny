import subprocess
import csv
import re
import msvcrt
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
    print('\n' + msg, end="")

    while True:
        if msvcrt.kbhit():
            choice = msvcrt.getch().decode("utf-8")

            if choice == 'y':
                return True

            if choice == 'n':
                return False

            if choice == '\x03':
                return False

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
