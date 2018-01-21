
import os
import shutil
import sys
from cx_Freeze import setup, Executable

ScriptDir = os.path.dirname(os.path.realpath(__file__))
BuildDir = os.path.join(ScriptDir, 'build')
BuildPlatformDir = os.path.join(BuildDir, 'exe.win32-3.6')
OutDir = os.path.join(ScriptDir, '../Bin/Data')

print("Removing previous build directories...")

if os.path.exists(BuildDir):
    shutil.rmtree(BuildDir)

if os.path.exists(OutDir):
    shutil.rmtree(OutDir)

print("Building exes..")
base = None
build_exe_options = {"packages": [], "excludes": []}
executables = [
    Executable(script = "prj/main/Prj.py", base = base, targetName = 'Prj.exe'),
    Executable(script = "prj/main/EditorApi.py", targetName = "EditorApi.exe", base=base),
    Executable(script = "prj/main/OpenInVisualStudio.py", targetName = "OpenInVisualStudio.exe", base=base),
    Executable(script = "prj/main/ReleaseManifestUpdater.py", targetName = "ReleaseManifestUpdater.exe", base=base)]

setup(  name = "Projeny",
      version = "0.1",
      description = "Projeny command line exes",
      options = { "build_exe": build_exe_options },
      executables = executables)

print("Copying to bin folder...")

shutil.move(BuildPlatformDir, OutDir)

print("Build completed successfully")
