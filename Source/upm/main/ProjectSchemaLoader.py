
import re
import os
from upm.config.ConfigIni import ConfigIni

from upm.util.Assert import *
from upm.util.PlatformUtil import Platforms
import upm.util.Util as Util
import upm.ioc.Container as Container
from upm.ioc.Inject import Inject
import upm.ioc.IocAssertions as Assertions
import upm.util.JunctionUtil as JunctionUtil

RequiredDependencies = ["Projeny"]

class ProjectSchemaLoader:
    _varMgr = Inject('VarManager')
    _log = Inject('Logger')

    def loadSchema(self, name, platform):
        schemaPath = self._varMgr.expandPath('[UnityProjectsDir]/{0}/project.ini'.format(name))
        schemaPathCustom = self._varMgr.expandPath('[UnityProjectsDir]/{0}/projectCustom.ini'.format(name))
        schemaPathCustomGlobal = self._varMgr.expandPath('[UnityProjectsDir]/projectCustom.ini')

        self._log.debug('Loading schema at path "{0}"'.format(schemaPath))
        config = ConfigIni([schemaPath, schemaPathCustom, schemaPathCustomGlobal])

        pluginDependencies = config.getList('Config', 'packagesPlugins')
        scriptsDependencies = config.getList('Config', 'packages')
        customProjects = config.getList('Config', 'solutionProjects')
        customFolders = config.getTuples('ProjectFolders')
        prebuiltProjects = config.getList('Config', 'prebuilt')

        # Check for duplicates
        Util.ensureNoDuplicates(scriptsDependencies, 'scriptsDependencies')
        Util.ensureNoDuplicates(pluginDependencies, 'pluginDependencies')

        for packageName in pluginDependencies:
            assertThat(not packageName in scriptsDependencies, "Found package '{0}' in both scripts and plugins.  Must be in only one or the other".format(packageName))

        for dependName in RequiredDependencies:
            assertThat(not dependName in scriptsDependencies)

            if not dependName in pluginDependencies:
                pluginDependencies.append(dependName)

        allDependencies = pluginDependencies + scriptsDependencies

        packageMap = {}

        # Resolve all dependencies for each package
        # by default, put any dependencies that are not declared explicitly into the plugins folder
        for packageName in allDependencies:

            configPath = self._varMgr.expandPath('[UnityPackagesDir]/{0}/package.ini'.format(packageName))

            if os.path.exists(configPath):
                packageConfig = ConfigIni([configPath])
            else:
                packageConfig = ConfigIni([])

            createCustomVsProject = self._checkCustomProjectMatch(packageName, customProjects)

            isPluginsDir = packageName in pluginDependencies

            if isPluginsDir:
                assertThat(not packageName in scriptsDependencies)
            else:
                assertThat(packageName in scriptsDependencies)

            if packageConfig.getBool('Config', 'ForceAssetsDirectory', False):
                isPluginsDir = False

            explicitDependencies = packageConfig.getList('Config', 'Dependencies')

            forcePluginsDir = packageConfig.getBool('Config', 'ForcePluginsDirectory', False)

            assertThat(not packageName in packageMap)
            packageMap[packageName] = PackageInfo(isPluginsDir, packageName, packageConfig, createCustomVsProject, explicitDependencies, forcePluginsDir)

            for dependName in explicitDependencies:
                if not dependName in allDependencies:
                    pluginDependencies.append(dependName)
                    # Yes, python is ok with changing allDependencies even while iterating over it
                    allDependencies.append(dependName)

            for dependName in packageConfig.getList('Config', 'Extras'):
                if not dependName in allDependencies:
                    if isPluginsDir:
                        pluginDependencies.append(dependName)
                    else:
                        scriptsDependencies.append(dependName)
                    # Yes, python is ok with changing allDependencies even while iterating over it
                    allDependencies.append(dependName)

        self._removePlatformSpecificPackages(packageMap, platform)

        self._printDependencyTree(packageMap)

        self._fillOutDependencies(packageMap)

        for customProj in customProjects:
            assertThat(customProj.startswith('/') or customProj in allDependencies, 'Given project "{0}" in schema is not included in either "scripts" or "plugins"'.format(customProj))

        self._addPrebuiltProjectsFromPackages(packageMap, prebuiltProjects)

        self._log.info('Found {0} packages in total for given schema'.format(len(allDependencies)))
        self._log.debug('Finished processing schema, found {0} dependencies in total'.format(', '.join(allDependencies)))

        # In Unity, the plugins folder can not have any dependencies on anything in the scripts folder
        # So if dependencies exist then just automatically move those packages to the scripts folder
        self._ensurePluginPackagesDoNotHaveDependenciesInAssets(packageMap)

        self._ensurePackagesThatAreNotProjectsDoNotHaveProjectDependencies(packageMap)

        projectsDir = self._varMgr.expandPath('[UnityProjectsDir]')
        prebuiltProjectPaths = [os.path.normpath(os.path.join(projectsDir, x)) for x in prebuiltProjects]

        for info in packageMap.values():
            if info.forcePluginsDir and not info.isPluginDir:
                assertThat(False, "Package '{0}' must be in plugins directory".format(info.name))

        return ProjectSchema(name, packageMap, customFolders, prebuiltProjectPaths)

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

    def _addPrebuiltProjectsFromPackages(self, packageMap, prebuiltProjects):
        for info in packageMap.values():
            prebuiltPaths = info.config.getList('Config', 'Prebuilt')

            for path in prebuiltPaths:
                if path not in prebuiltProjects:
                    prebuiltProjects.append(path)

    def _removePlatformSpecificPackages(self, packageMap, platform):

        for info in list(packageMap.values()):

            if info.folderType == FolderTypes.AndroidProject or info.folderType == FolderTypes.AndroidLibraries:
                platforms = [Platforms.Android]
            elif info.folderType == FolderTypes.Ios:
                platforms = [Platforms.Ios]
            elif info.folderType == FolderTypes.WebGl:
                platforms = [Platforms.WebGl]
            else:
                platforms = info.config.getList('Config', 'Platforms')

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
            if projPattern.startswith('/') and re.match(projPattern[1:], packageName):
                return True

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
    def __init__(self, name, packages, customFolderMap, prebuiltProjects):
        self.name = name
        self.packages = packages
        self.customFolderMap = customFolderMap
        self.prebuiltProjects = prebuiltProjects

class FolderTypes:
    Normal = "normal"
    WebGl = "webgl"
    AndroidProject = "androidproject"
    AndroidLibraries = "androidlibraries"
    Ios = "ios"
    StreamingAssets = "streamingassets"

class PackageInfo:
    def __init__(self, isPluginDir, name, config, createCustomVsProject, explicitDependencies, forcePluginsDir):
        self.isPluginDir = isPluginDir
        self.name = name
        self.explicitDependencies = explicitDependencies
        self.config = config
        self.createCustomVsProject = createCustomVsProject
        self.allDependencies = None
        self.folderType = self._getFolderTypeFromString(config.getString('Config', 'FolderType', ''))
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

