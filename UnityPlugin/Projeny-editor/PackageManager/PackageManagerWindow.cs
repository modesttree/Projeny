using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Projeny.Internal;

namespace Projeny
{
    public class PackageManagerWindow : EditorWindow
    {
        DraggableList _availableList;
        DraggableList _assetsList;
        DraggableList _pluginsList;

        PackageManagerWindowSkin _skin;
        ProjectConfigTypes _projectConfigType;

        void OnEnable()
        {
            _skin = Resources.Load<PackageManagerWindowSkin>("Projeny/PackageManagerSkin");

            if (_availableList == null)
            {
                _availableList = new DraggableList();
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

        void DrawHeaders(Rect headerRect)
        {
            var halfWidth = _skin.AvailablePercentWidth * headerRect.width;
            var rect1 = new Rect(headerRect.x, headerRect.y, halfWidth, headerRect.height);
            GUI.Label(rect1, "Available Packages", _skin.HeaderTextStyle);
        }

        void DrawLists(Rect windowRect)
        {
            DrawLeftList(windowRect);
            DrawRightLists(windowRect);
        }

        void DrawLeftList(Rect windowRect)
        {
            var startX = _skin.MarginLeft;
            var endX = _skin.AvailablePercentWidth * windowRect.width - 0.5f * _skin.ListVerticalSpacing;
            var startY = _skin.HeaderHeight;
            var endY = windowRect.height - _skin.MarginBottom - _skin.ApplyButtonHeight - _skin.ApplyButtonTopPadding;

            _availableList.Draw(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + _skin.ApplyButtonTopPadding;
            endY = windowRect.height - _skin.MarginBottom;

            if (GUI.Button(Rect.MinMaxRect(startX, startY, endX, endY), "Refresh Packages"))
            {
                RefreshPackages();
            }
        }

        void DrawRightLists(Rect windowRect)
        {
            var startX = _skin.AvailablePercentWidth * windowRect.width + 0.5f * _skin.ListVerticalSpacing;
            var endX = windowRect.width - _skin.MarginRight;
            var startY = _skin.HeaderHeight;
            var endY = startY + _skin.FileDropdownHeight;

            DrawFileDropdown(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY;
            endY = startY + _skin.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Assets Folder", _skin.HeaderTextStyle);

            startY = endY;
            endY = windowRect.height - _skin.MarginBottom - _skin.ApplyButtonHeight - _skin.ApplyButtonTopPadding;

            DrawRightLists2(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + _skin.ApplyButtonTopPadding;
            endY = windowRect.height - _skin.MarginBottom;

            DrawButtons(Rect.MinMaxRect(startX, startY, endX, endY));
        }

        void DrawButtons(Rect rect)
        {
            var halfWidth = rect.width * 0.5f;
            var padding = 0.5f * _skin.ProjectButtonsPadding;

            if (GUI.Button(new Rect(rect.x, rect.y, halfWidth - padding, rect.height), "Reload File"))
            {
                RefreshProject();
            }

            if (GUI.Button(Rect.MinMaxRect(rect.x + halfWidth + padding, rect.y, rect.xMax, rect.yMax), "Apply"))
            {
                ApplyChanges();
            }
        }

        void RefreshPackages()
        {
            // TODO
        }

        void RefreshProject()
        {
            var configPath = GetProjectConfigPath();

            if (File.Exists(configPath))
            {
                var project = ProjectConfigSerializer.Deserialize(File.ReadAllText(configPath));
                PopulateListsFromConfig(project);
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
        }

        void PopulateListsFromConfig(ProjectConfig config)
        {
            ClearProjectLists();

            _assetsList.AddRange(config.Packages);
            _pluginsList.AddRange(config.PluginPackages);
        }

        void ApplyChanges()
        {
            Log.Trace("TODO");
        }

        void DrawFileDropdown(Rect rect)
        {
            var desiredConfigType = (ProjectConfigTypes)EditorGUI.EnumPopup(rect, _projectConfigType, EditorStyles.popup);

            if (desiredConfigType != _projectConfigType)
            {
                // TODO: Confirm dialog if something changed
                _projectConfigType = desiredConfigType;
            }
        }

        void DrawRightLists2(Rect listRect)
        {
            var halfHeight = 0.5f * listRect.height;

            var rect1 = new Rect(listRect.x, listRect.y, listRect.width, halfHeight - 0.5f * _skin.ListHorizontalSpacing);
            var rect2 = new Rect(listRect.x, listRect.y + halfHeight + 0.5f * _skin.ListHorizontalSpacing, listRect.width, listRect.height - halfHeight - 0.5f * _skin.ListHorizontalSpacing);

            _assetsList.Draw(rect1);
            _pluginsList.Draw(rect2);

            GUI.Label(Rect.MinMaxRect(rect1.xMin, rect1.yMax, rect1.xMax, rect2.yMin), "Plugins Folder", _skin.HeaderTextStyle);
        }

        public void OnGUI()
        {
            var windowRect = this.position;

            var headerRect = new Rect(0, 0, windowRect.width, _skin.HeaderHeight);
            DrawHeaders(headerRect);

            DrawLists(windowRect);
        }

        enum ProjectConfigTypes
        {
            LocalProject,
            LocalProjectUser,
            AllProjects,
            AllProjectsUser,
        }
    }

}
