
import sys
import os
import fnmatch
import argparse

import prj.util.MiscUtil as MiscUtil
import prj.util.PlatformUtil as PlatformUtil

from prj.config.YamlConfigLoader import loadYamlFilesThatExist
from prj.config.Config import Config
from prj.util.VarManager import VarManager
from prj.util.ZipHelper import ZipHelper
from prj.log.Logger import Logger
from prj.util.SystemHelper import SystemHelper
from prj.log.LogStreamFile import LogStreamFile
from prj.log.LogStreamConsole import LogStreamConsole
from prj.util.ProcessRunner import ProcessRunner
from prj.util.JunctionHelper import JunctionHelper
from prj.main.VisualStudioSolutionGenerator import VisualStudioSolutionGenerator
from prj.main.VisualStudioHelper import VisualStudioHelper
from prj.main.ProjectSchemaLoader import ProjectSchemaLoader
from prj.util.ScriptRunner import ScriptRunner
from prj.util.CommonSettings import CommonSettings
from prj.reg.UnityPackageExtractor import UnityPackageExtractor
from prj.reg.UnityPackageAnalyzer import UnityPackageAnalyzer

from prj.main.ProjectConfigChanger import ProjectConfigChanger

from prj.main.ProjectSchemaLoader import ProjectConfigFileName

from prj.util.CommonSettings import ConfigFileName
from prj.reg.ReleaseSourceManager import ReleaseSourceManager

from prj.main.PrjRunner import PrjRunner

from prj.util.Assert import *

from prj.util.PlatformUtil import Platforms
from prj.main.PackageManager import PackageManager

import prj.ioc.Container as Container
from prj.ioc.Inject import Inject

from prj.util.UnityHelper import UnityHelper

