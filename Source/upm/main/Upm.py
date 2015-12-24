
import configparser
import msvcrt
import re
import time
import sys
import os
import fnmatch
import shlex
import subprocess
from datetime import datetime
import argparse
import shutil
import traceback
import webbrowser

import upm.util.MiscUtil as MiscUtil
import upm.util.PlatformUtil as PlatformUtil

from upm.config.ConfigXml import ConfigXml
from upm.util.VarManager import VarManager
from upm.util.ZipHelper import ZipHelper
from upm.log.Logger import Logger
from upm.util.SystemHelper import SystemHelper
from upm.log.LogStreamFile import LogStreamFile
from upm.log.LogStreamConsole import LogStreamConsole
from upm.util.ProcessRunner import ProcessRunner
from upm.util.JunctionHelper import JunctionHelper
from upm.main.VisualStudioSolutionGenerator import VisualStudioSolutionGenerator
from upm.main.VisualStudioHelper import VisualStudioHelper
from upm.main.ProjectSchemaLoader import ProjectSchemaLoader
from upm.util.ScriptRunner import ScriptRunner
from upm.util.CommonSettings import CommonSettings

from upm.util.PlatformUtil import Platforms
from upm.main.PackageManager import PackageManager

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.Assertions as Assertions

from upm.util.UnityHelper import UnityHelper

# We assume here that we always start in the python directory or the bin directory
PythonDir = os.getcwd()
ProjenyDir = os.path.realpath(os.path.join(PythonDir, '..'))

def addArguments(parser):
    parser.add_argument('-p', '--project', metavar='PROJECT_NAME', type=str, help="The project to apply changes to.")
    parser.add_argument('-pl', '--platform', type=str, default='win', choices=['win', 'webp', 'webgl', 'and', 'osx', 'ios', 'lin'], help='The platform to use.  If unspecified, windows is assumed.')

    parser.add_argument('-ul', '--updateLinks', action='store_true', help='Updates directory links for the given project using package manager')

    parser.add_argument('-lp', '--listProjects', action='store_true', help='Display the list of all projects that are in the UnityProjects directory')

    parser.add_argument('-uus', '--updateUnitySolution', action='store_true', help='Equivalent to executing the menu option "Assets/Sync MonoDevelop Project" in unity')
    parser.add_argument('-ucs', '--updateCustomSolution', action='store_true', help='Updates the custom solution for the given project with the files found in the Assets/ folder.  It will also take settings from the generated unity solution such as defines, and references.')

    parser.add_argument('-v', '--verbose', action='store_true', help='Output debug-level logging')
    parser.add_argument('-vv', '--veryVerbose', action='store_true', help='If set, detailed logging will be output to stdout rather than file')

    parser.add_argument('-b', '--buildCustomSolution', action='store_true', help='Build the generated custom solution for the given project')
    parser.add_argument('-d', '--openDocumentation', action='store_true', help='Opens the documentation page in a web browser')

    parser.add_argument('-clp', '--clearProjectGeneratedFiles', action='store_true', help='Remove the generated files for the given project')
    parser.add_argument('-cla', '--clearAllProjectGeneratedFiles', action='store_true', help='Remove all the generated files for all projects')
    parser.add_argument('-dal', '--deleteAllLinks', action='store_true', help='Delete all directory links for all projects')

    parser.add_argument('-ina', '--initAll', action='store_true', help='Initialize all projects for all platforms')

    parser.add_argument('-bf', '--buildFull', action='store_true', help='Perform a full build of the given project')

    parser.add_argument('-ou', '--openUnity', action='store_true', help='Open unity for the given project')
    parser.add_argument('-ocs', '--openCustomSolution', action='store_true', help='Open the solution for the given project/platform')
    parser.add_argument('-ocf', '--openCsFile', metavar='FILE_PATH', type=str, help="Open the given C# file in an active instance of visual studio")

def getConfigPaths():
    return [os.path.join(ProjenyDir, 'ProjenyConfig.xml'), os.path.join(ProjenyDir, 'ProjenyConfigCustom.xml')]

def installBindings(verbose, veryVerbose):
    Container.bind('Config').toSingle(ConfigXml, getConfigPaths())
    Container.bind('VarManager').toSingle(VarManager, PythonDir, { 'ProjenyDir': '[PythonDir]/..' })
    Container.bind('SystemHelper').toSingle(SystemHelper)
    Container.bind('Logger').toSingle(Logger)
    Container.bind('LogStream').toSingle(LogStreamFile)
    Container.bind('LogStream').toSingle(LogStreamConsole, verbose, veryVerbose)
    Container.bind('UnityHelper').toSingle(UnityHelper)
    Container.bind('ScriptRunner').toSingle(ScriptRunner)
    Container.bind('PackageManager').toSingle(PackageManager)
    Container.bind('ProcessRunner').toSingle(ProcessRunner)
    Container.bind('JunctionHelper').toSingle(JunctionHelper)
    Container.bind('VisualStudioSolutionGenerator').toSingle(VisualStudioSolutionGenerator)
    Container.bind('VisualStudioHelper').toSingle(VisualStudioHelper)
    Container.bind('ProjectSchemaLoader').toSingle(ProjectSchemaLoader)
    Container.bind('CommonSettings').toSingle(CommonSettings)
    Container.bind('ZipHelper').toSingle(ZipHelper)

def processArgs(args):
    if args.buildFull:
        args.updateLinks = True
        args.updateUnitySolution = True
        args.updateCustomSolution = True
        args.buildCustomSolution = True

def findAllFiles(directory, pattern):
    for root, dirs, files in os.walk(directory):
        for basename in files:
            if fnmatch.fnmatch(basename, pattern):
                filename = os.path.join(root, basename)
                yield filename

def installPlugins():
    import importlib

    pluginDir = os.path.join(PythonDir, 'plugins')

    for filePath in findAllFiles(pluginDir, '*.py'):
        basePath = filePath[len(pluginDir) + 1:]
        basePath = os.path.splitext(basePath)[0]
        basePath = basePath.replace('\\', '.')
        #print("Found plugin at {0}".format(basePath))
        importlib.import_module('plugins.' + basePath)

if __name__ == '__main__':
    if (sys.version_info < (3, 0)):
        print('Wrong version of python!  Install python 3 and try again')
        sys.exit(2)

    argv = sys.argv[1:]

    # If it's 2 then it only has the -cfg param
    if len(argv) == 0:
        print("""
.______   .______      ______          __   _______ .__   __. ____    ____
|   _  \  |   _  \    /  __  \        |  | |   ____||  \ |  | \   \  /   /
|  |_)  | |  |_)  |  |  |  |  |       |  | |  |__   |   \|  |  \   \/   /
|   ___/  |      /   |  |  |  | .--.  |  | |   __|  |  . `  |   \_    _/
|  |      |  |\  \-. |  `--'  | |  `--'  | |  |____ |  |\   |     |  |
| _|      | _| `.__|  \______/   \______/  |_______||__| \__|     |__|

                    Unity Package Manager""")
        print()
        print(' Run "Upm -d" to open up the documentation page.  If this is your first time using Projeny this is what you should do.')
        print(' Run "Upm -h" to print the full list of command line options')
        print()
        sys.exit(2)

    # Here we split out some functionality into various methods
    # so that other python code can make use of them
    # if they want to extend projeny
    parser = argparse.ArgumentParser(description='Unity Package Manager')
    addArguments(parser)
    args = parser.parse_args(sys.argv[1:])

    processArgs(args)

    installBindings(args.verbose, args.veryVerbose)
    installPlugins()

    from upm.main.UpmRunner import UpmRunner
    UpmRunner().run(args)

