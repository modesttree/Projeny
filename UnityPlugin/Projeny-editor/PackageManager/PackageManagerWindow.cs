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

        ProjectConfigTypes _projectConfigType;

        ViewStates _viewState = ViewStates.PackagesAndProject;

        GUIStyle _buttonStyle;
        GUIStyle _toggleStyle;

        PackageManagerWindowSkin _skin;

        [NonSerialized]
        float _split1 = 0;

        [NonSerialized]
        float _split2 = 0.5f;

        [NonSerialized]
        CoRoutine _backgroundTask;

        PackageManagerWindowSkin Skin
        {
            get
            {
                return _skin ?? (_skin = Resources.Load<PackageManagerWindowSkin>("Projeny/PackageManagerSkin"));
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

                _buttonStyle.fontSize = Skin.ButtonFontSize;

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

                _toggleStyle.fontSize = Skin.ButtonFontSize;

                return _toggleStyle;
            }
        }

        void OnEnable()
        {
            _buttonStyle = null;
            _toggleStyle = null;

            if (Skin.ProcessingPopupTextStyle == null)
            {
                Skin.ProcessingPopupTextStyle = new GUIStyle();
            }

            if (Skin.DropdownTextStyle == null)
            {
                Skin.DropdownTextStyle = new GUIStyle();
            }

            if (_allPackages == null)
            {
                _allPackages = new List<PackageInfo>();
            }

            if (_installedList == null)
            {
                _installedList = new DraggableList();
                _installedList.Handler = this;
            }

            if (_releasesList == null)
            {
                _releasesList = new DraggableList();
                _releasesList.Handler = this;
            }


            if (_assetsList == null)
            {
                _assetsList = new DraggableList();
                _assetsList.Handler = this;
            }

            if (_pluginsList == null)
            {
                _pluginsList = new DraggableList();
                _pluginsList.Handler = this;
            }
        }

        float GetDesiredSplit1()
        {
            if (_viewState == ViewStates.ReleasesAndPackages)
            {
                return 0.5f;
            }

            return 0;
        }

        float GetDesiredSplit2()
        {
            if (_viewState == ViewStates.ReleasesAndPackages)
            {
                return 1.0f;
            }

            if (_viewState == ViewStates.Project)
            {
                return 0;
            }

            return 0.5f;
        }

        void Update()
        {
            if (_backgroundTask != null)
            {
                if (!_backgroundTask.Pump())
                {
                    _backgroundTask = null;
                }
            }

            _split1 = Mathf.Lerp(_split1, GetDesiredSplit1(), Skin.InterpSpeed);
            _split2 = Mathf.Lerp(_split2, GetDesiredSplit2(), Skin.InterpSpeed);

            // Doesn't seem worth trying to detect changes, just redraw every frame
            Repaint();
        }

        public bool IsDragAllowed(DraggableList.DragData data, DraggableList list)
        {
            var sourceListType = ClassifyList(data.SourceList);
            var dropListType = ClassifyList(list);

            if (sourceListType == dropListType)
            {
                return true;
            }

            switch (dropListType)
            {
                case ListTypes.Package:
                {
                    return sourceListType == ListTypes.Release || sourceListType == ListTypes.AssetItem || sourceListType == ListTypes.PluginItem;
                }
                case ListTypes.Release:
                {
                    return false;
                }
                case ListTypes.AssetItem:
                {
                    return sourceListType == ListTypes.Package || sourceListType == ListTypes.PluginItem;
                }
                case ListTypes.PluginItem:
                {
                    return sourceListType == ListTypes.Package || sourceListType == ListTypes.AssetItem;
                }
            }

            Assert.Throw();
            return true;
        }

        public void OnDragDrop(DraggableList.DragData data, DraggableList dropList)
        {
            if (data.SourceList == dropList || !IsDragAllowed(data, dropList))
            {
                return;
            }

            var sourceListType = ClassifyList(data.SourceList);
            var dropListType = ClassifyList(dropList);

            switch (dropListType)
            {
                case ListTypes.Package:
                {
                    switch (sourceListType)
                    {
                        case ListTypes.PluginItem:
                        case ListTypes.AssetItem:
                        {
                            data.SourceList.Remove(data.Entry);
                            break;
                        }
                        case ListTypes.Release:
                        {
                            StartBackgroundTask(InstallReleaseAsync((ReleaseInfo)data.Entry.Tag));
                            break;
                        }
                        default:
                        {
                            Assert.Throw();
                            break;
                        }
                    }

                    break;
                }
                case ListTypes.PluginItem:
                case ListTypes.AssetItem:
                {
                    switch (sourceListType)
                    {
                        case ListTypes.AssetItem:
                        case ListTypes.PluginItem:
                        {
                            data.SourceList.Remove(data.Entry);
                            dropList.Add(data.Entry);
                            break;
                        }
                        case ListTypes.Package:
                        {
                            if (!dropList.DisplayValues.Contains(data.Entry.Name))
                            {
                                var otherList = dropListType == ListTypes.PluginItem ? _assetsList : _pluginsList;

                                if (otherList.DisplayValues.Contains(data.Entry.Name))
                                {
                                    otherList.Remove(data.Entry.Name);
                                }

                                dropList.Add(data.Entry.Name);
                            }
                            break;
                        }
                        default:
                        {
                            Assert.Throw();
                            break;
                        }
                    }

                    break;
                }
                case ListTypes.Release:
                {
                    switch (sourceListType)
                    {
                        case ListTypes.Package:
                        {
                            Log.Trace("TODO - uninstall package");
                            break;
                        }
                        default:
                        {
                            Assert.Throw();
                            break;
                        }
                    }

                    break;
                }
                default:
                {
                    Assert.Throw();
                    break;
                }
            }
        }

        IEnumerator InstallReleaseAsync(ReleaseInfo info)
        {
            var result = UpmInterface.InstallReleaseAsync(info.Title, info.Version);
            yield return result;

            if (result.Current)
            {
                yield return RefreshPackagesAsync();
            }
        }

        ListTypes ClassifyList(DraggableList list)
        {
            if (list == _installedList)
            {
                return ListTypes.Package;
            }

            if (list == _releasesList)
            {
                return ListTypes.Release;
            }

            if (list == _assetsList)
            {
                return ListTypes.AssetItem;
            }

            if (list == _pluginsList)
            {
                return ListTypes.PluginItem;
            }

            Assert.Throw();
            return ListTypes.AssetItem;
        }

        Rect GetRightSideRect(Rect windowRect, float split)
        {
            var startX = split * windowRect.width + 0.5f * Skin.ListVerticalSpacing;
            var endX = windowRect.width;
            var startY = Skin.ViewToggleHeight;
            var endY = windowRect.height;

            return Rect.MinMaxRect(startX, startY, endX, endY);
        }

        Rect GetLeftSideRect(Rect windowRect, float split)
        {
            var startX = 0;
            var endX = split * windowRect.width - 0.5f * Skin.ListVerticalSpacing;
            var startY = Skin.ViewToggleHeight;
            var endY = windowRect.yMax;

            return Rect.MinMaxRect(startX, startY, endX, endY);
        }

        void DrawPackagesPane(Rect windowRect)
        {
            var startX = windowRect.xMin + _split1 * windowRect.width + 0.5f * Skin.ListVerticalSpacing;
            var endX = windowRect.xMin + _split2 * windowRect.width - 0.5f * Skin.ListVerticalSpacing;
            var startY = windowRect.yMin + Skin.ViewToggleHeight;
            var endY = windowRect.yMax;

            DrawPackagesPane2(Rect.MinMaxRect(startX, startY, endX, endY));
        }

        void DrawPackagesPane2(Rect rect)
        {
            var startX = rect.xMin;
            var endX = rect.xMax;
            var startY = rect.yMin;
            var endY = startY + Skin.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Packages", Skin.HeaderTextStyle);

            startY = endY;
            endY = rect.yMax - Skin.ApplyButtonHeight - Skin.ApplyButtonTopPadding;

            _installedList.Draw(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + Skin.ApplyButtonTopPadding;
            endY = rect.yMax;

            if (GUI.Button(Rect.MinMaxRect(startX, startY, endX, endY), "Refresh", ButtonStyle))
            {
                StartBackgroundTask(RefreshPackagesAsync());
            }
        }

        void DrawProjectPane2(Rect rect)
        {
            var startX = rect.xMin;
            var endX = rect.xMax;
            var startY = rect.yMin;
            var endY = startY + Skin.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Project Settings", Skin.HeaderTextStyle);

            startY = endY;
            endY = startY + Skin.FileDropdownHeight;

            DrawFileDropdown(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY;
            endY = startY + Skin.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Assets Folder", Skin.HeaderTextStyle);

            startY = endY;
            endY = rect.yMax - Skin.ApplyButtonHeight - Skin.ApplyButtonTopPadding;

            DrawProjectPane3(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + Skin.ApplyButtonTopPadding;
            endY = rect.yMax;

            DrawButtons(Rect.MinMaxRect(startX, startY, endX, endY));
        }

        void DrawProjectPane3(Rect listRect)
        {
            var halfHeight = 0.5f * listRect.height;

            var rect1 = new Rect(listRect.x, listRect.y, listRect.width, halfHeight - 0.5f * Skin.ListHorizontalSpacing);
            var rect2 = new Rect(listRect.x, listRect.y + halfHeight + 0.5f * Skin.ListHorizontalSpacing, listRect.width, listRect.height - halfHeight - 0.5f * Skin.ListHorizontalSpacing);

            _assetsList.Draw(rect1);
            _pluginsList.Draw(rect2);

            GUI.Label(Rect.MinMaxRect(rect1.xMin, rect1.yMax, rect1.xMax, rect2.yMin), "Plugins Folder", Skin.HeaderTextStyle);
        }

        void DrawButtons(Rect rect)
        {
            var halfWidth = rect.width * 0.5f;
            var padding = 0.5f * Skin.ProjectButtonsPadding;

            if (GUI.Button(Rect.MinMaxRect(rect.x + halfWidth + padding, rect.y, rect.xMax, rect.yMax), "Apply", ButtonStyle))
            {
                OverwriteConfig();
                StartBackgroundTask(UpmInterface.UpdateLinksAsync());
            }
        }

        IEnumerator RefreshReleasesAsync()
        {
            var result = UpmInterface.LookupReleaseListAsync();
            yield return result;

            // Null indicates failure
            if (result.Current != null)
            {
                _allReleases = result.Current;
                UpdateAvailableReleasesList();
            }
        }

        void UpdateAvailableReleasesList()
        {
            _releasesList.Clear();
            _releasesList.AddRange(
                _allReleases.Select(x => new DraggableList.Entry("{0} v{1}".Fmt(x.Title, x.Version ?? "?"), x)));
        }

        void StartBackgroundTask(IEnumerator task)
        {
            Assert.IsNull(_backgroundTask);
            _backgroundTask = new CoRoutine(task);
        }

        IEnumerator RefreshPackagesAsync()
        {
            var allPackages = UpmInterface.LookupPackagesListAsync();
            yield return allPackages;

            if (allPackages.Current != null)
            // Returns null on failure
            {
                _allPackages = allPackages.Current;
                UpdateAvailablePackagesList();
            }
        }

        void UpdateAvailablePackagesList()
        {
            _installedList.Clear();
            _installedList.AddRange(_allPackages.Select(x => new DraggableList.Entry(x.Name, x)));
        }

        void RefreshProject()
        {
            var configPath = GetProjectConfigPath();

            if (File.Exists(configPath))
            {
                var savedConfig = DeserializeProjectConfig(configPath);

                // Null when file is empty
                if (savedConfig == null)
                {
                    ClearProjectLists();
                }
                else
                {
                    PopulateListsFromConfig(savedConfig);
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
            File.WriteAllText(GetProjectConfigPath(), GetSerializedProjectConfigFromLists());
        }

        bool HasProjectConfigChanged()
        {
            var configPath = GetProjectConfigPath();

            var currentConfig = GetProjectConfigFromLists();

            if (!File.Exists(configPath))
            {
                return !currentConfig.Packages.IsEmpty() || !currentConfig.PluginPackages.IsEmpty();
            }

            var savedConfig = DeserializeProjectConfig(configPath);

            if (savedConfig == null)
            {
                return !currentConfig.Packages.IsEmpty() || !currentConfig.PluginPackages.IsEmpty();
            }

            return !Enumerable.SequenceEqual(currentConfig.Packages.OrderBy(t => t), savedConfig.Packages.OrderBy(t => t))
                || !Enumerable.SequenceEqual(currentConfig.PluginPackages.OrderBy(t => t), savedConfig.PluginPackages.OrderBy(t => t));
        }

        ProjectConfig DeserializeProjectConfig(string configPath)
        {
            return YamlSerializer.Deserialize<ProjectConfig>(File.ReadAllText(configPath));
        }

        ProjectConfig GetProjectConfigFromLists()
        {
            var config = new ProjectConfig();

            config.Packages = _assetsList.DisplayValues.ToList();
            config.PluginPackages = _pluginsList.DisplayValues.ToList();

            return config;
        }

        string GetSerializedProjectConfigFromLists()
        {
            return YamlSerializer.Serialize<ProjectConfig>(GetProjectConfigFromLists());
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
                rect.xMin,
                rect.yMin,
                rect.xMax - Skin.FileDropdownOpenFileButtonWidth - Skin.FileDropdownOpenFileButtonLeftPadding - Skin.FileDropdownSaveFileButtonLeftPadding - Skin.FileDropdownSaveFileButtonWidth - Skin.FileDropdownReloadFileButtonWidth - Skin.FileDropdownSaveFileButtonLeftPadding,
                rect.yMax);

            ImguiUtil.DrawColoredQuad(dropDownRect, Skin.FileDropdownBackgroundColor);

            GUI.DrawTexture(new Rect(dropDownRect.xMax - Skin.ArrowSize.x + Skin.ArrowOffset.x, dropDownRect.yMin + Skin.ArrowOffset.y, Skin.ArrowSize.x, Skin.ArrowSize.y), Skin.FileDropdownArrow);

            var desiredConfigType = (ProjectConfigTypes)EditorGUI.Popup(dropDownRect, (int)_projectConfigType, GetConfigTypesDisplayValues(), Skin.DropdownTextStyle);

            if (desiredConfigType != _projectConfigType)
            {
                TryChangeProjectType(desiredConfigType);
            }

            if (Event.current.type == EventType.Repaint)
            {
                Graphics.DrawTexture(dropDownRect, Skin.FileDropdownBackground, Skin.FileDropdownBorder,
                Skin.FileDropdownBorder, Skin.FileDropdownBorder, Skin.FileDropdownBorder);
            }

            var reloadButtonRect = new Rect(
                dropDownRect.xMax + Skin.FileDropdownSaveFileButtonLeftPadding,
                dropDownRect.yMin,
                Skin.FileDropdownReloadFileButtonWidth,
                dropDownRect.height);

            if (GUI.Button(reloadButtonRect, "Reload", ButtonStyle))
            {
                RefreshProject();
            }

            var saveButtonRect = new Rect(
                dropDownRect.xMax + 2 * Skin.FileDropdownSaveFileButtonLeftPadding + Skin.FileDropdownReloadFileButtonWidth,
                dropDownRect.yMin,
                Skin.FileDropdownSaveFileButtonWidth,
                dropDownRect.height);

            if (GUI.Button(saveButtonRect, "Save", ButtonStyle))
            {
                OverwriteConfig();
            }

            var openButtonRect = Rect.MinMaxRect(rect.xMax - Skin.FileDropdownOpenFileButtonWidth, rect.yMin, rect.xMax, rect.yMax);

            var configPath = GetProjectConfigPath();
            bool wasEnabled = GUI.enabled;
            GUI.enabled = GUI.enabled && File.Exists(configPath);
            if (GUI.Button(openButtonRect, "Open", ButtonStyle))
            {
                InternalEditorUtility.OpenFileAtLineExternal(configPath, 1);
            }
            GUI.enabled = wasEnabled;
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

            var rect1 = Rect.MinMaxRect(rect.xMin + Skin.ViewSelectSpacing, rect.yMin, split1 - Skin.ViewSelectSpacing, rect.yMax);
            var rect2 = Rect.MinMaxRect(split1 + Skin.ViewSelectSpacing, rect.yMin, split2 - Skin.ViewSelectSpacing, rect.yMax);
            var rect3 = Rect.MinMaxRect(split2 + Skin.ViewSelectSpacing, rect.yMin, rect.xMax - Skin.ViewSelectSpacing, rect.yMax);

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
            // and confusing, so I decided to just draw using raw rect coordinates instead

            var windowRect = Rect.MinMaxRect(Skin.MarginLeft, Skin.MarginTop, this.position.width - Skin.MarginRight, this.position.height - Skin.MarginBottom);

            if (_backgroundTask != null)
            {
                // Do not allow any input processing when running an async task
                GUI.enabled = false;
            }

            var viewSelectRect = new Rect(windowRect.xMin, windowRect.yMin, windowRect.width, Skin.ViewToggleHeight);
            DrawViewSelect(viewSelectRect);

            if (_split2 >= 0.1f)
            {
                DrawPackagesPane(windowRect);
            }

            if (_split2 <= 0.92f)
            {
                DrawProjectPane(windowRect);
            }

            if (_split1 >= 0.1f)
            {
                DrawReleasePane(windowRect);
            }

            if (_backgroundTask != null)
            {
                var fullRect = new Rect(0, 0, this.position.width, this.position.height);
                ImguiUtil.DrawColoredQuad(fullRect, Skin.LoadingOverlayColor);
                GUI.Label(fullRect, "Processing...", Skin.ProcessingPopupTextStyle);
            }

            GUI.enabled = true;
        }

        void DrawReleasePane(Rect windowRect)
        {
            var startX = windowRect.xMin;
            var endX = windowRect.xMin + _split1 * windowRect.width - 0.5f * Skin.ListVerticalSpacing;
            var startY = windowRect.yMin + Skin.ViewToggleHeight;
            var endY = windowRect.yMax;

            DrawReleasePane2(Rect.MinMaxRect(startX, startY, endX, endY));
        }

        void DrawReleasePane2(Rect rect)
        {
            var startX = rect.xMin;
            var endX = rect.xMax;
            var startY = rect.yMin;
            var endY = startY + Skin.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Releases", Skin.HeaderTextStyle);

            startY = endY;
            endY = rect.yMax - Skin.ApplyButtonHeight - Skin.ApplyButtonTopPadding;

            _releasesList.Draw(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + Skin.ApplyButtonTopPadding;
            endY = rect.yMax;

            if (GUI.Button(Rect.MinMaxRect(startX, startY, endX, endY), "Refresh", ButtonStyle))
            {
                StartBackgroundTask(RefreshReleasesAsync());
            }
        }

        void DrawProjectPane(Rect windowRect)
        {
            var startX = windowRect.xMin + _split2 * windowRect.width + 0.5f * Skin.ListVerticalSpacing;
            var endX = windowRect.xMax;
            var startY = windowRect.yMin + Skin.HeaderHeight;
            var endY = windowRect.yMax;

            var rect = Rect.MinMaxRect(startX, startY, endX, endY);

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

        enum ListTypes
        {
            Package,
            Release,
            AssetItem,
            PluginItem
        }
    }
}
