@echo off
REM Make sure to use 32 bit python for this so it runs on all machines
%PYTHONHOME%\python ./BuildPrjExeSetup.py py2exe
%PYTHONHOME%\python ./BuildEditorApiExeSetup.py py2exe
%PYTHONHOME%\python ./BuildReleaseManifesterUpdaterExeSetup.py py2exe
%PYTHONHOME%\python ./BuildOpenInVisualStudio.py py2exe



