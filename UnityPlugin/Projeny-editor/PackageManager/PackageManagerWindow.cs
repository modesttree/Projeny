using System;
using System.IO;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Projeny.Internal;
using System.Linq;

namespace Projeny
{
    public class PackageManagerWindow : EditorWindow
    {
        DraggableList _installedList;
        DraggableList _releasesList;
        DraggableList _assetsList;
        DraggableList _pluginsList;

        List<PackageInfo> _allPackages;
        List<ReleaseInfo> _allReleases;

        PackageManagerWindowSkin _skin;
        ProjectConfigTypes _projectConfigType;

        ViewStates _viewState = ViewStates.PackagesAndProject;
        GUIStyle _buttonStyle;
        GUIStyle _toggleStyle;

        float PaneSplitPercent
        {
            get
            {
                return 0.5f;
            }
        }

        public GUIStyle ButtonStyle
        {
            get
            {
                if (_buttonStyle == null)
                {
                    _buttonStyle = new GUIStyle(EditorStyles.miniButtonMid);
                }

                _buttonStyle.fontSize = _skin.ButtonFontSize;

                return _buttonStyle;
            }
        }

        public GUIStyle ToggleStyle
        {
            get
            {
                if (_toggleStyle == null)
                {
                    _toggleStyle = new GUIStyle(EditorStyles.miniButtonMid);
                }

                _toggleStyle.fontSize = _skin.ButtonFontSize;

                return _toggleStyle;
            }
        }

        void OnEnable()
        {
            _buttonStyle = null;
            _toggleStyle = null;

            _skin = Resources.Load<PackageManagerWindowSkin>("Projeny/PackageManagerSkin");

            if (_skin.DropdownTextStyle == null)
            {
                _skin.DropdownTextStyle = new GUIStyle();
            }

            if (_skin.FileDropdownLabelTextStyle == null)
            {
                _skin.FileDropdownLabelTextStyle = new GUIStyle();
            }

            if (_skin.FileDropdownEditFileButtonTextStyle == null)
            {
                _skin.FileDropdownEditFileButtonTextStyle = new GUIStyle();
            }

            if (_allPackages == null)
            {
                _allPackages = new List<PackageInfo>();
            }

            if (_installedList == null)
            {
                _installedList = new DraggableList();
            }

            if (_releasesList == null)
            {
                _releasesList = new DraggableList();
            }

            if (_assetsList == null)
            {
                _assetsList = new DraggableList();
            }

            if (_pluginsList == null)
            {
                _pluginsList = new DraggableList();
            }
        }

        void Update()
        {
            // Doesn't seem worth trying to detect changes, just redraw every frame
            Repaint();
        }

        Rect GetRightSideRect(Rect windowRect)
        {
            var startX = PaneSplitPercent * windowRect.width + 0.5f * _skin.ListVerticalSpacing;
            var endX = windowRect.width - _skin.MarginRight;
            var startY = _skin.ViewToggleHeight + _skin.MarginTop;
            var endY = windowRect.height - _skin.MarginBottom;

            return Rect.MinMaxRect(startX, startY, endX, endY);
        }

        Rect GetLeftSideRect(Rect windowRect)
        {
            var startX = _skin.MarginLeft;
            var endX = PaneSplitPercent * windowRect.width - 0.5f * _skin.ListVerticalSpacing;
            var startY = _skin.ViewToggleHeight + _skin.MarginTop;
            var endY = windowRect.yMax - _skin.MarginBottom;

            return Rect.MinMaxRect(startX, startY, endX, endY);
        }

        void DrawPackagesPane(Rect windowRect)
        {
            Rect rect;
            if (_viewState == ViewStates.PackagesAndProject)
            {
                rect = GetLeftSideRect(windowRect);
            }
            else
            {
                Assert.IsEqual(_viewState, ViewStates.ReleasesAndPackages);
                rect = GetRightSideRect(windowRect);
            }

            DrawPackagesPane2(rect);
        }

