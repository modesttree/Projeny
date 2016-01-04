
import sys
import argparse
import os
import re

from prj.log.LogStreamFile import LogStreamFile
from prj.log.LogStreamConsole import LogStreamConsole

from prj.util.ProcessRunner import ProcessRunner
from prj.util.SystemHelper import SystemHelper
from prj.util.VarManager import VarManager
from prj.config.Config import Config
from prj.log.Logger import Logger

from prj.util.Assert import *

import prj.ioc.Container as Container
from prj.ioc.Inject import Inject

ScriptDir = os.path.dirname(os.path.realpath(__file__))
PythonDir = os.path.realpath(os.path.join(ScriptDir, '../..'))

class Runner:
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')
    _varMgr = Inject('VarManager')

    def run(self, args):
        self._args = args

        self._varMgr.add('PythonDir', PythonDir)

        versionStr = self._sys.readFileAsText('[PythonDir]/Version.txt')

        groups = re.match('^(\d+)\.(\d+)$', versionStr).groups()

        majorNumber = int(groups[0])
        minorNumber = int(groups[1])

        minorNumber += 1

        self._sys.executeAndReturnOutput("git tag -a v{0}.{1} -m 'Version {0}.{1}'".format(majorNumber, minorNumber))

        self._sys.writeFileAsText('[PythonDir]/Version.txt', '{0}.{1}'.format(majorNumber, minorNumber))

        self._log.info("Incremented version to {0}.{1}. Now commit and then run 'git push --tags'", majorNumber, minorNumber)

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

    parser = argparse.ArgumentParser(description='Version Incrementer')
    argv = sys.argv[1:]

    args = parser.parse_args(sys.argv[1:])

    installBindings()

    Runner().run(args)