def addArguments(parser):

    # Core
    parser.add_argument('-in', '--init', action='store_true', help='Initializes the directory links for all projects')
    parser.add_argument('-p', '--project', metavar='PROJECT_NAME', type=str, help="The project to apply changes to.  If unspecified, this will be set to the value for DefaultProject in {0}".format(ConfigFileName))
    parser.add_argument('-pl', '--platform', type=str, default='win', choices=['win', 'webp', 'webgl', 'and', 'osx', 'ios', 'lin'], help='The platform to use.  If unspecified, windows is assumed.')

    # Script settinsg
    parser.add_argument('-cfg', '--configPath', metavar='CONFIG_PATH', type=str, help="The path to the main {0} config file.  If unspecified, it will be assumed to exist at [CurrentDirectory]/{0}".format(ConfigFileName))

    parser.add_argument('-v', '--verbose', action='store_true', help='Output more detailed logging information to console')
    parser.add_argument('-vv', '--veryVerbose', action='store_true', help='Output absolutely all logging information to console.  This will result in the console output being identical to the contents of the log file')
    parser.add_argument('-sp', '--suppressPrompts', action='store_true', help='If unset, confirmation prompts will be displayed for important operations.')

    # Projects
    parser.add_argument('-lpr', '--listProjects', action='store_true', help='Display the list of all projects that are in the UnityProjects directory')
    parser.add_argument('-cpr', '--createProject', action='store_true', help='Creates a new directory in the UnityProjects directory, adds a default {0} file, and sets up directory links'.format(ProjectConfigFileName))
    parser.add_argument('-ul', '--updateLinks', action='store_true', help='Updates directory links for the given project and the given platform')
    parser.add_argument('-clp', '--clearProjectGeneratedFiles', action='store_true', help='Remove all generated files for the given project.  This can be reversed easily by re-initializing the project')
    parser.add_argument('-cla', '--clearAllProjectGeneratedFiles', action='store_true', help='Remove all the generated files for all projects. This can be reversed easily by running the init command')
    parser.add_argument('-dal', '--deleteAllLinks', action='store_true', help='Delete all directory links for all subdirectories')
    parser.add_argument('-dpr', '--deleteProject', metavar='PROJECT_NAME', type=str, help="Deletes the given project from the from UnityProjects directory")

    # Packages
    parser.add_argument('-cpa', '--createPackage', metavar='NEW_PACKAGE_NAME', type=str, help="Creates a new directory underneath the UnityPackages directory with the given name")
    parser.add_argument('-lpa', '--listPackages', action='store_true', help='Lists all the directories found in the UnityPackages directory')
    parser.add_argument('-dpa', '--deletePackage', metavar='PACKAGE_NAME', type=str, help="Deletes the directory at UnityPackages/x where x is the given value")

    # Releases
    parser.add_argument('-ins', '--installRelease', type=str, nargs=2, metavar=('RELEASE_NAME', 'RELEASE_VERSION'), help="Searches all release sources for the given release with given version")
    parser.add_argument('-lr', '--listReleases', action='store_true', help='Lists all releases found from all release sources')

    # Project manipulation
    parser.add_argument('-prap', '--projectAddPackage', metavar='PACKAGE_NAME', type=str, help="Adds the given package to the {0} for the given project".format(ProjectConfigFileName))

    # Visual Studio solution stuff
    parser.add_argument('-uus', '--updateUnitySolution', action='store_true', help='Equivalent to executing the menu option "Assets/Open C# Project" in unity (without actually opening it)')
    parser.add_argument('-ucs', '--updateCustomSolution', action='store_true', help='Updates the custom solution for the given project with the files found in the Assets/ folder.  It will also take settings from the generated unity solution such as defines, and references.')
    parser.add_argument('-b', '--buildCustomSolution', action='store_true', help='Build the generated custom solution for the given project')
    parser.add_argument('-ocs', '--openCustomSolution', action='store_true', help='Open the solution for the given project/platform in visual studio')
    parser.add_argument('-bf', '--buildFull', action='store_true', help='Perform a full build of the given project, including updating directory links, generating the C# solution, and building the solution')
    parser.add_argument('-bpb', '--buildPrebuild', action='store_true', help='Build the prebuild solution, if set.')

    # Misc
    parser.add_argument('-epy', '--editProjectYaml', action='store_true', help='Opens up the {0} for the given project'.format(ProjectConfigFileName))
    parser.add_argument('-cc', '--createConfig', action='store_true', help='Sets up a new collection of projeny based unity projects/packages in the current directory.  Adds a {0} file and also adds UnityPackages and UnityProjects directories'.format(ConfigFileName))
    parser.add_argument('-ou', '--openUnity', action='store_true', help='Opens up Unity for the given project')
    parser.add_argument('-d', '--openDocumentation', action='store_true', help='Opens the documentation page in a web browser')

def getProjenyDir():
    # This works for both exe builds (Bin/Prj/Data/Prj.exe) and running from source (Source/prj/main/Prj.py) by coincidence
    return os.path.join(MiscUtil.getExecDirectory(), '../../..')

def getExtraUserConfigPaths():
    return [os.path.join(os.path.expanduser('~'), ConfigFileName)]

