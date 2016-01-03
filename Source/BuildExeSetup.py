
import shutil
import os
from distutils.core import setup
import py2exe

ScriptDir = os.path.dirname(os.path.realpath(__file__))

outputPath = os.path.join(ScriptDir, '../Bin/Prj/Data')

if os.path.exists(outputPath):
    shutil.rmtree(outputPath)

os.makedirs(outputPath)

setup(
    console=['prj/main/Prj.py'],
    options = {
        "py2exe": {
            "dist_dir": outputPath
        }
    })

