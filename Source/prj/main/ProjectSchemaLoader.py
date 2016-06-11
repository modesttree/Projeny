
import sys
import re
import os

from mtm.util.Assert import *
from mtm.util.Platforms import Platforms
import mtm.util.Util as Util
import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.IocAssertions as Assertions
import mtm.util.JunctionUtil as JunctionUtil
from mtm.config.Config import Config
from mtm.config.YamlConfigLoader import loadYamlFilesThatExist

from prj.main.CsProjAnalyzer import NsPrefix, CsProjAnalyzer
from prj.main.ProjenyConstants import ProjectConfigFileName, PackageConfigFileName, ProjectUserConfigFileName
from prj.main.ProjectConfig import ProjectConfig

from collections import OrderedDict
import xml.etree.ElementTree as ET

class ProjectSchemaLoader:
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')
    _sys = Inject('SystemHelper')

    def loadSchema(self, name, platform):
        try:
            return self._loadSchemaInternal(name, platform)
        except Exception as e:
            raise Exception("Failed while processing config yaml for project '{0}' (platform '{1}'). Details: {2}".format(name, platform, str(e))) from e

    def loadProjectConfig(self, name):
        schemaPath = self._varMgr.expandPath('[UnityProjectsDir]/{0}/{1}'.format(name, ProjectConfigFileName))
        schemaPathUser = self._varMgr.expandPath('[UnityProjectsDir]/{0}/{1}'.format(name, ProjectUserConfigFileName))
        schemaPathGlobal = self._varMgr.expandPath('[UnityProjectsDir]/{0}'.format(ProjectConfigFileName))
        schemaPathUserGlobal = self._varMgr.expandPath('[UnityProjectsDir]/{0}'.format(ProjectUserConfigFileName))

        self._log.debug('Loading schema at path "{0}"'.format(schemaPath))
        yamlConfig = Config(loadYamlFilesThatExist(schemaPath, schemaPathUser, schemaPathGlobal, schemaPathUserGlobal))

        config = ProjectConfig()

        config.pluginsFolder = yamlConfig.tryGetList([], 'PluginsFolder')
        config.assetsFolder = yamlConfig.tryGetList([], 'AssetsFolder')
        config.solutionProjects = yamlConfig.tryGetList([], 'SolutionProjects')
        config.targetPlatforms = yamlConfig.tryGetList([Platforms.Windows], 'TargetPlatforms')
        config.solutionFolders = yamlConfig.tryGetOrderedDictionary(OrderedDict(), 'SolutionFolders')
        config.packageFolders = yamlConfig.getList('PackageFolders')
        config.projectSettingsPath = yamlConfig.getString('ProjectSettingsPath')

        # Remove duplicates
        config.assetsFolder = list(set(config.assetsFolder))
        config.pluginsFolder = list(set(config.pluginsFolder))

        for packageName in config.pluginsFolder:
            assertThat(not packageName in config.assetsFolder, "Found package '{0}' in both scripts and plugins.  Must be in only one or the other".format(packageName))

        return config

    def _loadSchemaInternal(self, name, platform):

        config = self.loadProjectConfig(name)

        # Search all the given packages and any new packages that are dependencies and create PackageInfo() objects for each
        packageMap = self._getAllPackageInfos(config, platform)

        self._addGroupedDependenciesAsExplicitDependencies(packageMap)

        self._ensurePrebuiltProjectsHaveNoScripts(packageMap)

        self._ensurePrebuiltProjectDependenciesArePrebuilt(packageMap)

        # We have all the package infos, but we don't know which packages depend on what so calculate that
        self._calculateDependencyListForEachPackage(packageMap)

        # For the pre-built assembly projects, if we add one of them to our solution,
        # then we need to add all the pre-built dependencies, since unlike generated projects
        # we can't make the prebuilt projects use the dll directly
        self._ensureVisiblePrebuiltProjectHaveVisibleDependencies(packageMap)

        self._printDependencyTree(packageMap)

        for customProj in config.solutionProjects:
            assertThat(customProj.startswith('/') or customProj in packageMap, 'Given project "{0}" in schema is not included in either "scripts" or "plugins"'.format(customProj))

        self._log.debug('Found {0} packages in total for given schema'.format(len(packageMap)))

        # In Unity, the plugins folder can not have any dependencies on anything in the scripts folder
        # So if dependencies exist then just automatically move those packages to the scripts folder
        self._ensurePluginPackagesDoNotHaveDependenciesInAssets(packageMap)

        self._ensurePackagesThatAreNotProjectsDoNotHaveProjectDependencies(packageMap)

        for info in packageMap.values():
            if info.forcePluginsDir and not info.isPluginDir:
                assertThat(False, "Package '{0}' must be in plugins directory".format(info.name))

        self._ensureAllPackagesExist(packageMap)

        return ProjectSchema(name, packageMap, config.solutionFolders, config.projectSettingsPath)

    def _shouldIncludeForPlatform(self, packageName, packageConfig, folderType, platform):

        if folderType == FolderTypes.AndroidProject or folderType == FolderTypes.AndroidLibraries:
            allowedPlatforms = [Platforms.Android]
        elif folderType == FolderTypes.Ios:
            allowedPlatforms = [Platforms.Ios]
        elif folderType == FolderTypes.WebGl:
            allowedPlatforms = [Platforms.WebGl]
        else:
            allowedPlatforms = packageConfig.tryGetList([], 'Platforms')

            if len(allowedPlatforms) == 0:
                return True

        if platform not in allowedPlatforms:
            self._log.debug("Skipped project '{0}' since it is not enabled for platform '{1}'".format(packageName, platform))
            return False

        return True

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

    def _getAllPackageInfos(self, projectConfig, platform):
        configRefDesc = "'{0}' or '{1}'".format(ProjectConfigFileName, ProjectUserConfigFileName)
        allPackageRefs = [PackageReference(x, configRefDesc) for x in projectConfig.pluginsFolder + projectConfig.assetsFolder]

        packageMap = {}

        # Resolve all dependencies for each package
        # by default, put any dependencies that are not declared explicitly into the plugins folder
        for packageRef in allPackageRefs:

            packageName = packageRef.name
            packageDir = None

            for packageFolder in projectConfig.packageFolders:
                candidatePackageDir = os.path.join(packageFolder, packageName)

                if self._sys.directoryExists(candidatePackageDir):
                    packageDir = self._varMgr.expandPath(candidatePackageDir)
                    break

            assertIsNotNone(packageDir, "Could not find package '{0}' in any of the package directories!  Referenced in {1}", packageName, packageRef.sourceDesc)

            configPath = os.path.join(packageDir, PackageConfigFileName)

            if os.path.exists(configPath):
                packageConfig = Config(loadYamlFilesThatExist(configPath))
            else:
                packageConfig = Config([])

            folderType = self._getFolderTypeFromString(packageConfig.tryGetString('', 'FolderType'))

            if not self._shouldIncludeForPlatform(packageName, packageConfig, folderType, platform):
                continue

            createCustomVsProject = self._shouldCreateVsProjectForName(packageName, projectConfig.solutionProjects)

            isPluginsDir = True

            if packageName in projectConfig.assetsFolder:
                assertThat(not packageName in projectConfig.pluginsFolder)
                isPluginsDir = False

            if packageConfig.tryGetBool(False, 'ForceAssetsDirectory'):
                isPluginsDir = False

            explicitDependencies = packageConfig.tryGetList([], 'Dependencies')

            forcePluginsDir = packageConfig.tryGetBool(False, 'ForcePluginsDirectory')

            assemblyProjInfo = self._tryGetAssemblyProjectInfo(packageConfig, packageName)

            sourceDesc = '"{0}"'.format(configPath)

            if assemblyProjInfo != None:
                for assemblyDependName in assemblyProjInfo.dependencies:
                    if assemblyDependName not in [x.name for x in allPackageRefs]:
                        allPackageRefs.append(PackageReference(assemblyDependName, sourceDesc))

                explicitDependencies += assemblyProjInfo.dependencies

            groupedDependencies = packageConfig.tryGetList([], 'GroupWith')
            extraDependencies = packageConfig.tryGetList([], 'Extras')

            assertThat(not packageName in packageMap, "Found duplicate package with name '{0}'", packageName)

            packageMap[packageName] = PackageInfo(
                isPluginsDir, packageName, packageConfig, createCustomVsProject,
                explicitDependencies, forcePluginsDir, folderType, assemblyProjInfo, packageDir, groupedDependencies)

            for dependName in (explicitDependencies + groupedDependencies + extraDependencies):
                if dependName not in [x.name for x in allPackageRefs]:
                    # Yes, python is ok with changing allPackageRefs even while iterating over it
                    allPackageRefs.append(PackageReference(dependName, sourceDesc))

        return packageMap

    def _tryGetAssemblyProjectInfo(self, packageConfig, packageName):
        assemblyProjectRelativePath = packageConfig.tryGetString(None, 'AssemblyProject', 'Path')

        if assemblyProjectRelativePath == None:
            return None

        projFullPath = self._varMgr.expand(assemblyProjectRelativePath)

        if not os.path.isabs(projFullPath):
            projFullPath = os.path.join(packageDir, assemblyProjectRelativePath)

        assertThat(self._sys.fileExists(projFullPath), "Expected to find file at '{0}'.", projFullPath)

        projAnalyzer = CsProjAnalyzer(projFullPath)

        assemblyName = projAnalyzer.getAssemblyName()
        assertThat(assemblyName == '$(MSBuildProjectName)' or assemblyName.lower() == packageName.lower(), 'Packages that represent assembly projects must have the same name as the assembly')

        assertIsEqual(self._sys.getFileNameWithoutExtension(projFullPath).lower(), packageName.lower(),
          'Assembly projects must have the same name as their package')

        projConfig = packageConfig.tryGetString(None, 'AssemblyProject', 'Config')
        dependencies = projAnalyzer.getProjectReferences()

        return AssemblyProjectInfo(
            projFullPath, projAnalyzer.root, projConfig, dependencies)

    def getDependenciesFromCsProj(self, projectRoot):
        result = []
        for projRef in projectRoot.findall('./{0}ItemGroup/{0}ProjectReference/{0}Name'.format(NsPrefix)):
            result.append(projRef.text)
        return result

    def _ensureAllPackagesExist(self, packageMap):
        for package in packageMap.values():
            assertThat(self._sys.directoryExists(package.dirPath),
               "Could not find directory for package '{0}'", package.name)

    def _ensureVisiblePrebuiltProjectHaveVisibleDependencies(self, packageMap):
        for package in packageMap.values():
            if package.assemblyProjectInfo != None and package.createCustomVsProject:
                self._makeAllPrebuiltDependenciesVisible(package, packageMap)

    def _makeAllPrebuiltDependenciesVisible(self, package, packageMap):
        for dependName in package.explicitDependencies:
            depend = packageMap[dependName]

            if not depend.createCustomVsProject:
                depend.createCustomVsProject = True
                self._makeAllPrebuiltDependenciesVisible(depend, packageMap)

    def _ensurePrebuiltProjectDependenciesArePrebuilt(self, packageMap):
        for packageInfo in packageMap.values():
            assInfo = packageInfo.assemblyProjectInfo

            if assInfo == None:
                continue

            for dependName in assInfo.dependencies:
                depend = packageMap[dependName]
                assertThat(depend.assemblyProjectInfo != None,
                   "Expected package '{0}' to have an assembly project defined, since another assembly project ({1}) depends on it", dependName, packageInfo.name)

    def _ensurePrebuiltProjectsHaveNoScripts(self, packageMap):
        for package in packageMap.values():
            if package.assemblyProjectInfo != None:
                assertThat(not any(self._sys.findFilesByPattern(package.dirPath, '*.cs')),
                   "Found C# scripts in assembly project '{0}'.  This is not allowed - please move to a separate package.", package.name)

    def _ensurePackagesThatAreNotProjectsDoNotHaveProjectDependencies(self, packageMap):
        changedOne = True

        while changedOne:
            changedOne = False

            for info in packageMap.values():
                if not info.createCustomVsProject and self._hasVsProjectDependency(info, packageMap):
                    info.createCustomVsProject = True
                    self._log.debug('Created visual studio project for {0} package even though it wasnt marked as one, because it has csproj dependencies'.format(info.name))
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
                    self._log.debug('Moved {0} package to scripts folder since it has dependencies there and therefore cannot be in plugins'.format(info.name))
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

    def _shouldCreateVsProjectForName(self, packageName, solutionProjects):
        if packageName in solutionProjects:
            return True

        # Allow regex's!
        for projPattern in solutionProjects:
            if projPattern.startswith('/'):
                projPattern = projPattern[1:]
                try:
                    if re.match(projPattern, packageName):
                        return True
                except Exception as e:
                    raise Exception("Failed while parsing project regex '/{0}' from {1}/{2}.  Details: {3}".format(projPattern, self._varMgr.expand('ProjectName'), ProjectConfigFileName, str(e)))

        return False

    def _addGroupedDependenciesAsExplicitDependencies(self, packageMap):

        # There is a bug here where it won't handle grouped dependencies within grouped dependencies
        for info in packageMap.values():
            extras = set()

            for explicitDependName in info.explicitDependencies:
                if explicitDependName not in packageMap:
                    continue

                explicitDependInfo = packageMap[explicitDependName]

                for groupedDependName in explicitDependInfo.groupedDependencies:
                    if info.name != groupedDependName:
                        extras.add(groupedDependName)

            info.explicitDependencies += list(extras)

    def _calculateDependencyListForEachPackage(self, packageMap):

        self._log.debug('Processing dependency tree')

        inProgress = set()

        for info in packageMap.values():
            self._calculateDependencyListForPackage(info, packageMap, inProgress)

    def _calculateDependencyListForPackage(self, packageInfo, packageMap, inProgress):

        if packageInfo.name in inProgress:
            assertThat(False, "Found circular dependency when processing package {0}.  Dependency list: {1}".format(packageInfo.name, ' -> '.join([x for x in inProgress]) + '-> ' + packageInfo.name))

        inProgress.add(packageInfo.name)
        allDependencies = set(packageInfo.explicitDependencies)

        for explicitDependName in packageInfo.explicitDependencies:
            if explicitDependName not in packageMap:
                # This can happen if a package depends on another package that is platform specific
                continue

            explicitDependInfo = packageMap[explicitDependName]

            if explicitDependInfo.allDependencies == None:
                self._calculateDependencyListForPackage(explicitDependInfo, packageMap, inProgress)

            for dependName in explicitDependInfo.allDependencies:
                allDependencies.add(dependName)

        packageInfo.allDependencies = list(allDependencies)
        inProgress.remove(packageInfo.name)

