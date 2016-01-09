
import prj.util.YamlSerializer as YamlSerializer

from prj.util.Assert import *

import prj.ioc.Container as Container
from prj.ioc.Inject import Inject
from prj.ioc.Inject import InjectMany
import prj.ioc.IocAssertions as Assertions

from prj.util.PlatformUtil import Platforms

from prj.main.ProjectSchemaLoader import ProjectConfigFileName
from prj.main.ProjectConfig import ProjectConfig

class ProjectConfigChanger:
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')
    _packageManager = Inject('PackageManager')
    _varMgr = Inject('VarManager')

    def _getProjectConfigPath(self, projectName):
        return self._varMgr.expandPath('[UnityProjectsDir]/{0}/{1}'.format(projectName, ProjectConfigFileName))

    def _loadProjectConfig(self, projectName):
        configPath = self._getProjectConfigPath(projectName)

        yamlData = YamlSerializer.deserialize(self._sys.readFileAsText(configPath))

        result = ProjectConfig()

        for pair in yamlData.__dict__.items():
            result.__dict__[pair[0]] = pair[1]

        return result

    def _saveProjectConfig(self, projectName, projectConfig):
        configPath = self._getProjectConfigPath(projectName)
        self._sys.writeFileAsText(configPath, YamlSerializer.serialize(projectConfig))

    def addPackage(self, projectName, packageName):
        self._log.heading('Adding package {0} to project {1}'.format(packageName, projectName))

        assertThat(packageName in self._packageManager.getAllPackageNames(), "Could not find the given package '{0}' in the UnityPackages folder", packageName)

        self._packageManager.setPathsForProject(projectName, Platforms.Windows)

        projConfig = self._loadProjectConfig(projectName)

        assertThat(packageName not in projConfig.assetsFolder and packageName not in projConfig.pluginsFolder,
           "Given package '{0}' has already been added to project config", packageName)

        projConfig.assetsFolder.append(packageName)

        self._saveProjectConfig(projectName, projConfig)

        self._log.good("Added package '{0}' to file '{1}/{2}'", packageName, projectName, ProjectConfigFileName)

