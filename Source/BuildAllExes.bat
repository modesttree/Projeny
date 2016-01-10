REM @echo off
REM Make sure to use 32 bit python for this so it runs on all machines
C:\Utils\Python\WinPython-3.4.3.7-32\python-3.4.3\python ./BuildPrjExeSetup.py py2exe
C:\Utils\Python\WinPython-3.4.3.7-32\python-3.4.3\python ./BuildEditorApiExeSetup.py py2exe
C:\Utils\Python\WinPython-3.4.3.7-32\python-3.4.3\python ./BuildReleaseManifesterUpdaterExeSetup.py py2exe
C:\Utils\Python\WinPython-3.4.3.7-32\python-3.4.3\python ./BuildOpenInVisualStudio.py py2exe
