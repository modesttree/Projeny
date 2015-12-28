using Projeny.Internal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Projeny
{
    public class DraggableList
    {
        static readonly Color BackgroundColor = Color.white;
        static readonly Color SelectedItemColor = Color.blue;

        static readonly string DragId = "DraggableListData";

        readonly DraggableListSkin _skin;
        readonly List<Entry> _entryList = new List<Entry>();
        Vector2 _scrollPos;

        public DraggableList()
        {
            _skin = Resources.Load<DraggableListSkin>("DraggableListSkin");
        }

        public void Add(string name)
        {
            _entryList.Add(new Entry(name));
        }

        public void ListField(params GUILayoutOption[] opts)
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, false, true, opts);
            {
                ListFieldInternal();
            }
            EditorGUILayout.EndScrollView();
        }

        void ListFieldInternal()
        {
            Rect totalArea = GUILayoutUtility.GetRect(GUIContent.none, _skin.ListStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlID);

            bool isUnderMouse = totalArea.Contains(Event.current.mousePosition);

            switch (eventType)
            {
                case EventType.Repaint:
                {
                    GUI.color = isUnderMouse ? _skin.ListHoverColor : _skin.ListColor;
                    GUI.DrawTexture(totalArea, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    break;
                }
                case EventType.MouseUp:
                {
                    // Clear our drag info in DragAndDrop so that we know that we are not dragging
                    DragAndDrop.PrepareStartDrag();
                    break;
                }
                case EventType.DragPerform:
                {
                    if (isUnderMouse)
                    {
                        DragAndDrop.AcceptDrag();

                        var receivedDragData = DragAndDrop.GetGenericData(DragId) as DragData;

                        if (receivedDragData != null)
                        {
                            OnDragDrop(receivedDragData);
                        }
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    var existingDragData = DragAndDrop.GetGenericData(DragId) as DragData;

                    if (existingDragData != null)
                    {
                        DragAndDrop.StartDrag("Dragging List ELement");
                        Event.current.Use();
                    }

                    break;
                }
                case EventType.DragUpdated:
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                    break;
                }
            }

            float yPos = totalArea.y;

            foreach (var entry in _entryList)
            {
                var itemRect = new Rect(totalArea.x, yPos, totalArea.width, _skin.ItemHeight);
                ItemField(entry, itemRect);
                yPos += _skin.ItemHeight;
            }
        }

        void OnDragDrop(DragData data)
        {
            if (data.SourceList == this)
            {
                return;
            }

            Assert.That(!_entryList.Contains(data.Entry));
            data.SourceList._entryList.RemoveWithConfirm(data.Entry);
            _entryList.Add(data.Entry);
        }

        void OnDragStart(Entry entry, out object dragData)
        {
            dragData = new DragData()
            {
                Entry = entry,
                SourceList = this,
            };
        }

        void ItemField(Entry entry, Rect totalArea)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlID);

            bool isUnderMouse = totalArea.Contains(Event.current.mousePosition);

            switch (eventType)
            {
                case EventType.Repaint:
                {
                    GUI.color = isUnderMouse ? _skin.ListItemHoverColor : _skin.ListItemColor;
                    GUI.DrawTexture(totalArea, Texture2D.whiteTexture);
                    GUI.color = Color.white;

                    _skin.ItemTextStyle.Draw(totalArea, entry.Name, isUnderMouse, true, true, false);
                    break;
                }
                case EventType.MouseDown:
                {
                    if (Event.current.button == 0 && isUnderMouse)
                    {
                        DragAndDrop.PrepareStartDrag();
                        object dragData;
                        OnDragStart(entry, out dragData);
                        DragAndDrop.SetGenericData(DragId, dragData);
                        DragAndDrop.objectReferences = new Object[0];
                        Event.current.Use();
                    }
                    break;
                }
            }
        }

        public class DragData
        {
            public Entry Entry;
            public DraggableList SourceList;
        }

        public class Entry
        {
            public string Name = "";

            public Entry(string name)
            {
                Name = name;
            }
        }
    }
}
