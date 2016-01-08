
import shutil
import os
from distutils.core import setup
import py2exe

ScriptDir = os.path.dirname(os.path.realpath(__file__))
OutputDataDir = os.path.join(ScriptDir, '../Bin/Data')

outputPath = os.path.join(OutputDataDir, 'ReleaseManifestUpdater')

if os.path.exists(outputPath):
    shutil.rmtree(outputPath)

os.makedirs(outputPath)

setup(
    console=['prj/main/ReleaseManifestUpdater.py'],
    options = {
        "py2exe": {
            "dist_dir": outputPath
        }
    })

