
import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
from mtm.ioc.Inject import InjectMany
import mtm.ioc.IocAssertions as Assertions
import mtm.util.MiscUtil as MiscUtil

from mtm.util.CommonSettings import ConfigFileName

import win32api
import win32com.client

from mtm.util.Assert import *

class ProjenyVisualStudioHelper:
    _vsHelper = Inject('VisualStudioHelper')
    _vsSolutionGenerator = Inject('VisualStudioSolutionGenerator')
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')
    _packageManager = Inject('PackageManager')
    _unityHelper = Inject('UnityHelper')

    def updateCustomSolution(self, project, platform):
        self._vsSolutionGenerator.updateVisualStudioSolution(project, platform)

    def openCustomSolution(self, project, platform, filePath = None):
        self._vsHelper.openVisualStudioSolution(self._getCustomSolutionPath(project, platform), filePath)

    def buildCustomSolution(self, project, platform):
        solutionPath = self._getCustomSolutionPath(project, platform)

        if not self._sys.fileExists(solutionPath):
            self._log.warn('Could not find generated custom solution.  Generating now.')
            self._vsSolutionGenerator.updateVisualStudioSolution(project, platform)

        with self._log.heading('Building {0}-{1}.sln'.format(project, platform)):
            self._vsHelper.buildVisualStudioProject(solutionPath, 'Debug')

    def _getCustomSolutionPath(self, project, platform):
        return '[UnityProjectsDir]/{0}/{0}-{1}.sln'.format(project, platform)

    def updateUnitySolution(self, projectName, platform):
        """
        Simply runs unity and then generates the monodevelop solution file using an editor script
        This is used when generating the Visual Studio Solution to get DLL references and defines etc.
        """
        with self._log.heading('Updating unity generated solution for project {0} ({1})'.format(projectName, platform)):
            self._packageManager.checkProjectInitialized(projectName, platform)

            # This will generate the unity csproj files which we need to generate Modest3d.sln correctly
            # It's also necessary to run this first on clean checkouts to initialize unity properly
            self._unityHelper.runEditorFunction(projectName, platform, 'Projeny.ProjenyEditorUtil.ForceGenerateUnitySolution')

