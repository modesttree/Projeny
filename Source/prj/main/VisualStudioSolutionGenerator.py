
import xml.etree.ElementTree as ET
from xml.dom import minidom

import uuid
import re
import os

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
from mtm.ioc.Inject import InjectMany
import mtm.ioc.IocAssertions as Assertions
from mtm.util.Assert import *
from prj.main.ProjenyConstants import ProjectConfigFileName, PackageConfigFileName, ProjectUserConfigFileName

from prj.main.CsProjAnalyzer import NsPrefix

CsProjTypeGuid = 'FAE04EC0-301F-11D3-BF4B-00C04F79EFBC'
SolutionFolderTypeGuid = '2150E333-8FDC-42A3-9474-1A3956D46DE8'
EditorProjectNameSuffix = "-editor"

ProjenyDirectoryIgnorePattern = re.compile(r'.*Assets\\Plugins\\Projeny\\.*')
ProjenyGeneratedDirectoryIgnorePattern = re.compile(r'.*Assets\\Plugins\\ProjenyGenerated\\.*')

PluginsProjectName = 'PluginsFolder'
AssetsProjectName = 'AssetsFolder'
AssetsEditorProjectName = 'AssetsFolder-Editor'
PluginsEditorProjectName = 'PluginsFolder-Editor'

