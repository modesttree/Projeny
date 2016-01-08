@echo off
REM Make sure to use 32 bit python for this so it runs on all machines
C:\Utils\Python\Python-34-32\python ./BuildPrjExeSetup.py py2exe
C:\Utils\Python\Python-34-32\python ./BuildEditorApiExeSetup.py py2exe
C:\Utils\Python\Python-34-32\python ./BuildReleaseManifesterUpdaterExeSetup.py py2exe
C:\Utils\Python\Python-34-32\python ./BuildOpenInVisualStudio.py py2exe
