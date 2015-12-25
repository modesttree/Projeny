
import shutil
import os
from distutils.core import setup
import py2exe

ScriptDir = os.path.dirname(os.path.realpath(__file__))

outputPath = os.path.join(ScriptDir, '../Bin/Upm/Data')

if os.path.exists(outputPath):
    shutil.rmtree(outputPath)

os.makedirs(outputPath)

setup(
    console=['upm/main/Upm.py'],
    options = {
        "py2exe": {
            "dist_dir": outputPath
        }
    })