class VisualStudioSolutionGenerator:
    """
    Handler for creating custom visual studio solutions based on ProjenyProject.yaml files
    """
    _log = Inject('Logger')
    _packageManager = Inject('PackageManager')
    _schemaLoader = Inject('ProjectSchemaLoader')
    _unityHelper = Inject('UnityHelper')
    _config = Inject('Config')
    _varMgr = Inject('VarManager')
    _sys = Inject('SystemHelper')

    def updateVisualStudioSolution(self, projectName, platform):
        with self._log.heading('Updating Visual Studio solution for project "{0}"'.format(projectName)):
            self._packageManager.setPathsForProjectPlatform(projectName, platform)
            self._packageManager.checkProjectInitialized(projectName, platform)

            schema = self._schemaLoader.loadSchema(projectName, platform)

            self._updateVisualStudioSolutionInternal(
                schema.packages.values(), schema.customFolderMap)

    def _prettify(self, doc):
        return minidom.parseString(ET.tostring(doc)).toprettyxml(indent="    ")

    def _getDefineConstantsElement(self, root):
        return root.findall('.//{0}DefineConstants'.format(NsPrefix))[0].text

    def _getUnityProjectReferencesItems(self, root):
        items = []
        refElems = root.findall('./{0}ItemGroup/{0}Reference'.format(NsPrefix))

        for refElem in refElems:
            name = refElem.get('Include')
            children = refElem.getchildren()

            hintPath = None

            if len(children) > 0:
                hintPathElem = children[0]
                assertThat(hintPathElem.tag == '{0}HintPath'.format(NsPrefix))

                hintPath = hintPathElem.text.replace('/', '\\')

            if hintPath:
                if not os.path.isabs(hintPath):
                    hintPath = self._varMgr.expandPath('[ProjectPlatformRoot]/{0}'.format(hintPath))

                assertThat(self._sys.fileExists(hintPath), "Expected to find file at '{0}'.  Try updating the unity generated solution, the assembly references might be out of date.".format(hintPath))

            items.append(RefInfo(name, hintPath))

        return items

    def _chooseMostRecentFile(self, path1, path2, path3):
        path1 = self._varMgr.expandPath(path1)
        path2 = self._varMgr.expandPath(path2)
        path3 = self._varMgr.expandPath(path3)

        if self._sys.fileExists(path3):
            return path3

        # If they both exist choose most recent
        if self._sys.fileExists(path1) and self._sys.fileExists(path2):
            modtime1 = os.path.getmtime(path1)
            modtime2 = os.path.getmtime(path2)

            if modtime1 > modtime2:
                return path1

            return path2

        if self._sys.fileExists(path1):
            return path1

        if self._sys.fileExists(path2):
            return path2

        return None

    def _parseGeneratedUnityProject(self):

        # Annoyingly, unity does generate the solution using different paths
        # depending on settings
        # If visual studio is set to external editor, it names it the first one
        # and otherwise it names it the second one
        # So check modification times for the case where the user changes this setting
        unityProjPath = self._chooseMostRecentFile(
            '[UnityGeneratedProjectPath]', '[UnityGeneratedProjectPath2]', '[UnityGeneratedProjectPath3]')

        unityEditorProjPath = self._chooseMostRecentFile(
            '[UnityGeneratedProjectEditorPath]', '[UnityGeneratedProjectEditorPath2]', '[UnityGeneratedProjectEditorPath3]')

        assertThat(unityProjPath and self._sys.fileExists(unityProjPath) and unityEditorProjPath and self._sys.fileExists(unityEditorProjPath), \
            'Could not find unity-generated project when generating custom solution.  This is necessary so the custom solution can add things like unity defines and DLL references within the unity project.')

        unityProjRoot = ET.parse(unityProjPath)
        unityProjEditorRoot = ET.parse(unityEditorProjPath)

        defines = self._getDefineConstantsElement(unityProjRoot)
        references = self._getUnityProjectReferencesItems(unityProjRoot)
        referencesEditor = self._getUnityProjectReferencesItems(unityProjEditorRoot)

        return UnityGeneratedProjInfo(defines, references, referencesEditor)

    def _updateVisualStudioSolutionInternal(self, allPackages, customFolderMap):

        # Necessary to avoid having ns0: prefixes everywhere on output
        ET.register_namespace('', 'http://schemas.microsoft.com/developer/msbuild/2003')

        unifyProjInfo = self._parseGeneratedUnityProject()

        projectMap = self._createProjectMap(allPackages)

        self._initDependenciesForAllProjects(
            allPackages, projectMap, unifyProjInfo)

        self._addFilesForAllProjects(
            projectMap, unifyProjInfo)

        self._writeCsProjFiles(
            projectMap, unifyProjInfo)

        self._createSolution(projectMap.values(), customFolderMap)

    def _createProjectMap(self, allPackages):
        projectMap = {}
        self._addStandardProjects(projectMap)
        self._addCustomProjects(allPackages, projectMap)
        return projectMap

    def _addStandardProjects(self, projectMap):
        projectMap[PluginsProjectName] = self._createStandardCsProjInfo(
            PluginsProjectName, '[PluginsDir]')

        projectMap[AssetsProjectName] = self._createStandardCsProjInfo(
            AssetsProjectName, '[ProjectAssetsDir]')

        projectMap[AssetsEditorProjectName] = self._createStandardCsProjInfo(
            AssetsEditorProjectName, '[ProjectAssetsDir]')

        projectMap[PluginsEditorProjectName] = self._createStandardCsProjInfo(
            PluginsEditorProjectName, '[PluginsDir]')

    def _addFilesForAllProjects(
        self, projectMap, unifyProjInfo):

        excludeDirs = []

        for projInfo in projectMap.values():
            if projInfo.packageInfo != None:
                packageDir = self._varMgr.expandPath(os.path.join(projInfo.packageInfo.outputDirVar, projInfo.packageInfo.name))
                excludeDirs.append(packageDir)

        self._initFilesForStandardCsProjForDirectory(
            projectMap[PluginsEditorProjectName], excludeDirs, unifyProjInfo, True)

        self._initFilesForStandardCsProjForDirectory(
            projectMap[PluginsProjectName], excludeDirs, unifyProjInfo, False)

        excludeDirs.append(self._varMgr.expandPath('[PluginsDir]'))

        self._initFilesForStandardCsProjForDirectory(
            projectMap[AssetsProjectName], excludeDirs, unifyProjInfo, False)

        self._initFilesForStandardCsProjForDirectory(
            projectMap[AssetsEditorProjectName], excludeDirs, unifyProjInfo, True)

    def _writeCsProjFiles(
        self, projectMap, unifyProjInfo):

        for projInfo in projectMap.values():
            if projInfo.projectType != ProjectType.Custom and projInfo.projectType != ProjectType.CustomEditor:
                continue

            if projInfo.projectType == ProjectType.CustomEditor:
                refItems = unifyProjInfo.referencesEditor
            else:
                refItems = unifyProjInfo.references

            self._writeCsProject(projInfo, projectMap, projInfo.files, refItems, unifyProjInfo.defines)

        self._writeStandardCsProjForDirectory(
            projectMap[PluginsEditorProjectName], projectMap, unifyProjInfo, True)

        self._writeStandardCsProjForDirectory(
            projectMap[PluginsProjectName], projectMap, unifyProjInfo, False)

        self._writeStandardCsProjForDirectory(
            projectMap[AssetsProjectName], projectMap, unifyProjInfo, False)

        self._writeStandardCsProjForDirectory(
            projectMap[AssetsEditorProjectName], projectMap, unifyProjInfo, True)

    def _initDependenciesForAllProjects(
        self, allPackages, projectMap, unifyProjInfo):

        for projInfo in projectMap.values():
            if projInfo.projectType != ProjectType.Custom and projInfo.projectType != ProjectType.CustomEditor:
                continue

            assertThat(projInfo.packageInfo.createCustomVsProject)

            self._log.debug('Processing generated project "{0}"'.format(projInfo.name))

            projInfo.dependencies = self._getProjectDependencies(projectMap, projInfo)

        pluginsProj = projectMap[PluginsProjectName]
        self._log.debug('Processing project "{0}"'.format(pluginsProj.name))

        prebuiltProjectInfos = [x for x in projectMap.values() if x.projectType == ProjectType.Prebuilt]

        pluginsProj.dependencies = prebuiltProjectInfos

        pluginsEditorProj = projectMap[PluginsEditorProjectName]
        pluginsEditorProj.dependencies = [pluginsProj] + prebuiltProjectInfos

        for packageInfo in allPackages:
            if packageInfo.createCustomVsProject and packageInfo.isPluginDir:
                pluginsEditorProj.dependencies.append(projectMap[packageInfo.name])

        scriptsProj = projectMap[AssetsProjectName]

        self._log.debug('Processing project "{0}"'.format(scriptsProj.name))
        scriptsProj.dependencies = [pluginsProj] + prebuiltProjectInfos

        scriptsEditorProj = projectMap[AssetsEditorProjectName]
        scriptsEditorProj.dependencies = scriptsProj.dependencies + [scriptsProj, pluginsEditorProj]

    def _addCustomProjects(
        self, allPackages, allCustomProjects):

        for packageInfo in allPackages:
            if not packageInfo.createCustomVsProject:
                continue

            if packageInfo.assemblyProjectInfo == None:
                customProject = self._createGeneratedCsProjInfo(packageInfo, False)
                allCustomProjects[customProject.name] = customProject

                customEditorProject = self._createGeneratedCsProjInfo(packageInfo, True)
                allCustomProjects[customEditorProject.name] = customEditorProject
            else:
                projId = self._getCsProjIdFromFile(packageInfo.assemblyProjectInfo.root)
                customProject = CsProjInfo(
                    projId, packageInfo.assemblyProjectInfo.path, packageInfo.name,
                    [], False, packageInfo.assemblyProjectInfo.config, ProjectType.Prebuilt, packageInfo)
                allCustomProjects[customProject.name] = customProject

    def _getCsProjIdFromFile(self, projectRoot):
        projId = projectRoot.findall('./{0}PropertyGroup/{0}ProjectGuid'.format(NsPrefix))[0].text
        return re.match('^{(.*)}$', projId).groups()[0]

    def _getFolderName(self, packageName, customFolders):

        for item in customFolders.items():
            folderName = item[0]
            pattern = item[1]
            if packageName == pattern or (pattern.startswith('/') and re.match(pattern[1:], packageName)):
                return folderName

        return None

    def _createGeneratedCsProjInfo(self, packageInfo, isEditor):

        projId = self._createProjectGuid()
        outputDir = self._varMgr.expandPath(packageInfo.outputDirVar)

        csProjectName = packageInfo.name

        if isEditor:
            csProjectName += EditorProjectNameSuffix

        outputPath = os.path.join(outputDir, csProjectName + ".csproj")

        packageDir = os.path.join(outputDir, packageInfo.name)

        files = []
        self._addCsFilesInDirectory(packageDir, [], files, isEditor, True)

        isIgnored = (len(files) == 0 or (len(files) == 1 and os.path.basename(files[0]) == PackageConfigFileName))

        return CsProjInfo(
            projId, outputPath, csProjectName, files, isIgnored, None, ProjectType.CustomEditor if isEditor else ProjectType.Custom, packageInfo)

    def _getProjectDependencies(self, projectMap, projInfo):

        packageInfo = projInfo.packageInfo
        assertIsNotNone(packageInfo)

        projDependencies = []

        isEditor = projInfo.projectType == ProjectType.CustomEditor

        if isEditor:
            projDependencies.append(projectMap[PluginsProjectName])
            projDependencies.append(projectMap[PluginsEditorProjectName])

            projDependencies.append(projectMap[packageInfo.name])

            if not packageInfo.isPluginDir:
                projDependencies.append(projectMap[AssetsProjectName])
                projDependencies.append(projectMap[AssetsEditorProjectName])
        else:
            projDependencies.append(projectMap[PluginsProjectName])

            if not packageInfo.isPluginDir:
                projDependencies.append(projectMap[AssetsProjectName])

        for dependName in packageInfo.allDependencies:
            assertThat(not dependName in projDependencies)

            if dependName in projectMap:
                dependProj = projectMap[dependName]

                projDependencies.append(dependProj)

            if isEditor:
                dependEditorName = dependName + EditorProjectNameSuffix

                if dependEditorName in projectMap:
                    dependEditorProj = projectMap[dependEditorName]

                    projDependencies.append(dependEditorProj)

        return projDependencies

    def _createSolution(self, projects, customFolderMap):

        with open(self._varMgr.expandPath('[CsSolutionTemplate]'), 'r', encoding='utf-8', errors='ignore') as inputFile:
            solutionStr = inputFile.read()

        outputPath = self._varMgr.expandPath('[SolutionPath]')
        outputDir = os.path.dirname(outputPath)

        projectList = ''
        postSolution = ''
        projectFolderMapsStr = ''

        folderIds = {}

        usedFolders = set()

        for folderName in customFolderMap:
            folderId = self._createProjectGuid()
            folderIds[folderName] = folderId

        for proj in projects:
            assertThat(proj.name)
            assertThat(proj.id)
            assertThat(proj.absPath)

            if proj.isIgnored:
                continue

            projectList += 'Project("{{{0}}}") = "{1}", "{2}", "{{{3}}}"\n' \
                .format(CsProjTypeGuid, proj.name, os.path.relpath(proj.absPath, outputDir), proj.id)

            if len(proj.dependencies) > 0:
                projectList += '\tProjectSection(ProjectDependencies) = postProject\n'
                for projDepend in proj.dependencies:
                    if not projDepend.isIgnored:
                        projectList += '\t\t{{{0}}} = {{{0}}}\n'.format(projDepend.id)
                projectList += '\tEndProjectSection\n'

            projectList += 'EndProject\n'

            if len(postSolution) != 0:
                postSolution += '\n'

            if proj.configType != None:
                buildConfig = proj.configType
            else:
                buildConfig = 'Debug'

            postSolution += \
                '\t\t{{{0}}}.Debug|Any CPU.ActiveCfg = {1}|Any CPU\n\t\t{{{0}}}.Debug|Any CPU.Build.0 = {1}|Any CPU' \
                .format(proj.id, buildConfig)

            folderName = self._getFolderName(proj.name, customFolderMap)

            if folderName:
                usedFolders.add(folderName)

                folderId = folderIds[folderName]

                if len(projectFolderMapsStr) != 0:
                    projectFolderMapsStr += '\n'

                projectFolderMapsStr += \
                    '\t\t{{{0}}} = {{{1}}}' \
                    .format(proj.id, folderId)

        projectFolderStr = ''
        for folderName, folderId in folderIds.items():
            if folderName in usedFolders:
                projectFolderStr += 'Project("{{{0}}}") = "{1}", "{2}", "{{{3}}}"\nEndProject\n' \
                    .format(SolutionFolderTypeGuid, folderName, folderName, folderId)

        solutionStr = solutionStr.replace('[ProjectList]', projectList)

        if len(postSolution.strip()) > 0:
            solutionStr = solutionStr.replace('[PostSolution]', """
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
{0}
    EndGlobalSection""".format(postSolution))
        else:
            solutionStr = solutionStr.replace('[PostSolution]', '')

        solutionStr = solutionStr.replace('[ProjectFolders]', projectFolderStr)

        if len(projectFolderMapsStr) > 0:
            fullStr = '\tGlobalSection(NestedProjects) = preSolution\n{0}\n\tEndGlobalSection'.format(projectFolderMapsStr)
            solutionStr = solutionStr.replace('[ProjectFolderMaps]', fullStr)
        else:
            solutionStr = solutionStr.replace('[ProjectFolderMaps]', '')

        with open(outputPath, 'w', encoding='utf-8', errors='ignore') as outFile:
            outFile.write(solutionStr)

        self._log.debug('Saved new solution file at "{0}"'.format(outputPath))

    def _createStandardCsProjInfo(self, projectName, outputDir):

        outputDir = self._varMgr.expandPath(outputDir)
        outputPath = os.path.join(outputDir, projectName + ".csproj")

        projId = self._createProjectGuid()

        return CsProjInfo(
            projId, outputPath, projectName, [], False, None, ProjectType.Standard, None)

    def _initFilesForStandardCsProjForDirectory(
        self, projInfo, excludeDirs, unityProjInfo, isEditor):

        outputDir = os.path.dirname(projInfo.absPath)

        projInfo.files = []
        self._addCsFilesInDirectory(outputDir, excludeDirs, projInfo.files, isEditor, False)

        # If it only contains the project config file then ignore it
        if len([x for x in projInfo.files if not x.endswith('.yaml')]) == 0:
            projInfo.isIgnored = True

    def _writeStandardCsProjForDirectory(
        self, projInfo, projectMap, unityProjInfo, isEditor):

        if projInfo.isIgnored:
            return

        if isEditor:
            references = unityProjInfo.referencesEditor
        else:
            references = unityProjInfo.references

        self._writeCsProject(
            projInfo, projectMap, projInfo.files, references, unityProjInfo.defines)

    def _createProjectGuid(self):
        return str(uuid.uuid4()).upper()

    def _shouldReferenceBeCopyLocal(self, refName):
        return refName != 'System' and refName != 'System.Core'

    def _writeCsProject(self, projInfo, projectMap, files, refItems, defines):

        outputDir = os.path.dirname(projInfo.absPath)

        doc = ET.parse(self._varMgr.expandPath('[CsProjectTemplate]'))

        root = doc.getroot()
        self._stripWhitespace(root)

        refsItemGroupElem = root.findall('./{0}ItemGroup[{0}Reference]'.format(NsPrefix))[0]
        refsItemGroupElem.clear()

        prebuiltProjectInfos = [x for x in projectMap.values() if x.projectType == ProjectType.Prebuilt]

        # Add reference items given from unity project
        for refInfo in refItems:

            if any([x for x in prebuiltProjectInfos if x.name == refInfo.name]):
                self._log.debug('Ignoring reference for prebuilt project "{0}"'.format(refInfo.name))
                continue

            refElem = ET.SubElement(refsItemGroupElem, 'Reference')
            refElem.set('Include', refInfo.name)

            if refInfo.hintPath:
                refPath = refInfo.hintPath
                assertThat(os.path.isabs(refPath), "Invalid path '{0}'".format(refPath))

                if refPath.startswith(outputDir):
                    refPath = os.path.relpath(refPath, outputDir)

                hintPathElem = ET.SubElement(refElem, 'HintPath')
                hintPathElem.text = refPath

            ET.SubElement(refElem, 'Private').text = 'True' if self._shouldReferenceBeCopyLocal(refInfo.name) else 'False'

        # Add cs files 'compile' items
        filesItemGroupElem = root.findall('./{0}ItemGroup[{0}Compile]'.format(NsPrefix))[0]
        filesItemGroupElem.clear()

        for filePath in files:
            if filePath.endswith('.cs'):
                compileElem = ET.SubElement(filesItemGroupElem, 'Compile')
            else:
                compileElem = ET.SubElement(filesItemGroupElem, 'None')

            compileElem.set('Include', os.path.relpath(filePath, outputDir))

        root.findall('./{0}PropertyGroup/{0}RootNamespace'.format(NsPrefix))[0] \
            .text = self._config.tryGetString('', 'SolutionGeneration', 'RootNamespace')

        root.findall('./{0}PropertyGroup/{0}ProjectGuid'.format(NsPrefix))[0] \
            .text = '{' + projInfo.id + '}'

        root.findall('./{0}PropertyGroup/{0}OutputPath'.format(NsPrefix))[0] \
            .text = os.path.relpath(self._varMgr.expandPath('[ProjectPlatformRoot]/Bin'), outputDir)

        root.findall('./{0}PropertyGroup/{0}AssemblyName'.format(NsPrefix))[0] \
            .text = projInfo.name

        root.findall('./{0}PropertyGroup/{0}DefineConstants'.format(NsPrefix))[0] \
            .text = defines

        tempFilesDir = os.path.relpath(self._varMgr.expandPath('[IntermediateFilesDir]'), outputDir)

        root.findall('./{0}PropertyGroup/{0}IntermediateOutputPath'.format(NsPrefix))[0] \
            .text = tempFilesDir

        root.findall('./{0}PropertyGroup/{0}BaseIntermediateOutputPath'.format(NsPrefix))[0] \
            .text = tempFilesDir

        # Add project references
        projectRefGroupElem = root.findall('./{0}ItemGroup[{0}ProjectReference]'.format(NsPrefix))[0]
        projectRefGroupElem.clear()

        for dependInfo in projInfo.dependencies:
            if dependInfo.isIgnored:
                continue

            projectRefElem = ET.SubElement(projectRefGroupElem, 'ProjectReference')
            projectRefElem.set('Include', os.path.relpath(dependInfo.absPath, outputDir))

            ET.SubElement(projectRefElem, 'Project').text = '{' + dependInfo.id + '}'
            ET.SubElement(projectRefElem, 'Name').text = dependInfo.name

        self._sys.makeMissingDirectoriesInPath(projInfo.absPath)

        with open(projInfo.absPath, 'w', encoding='utf-8', errors='ignore') as outputFile:
            outputFile.write(self._prettify(root))

    def _stripWhitespace(self, elem):
        for x in ET.ElementTree(elem).getiterator():
            if x.text: x.text = x.text.strip()
            if x.tail: x.tail = x.tail.strip()

    def _prettify(self, doc):
        return minidom.parseString(ET.tostring(doc)).toprettyxml(indent="    ")

    def _shouldIgnoreCsProjFile(self, fullPath):

        if ProjenyGeneratedDirectoryIgnorePattern.match(fullPath):
            # Never include the generated stuff
            return True

        return ProjenyDirectoryIgnorePattern.match(fullPath)

    def _addCsFilesInDirectory(self, dirPath, excludeDirs, files, isForEditor, includeYaml):
        isInsideEditorFolder = re.match(r'.*\\Editor($|\\).*', dirPath)

        if not isForEditor and isInsideEditorFolder:
            return

        if dirPath in excludeDirs:
            return

        #self._log.debug('Processing ' + dirPath)

        for excludeDir in excludeDirs:
            assertThat(not dirPath.startswith(excludeDir + "\\"))

        if not self._sys.directoryExists(dirPath):
            return

        for itemName in os.listdir(dirPath):
            fullPath = os.path.join(dirPath, itemName)

            if self._shouldIgnoreCsProjFile(fullPath):
                continue

            if os.path.isdir(fullPath):
                self._addCsFilesInDirectory(fullPath, excludeDirs, files, isForEditor, includeYaml)
            else:
                if itemName.endswith('.cs') or itemName.endswith('.txt') or (includeYaml and itemName.endswith('.yaml')):
                    if not isForEditor or isInsideEditorFolder or itemName == PackageConfigFileName:
                        files.append(fullPath)

class RefInfo:
    def __init__(self, name, hintPath):
        self.name = name
        self.hintPath = hintPath

class UnityGeneratedProjInfo:
    def __init__(self, defines, references, referencesEditor):
        self.defines = defines
        self.references = references
        self.referencesEditor = referencesEditor

class ProjectType:
    Prebuilt = 1
    Custom = 2
    CustomEditor = 3
    Standard = 4

class CsProjInfo:
    def __init__(self, id, absPath, name, files, isIgnored, configType, projectType, packageInfo):
        assertThat(name)

        self.id = id
        self.absPath = absPath
        self.name = name
        self.dependencies = []
        self.files = files
        self.isIgnored = isIgnored
        self.configType = configType
        self.projectType = projectType
        self.packageInfo = packageInfo