def installBindings(mainConfigPath = None):

    projenyDir = getProjenyDir()
    projenyConfigPath = os.path.join(projenyDir, ConfigFileName)

    # Put the standard config first so it can be over-ridden by user settings
    configPaths = [projenyConfigPath]

    if mainConfigPath:
        assertThat(os.path.isfile(mainConfigPath), 'Could not find file at "{0}"', mainConfigPath)
        configPaths += [mainConfigPath]

    configPaths += getExtraUserConfigPaths()

    Container.bind('Config').toSingle(Config, loadYamlFilesThatExist(*configPaths))

    initialVars = { 'ProjenyDir': projenyDir, }

    if mainConfigPath:
        initialVars['ConfigDir'] = os.path.dirname(mainConfigPath)

    if not MiscUtil.isRunningAsExe():
        initialVars['PythonPluginDir'] = getPluginDirPath()

    Container.bind('VarManager').toSingle(VarManager, initialVars)
    Container.bind('SystemHelper').toSingle(SystemHelper)
    Container.bind('Logger').toSingle(Logger)
    Container.bind('UnityHelper').toSingle(UnityHelper)
    Container.bind('ScriptRunner').toSingle(ScriptRunner)
    Container.bind('PackageManager').toSingle(PackageManager)
    Container.bind('ProcessRunner').toSingle(ProcessRunner)
    Container.bind('JunctionHelper').toSingle(JunctionHelper)
    Container.bind('VisualStudioSolutionGenerator').toSingle(VisualStudioSolutionGenerator)
    Container.bind('VisualStudioHelper').toSingle(VisualStudioHelper)
    Container.bind('ProjectSchemaLoader').toSingle(ProjectSchemaLoader)
    Container.bind('CommonSettings').toSingle(CommonSettings)
    Container.bind('UnityPackageExtractor').toSingle(UnityPackageExtractor)
    Container.bind('ZipHelper').toSingle(ZipHelper)
    Container.bind('UnityPackageAnalyzer').toSingle(UnityPackageAnalyzer)
    Container.bind('ProjectConfigChanger').toSingle(ProjectConfigChanger)

    Container.bind('ReleaseSourceManager').toSingle(ReleaseSourceManager)

def processArgs(args):
    if args.buildFull:
        args.buildPrebuild = True
        args.updateLinks = True
        args.updateUnitySolution = True
        args.updateCustomSolution = True
        args.buildCustomSolution = True

def findFilesByPattern(directory, pattern):
    for root, dirs, files in os.walk(directory):
        for basename in files:
            if fnmatch.fnmatch(basename, pattern):
                filename = os.path.join(root, basename)
                yield filename

def getPluginDirPath():
    return os.path.join(MiscUtil.getExecDirectory(), '../../plugins')

def installPlugins():

    if MiscUtil.isRunningAsExe():
        # Must be running from source for plugins
        return

    import importlib

    pluginDir = getPluginDirPath()

    for filePath in findFilesByPattern(pluginDir, '*.py'):
        basePath = filePath[len(pluginDir) + 1:]
        basePath = os.path.splitext(basePath)[0]
        basePath = basePath.replace('\\', '.')
        importlib.import_module('plugins.' + basePath)

def tryGetMainConfigPath(args):
    if args.configPath:
        assertThat(os.path.isfile(args.configPath), "Could not find config file at '{0}'", args.configPath)
        return args.configPath

    configPathGuess = os.path.join(os.getcwd(), ConfigFileName)

    if os.path.isfile(configPathGuess):
        return configPathGuess

    return None

def main():
    # Here we split out some functionality into various methods
    # so that other python code can make use of them
    # if they want to extend projeny
    parser = argparse.ArgumentParser(description='Unity Package Manager')
    addArguments(parser)

    argv = sys.argv[1:]

    # If it's 2 then it only has the -cfg param
    if len(argv) == 0:
        parser.print_help()
        sys.exit(2)

    args = parser.parse_args(sys.argv[1:])

    processArgs(args)

    Container.bind('LogStream').toSingle(LogStreamFile)
    Container.bind('LogStream').toSingle(LogStreamConsole, args.verbose, args.veryVerbose)

    installBindings(tryGetMainConfigPath(args))
    installPlugins()

    PrjRunner().run(args)

if __name__ == '__main__':

    if (sys.version_info < (3, 0)):
        print('Wrong version of python!  Install python 3 and try again')
        sys.exit(2)

    succeeded = True

    try:
        main()

    except KeyboardInterrupt as e:
        print('Operation aborted by user by hitting CTRL+C')
        succeeded = False

    except Exception as e:
        sys.stderr.write(str(e))

        if not MiscUtil.isRunningAsExe():
            sys.stderr.write('\n' + traceback.format_exc())

        succeeded = False

    if not succeeded:
        sys.exit(1)
