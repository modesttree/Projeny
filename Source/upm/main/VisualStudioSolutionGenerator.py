
import xml.etree.ElementTree as ET
from xml.dom import minidom

import uuid
import re
import os
import sys

from upm.config.ConfigIni import ConfigIni

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
from mtm.ioc.Inject import InjectMany
import mtm.ioc.Assertions as Assertions

CsProjXmlNs = 'http://schemas.microsoft.com/developer/msbuild/2003'
NsPrefix = '{' + CsProjXmlNs + '}'

CsProjTypeGuid = 'FAE04EC0-301F-11D3-BF4B-00C04F79EFBC'
SolutionFolderTypeGuid = '2150E333-8FDC-42A3-9474-1A3956D46DE8'
EditorProjectNameSuffix = "-editor"

UpmDirectoryIgnorePattern = re.compile(r'.*Assets\\Plugins\\Projeny\\.*')

class VisualStudioSolutionGenerator:
    """
    Handler for creating custom visual studio solutions based on project.ini files
    """
    _log = Inject('Logger')
    _packageManager = Inject('PackageManager')
    _schemaLoader = Inject('ProjectSchemaLoader')
    _unityHelper = Inject('UnityHelper')
    _config = Inject('Config')
    _varMgr = Inject('VarManager')
    _sys = Inject('SystemHelper')

    def updateVisualStudioSolution(self, projectName, platform):
        self._log.heading('Updating Visual Studio solution for project "{0}"'.format(projectName))

        self._packageManager.setPathsForProject(projectName, platform)
        self._packageManager.checkProjectInitialized(projectName, platform)

        schema = self._schemaLoader.loadSchema(projectName, platform)
        self._updateProjects(schema)

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
                assert hintPathElem.tag == '{0}HintPath'.format(NsPrefix)

                hintPath = hintPathElem.text.replace('/', '\\')

            if hintPath:
                if not os.path.isabs(hintPath):
                    hintPath = self._varMgr.expandPath('[ProjectPlatformRoot]/{0}'.format(hintPath))

                assert self._sys.fileExists(hintPath), "Expected to find file at '{0}'".format(hintPath)

            items.append(RefInfo(name, hintPath))

        return items

    def _chooseMostRecentFile(self, path1, path2):
        path1 = self._varMgr.expandPath(path1)
        path2 = self._varMgr.expandPath(path2)

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
            '[UnityGeneratedProjectPath]', '[UnityGeneratedProjectPath2]')

        unityEditorProjPath = self._chooseMostRecentFile(
            '[UnityGeneratedProjectEditorPath]', '[UnityGeneratedProjectEditorPath2]')

        assert unityProjPath and self._sys.fileExists(unityProjPath) and unityEditorProjPath and self._sys.fileExists(unityEditorProjPath), \
            'Could not find unity-generated project when generating custom solution.  This is necessary so the custom solution can add things like unity defines and DLL references within the unity project.'

        unityProjRoot = ET.parse(unityProjPath)
        unityProjEditorRoot = ET.parse(unityEditorProjPath)

        unityDefines = self._getDefineConstantsElement(unityProjRoot)

        unityRefItems = self._getUnityProjectReferencesItems(unityProjRoot)
        unityRefItemsEditor = self._getUnityProjectReferencesItems(unityProjEditorRoot)

        return unityDefines, unityRefItems, unityRefItemsEditor

    def _updateProjects(self, schema):

        # Necessary to avoid having ns0: prefixes everywhere on output
        ET.register_namespace('', 'http://schemas.microsoft.com/developer/msbuild/2003')

        unityDefines, unityRefItems, unityRefItemsEditor = self._parseGeneratedUnityProject()

        excludeDirs = []
        includedProjects = []
        allCustomProjects = {}

        pluginsProj = self._createStandardCsProjInfo('Packages-Plugins', '[PluginsDir]')
        includedProjects.append(pluginsProj)

        scriptsEditorProj = self._createStandardCsProjInfo('Packages-Editor', '[ProjectAssetsDir]')
        includedProjects.append(scriptsEditorProj)

        scriptsProj = self._createStandardCsProjInfo('Packages', '[ProjectAssetsDir]')
        includedProjects.append(scriptsProj)

        pluginsEditorProj = self._createStandardCsProjInfo('Packages-Plugins-Editor', '[PluginsDir]')
        includedProjects.append(pluginsEditorProj)

        allPackages = schema.packages.values()

        prebuiltProjectInfos = self._createPrebuiltProjInfo(schema.prebuiltProjects)

        customEditorProjects = []
        customAssetsProjects = []

        # Need to populate allCustomProjects first so we can get references in _tryCreateCustomProject
        for packageInfo in allPackages:
            if packageInfo.createCustomVsProject:
                customProject = self._createCsProjInfo(packageInfo, False)
                allCustomProjects[customProject.name] = customProject
                customAssetsProjects.append(customProject)

                customEditorProject = self._createCsProjInfo(packageInfo, True)
                allCustomProjects[customEditorProject.name] = customEditorProject
                customEditorProjects.append(customEditorProject)

        for packageInfo in allPackages:
            if packageInfo.createCustomVsProject:
                self._log.debug('Processing project "{0}"'.format(packageInfo.name))

                customProject = self._tryCreateCustomProject( \
                     False, allCustomProjects, packageInfo, unityRefItems, unityRefItemsEditor, unityDefines, excludeDirs, scriptsProj, pluginsProj, scriptsEditorProj, pluginsEditorProj, prebuiltProjectInfos)

                if customProject:
                    includedProjects.append(customProject)

                customEditorProject = self._tryCreateCustomProject( \
                     True, allCustomProjects, packageInfo, unityRefItems, unityRefItemsEditor, unityDefines, excludeDirs, scriptsProj, pluginsProj, scriptsEditorProj, pluginsEditorProj, prebuiltProjectInfos)

                if customEditorProject:
                    includedProjects.append(customEditorProject)

        self._log.debug('Processing project "{0}"'.format(pluginsProj.name))

        pluginsProj.dependencies = prebuiltProjectInfos
        self._createStandardCsProjForDirectory(pluginsProj, excludeDirs, unityRefItems, unityDefines, False, prebuiltProjectInfos)

        pluginsEditorProj.dependencies = [pluginsProj] + prebuiltProjectInfos

        for packageInfo in allPackages:
            if packageInfo.createCustomVsProject and packageInfo.isPluginDir:
                pluginsEditorProj.dependencies.append(allCustomProjects[packageInfo.name])

        self._log.debug('Processing project "{0}"'.format(pluginsEditorProj.name))
        self._createStandardCsProjForDirectory(pluginsEditorProj, excludeDirs, unityRefItemsEditor, unityDefines, True, prebuiltProjectInfos)

        excludeDirs.append(self._varMgr.expandPath('[PluginsDir]'))

        self._log.debug('Processing project "{0}"'.format(scriptsProj.name))
        scriptsProj.dependencies = [pluginsProj] + prebuiltProjectInfos
        self._createStandardCsProjForDirectory(scriptsProj, excludeDirs, unityRefItems, unityDefines, False, prebuiltProjectInfos)

        scriptsEditorProj.dependencies = scriptsProj.dependencies + [scriptsProj, pluginsEditorProj]

        self._log.debug('Processing project "{0}"'.format(scriptsEditorProj.name))
        self._createStandardCsProjForDirectory(scriptsEditorProj, excludeDirs, unityRefItemsEditor, unityDefines, True, prebuiltProjectInfos)

        self._ensureNoMatchingPrebuiltCsProjNames(includedProjects, prebuiltProjectInfos)

        for prebuiltProj in prebuiltProjectInfos:
            includedProjects.append(prebuiltProj)

        self._createSolution(includedProjects, schema.customFolderMap)

    def _ensureNoMatchingPrebuiltCsProjNames(self, projects, prebuiltProjects):
        for proj in projects:
            for prebuiltProj in prebuiltProjects:
                assert prebuiltProj.name != proj.name, 'Found prebuilt project and normal project both using the same name "{0}".  This will not work in the same visual studio solution'.format(prebuiltProj.name)

    def _createPrebuiltProjInfo(self, prebuiltProjects):

        processedList = []

        for projAbsPath in prebuiltProjects:
            projId = self._getProjectIdFromFile(projAbsPath)
            projName = os.path.splitext(os.path.basename(projAbsPath))[0]

            processedList.append(CsProjInfo(projId, projAbsPath, projName, True, [], False))

        return processedList

    def _getFolderName(self, packageName, customFolders):

        for folderName, pattern in customFolders:
            if packageName == pattern or (pattern.startswith('/') and re.match(pattern[1:], packageName)):
                return folderName

        return None

    def _createCsProjInfo(self, packageInfo, isEditor):

        projId = self._createProjectGuid()
        outputDir = self._varMgr.expandPath(packageInfo.outputDirVar)

        csProjectName = packageInfo.name

        if isEditor:
            csProjectName += EditorProjectNameSuffix

        outputPath = os.path.join(outputDir, csProjectName + ".csproj")

        packageDir = os.path.join(outputDir, packageInfo.name)

        files = []
        self._addCsFilesInDirectory(packageDir, [], files, isEditor)

        isIgnored = (len(files) == 0)

        return CsProjInfo(projId, outputPath, csProjectName, False, files, isIgnored)

    def _tryCreateCustomProject(self, isEditor, customCsProjects, packageInfo, unityRefItems, unityRefItemsEditor, defines, excludeDirs, scriptsProj, pluginsProj, scriptsEditorProj, pluginsEditorProj, prebuiltProjects):

        projName = packageInfo.name

        if isEditor:
            projName += EditorProjectNameSuffix

        csProjInfo = customCsProjects[projName]

        outputDir = os.path.dirname(csProjInfo.absPath)
        packageDir = os.path.join(outputDir, packageInfo.name)
        excludeDirs.append(packageDir)

        if csProjInfo.isIgnored:
            return None

        projDependencies = []

        if isEditor:
            projDependencies.append(pluginsProj)
            projDependencies.append(pluginsEditorProj)

            projDependencies.append(customCsProjects[packageInfo.name])

            if not packageInfo.isPluginDir:
                projDependencies.append(scriptsProj)
                projDependencies.append(scriptsEditorProj)
        else:
            projDependencies.append(pluginsProj)

            if not packageInfo.isPluginDir:
                projDependencies.append(scriptsProj)

        for dependName in packageInfo.allDependencies:
            assert not dependName in projDependencies

            if dependName in customCsProjects:
                dependProj = customCsProjects[dependName]

                if not dependProj.isIgnored:
                    projDependencies.append(dependProj)

            if isEditor:
                dependEditorName = dependName + EditorProjectNameSuffix

                if dependEditorName in customCsProjects:
                    dependEditorProj = customCsProjects[dependEditorName]

                    if not dependEditorProj.isIgnored:
                        projDependencies.append(dependEditorProj)

        if isEditor:
            refItems = unityRefItemsEditor
        else:
            refItems = unityRefItems

        csProjInfo.dependencies = list(projDependencies)

        for proj in prebuiltProjects:
            csProjInfo.dependencies.append(proj)

        self._writeCsProject(csProjInfo, csProjInfo.files, refItems, defines, prebuiltProjects)

        return csProjInfo

    def _getProjectIdFromFile(self, projPath):

        doc = ET.parse(projPath)
        root = doc.getroot()

        projId = root.findall('./{0}PropertyGroup/{0}ProjectGuid'.format(NsPrefix))[0].text
        return re.match('^{(.*)}$', projId).groups()[0]

    def _createSolution(self, projects, customFolderMap):

        with open(self._varMgr.expandPath('[CsSolutionTemplate]'), 'r', encoding='utf-8', errors='ignore') as inputFile:
            solutionStr = inputFile.read()

        outputPath = self._varMgr.expandPath('[SolutionPath]')
        outputDir = os.path.dirname(outputPath)

        projectList = ''
        postSolution = ''
        projectFolderStr = ''
        projectFolderMapsStr = ''

        folderIds = {}

        folderNames = [x[0] for x in customFolderMap]

        for folderName in folderNames:
            folderId = self._createProjectGuid()
            folderIds[folderName] = folderId
            projectFolderStr += 'Project("{{{0}}}") = "{1}", "{2}", "{{{3}}}"\nEndProject\n' \
                .format(SolutionFolderTypeGuid, folderName, folderName, folderId)

        for proj in projects:
            assert proj.name
            assert proj.id
            assert proj.absPath

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

            buildConfig = 'Unity Debug' if proj.isPrebuild else 'Debug'

            postSolution += \
                '\t\t{{{0}}}.Debug|Any CPU.ActiveCfg = {1}|Any CPU\n\t\t{{{0}}}.Debug|Any CPU.Build.0 = {1}|Any CPU' \
                .format(proj.id, buildConfig)

            folderName = self._getFolderName(proj.name, customFolderMap)

            if folderName:
                folderId = folderIds[folderName]

                if len(projectFolderMapsStr) != 0:
                    projectFolderMapsStr += '\n'

                projectFolderMapsStr += \
                    '\t\t{{{0}}} = {{{1}}}' \
                    .format(proj.id, folderId)

        solutionStr = solutionStr.replace('[ProjectList]', projectList).replace('[PostSolution]', postSolution) \
            .replace('[ProjectFolders]', projectFolderStr)

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

        return CsProjInfo(projId, outputPath, projectName, False, [], False)

    def _createStandardCsProjForDirectory(self, projInfo, excludeDirs, unityRefItems, defines, isEditor, prebuiltProjects):

        outputDir = os.path.dirname(projInfo.absPath)

        files = []
        self._addCsFilesInDirectory(outputDir, excludeDirs, files, isEditor)

        if len(files) == 0:
            projInfo.isIgnored = True
            return

        self._writeCsProject(projInfo, files, unityRefItems, defines, prebuiltProjects)

    def _createProjectGuid(self):
        return str(uuid.uuid4()).upper()

    def _shouldReferenceBeCopyLocal(self, refName):
        return refName != 'System' and refName != 'System.Core'

    def _writeCsProject(self, projInfo, files, refItems, defines, prebuiltProjects):

        outputDir = os.path.dirname(projInfo.absPath)

        doc = ET.parse(self._varMgr.expandPath('[CsProjectTemplate]'))

        root = doc.getroot()
        self._stripWhitespace(root)

        refsItemGroupElem = root.findall('./{0}ItemGroup[{0}Reference]'.format(NsPrefix))[0]
        refsItemGroupElem.clear()

        # Add reference items given from unity project
        for refInfo in refItems:

            if len([x for x in prebuiltProjects if x.name == refInfo.name]) > 0:
                #self._log.debug('Ignoring reference for prebuilt project "{0}"'.format(refInfo.name))
                continue

            refElem = ET.SubElement(refsItemGroupElem, 'Reference')
            refElem.set('Include', refInfo.name)

            if refInfo.hintPath:
                refPath = refInfo.hintPath
                assert os.path.isabs(refPath), "Invalid path '{0}'".format(refPath)

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
            .text = projInfo.name

        root.findall('./{0}PropertyGroup/{0}ProjectGuid'.format(NsPrefix))[0] \
            .text = '{' + projInfo.id + '}'

        root.findall('./{0}PropertyGroup/{0}OutputPath'.format(NsPrefix))[0] \
            .text = os.path.relpath(self._varMgr.expandPath('[ProjectPlatformRoot]/Bin'), outputDir)

        root.findall('./{0}PropertyGroup/{0}AssemblyName'.format(NsPrefix))[0] \
            .text = projInfo.name

        root.findall('./{0}PropertyGroup/{0}RootNamespace'.format(NsPrefix))[0] \
            .text = "ModestTree"

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
        if self._config.getBool('Projeny', 'IncludeProjenyInGeneratedSolution', False):
            return False

        return UpmDirectoryIgnorePattern.match(fullPath)

    def _addCsFilesInDirectory(self, dirPath, excludeDirs, files, isForEditor):
        isInsideEditorFolder = re.match(r'.*\\Editor($|\\).*', dirPath)

        if not isForEditor and isInsideEditorFolder:
            return

        if dirPath in excludeDirs:
            return

        #self._log.debug('Processing ' + dirPath)

        for excludeDir in excludeDirs:
            assert not dirPath.startswith(excludeDir + "\\")

        if not self._sys.directoryExists(dirPath):
            return

        for itemName in os.listdir(dirPath):
            fullPath = os.path.join(dirPath, itemName)

            if self._shouldIgnoreCsProjFile(fullPath):
                continue

            if os.path.isdir(fullPath):
                self._addCsFilesInDirectory(fullPath, excludeDirs, files, isForEditor)
            else:
                if re.match('.*\.(cs|txt)$', itemName):
                    if not isForEditor or isInsideEditorFolder:
                        files.append(fullPath)

class RefInfo:
    def __init__(self, name, hintPath):
        self.name = name
        self.hintPath = hintPath

class CsProjInfo:
    def __init__(self, id, absPath, name, isPrebuild, files, isIgnored):
        assert name

        self.id = id
        self.absPath = absPath
        self.name = name
        self.isPrebuild = isPrebuild
        self.dependencies = []
        self.files = files
        self.isIgnored = isIgnored

