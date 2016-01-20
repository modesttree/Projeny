
import sys
import argparse

import mtm.util.MiscUtil as MiscUtil
import mtm.util.PlatformUtil as PlatformUtil

from mtm.config.YamlConfigLoader import loadYamlFilesThatExist
from mtm.config.Config import Config
from mtm.util.VarManager import VarManager
from mtm.util.ZipHelper import ZipHelper
from mtm.log.Logger import Logger
from mtm.util.SystemHelper import SystemHelper
from mtm.log.LogStreamFile import LogStreamFile
from mtm.log.LogStreamConsole import LogStreamConsole
from mtm.util.ProcessRunner import ProcessRunner
from mtm.util.JunctionHelper import JunctionHelper
from prj.main.VisualStudioSolutionGenerator import VisualStudioSolutionGenerator
from prj.main.VisualStudioHelper import VisualStudioHelper
from prj.main.ProjectSchemaLoader import ProjectSchemaLoader
from mtm.util.ScriptRunner import ScriptRunner
from mtm.util.CommonSettings import CommonSettings
from prj.reg.UnityPackageExtractor import UnityPackageExtractor
from prj.reg.UnityPackageAnalyzer import UnityPackageAnalyzer

from mtm.util.CommonSettings import ConfigFileName
from prj.reg.ReleaseSourceManager import ReleaseSourceManager

from prj.main.PrjRunner import PrjRunner

from mtm.util.Assert import *

from mtm.util.PlatformUtil import Platforms
from prj.main.PackageManager import PackageManager

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject

from mtm.util.UnityHelper import UnityHelper

class Runner:
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')

    def run(self, args):
        self._args = args

        for filePath in self._sys.findFilesByPattern(self._args.directory, '*.py'):
            self._sys.executeAndWait('autoflake --in-place --remove-unused-variables "{0}"'.format(filePath))

def installBindings():

    Container.bind('LogStream').toSingle(LogStreamFile)
    Container.bind('LogStream').toSingle(LogStreamConsole, True, True)
    Container.bind('Config').toSingle(Config, [])

    Container.bind('VarManager').toSingle(VarManager)
    Container.bind('SystemHelper').toSingle(SystemHelper)
    Container.bind('Logger').toSingle(Logger)
    Container.bind('ProcessRunner').toSingle(ProcessRunner)

if __name__ == '__main__':

    if (sys.version_info < (3, 0)):
        print('Wrong version of python!  Install python 3 and try again')
        sys.exit(2)

    parser = argparse.ArgumentParser(description='Python cleaner')
    parser.add_argument("directory", help="")

    argv = sys.argv[1:]

    # If it's 2 then it only has the -cfg param
    if len(argv) == 0:
        parser.print_help()
        sys.exit(2)

    args = parser.parse_args(sys.argv[1:])

    installBindings()

    Runner().run(args)

