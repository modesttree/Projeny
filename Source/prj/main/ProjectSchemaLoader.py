
import sys
import re
import os

from prj.util.Assert import *
from prj.util.PlatformUtil import Platforms
import prj.util.Util as Util
import prj.ioc.Container as Container
from prj.ioc.Inject import Inject
import prj.ioc.IocAssertions as Assertions
import prj.util.JunctionUtil as JunctionUtil
from prj.config.Config import Config
from prj.config.YamlConfigLoader import loadYamlFilesThatExist

from prj.main.CsProjParserHelper import NsPrefix

import xml.etree.ElementTree as ET

ProjectConfigFileName = 'ProjenyProject.yaml'
ProjectUserConfigFileName = 'ProjenyProjectCustom.yaml'

class ProjectSchemaLoader:
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')

    def loadSchema(self, name, platform):
        schemaPath = self._varMgr.expandPath('[UnityProjectsDir]/{0}/{1}'.format(name, ProjectConfigFileName))
        schemaPathUser = self._varMgr.expandPath('[UnityProjectsDir]/{0}/{1}'.format(name, ProjectUserConfigFileName))
        schemaPathGlobal = self._varMgr.expandPath('[UnityProjectsDir]/{0}'.format(ProjectConfigFileName))
        schemaPathUserGlobal = self._varMgr.expandPath('[UnityProjectsDir]/{0}'.format(ProjectUserConfigFileName))

        self._log.debug('Loading schema at path "{0}"'.format(schemaPath))
        config = Config(loadYamlFilesThatExist(schemaPath, schemaPathUser, schemaPathGlobal, schemaPathUserGlobal))

        pluginDependencies = config.tryGetList([], 'PluginsFolder')
        scriptsDependencies = config.tryGetList([], 'AssetsFolder')
        customProjects = config.tryGetList([], 'SolutionProjects')
        customFolders = config.tryGetDictionary({}, 'SolutionFolders')

        # Remove duplicates
        scriptsDependencies = list(set(scriptsDependencies))
        pluginDependencies = list(set(pluginDependencies))

        for packageName in pluginDependencies:
            assertThat(not packageName in scriptsDependencies, "Found package '{0}' in both scripts and plugins.  Must be in only one or the other".format(packageName))

        allDependencies = pluginDependencies + scriptsDependencies

        packageMap = {}

        # Resolve all dependencies for each package
        # by default, put any dependencies that are not declared explicitly into the plugins folder
        for packageName in allDependencies:

            packageDir = self._varMgr.expandPath('[UnityPackagesDir]/{0}'.format(packageName))
            configPath = os.path.join(packageDir, 'ProjenyPackage.yaml')

            if os.path.exists(configPath):
                packageConfig = Config(loadYamlFilesThatExist(configPath))
            else:
                packageConfig = Config([])

            createCustomVsProject = self._checkCustomProjectMatch(packageName, customProjects)

            isPluginsDir = packageName in pluginDependencies

            if isPluginsDir:
                assertThat(not packageName in scriptsDependencies)
            else:
                assertThat(packageName in scriptsDependencies)

            if packageConfig.tryGetBool(False, 'ForceAssetsDirectory'):
                isPluginsDir = False

            explicitDependencies = packageConfig.tryGetList([], 'Dependencies')

            forcePluginsDir = packageConfig.tryGetBool(False, 'ForcePluginsDirectory')

            assemblyProjectRelativePath = packageConfig.tryGetString(None, 'AssemblyProject', 'Path')

            assemblyProjectFullPath = None
            assemblyProjectRoot = None
            assemblyProjectConfig = None

            if assemblyProjectRelativePath != None:
                assemblyProjectFullPath = self._varMgr.expand(assemblyProjectRelativePath)

                if not os.path.isabs(assemblyProjectFullPath):
                    assemblyProjectFullPath = os.path.join(packageDir, assemblyProjectRelativePath)

                assertThat(self._sys.fileExists(assemblyProjectFullPath), "Expected to find file at '{0}'", assemblyProjectFullPath)
                assemblyProjectRoot = ET.parse(assemblyProjectFullPath).getroot()

                self._ensureAssemblyNameIs(assemblyProjectRoot, packageName)

                assertIsEqual(self._sys.getFileNameWithoutExtension(assemblyProjectFullPath), packageName,
                  'Assembly projects must have the same name as their package')

                assemblyProjectConfig = packageConfig.tryGetString(None, 'AssemblyProject', 'Config')
                explicitDependencies += self._getDependenciesFromCsProj(assemblyProjectRoot)

            assertThat(not packageName in packageMap)
            packageMap[packageName] = PackageInfo(
                isPluginsDir, packageName, packageConfig, createCustomVsProject,
                explicitDependencies, forcePluginsDir, assemblyProjectFullPath, assemblyProjectRoot, assemblyProjectConfig)

            for dependName in explicitDependencies:
                if not dependName in allDependencies:
                    pluginDependencies.append(dependName)
                    # Yes, python is ok with changing allDependencies even while iterating over it
                    allDependencies.append(dependName)

            for dependName in packageConfig.tryGetList([], 'Extras'):
                if not dependName in allDependencies:
                    if isPluginsDir:
                        pluginDependencies.append(dependName)
                    else:
                        scriptsDependencies.append(dependName)
                    # Yes, python is ok with changing allDependencies even while iterating over it
                    allDependencies.append(dependName)

        self._removePlatformSpecificPackages(packageMap, platform)

        self._ensurePrebuiltProjectHasNoScripts(packageMap)

        self._fillOutDependencies(packageMap)

        # For the pre-built assembly projects, if we add one of them to our solution,
        # then we need to add all the pre-built dependencies, since unlike generated projects
        # we can't make the prebuilt projects use the dll directly
        self._ensureVisiblePrebuiltProjectHaveVisibleDependencies(packageMap)

        self._printDependencyTree(packageMap)

        for customProj in customProjects:
            assertThat(customProj.startswith('/') or customProj in allDependencies, 'Given project "{0}" in schema is not included in either "scripts" or "plugins"'.format(customProj))

        self._log.info('Found {0} packages in total for given schema'.format(len(allDependencies)))

        # In Unity, the plugins folder can not have any dependencies on anything in the scripts folder
        # So if dependencies exist then just automatically move those packages to the scripts folder
        self._ensurePluginPackagesDoNotHaveDependenciesInAssets(packageMap)

        self._ensurePackagesThatAreNotProjectsDoNotHaveProjectDependencies(packageMap)

        projectsDir = self._varMgr.expandPath('[UnityProjectsDir]')

        for info in packageMap.values():
            if info.forcePluginsDir and not info.isPluginDir:
                assertThat(False, "Package '{0}' must be in plugins directory".format(info.name))

        return ProjectSchema(name, packageMap, customFolders)

    def _ensureAssemblyNameIs(self, projectRoot, packageName):
        assemblyName = projectRoot.findall('./{0}PropertyGroup/{0}AssemblyName'.format(NsPrefix))[0].text
        assertIsEqual(assemblyName, packageName, 'Packages that represent assembly projects must have the same name as the assembly')

    def _getDependenciesFromCsProj(self, projectRoot):
        result = []
        for projRef in projectRoot.findall('./{0}ItemGroup/{0}ProjectReference/{0}Name'.format(NsPrefix)):
            result.append(projRef.text)
        return result

    def _ensureVisiblePrebuiltProjectHaveVisibleDependencies(self, packageMap):
        for package in packageMap.values():
            if package.assemblyProjectPath != None and package.createCustomVsProject:
                self._makeAllPrebuiltDependenciesVisible(package, packageMap)

    def _makeAllPrebuiltDependenciesVisible(self, package, packageMap):
        for dependName in package.explicitDependencies:
            depend = packageMap[dependName]

            if not depend.createCustomVsProject:
                depend.createCustomVsProject = True
                self._makeAllPrebuiltDependenciesVisible(depend, packageMap)

    def _ensurePrebuiltProjectHasNoScripts(self, packageMap):
        for package in packageMap.values():
            if package.assemblyProjectPath != None:
                packageDir = self._varMgr.expandPath('[UnityPackagesDir]/{0}'.format(package.name))
                assertThat(not any(self._sys.findFilesByPattern(packageDir, '*.cs')),
                   "Found C# scripts in assembly project '{0}'.  This is not allowed - please move to a separate package.", package.name)

    def _ensurePackagesThatAreNotProjectsDoNotHaveProjectDependencies(self, packageMap):
        changedOne = True

        while changedOne:
            changedOne = False

            for info in packageMap.values():
                if not info.createCustomVsProject and self._hasVsProjectDependency(info, packageMap):
                    info.createCustomVsProject = True
                    self._log.warn('Created visual studio project for {0} package even though it wasnt marked as one, because it has csproj dependencies'.format(info.name))
                    changedOne = True

    def _hasVsProjectDependency(self, info, packageMap):
        for dependName in info.allDependencies:
            if not dependName in packageMap:
                # For eg. a platform specific dependency
                continue

            dependInfo = packageMap[dependName]

            if dependInfo.createCustomVsProject:
                return True

        return False

    def _ensurePluginPackagesDoNotHaveDependenciesInAssets(self, packageMap):
        movedProject = True

        while movedProject:
            movedProject = False

            for info in packageMap.values():
                if info.isPluginDir and self._hasAssetsDependency(info, packageMap):
                    info.isPluginDir = False
                    self._log.warn('Moved {0} package to scripts folder since it has dependencies there and therefore cannot be in plugins'.format(info.name))
                    movedProject = True

    def _hasAssetsDependency(self, info, packageMap):
        for dependName in info.allDependencies:
            if not dependName in packageMap:
                # For eg. a platform specific dependency
                continue

            dependInfo = packageMap[dependName]

            if not dependInfo.isPluginDir:
                return True

        return False

    def _removePlatformSpecificPackages(self, packageMap, platform):

        for info in list(packageMap.values()):

            if info.folderType == FolderTypes.AndroidProject or info.folderType == FolderTypes.AndroidLibraries:
                platforms = [Platforms.Android]
            elif info.folderType == FolderTypes.Ios:
                platforms = [Platforms.Ios]
            elif info.folderType == FolderTypes.WebGl:
                platforms = [Platforms.WebGl]
            else:
                platforms = info.config.tryGetList([], 'Platforms')

                if len(platforms) == 0:
                    continue

            if platform not in platforms:
                del packageMap[info.name]
                self._log.debug('Skipped project {0} since it is not enabled for platform {0}'.format(info.name, platform))

    def _printDependencyTree(self, packageMap):
        packages = sorted(packageMap.values(), key = lambda p: (p.isPluginDir, -len(p.explicitDependencies)))

        done = {}

        for pack in packages:
            self._printDependency(pack, done, 1, packageMap)

    def _printDependency(self, package, done, indentCount, packageMap):
        done[package.name] = True

        indentInterval = '    '

        indent = ((indentCount - 1) * (indentInterval + '.')) + indentInterval
        self._log.debug(indent + '|-' + package.name)

        for dependName in package.explicitDependencies:
            if dependName in packageMap:
                subPackage = packageMap[dependName]

                if subPackage.name in done:
                    self._log.debug(indent + '.' + indentInterval + '|~' + subPackage.name)
                else:
                    self._printDependency(subPackage, done, indentCount+1, packageMap)

    def _checkCustomProjectMatch(self, packageName, customProjects):
        if packageName in customProjects:
            return True

        # Allow regex's!
        for projPattern in customProjects:
            if projPattern.startswith('/'):
                projPattern = projPattern[1:]
                try:
                    if re.match(projPattern, packageName):
                        return True
                except Exception as e:
                    raise Exception("Failed while parsing project regex '/{0}' from {1}/{2}.  Details: {3}".format(projPattern, self._varMgr.expand('ProjectName'), ProjectConfigFileName, str(e)))

        return False

    def _fillOutDependencies(self, packageMap):

        self._log.debug('Processing dependency tree')

        inProgress = set()
        for info in packageMap.values():
            self._fillOutDependenciesForPackage(info, packageMap, inProgress)

    def _fillOutDependenciesForPackage(self, packageInfo, packageMap, inProgress):

        if packageInfo.name in inProgress:
            assertThat(False, "Found circular dependency when processing package {0}.  Dependency list: {1}".format(packageInfo.name, ' -> '.join([x for x in inProgress]) + '-> ' + packageInfo.name))

        inProgress.add(packageInfo.name)
        allDependencies = set(packageInfo.explicitDependencies)

        for explicitDependName in packageInfo.explicitDependencies:
            if explicitDependName not in packageMap:
                # Might be stripped out based on platform or something so just ignore
                continue

            explicitDependInfo = packageMap[explicitDependName]

            if not explicitDependInfo.allDependencies:
                self._fillOutDependenciesForPackage(explicitDependInfo, packageMap, inProgress)

            for dependName in explicitDependInfo.allDependencies:
                allDependencies.add(dependName)

        packageInfo.allDependencies = list(allDependencies)
        inProgress.remove(packageInfo.name)

