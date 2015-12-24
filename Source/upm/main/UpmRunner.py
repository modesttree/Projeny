
import configparser
import msvcrt
import re
import time
import sys
import os
import shlex
import subprocess
from datetime import datetime
import argparse
import shutil
from glob import glob
import traceback
import webbrowser

from upm.util.Assert import *
import upm.util.MiscUtil as MiscUtil
import upm.util.PlatformUtil as PlatformUtil

from upm.config.ConfigXml import ConfigXml
from upm.util.VarManager import VarManager
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

from upm.util.PlatformUtil import Platforms
from upm.main.PackageManager import PackageManager

import upm.ioc.Container as Container
from upm.ioc.Inject import Inject
import upm.ioc.IocAssertions as Assertions

from upm.util.UnityHelper import UnityHelper

class UpmRunner:
    _scriptRunner = Inject('ScriptRunner')
    _config = Inject('Config')
    _packageMgr = Inject('PackageManager')
    _unityHelper = Inject('UnityHelper')
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')
    _vsSolutionHelper = Inject('VisualStudioHelper')

    def run(self, args):
        self._args = args
        success = self._scriptRunner.runWrapper(self._runInternal)

        if not success:
            sys.exit(1)

    def _runPreBuild(self):
        if self._args.openDocumentation:
            self._openDocumentation()

        if self._args.clearProjectGeneratedFiles:
            self._packageMgr.clearProjectGeneratedFiles(self._project)

        if self._args.clearAllProjectGeneratedFiles:
            self._packageMgr.clearAllProjectGeneratedFiles()

        if self._args.deleteAllLinks:
            self._packageMgr.deleteAllLinks()

        if self._args.initAll:
            self._packageMgr.initAllProjects()

        if self._args.updateLinks:
            self._packageMgr.updateProjectJunctions(self._project, self._platform)

        if self._args.updateUnitySolution:
            self._vsSolutionHelper.updateUnitySolution(self._project, self._platform)

        if self._args.updateCustomSolution:
            self._vsSolutionHelper.updateCustomSolution(self._project, self._platform)

    def _openDocumentation(self):
        webbrowser.open('https://github.com/modesttree/ModestUnityPackageManager')

    def _runBuild(self):
        if self._args.buildCustomSolution:
            self._vsSolutionHelper.buildCustomSolution(self._project, self._platform)

    def _runPostBuild(self):
        if self._args.listProjects:
            self._packageMgr.listAllProjects()

        if self._args.openUnity:
            self._packageMgr.checkProjectInitialized(self._project, self._platform)
            self._unityHelper.openUnity(self._project, self._platform)

        if self._args.openCustomSolution:
            self._vsSolutionHelper.openCustomSolution(self._project, self._platform)

        if self._args.openCsFile:
            filePath, lineNo = self._parseFile(self._args.openCsFile)
            self._vsSolutionHelper.openFile(filePath, lineNo, self._project, self._platform)

    def _parseFile(self, filePathAndLine):
        delPos = filePathAndLine.rfind(':')

        if delPos == -1:
            return filePathAndLine, 1

        filePath = filePathAndLine[0:delPos]
        lineNoStr = filePathAndLine[delPos+1:]
        lineNo = int(lineNoStr)

        return filePath, lineNo

    def _runInternal(self):
        self._log.debug("Started UPM with arguments: {0}".format(" ".join(sys.argv[1:])))

        self.processArgs()
        self._validateArgs()
        self._runPreBuild()
        self._runBuild()
        self._runPostBuild()

    def processArgs(self):

        if self._args.openCsFile != None and (self._args.platform == None or self._args.project == None):
            self._project, self._platform = self._getProjectAndPlatformFromFilePath(self._args.openCsFile)
        else:
            self._project = self._args.project

            if not self._project:
                self._project = self._config.getString('Projeny', 'DefaultProject', None)

            if self._project and not self._packageMgr.projectExists(self._project):
                self._project = self._packageMgr.getProjectFromAlias(self._project)

            self._platform = PlatformUtil.fromPlatformArgName(self._args.platform)

    def _getProjectAndPlatformFromFilePath(self, filePath):
        unityProjectsDir = self._sys.cleanUpPath(self._varMgr.expand('[UnityProjectsDir]'))
        filePath = self._sys.cleanUpPath(filePath)

        if not filePath.startswith(unityProjectsDir):
            raise Exception("The given file path is not within the UnityProjects directory")

        relativePath = filePath[len(unityProjectsDir)+1:]
        dirs = relativePath.split(os.path.sep)

        projectName = dirs[0]

        platformProjectDirName = dirs[1]
        platformDirName = platformProjectDirName[platformProjectDirName.rfind('-')+1:]

        platform = PlatformUtil.fromPlatformFolderName(platformDirName)

        return projectName, platform

    def _validateArgs(self):
        requiresProject = self._args.updateLinks or self._args.updateUnitySolution \
           or self._args.updateCustomSolution or self._args.buildCustomSolution \
           or self._args.clearProjectGeneratedFiles or self._args.buildFull \
           or self._args.openUnity or self._args.openCustomSolution \
           or (self._args.openCsFile != None)

        if requiresProject and not self._project:
            assertThat(False, "Cannot execute the given arguments without a project specified, or a default project defined in the ProjenyConfig.xml file")