        void DrawPackagesPane2(Rect rect)
        {
            var startX = rect.xMin;
            var endX = rect.xMax;
            var startY = rect.yMin;
            var endY = startY + _skin.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Packages", _skin.HeaderTextStyle);

            startY = endY;
            endY = rect.yMax - _skin.ApplyButtonHeight - _skin.ApplyButtonTopPadding;

            _installedList.Draw(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + _skin.ApplyButtonTopPadding;
            endY = rect.yMax;

            if (GUI.Button(Rect.MinMaxRect(startX, startY, endX, endY), "Refresh", ButtonStyle))
            {
                RefreshPackages();
            }
        }

        void DrawProjectPane2(Rect rect)
        {
            var startX = rect.xMin;
            var endX = rect.xMax;
            var startY = rect.yMin;
            var endY = startY + _skin.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Project Settings", _skin.HeaderTextStyle);

            startY = endY;
            endY = startY + _skin.FileDropdownHeight;

            DrawFileDropdown(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY;
            endY = startY + _skin.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Assets Folder", _skin.HeaderTextStyle);

            startY = endY;
            endY = rect.yMax - _skin.ApplyButtonHeight - _skin.ApplyButtonTopPadding;

            DrawProjectPane3(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + _skin.ApplyButtonTopPadding;
            endY = rect.yMax;

            DrawButtons(Rect.MinMaxRect(startX, startY, endX, endY));
        }

        void DrawProjectPane3(Rect listRect)
        {
            var halfHeight = 0.5f * listRect.height;

            var rect1 = new Rect(listRect.x, listRect.y, listRect.width, halfHeight - 0.5f * _skin.ListHorizontalSpacing);
            var rect2 = new Rect(listRect.x, listRect.y + halfHeight + 0.5f * _skin.ListHorizontalSpacing, listRect.width, listRect.height - halfHeight - 0.5f * _skin.ListHorizontalSpacing);

            _assetsList.Draw(rect1);
            _pluginsList.Draw(rect2);

            GUI.Label(Rect.MinMaxRect(rect1.xMin, rect1.yMax, rect1.xMax, rect2.yMin), "Plugins Folder", _skin.HeaderTextStyle);
        }

        void DrawButtons(Rect rect)
        {
            var halfWidth = rect.width * 0.5f;
            var padding = 0.5f * _skin.ProjectButtonsPadding;

            if (GUI.Button(Rect.MinMaxRect(rect.x + halfWidth + padding, rect.y, rect.xMax, rect.yMax), "Apply", ButtonStyle))
            {
                OverwriteConfig();
                ProjenyEditorUtil.UpdateLinks();
            }
        }

        void RefreshReleases()
        {
            _allReleases = ProjenyEditorUtil.LookupReleaseList();

            UpdateAvailableReleasesList();
        }

        void UpdateAvailableReleasesList()
        {
            _releasesList.Clear();
            _releasesList.AddRange(
                _allReleases.Select(x => x.Title).Where(x => !_installedList.Values.Contains(x)));
        }

        void RefreshPackages()
        {
            _allPackages = ProjenyEditorUtil.LookupPackagesList();

            UpdateAvailablePackagesList();
        }

        void UpdateAvailablePackagesList()
        {
            _installedList.Clear();
            _installedList.AddRange(_allPackages
                .Select(x => x.Name).Where(x => !_assetsList.Values.Contains(x) && !_pluginsList.Values.Contains(x)));
        }

        void RefreshProject()
        {
            var configPath = GetProjectConfigPath();

            if (File.Exists(configPath))
            {
                var project = YamlSerializer.Deserialize<ProjectConfig>(File.ReadAllText(configPath));

                // Null when file is empty
                if (project == null)
                {
                    ClearProjectLists();
                }
                else
                {
                    PopulateListsFromConfig(project);
                }
            }
            else
            {
                ClearProjectLists();
            }
        }

        string GetProjectConfigPath()
        {
            var projectRootDir = Path.Combine(Application.dataPath, "../..");
            var unityProjectsDir = Path.Combine(projectRootDir, "..");

            switch (_projectConfigType)
            {
                case ProjectConfigTypes.LocalProject:
                {
                    return Path.Combine(projectRootDir, ProjenyEditorUtil.ProjectConfigFileName);
                }
                case ProjectConfigTypes.LocalProjectUser:
                {
                    return Path.Combine(projectRootDir, ProjenyEditorUtil.ProjectConfigUserFileName);
                }
                case ProjectConfigTypes.AllProjects:
                {
                    return Path.Combine(unityProjectsDir, ProjenyEditorUtil.ProjectConfigFileName);
                }
                case ProjectConfigTypes.AllProjectsUser:
                {
                    return Path.Combine(unityProjectsDir, ProjenyEditorUtil.ProjectConfigUserFileName);
                }
            }

            return null;
        }

        void ClearProjectLists()
        {
            _pluginsList.Clear();
            _assetsList.Clear();

            UpdateAvailablePackagesList();
        }

        void PopulateListsFromConfig(ProjectConfig config)
        {
            _pluginsList.Clear();
            _assetsList.Clear();

            _assetsList.AddRange(config.Packages);
            _pluginsList.AddRange(config.PluginPackages);

            UpdateAvailablePackagesList();
        }

        void OverwriteConfig()
        {
            File.WriteAllText(GetProjectConfigPath(), SerializeProjectConfig());
        }

        bool HasProjectConfigChanged()
        {
            var configPath = GetProjectConfigPath();

            var newYamlStr = SerializeProjectConfig();

            if (!File.Exists(configPath))
            {
                return newYamlStr.Trim().Length > 0;
            }

            var currentYamlStr = File.ReadAllText(configPath);

            return newYamlStr != currentYamlStr;
        }

        string SerializeProjectConfig()
        {
            var config = new ProjectConfig();

            config.Packages = _assetsList.Values.ToList();
            config.PluginPackages = _pluginsList.Values.ToList();

            return YamlSerializer.Serialize<ProjectConfig>(config);
        }

        void TryChangeProjectType(ProjectConfigTypes configType)
        {
            if (HasProjectConfigChanged())
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "Project Changes Detected",
                    "Do you want to save config changes?",
                    "Save", "Don't Save", "Cancel");

                switch (choice)
                {
                    case 0:
                    {
                        OverwriteConfig();
                        break;
                    }
                    case 1:
                    {
                        // Do nothing
                        break;
                    }
                    case 2:
                    {
                        return;
                    }
                    default:
                    {
                        Assert.Throw();
                        break;
                    }
                }
            }

            _projectConfigType = configType;
            RefreshProject();
        }

        void DrawFileDropdown(Rect rect)
        {
            var dropDownRect = Rect.MinMaxRect(
                rect.xMin + _skin.FileSelectLabelWidth,
                rect.yMin,
                rect.xMax - _skin.FileDropdownOpenFileButtonWidth - _skin.FileDropdownOpenFileButtonLeftPadding - _skin.FileDropdownSaveFileButtonLeftPadding - _skin.FileDropdownSaveFileButtonWidth,
                rect.yMax);

            ImguiUtil.DrawColoredQuad(dropDownRect, _skin.FileDropdownBackgroundColor);

            GUI.DrawTexture(new Rect(dropDownRect.right - _skin.ArrowSize.x + _skin.ArrowOffset.x, dropDownRect.top + _skin.ArrowOffset.y, _skin.ArrowSize.x, _skin.ArrowSize.y), _skin.FileDropdownArrow);

            var desiredConfigType = (ProjectConfigTypes)EditorGUI.Popup(dropDownRect, (int)_projectConfigType, GetConfigTypesDisplayValues(), _skin.DropdownTextStyle);

            var labelRect = Rect.MinMaxRect(rect.xMin, rect.yMin, _skin.FileSelectLabelWidth, rect.yMax);
            GUI.Label(labelRect, "File:", _skin.FileDropdownLabelTextStyle);

            if (desiredConfigType != _projectConfigType)
            {
                TryChangeProjectType(desiredConfigType);
            }

            if (Event.current.type == EventType.Repaint)
            {
                Graphics.DrawTexture(dropDownRect, _skin.FileDropdownBackground, _skin.FileDropdownBorder,
                _skin.FileDropdownBorder, _skin.FileDropdownBorder, _skin.FileDropdownBorder);
            }

            var saveButtonRect = new Rect(
                dropDownRect.xMax + _skin.FileDropdownSaveFileButtonLeftPadding,
                dropDownRect.yMin,
                _skin.FileDropdownSaveFileButtonWidth,
                dropDownRect.height);

            if (GUI.Button(saveButtonRect, "Save", ButtonStyle))
            {
                OverwriteConfig();
            }

            var openButtonRect = Rect.MinMaxRect(rect.xMax - _skin.FileDropdownOpenFileButtonWidth, rect.yMin, rect.xMax, rect.yMax);

            var configPath = GetProjectConfigPath();
            GUI.enabled = File.Exists(configPath);
            if (GUI.Button(openButtonRect, "Open", ButtonStyle))
            {
                InternalEditorUtility.OpenFileAtLineExternal(configPath, 1);
            }
            GUI.enabled = true;
        }

        string[] GetConfigTypesDisplayValues()
        {
            return new[]
            {
                ProjenyEditorUtil.ProjectConfigFileName,
                ProjenyEditorUtil.ProjectConfigUserFileName,
                ProjenyEditorUtil.ProjectConfigFileName + " (global)",
                ProjenyEditorUtil.ProjectConfigUserFileName + " (global)",
            };
        }

        void DrawViewSelect(Rect rect)
        {
            var split1 = rect.width / 3.0f;
            var split2 = rect.width * 2.0f / 3.0f;

            var rect1 = Rect.MinMaxRect(rect.xMin + _skin.ViewSelectSpacing, rect.yMin, split1 - _skin.ViewSelectSpacing, rect.yMax);
            var rect2 = Rect.MinMaxRect(split1 + _skin.ViewSelectSpacing, rect.yMin, split2 - _skin.ViewSelectSpacing, rect.yMax);
            var rect3 = Rect.MinMaxRect(split2 + _skin.ViewSelectSpacing, rect.yMin, rect.xMax - _skin.ViewSelectSpacing, rect.yMax);

            bool isOn;

            isOn = _viewState == ViewStates.ReleasesAndPackages;
            GUI.contentColor = isOn ? Color.black : Color.white;
            if (GUI.Toggle(rect1, isOn, "Releases", ToggleStyle))
            {
                _viewState = ViewStates.ReleasesAndPackages;
            }
            GUI.contentColor = Color.white;

            isOn = _viewState == ViewStates.PackagesAndProject;
            GUI.contentColor = isOn ? Color.black : Color.white;
            if (GUI.Toggle(rect2, isOn, "Packages", ToggleStyle))
            {
                _viewState = ViewStates.PackagesAndProject;
            }
            GUI.contentColor = Color.white;

            isOn = _viewState == ViewStates.Project;
            GUI.contentColor = isOn ? Color.black : Color.white;
            if (GUI.Toggle(rect3, isOn, "Project", ToggleStyle))
            {
                _viewState = ViewStates.Project;
            }
            GUI.contentColor = Color.white;
        }

        public void OnGUI()
        {
            // I tried using the GUILayout / EditorGUILayout but found it incredibly frustrating
            // and confusing, so I decided to just draw using raw rect coordinates instead :|

            var windowRect = new Rect(0, 0, this.position.width, this.position.height);

            var viewSelectRect = new Rect(0, _skin.MarginTop, windowRect.width, _skin.ViewToggleHeight);
            DrawViewSelect(viewSelectRect);

            if (_viewState == ViewStates.PackagesAndProject || _viewState == ViewStates.ReleasesAndPackages)
            {
                DrawPackagesPane(windowRect);
            }

            if (_viewState == ViewStates.PackagesAndProject || _viewState == ViewStates.Project)
            {
                DrawProjectPane(windowRect);
            }

            if (_viewState == ViewStates.ReleasesAndPackages)
            {
                DrawReleasePane(windowRect);
            }
        }

        void DrawReleasePane(Rect windowRect)
        {
            var rect = GetLeftSideRect(windowRect);

            DrawReleasePane2(rect);
        }

        void DrawReleasePane2(Rect rect)
        {
            var startX = rect.xMin;
            var endX = rect.xMax;
            var startY = rect.yMin;
            var endY = startY + _skin.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Releases", _skin.HeaderTextStyle);

            startY = endY;
            endY = rect.yMax - _skin.ApplyButtonHeight - _skin.ApplyButtonTopPadding;

            _releasesList.Draw(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + _skin.ApplyButtonTopPadding;
            endY = rect.yMax;

            if (GUI.Button(Rect.MinMaxRect(startX, startY, endX, endY), "Refresh", ButtonStyle))
            {
                RefreshReleases();
            }
        }

        void DrawProjectPane(Rect windowRect)
        {
            var rect = GetRightSideRect(windowRect);

            if (_viewState == ViewStates.Project)
            {
                rect = Rect.MinMaxRect(_skin.MarginLeft, rect.yMin, rect.xMax, rect.yMax);
            }

            DrawProjectPane2(rect);
        }

        enum ProjectConfigTypes
        {
            LocalProject,
            LocalProjectUser,
            AllProjects,
            AllProjectsUser,
        }

        enum ViewStates
        {
            Project,
            PackagesAndProject,
            ReleasesAndPackages,
        }
    }
}
