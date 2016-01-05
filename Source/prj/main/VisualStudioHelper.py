
import prj.ioc.Container as Container
from prj.ioc.Inject import Inject
from prj.ioc.Inject import InjectMany
import prj.ioc.IocAssertions as Assertions
import prj.util.MiscUtil as MiscUtil

import win32api
import win32com.client

from prj.util.Assert import *

class VisualStudioHelper:
    _log = Inject('Logger')
    _config = Inject('Config')
    _packageManager = Inject('PackageManager')
    _unityHelper = Inject('UnityHelper')
    _varMgr = Inject('VarManager')
    _sys = Inject('SystemHelper')
    _vsSolutionGenerator = Inject('VisualStudioSolutionGenerator')

    def openFile(self, filePath, lineNo, project, platform):
        if not lineNo or lineNo <= 0:
            lineNo = 1

        if MiscUtil.doesProcessExist('^devenv\.exe$'):
            self.openFileInExistingVisualStudioInstance(filePath, lineNo)

            # This works too but doesn't allow going to a specific line
            #self._sys.executeNoWait('[VisualStudioCommandLinePath] /edit "{0}"'.format(filePath))
        else:
            # Unfortunately, in this case we can't pass in the line number
            self.openCustomSolution(project, platform, filePath)

    def openFileInExistingVisualStudioInstance(self, filePath, lineNo):
        try:
            dte = win32com.client.GetActiveObject("VisualStudio.DTE.12.0")

            dte.MainWindow.Activate
            dte.ItemOperations.OpenFile(self._sys.canonicalizePath(filePath))
            dte.ActiveDocument.Selection.MoveToLineAndOffset(lineNo, 1)
        except Exception as error:
            raise Exception("COM Error.  This is often triggered when given a bad line number. Details: {0}".format(win32api.FormatMessage(error.excepinfo[5])))

    def openVisualStudioSolution(self, solutionPath, filePath = None):
        if not self._varMgr.hasKey('VisualStudioIdePath'):
            assertThat(False, "Path to visual studio has not been defined.  Please set <VisualStudioIdePath> within one of your {0} files", ConfigFileName)

        if self._sys.fileExists('[VisualStudioIdePath]'):
            self._sys.executeNoWait('"[VisualStudioIdePath]" {0} {1}'.format(self._sys.canonicalizePath(solutionPath), self._sys.canonicalizePath(filePath) if filePath else ""))
        else:
            assertThat(False, "Cannot find path to visual studio.  Expected to find it at '{0}'".format(self._varMgr.expand('[VisualStudioIdePath]')))

    def updateCustomSolution(self, project, platform):
        self._vsSolutionGenerator.updateVisualStudioSolution(project, platform)

    def openCustomSolution(self, project, platform, filePath = None):
        self.openVisualStudioSolution(self._getCustomSolutionPath(project, platform), filePath)

    def buildCustomSolution(self, project, platform):
        solutionPath = self._getCustomSolutionPath(project, platform)

        if not self._sys.fileExists(solutionPath):
            self._log.warn('Could not find generated custom solution.  Generating now.')
            self._vsSolutionGenerator.updateVisualStudioSolution(project, platform)

        self._log.heading('Building {0}-{1}.sln'.format(project, platform))
        self.buildVisualStudioProject(solutionPath, 'Debug')

    def buildVisualStudioProject(self, solutionPath, buildConfig):
        if self._config.getBool('Compilation', 'UseDevenv'):
            buildCommand = '"[VisualStudioCommandLinePath]" {0} /build "{1}"'.format(solutionPath, buildConfig)
        else:
            buildCommand = '"[MsBuildExePath]" /p:VisualStudioVersion=12.0'
            #if rebuild:
                #buildCommand += ' /t:Rebuild'
            buildCommand += ' /p:Configuration="{0}" "{1}"'.format(buildConfig, solutionPath)

        self._sys.executeAndWait(buildCommand)

    def _getCustomSolutionPath(self, project, platform):
        return '[UnityProjectsDir]/{0}/{0}-{1}.sln'.format(project, platform)

    def updateUnitySolution(self, projectName, platform):
        """
        Simply runs unity and then generates the monodevelop solution file using an editor script
        This is used when generating the Visual Studio Solution to get DLL references and defines etc.
        """
        self._log.heading('Updating unity generated solution for project {0} ({1})'.format(projectName, platform))

        self._packageManager.checkProjectInitialized(projectName, platform)

        # This will generate the unity csproj files which we need to generate Modest3d.sln correctly
        # It's also necessary to run this first on clean checkouts to initialize unity properly
        self._unityHelper.runEditorFunction(projectName, platform, 'Projeny.ProjenyEditorUtil.UpdateMonodevelopProject')
