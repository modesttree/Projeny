using System;
using Projeny.Internal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Projeny
{
    [Serializable]
    public class DraggableList
    {
        static readonly Color BackgroundColor = Color.white;
        static readonly Color SelectedItemColor = Color.blue;

        static readonly string DragId = "DraggableListData";

        readonly DraggableListSkin _skin;

        [SerializeField]
        List<Entry> _entryList = new List<Entry>();

        [SerializeField]
        Vector2 _scrollPos;

        public DraggableList()
        {
            _skin = Resources.Load<DraggableListSkin>("DraggableListSkin");
        }

        public void Add(string name)
        {
            _entryList.Add(new Entry(name));
        }

        public void Draw(Rect listRect)
        {
            // Can this be calculated instead?
            var widthOfScrollBar = 50.0f;

            var viewRect = new Rect(0, 0, listRect.width - widthOfScrollBar, _entryList.Count * _skin.ItemHeight);

            DrawColor(listRect, _skin.ListColor);

            var isListUnderMouse = listRect.Contains(Event.current.mousePosition);

            switch (Event.current.type)
            {
                case EventType.MouseUp:
                {
                    // Clear our drag info in DragAndDrop so that we know that we are not dragging
                    DragAndDrop.PrepareStartDrag();
                    break;
                }
                case EventType.DragPerform:
                {
                    if (isListUnderMouse)
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
                        DragAndDrop.StartDrag("Dragging List Element");
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

            float yPos = 0;
            _scrollPos = GUI.BeginScrollView(listRect, _scrollPos, viewRect);
            {
                foreach (var entry in _entryList)
                {
                    var labelRect = new Rect(0, yPos, listRect.width, _skin.ItemHeight);

                    bool isItemUnderMouse = labelRect.Contains(Event.current.mousePosition);

                    DrawColor(labelRect, isItemUnderMouse ? _skin.ListItemHoverColor : _skin.ListItemColor);

                    switch (Event.current.type)
                    {
                        case EventType.MouseDown:
                        {
                            if (Event.current.button == 0 && isItemUnderMouse)
                            {
                                DragAndDrop.PrepareStartDrag();

                                object dragData;
                                OnDragStart(entry, out dragData);

                                DragAndDrop.SetGenericData(DragId, dragData);
                                DragAndDrop.objectReferences = new UnityEngine.Object[0];
                                Event.current.Use();
                            }
                            break;
                        }
                    }

                    GUI.Label(labelRect, entry.Name, _skin.ItemTextStyle);

                    yPos += _skin.ItemHeight;
                }
            }
            GUI.EndScrollView();
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

        void DrawColor(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public class DragData
        {
            public Entry Entry;
            public DraggableList SourceList;
        }

        [Serializable]
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
