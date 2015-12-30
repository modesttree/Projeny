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

        PackageManagerWindowSkin _skin;

        List<DraggableListEntry> _selected;

        [NonSerialized]
        List<DraggableListEntry> _contextMenuSelectedSnapshot;

        [NonSerialized]
        float _split1 = 0;

        [NonSerialized]
        float _split2 = 0.5f;

        [NonSerialized]
        float _lastTime = 0.5f;

        [NonSerialized]
        CoRoutine _backgroundTask;

        PackageManagerWindowSkin Skin
        {
            get
            {
                return _skin ?? (_skin = Resources.Load<PackageManagerWindowSkin>("Projeny/PackageManagerSkin"));
            }
        }

        GUIStyle HeaderTextStyle
        {
            get
            {
                return Skin.GUISkin.GetStyle("HeaderTextStyle");
            }
        }

        GUIStyle ProcessingPopupTextStyle
        {
            get
            {
                return Skin.GUISkin.GetStyle("ProcessingPopupTextStyle");
            }
        }

        GUIStyle DropdownTextStyle
        {
            get
            {
                return Skin.GUISkin.GetStyle("DropdownTextStyle");
            }
        }

        public IEnumerable<DraggableListEntry> Selected
        {
            get
            {
                return _selected;
            }
        }

        public void ClearSelected()
        {
            _selected.Clear();
        }

        public void Select(DraggableListEntry newEntry)
        {
            if (_selected.Contains(newEntry))
            {
                if (Event.current.control)
                {
                    _selected.RemoveWithConfirm(newEntry);
                }

                return;
            }

            if (!Event.current.control && !Event.current.shift)
            {
                _selected.Clear();
            }

            // The selection entry list should all be from the same list
            foreach (var existingEntry in _selected.ToList())
            {
                if (existingEntry.ListOwner != newEntry.ListOwner)
                {
                    _selected.Remove(existingEntry);
                }
            }

            if (Event.current.shift && !_selected.IsEmpty())
            {
                var closestEntry = _selected.Select(x => new { Distance = Mathf.Abs(x.Index - newEntry.Index), Entry = x }).OrderBy(x => x.Distance).Select(x => x.Entry).First();

                int startIndex;
                int endIndex;

                if (closestEntry.Index > newEntry.Index)
                {
                    startIndex = newEntry.Index + 1;
                    endIndex = closestEntry.Index - 1;
                }
                else
                {
                    startIndex = closestEntry.Index + 1;
                    endIndex = newEntry.Index - 1;
                }

                for (int i = startIndex; i <= endIndex; i++)
                {
                    var inBetweenEntry = closestEntry.ListOwner.GetAtIndex(i);

                    if (!_selected.Contains(inBetweenEntry))
                    {
                        _selected.Add(inBetweenEntry);
                    }
                }
            }

            _selected.Add(newEntry);
        }

        void OnEnable()
        {
            if (_selected == null)
            {
                _selected = new List<DraggableListEntry>();
            }

            if (Skin.Theme.DropdownTextStyle == null)
            {
                Skin.Theme.DropdownTextStyle = new GUIStyle();
            }

            if (_allPackages == null)
            {
                _allPackages = new List<PackageInfo>();
            }

            if (_allReleases == null)
            {
                _allReleases = new List<ReleaseInfo>();
            }

            if (_installedList == null)
            {
                _installedList = ScriptableObject.CreateInstance<DraggableList>();
                _installedList.Manager = this;
            }

            if (_releasesList == null)
            {
                _releasesList = ScriptableObject.CreateInstance<DraggableList>();
                _releasesList.Manager = this;
            }


            if (_assetsList == null)
            {
                _assetsList = ScriptableObject.CreateInstance<DraggableList>();
                _assetsList.Manager = this;
            }

            if (_pluginsList == null)
            {
                _pluginsList = ScriptableObject.CreateInstance<DraggableList>();
                _pluginsList.Manager = this;
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

            return 0.4f;
        }

        void Update()
        {
            if (_backgroundTask != null)
            {
                // NOTE: If the tab isn't focused this task will take awhile
                if (!_backgroundTask.Pump())
                {
                    _backgroundTask = null;
                }
            }

            var deltaTime = Time.realtimeSinceStartup - _lastTime;
            _lastTime = Time.realtimeSinceStartup;

            var px = Mathf.Clamp(deltaTime * Skin.InterpSpeed, 0, 1);

            _split1 = Mathf.Lerp(_split1, GetDesiredSplit1(), px);
            _split2 = Mathf.Lerp(_split2, GetDesiredSplit2(), px);

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

        public void OpenContextMenu(DraggableList sourceList)
        {
            var listType = ClassifyList(sourceList);

            GenericMenu contextMenu = new GenericMenu();

            _contextMenuSelectedSnapshot = _selected.ToList();

            switch (listType)
            {
                case ListTypes.Release:
                {
                    contextMenu.AddDisabledItem(new GUIContent("TODO"));
                    break;
                }
                case ListTypes.Package:
                {
                    contextMenu.AddItem(new GUIContent("Delete"), false, DeleteSelectedPackages);
                    break;
                }
                case ListTypes.AssetItem:
                case ListTypes.PluginItem:
                {
                    contextMenu.AddDisabledItem(new GUIContent("TODO"));
                    break;
                }
                default:
                {
                    Assert.Throw();
                    break;
                }
            }

            contextMenu.ShowAsContext();
        }

        void DeleteSelectedPackages()
        {
            StartBackgroundTask(DeleteSelectedPackagesAsync());
        }

        IEnumerator DeleteSelectedPackagesAsync()
        {
            var selected = _contextMenuSelectedSnapshot;

            foreach (var sel in selected)
            {
                UnityEngine.Debug.Log("Deleting package " + sel.Name);
            }

            Assert.That(selected.All(x => ClassifyList(x.ListOwner) == ListTypes.Package));

            var result = UpmInterface.DeletePackagesAsync(selected.Select(x => (PackageInfo)x.Tag).ToList());
            yield return result;

            // Do this regardless of whether result.Current is true since
            // some packages might have been deleted
            yield return RefreshPackagesAsync();
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
                            foreach (var entry in data.Entries)
                            {
                                data.SourceList.Remove(entry);
                            }
                            break;
                        }
                        case ListTypes.Release:
                        {
                            StartBackgroundTask(InstallReleasesAsync(data.Entries.Select(x => (ReleaseInfo)x.Tag).ToList()));
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
                            foreach (var entry in data.Entries)
                            {
                                data.SourceList.Remove(entry);
                                dropList.Add(entry.Name, entry.Tag);
                            }
                            break;
                        }
                        case ListTypes.Package:
                        {
                            foreach (var entry in data.Entries)
                            {
                                if (!dropList.DisplayValues.Contains(entry.Name))
                                {
                                    var otherList = dropListType == ListTypes.PluginItem ? _assetsList : _pluginsList;

                                    if (otherList.DisplayValues.Contains(entry.Name))
                                    {
                                        otherList.Remove(entry.Name);
                                    }

                                    dropList.Add(entry.Name);
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

                    break;
                }
                case ListTypes.Release:
                {
                    // Nothing can drag here
                    break;
                }
                default:
                {
                    Assert.Throw();
                    break;
                }
            }
        }

        IEnumerator InstallReleasesAsync(List<ReleaseInfo> infos)
        {
            var result = UpmInterface.InstallReleasesAsync(infos);
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

        void DrawPackagesPane(Rect windowRect)
        {
            var startX = windowRect.xMin + _split1 * windowRect.width + Skin.ListVerticalSpacing;
            var endX = windowRect.xMin + _split2 * windowRect.width - Skin.ListVerticalSpacing;
            var startY = windowRect.yMin;
            var endY = windowRect.yMax;

            DrawPackagesPane2(Rect.MinMaxRect(startX, startY, endX, endY));
        }

        void DrawPackagesPane2(Rect rect)
        {
            var startX = rect.xMin;
            var endX = rect.xMax;
            var startY = rect.yMin;
            var endY = startY + Skin.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Packages", HeaderTextStyle);

            startY = endY;
            endY = rect.yMax - Skin.ApplyButtonHeight - Skin.ApplyButtonTopPadding;

            _installedList.Draw(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + Skin.ApplyButtonTopPadding;
            endY = rect.yMax;

            var refreshButtonRect = Rect.MinMaxRect(startX, startY, endX, endY);

            if (GUI.Button(refreshButtonRect, "Refresh"))
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

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Project", HeaderTextStyle);

            startY = endY;
            endY = startY + Skin.FileDropdownHeight;

            DrawFileDropdown(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY;
            endY = startY + Skin.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Assets Folder", HeaderTextStyle);

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

            GUI.Label(Rect.MinMaxRect(rect1.xMin, rect1.yMax, rect1.xMax, rect2.yMin), "Plugins Folder", HeaderTextStyle);
        }

        void DrawButtons(Rect rect)
        {
            var halfWidth = rect.width * 0.5f;
            var padding = 0.5f * Skin.ProjectButtonsPadding;

            if (GUI.Button(Rect.MinMaxRect(rect.x + halfWidth + padding, rect.y, rect.xMax, rect.yMax), "Apply"))
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

            foreach (var info in _allReleases)
            {
                _releasesList.Add(
                    "{0} v{1}".Fmt(info.Title, info.Version ?? "?"), info);
            }
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

            foreach (var info in _allPackages)
            {
                _installedList.Add(info.Name, info);
            }
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

            foreach (var name in config.Packages)
            {
                _assetsList.Add(name);
            }

            foreach (var name in config.PackagesPlugins)
            {
                _pluginsList.Add(name);
            }

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
                return !currentConfig.Packages.IsEmpty() || !currentConfig.PackagesPlugins.IsEmpty();
            }

            var savedConfig = DeserializeProjectConfig(configPath);

            if (savedConfig == null)
            {
                return !currentConfig.Packages.IsEmpty() || !currentConfig.PackagesPlugins.IsEmpty();
            }

            return !Enumerable.SequenceEqual(currentConfig.Packages.OrderBy(t => t), savedConfig.Packages.OrderBy(t => t))
                || !Enumerable.SequenceEqual(currentConfig.PackagesPlugins.OrderBy(t => t), savedConfig.PackagesPlugins.OrderBy(t => t));
        }

        ProjectConfig DeserializeProjectConfig(string configPath)
        {
            return YamlSerializer.Deserialize<ProjectConfig>(File.ReadAllText(configPath));
        }

        ProjectConfig GetProjectConfigFromLists()
        {
            var config = new ProjectConfig();

            config.Packages = _assetsList.DisplayValues.ToList();
            config.PackagesPlugins = _pluginsList.DisplayValues.ToList();

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
                rect.xMax - Skin.FileButtonsPercentWidth * rect.width,
                rect.yMax);

            var displayValues = GetConfigTypesDisplayValues();
            var desiredConfigType = (ProjectConfigTypes)EditorGUI.Popup(dropDownRect, (int)_projectConfigType, displayValues, DropdownTextStyle);

            GUI.Button(dropDownRect, displayValues[(int)desiredConfigType]);

            if (desiredConfigType != _projectConfigType)
            {
                TryChangeProjectType(desiredConfigType);
            }

            GUI.DrawTexture(new Rect(dropDownRect.xMax - Skin.ArrowSize.x + Skin.ArrowOffset.x, dropDownRect.yMin + Skin.ArrowOffset.y, Skin.ArrowSize.x, Skin.ArrowSize.y), Skin.FileDropdownArrow);

            var startX = rect.xMax - Skin.FileButtonsPercentWidth * rect.width;
            var startY = rect.yMin;
            var endX = rect.xMax;
            var endY = rect.yMax;

            var buttonPadding = Skin.FileButtonsPadding;
            var buttonWidth = ((endX - startX) - 3 * buttonPadding) / 3.0f;
            var buttonHeight = endY - startY;

            startX = startX + buttonPadding;

            if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Revert"))
            {
                RefreshProject();
            }

            startX = startX + buttonWidth + buttonPadding;

            if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Save"))
            {
                OverwriteConfig();
            }

            startX = startX + buttonWidth + buttonPadding;

            if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Open"))
            {
                var configPath = GetProjectConfigPath();
                InternalEditorUtility.OpenFileAtLineExternal(configPath, 1);
            }
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

        void DrawArrowColumns(Rect fullRect)
        {
            var halfHeight = 0.5f * fullRect.height;

            var rect1 = new Rect(Skin.ListVerticalSpacing, halfHeight - 0.5f * Skin.ArrowHeight, Skin.ArrowWidth, Skin.ArrowHeight);

            if ((int)_viewState > 0)
            {
                if (GUI.Button(rect1, ""))
                {
                    _viewState = (ViewStates)((int)_viewState - 1);
                }

                if (Skin.ArrowLeftTexture != null)
                {
                    GUI.DrawTexture(new Rect(rect1.xMin + 0.5f * rect1.width - 0.5f * Skin.ArrowButtonIconWidth, rect1.yMin + 0.5f * rect1.height - 0.5f * Skin.ArrowButtonIconHeight, Skin.ArrowButtonIconWidth, Skin.ArrowButtonIconHeight), Skin.ArrowLeftTexture);
                }
            }

            var rect2 = new Rect(fullRect.xMax - Skin.ListVerticalSpacing - Skin.ArrowWidth, halfHeight - 0.5f * Skin.ArrowHeight, Skin.ArrowWidth, Skin.ArrowHeight);

            var numValues = Enum.GetValues(typeof(ViewStates)).Length;

            if ((int)_viewState < numValues-1)
            {
                if (GUI.Button(rect2, ""))
                {
                    _viewState = (ViewStates)((int)_viewState + 1);
                }

                if (Skin.ArrowRightTexture != null)
                {
                    GUI.DrawTexture(new Rect(rect2.xMin + 0.5f * rect2.width - 0.5f * Skin.ArrowButtonIconWidth, rect2.yMin + 0.5f * rect2.height - 0.5f * Skin.ArrowButtonIconHeight, Skin.ArrowButtonIconWidth, Skin.ArrowButtonIconHeight), Skin.ArrowRightTexture);
                }
            }
        }

        public void OnGUI()
        {
            GUI.skin = Skin.GUISkin;
            // I tried using the GUILayout / EditorGUILayout but found it incredibly frustrating
            // and confusing, so I decided to just draw using raw rect coordinates instead

            var fullRect = new Rect(0, 0, this.position.width, this.position.height);

            if (_backgroundTask != null)
            {
                // Do not allow any input processing when running an async task
                GUI.enabled = false;
            }

            DrawArrowColumns(fullRect);

            var windowRect = Rect.MinMaxRect(
                Skin.ListVerticalSpacing + Skin.ArrowWidth,
                Skin.MarginTop,
                this.position.width - Skin.ListVerticalSpacing - Skin.ArrowWidth,
                this.position.height - Skin.MarginBottom);

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

            GUI.enabled = true;

            if (_backgroundTask != null)
            {
                ImguiUtil.DrawColoredQuad(fullRect, Skin.Theme.LoadingOverlayColor);

                var size = Skin.ProcessingPopupSize;
                var popupRect = new Rect(fullRect.width * 0.5f - 0.5f * size.x, 0.5f * fullRect.height - 0.5f * size.y, size.x, size.y);

                ImguiUtil.DrawColoredQuad(popupRect, Skin.Theme.LoadingOverlapPopupColor);

                var message = "Processing";

                int numExtraDots = (int)(Time.realtimeSinceStartup * Skin.ProcessingDotRepeatRate) % 5;

                for (int i = 0; i < numExtraDots; i++)
                {
                    message += ".";
                }

                GUI.Label(popupRect, message, ProcessingPopupTextStyle);
            }
        }

        void DrawReleasePane(Rect windowRect)
        {
            var startX = windowRect.xMin;
            var endX = windowRect.xMin + _split1 * windowRect.width - Skin.ListVerticalSpacing;
            var startY = windowRect.yMin;
            var endY = windowRect.yMax;

            DrawReleasePane2(Rect.MinMaxRect(startX, startY, endX, endY));
        }

        void DrawReleasePane2(Rect rect)
        {
            var startX = rect.xMin;
            var endX = rect.xMax;
            var startY = rect.yMin;
            var endY = startY + Skin.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Releases", HeaderTextStyle);

            startY = endY;
            endY = rect.yMax - Skin.ApplyButtonHeight - Skin.ApplyButtonTopPadding;

            _releasesList.Draw(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + Skin.ApplyButtonTopPadding;
            endY = rect.yMax;

            if (GUI.Button(Rect.MinMaxRect(startX, startY, endX, endY), "Refresh"))
            {
                StartBackgroundTask(RefreshReleasesAsync());
            }
        }

        void DrawProjectPane(Rect windowRect)
        {
            var startX = windowRect.xMin + _split2 * windowRect.width + Skin.ListVerticalSpacing;
            var endX = windowRect.xMax - Skin.ListVerticalSpacing;
            var startY = windowRect.yMin;
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
            ReleasesAndPackages,
            PackagesAndProject,
            Project,
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
