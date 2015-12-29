using System;
using Projeny.Internal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Projeny
{
    [Serializable]
    public class DraggableList
    {
        static readonly string DragId = "DraggableListData";

        [SerializeField]
        List<Entry> _entryList = new List<Entry>();

        [SerializeField]
        Vector2 _scrollPos;

        [SerializeField]
        PackageManagerWindow _owner;

        static DraggableListSkin _skin;

        DraggableListSkin Skin
        {
            get
            {
                return _skin ?? (_skin = Resources.Load<DraggableListSkin>("Projeny/DraggableListSkin"));
            }
        }

        public PackageManagerWindow Handler
        {
            set
            {
                _owner = value;
            }
        }

        public IEnumerable<Entry> Values
        {
            get
            {
                return _entryList;
            }
        }

        public IEnumerable<string> DisplayValues
        {
            get
            {
                return _entryList.Select(x => x.Name);
            }
        }

        public void Remove(Entry entry)
        {
            _entryList.RemoveWithConfirm(entry);
        }

        public void Remove(string name)
        {
            _entryList.RemoveWithConfirm(_entryList.Where(x => x.Name == name).Single());
        }

        public void Add(Entry entry)
        {
            _entryList.Add(entry);
        }

        public void Add(string entry)
        {
            _entryList.Add(new Entry(entry, null));
        }

        public void AddRange(IEnumerable<Entry> entries)
        {
            _entryList.AddRange(entries);
        }

        public void AddRange(IEnumerable<string> entries)
        {
            _entryList.AddRange(entries.Select(x => new Entry(x, null)));
        }

        public void Clear()
        {
            _entryList.Clear();
        }

        public void Draw(Rect listRect)
        {
            // Can this be calculated instead?
            var widthOfScrollBar = 50.0f;

            var viewRect = new Rect(0, 0, listRect.width - widthOfScrollBar, _entryList.Count * Skin.ItemHeight);

            ImguiUtil.DrawColoredQuad(listRect, Skin.ListColor);

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
                // Drag has completed
                {
                    if (isListUnderMouse)
                    {
                        DragAndDrop.AcceptDrag();

                        var receivedDragData = DragAndDrop.GetGenericData(DragId) as DragData;

                        if (receivedDragData != null)
                        {
                            DragAndDrop.PrepareStartDrag();
                            _owner.OnDragDrop(receivedDragData, this);
                        }
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    if (isListUnderMouse)
                    {
                        var existingDragData = DragAndDrop.GetGenericData(DragId) as DragData;

                        if (existingDragData != null)
                        {
                            DragAndDrop.StartDrag("Dragging List Element");
                            Event.current.Use();
                        }
                    }

                    break;
                }
                case EventType.DragUpdated:
                {
                    if (isListUnderMouse)
                    {
                        var existingDragData = DragAndDrop.GetGenericData(DragId) as DragData;

                        if (existingDragData != null && (_owner != null && _owner.IsDragAllowed(existingDragData, this)))
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            Event.current.Use();
                        }
                        else
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        }
                    }

                    break;
                }
            }

            float yPos = 0;
            _scrollPos = GUI.BeginScrollView(listRect, _scrollPos, viewRect);
            {
                foreach (var entry in _entryList)
                {
                    if (!entry.IsVisible)
                    {
                        continue;
                    }

                    var labelRect = new Rect(0, yPos, listRect.width, Skin.ItemHeight);

                    bool isItemUnderMouse = labelRect.Contains(Event.current.mousePosition);

                    ImguiUtil.DrawColoredQuad(labelRect, isItemUnderMouse ? Skin.ListItemHoverColor : Skin.ListItemColor);

                    switch (Event.current.type)
                    {
                        case EventType.MouseDown:
                        {
                            if (Event.current.button == 0 && isItemUnderMouse)
                            {
                                DragAndDrop.PrepareStartDrag();

                                var dragData = new DragData()
                                {
                                    Entry = entry,
                                    SourceList = this,
                                };

                                DragAndDrop.SetGenericData(DragId, dragData);
                                DragAndDrop.objectReferences = new UnityEngine.Object[0];
                                Event.current.Use();
                            }
                            break;
                        }
                    }

                    GUI.Label(labelRect, entry.Name, Skin.ItemTextStyle);

                    yPos += Skin.ItemHeight;
                }
            }
            GUI.EndScrollView();
        }

        public class DragData
        {
            public Entry Entry;
            public DraggableList SourceList;
        }

        [Serializable]
        public class Entry
        {
            public string Name;
            public object Tag;
            public bool IsVisible = true;

            public Entry(string name, object tag)
            {
                Name = name;
                Tag = tag;
            }
        }
    }
}