class PackageReference:
    def __init__(self, name, sourceDesc):
        self.name = name
        self.sourceDesc = sourceDesc

class ProjectSchema:
    def __init__(self, name, packages, customFolderMap, projectSettingsPath):
        self.name = name
        self.packages = packages
        self.customFolderMap = customFolderMap
        self.projectSettingsPath = projectSettingsPath

class FolderTypes:
    Normal = "normal"
    WebGl = "webgl"
    AndroidProject = "androidproject"
    AndroidLibraries = "androidlibraries"
    Ios = "ios"
    StreamingAssets = "streamingassets"

class AssemblyProjectInfo:
    def __init__(self, path, root, config, dependencies):
        self.path = path
        self.root = root
        self.config = config
        self.dependencies = dependencies

class PackageInfo:
    def __init__(
        self, isPluginDir, name, config, createCustomVsProject,
        explicitDependencies, forcePluginsDir, folderType, assemblyProjectInfo, dirPath, groupedDependencies):

        self.isPluginDir = isPluginDir
        self.name = name
        self.explicitDependencies = explicitDependencies
        self.config = config
        self.createCustomVsProject = createCustomVsProject
        self.allDependencies = None
        self.folderType = folderType
        self.assemblyProjectInfo = assemblyProjectInfo
        self.forcePluginsDir = forcePluginsDir
        self.dirPath = dirPath
        self.groupedDependencies = groupedDependencies

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


