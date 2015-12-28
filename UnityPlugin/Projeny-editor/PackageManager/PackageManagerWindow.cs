using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Projeny
{
    public class PackageManagerWindow : EditorWindow
    {
        DraggableList _availableList;
        DraggableList _assetsList;
        DraggableList _pluginsList;

        PackageManagerWindowSkin _skin;

        void OnEnable()
        {
            _skin = Resources.Load<PackageManagerWindowSkin>("Projeny/PackageManagerSkin");

            //if (_availableList == null)
            {
                _availableList = new DraggableList();

                _availableList.Add("bob");
                _availableList.Add("joe");
                _availableList.Add("frank");
                _availableList.Add("mary");
                _availableList.Add("mary");
                _availableList.Add("zxcv");
                _availableList.Add("wetqsdf");
                _availableList.Add("dsgfasdgz");
                _availableList.Add("235325");
                _availableList.Add("623");
            }

            //if (_assetsList == null)
            {
                _assetsList = new DraggableList();

                _assetsList.Add("john");
                _assetsList.Add("zack");
            }

            //if (_pluginsList == null)
            {
                _pluginsList = new DraggableList();

                _pluginsList.Add("asdf");
                _pluginsList.Add("zxcv");
            }
        }

        void Update()
        {
            // Doesn't seem worth trying to detect changes, just redraw every frame
            Repaint();
        }

        void DrawHeaders(Rect windowRect)
        {
            var headerRect = new Rect(0, 0, windowRect.width, _skin.HeaderHeight);

            var halfWidth = 0.5f * headerRect.width;
            var rect1 = new Rect(headerRect.x, headerRect.y, halfWidth, headerRect.height);
            GUI.Label(rect1, "Available Packages", _skin.HeaderTextStyle);

            var rect2 = new Rect(headerRect.x + halfWidth, headerRect.y, halfWidth, headerRect.height);
            GUI.Label(rect2, "Assets Folder", _skin.HeaderTextStyle);
        }

        void DrawLists(Rect windowRect)
        {
            DrawLeftList(windowRect);
            DrawRightLists(windowRect);
        }

        void DrawLeftList(Rect windowRect)
        {
            var startX = _skin.MarginLeft;
            var endX = 0.5f * windowRect.width - 0.5f * _skin.ListVerticalSpacing;
            var startY = _skin.HeaderHeight;
            var endY = windowRect.height - _skin.MarginBottom;

            _availableList.Draw(Rect.MinMaxRect(startX, startY, endX, endY));
        }

        void DrawRightLists(Rect windowRect)
        {
            var startX = 0.5f * windowRect.width + 0.5f * _skin.ListVerticalSpacing;
            var endX = windowRect.width - _skin.MarginRight;
            var startY = _skin.HeaderHeight;
            var endY = windowRect.height - _skin.MarginBottom;

            DrawRightLists2(Rect.MinMaxRect(startX, startY, endX, endY));
        }

        void DrawRightLists2(Rect listRect)
        {
            var halfHeight = 0.5f * listRect.height;

            var rect1 = new Rect(listRect.x, listRect.y, listRect.width, halfHeight - 0.5f * _skin.ListHorizontalSpacing);
            var rect2 = new Rect(listRect.x, listRect.y + halfHeight + 0.5f * _skin.ListHorizontalSpacing, listRect.width, listRect.height - halfHeight - 0.5f * _skin.ListHorizontalSpacing);

            _assetsList.Draw(rect1);
            _pluginsList.Draw(rect2);

            GUI.Label(Rect.MinMaxRect(rect1.left, rect1.bottom, rect1.right, rect2.top), "Plugins Folder", _skin.HeaderTextStyle);
        }

        public void OnGUI()
        {
            var windowRect = this.position;

            DrawHeaders(windowRect);
            DrawLists(windowRect);
        }
    }

}