class ProjectSchema:
    def __init__(self, name, packages, customFolderMap):
        self.name = name
        self.packages = packages
        self.customFolderMap = customFolderMap

class FolderTypes:
    Normal = "normal"
    WebGl = "webgl"
    AndroidProject = "androidproject"
    AndroidLibraries = "androidlibraries"
    Ios = "ios"
    StreamingAssets = "streamingassets"

class PackageInfo:
    def __init__(
        self, isPluginDir, name, config, createCustomVsProject,
        explicitDependencies, forcePluginsDir, assemblyProjectPath, assemblyProjectRoot, assemblyProjectConfig):

        self.isPluginDir = isPluginDir
        self.name = name
        self.explicitDependencies = explicitDependencies
        self.config = config
        self.createCustomVsProject = createCustomVsProject
        self.allDependencies = None
        self.folderType = self._getFolderTypeFromString(config.tryGetString('', 'FolderType'))
        self.assemblyProjectPath = assemblyProjectPath
        self.assemblyProjectRoot = assemblyProjectRoot
        self.assemblyProjectConfig = assemblyProjectConfig
        self.forcePluginsDir = forcePluginsDir

    def _getFolderTypeFromString(self, value):
        value = value.lower()

        if not value or value == FolderTypes.Normal or len(value) == 0:
            return FolderTypes.Normal

        if value == FolderTypes.AndroidProject:
            return FolderTypes.AndroidProject

        if value == FolderTypes.AndroidLibraries:
            return FolderTypes.AndroidLibraries

        if value == FolderTypes.Ios:
            return FolderTypes.Ios

        if value == FolderTypes.WebGl:
            return FolderTypes.WebGl

        if value == FolderTypes.StreamingAssets:
            return FolderTypes.StreamingAssets

        assertThat(False, "Unrecognized folder type '{0}'".format(value))
        return ""

    @property
    def outputDirVar(self):

        if self.folderType == FolderTypes.AndroidProject:
            return '[PluginsAndroidDir]'

        if self.folderType == FolderTypes.AndroidLibraries:
            return '[PluginsAndroidLibraryDir]'

        if self.folderType == FolderTypes.Ios:
            return '[PluginsIosLibraryDir]'

        if self.folderType == FolderTypes.WebGl:
            return '[PluginsWebGlLibraryDir]'

        if self.folderType == FolderTypes.StreamingAssets:
            return '[StreamingAssetsDir]'

        if self.isPluginDir:
            return '[PluginsDir]'

        return '[ProjectAssetsDir]'

