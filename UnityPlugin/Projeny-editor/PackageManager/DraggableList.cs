using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Projeny.Internal;

namespace Projeny.Internal
{
    [Serializable]
    public class DraggableListEntry : ScriptableObject
    {
        public DraggableList ListOwner;
        public string Name;
        public UnityEngine.Object Tag;
        public bool IsVisible = true;
        public int Index;
    }

    [Serializable]
    public class DraggableList : ScriptableObject
    {
        static readonly string DragId = "DraggableListData";

        [SerializeField]
        List<DraggableListEntry> _entryList = new List<DraggableListEntry>();

        [SerializeField]
        Vector2 _scrollPos;

        [SerializeField]
        PackageManagerWindow _manager;

        static DraggableListSkin _skin;

        DraggableListSkin Skin
        {
            get
            {
                return _skin ?? (_skin = Resources.Load<DraggableListSkin>("Projeny/DraggableListSkin"));
            }
        }

        public PackageManagerWindow Manager
        {
            set
            {
                _manager = value;
            }
        }

        public IEnumerable<DraggableListEntry> Values
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

        public void Remove(DraggableListEntry entry)
        {
            _entryList.RemoveWithConfirm(entry);
            SortList();
        }

        public DraggableListEntry GetAtIndex(int index)
        {
            return _entryList[index];
        }

        public void Remove(string name)
        {
            _entryList.RemoveWithConfirm(_entryList.Where(x => x.Name == name).Single());
            SortList();
        }

        public void Add(string name, UnityEngine.Object tag)
        {
            var entry = ScriptableObject.CreateInstance<DraggableListEntry>();

            entry.Name = name;
            entry.Tag = tag;
            entry.ListOwner = this;

            _entryList.Add(entry);
            SortList();
        }

        public void Add(string entry)
        {
            Add(entry, null);
        }

        public void Clear()
        {
            _entryList.Clear();
        }

        void SortList()
        {
            _entryList = _entryList.OrderBy(x => x.Name).ToList();

            for (int i = 0; i < _entryList.Count; i++)
            {
                _entryList[i].Index = i;
            }
        }

        public void Draw(Rect listRect)
        {
            // Can this be calculated instead?
            var widthOfScrollBar = 15.0f;

            var viewRect = new Rect(0, 0, listRect.width - 30.0f, _entryList.Count * Skin.ItemHeight);

            var isListUnderMouse = listRect.Contains(Event.current.mousePosition);

            ImguiUtil.DrawColoredQuad(listRect, GUI.enabled && isListUnderMouse ? Skin.Theme.ListHoverColor : Skin.Theme.ListColor);

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
                            _manager.OnDragDrop(receivedDragData, this);
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

                        if (existingDragData != null && (_manager != null && _manager.IsDragAllowed(existingDragData, this)))
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

            bool clickedItem = false;

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

                    Color itemColor;

                    if (_manager.Selected.Contains(entry))
                    {
                        itemColor = Skin.Theme.ListItemSelectedColor;
                    }
                    else
                    {
                        itemColor = GUI.enabled && isItemUnderMouse ? Skin.Theme.ListItemHoverColor : Skin.Theme.ListItemColor;
                    }

                    ImguiUtil.DrawColoredQuad(labelRect, itemColor);

                    switch (Event.current.type)
                    {
                        case EventType.ContextClick:
                        {
                            if (isListUnderMouse)
                            {
                                _manager.OpenContextMenu(this);
                                Event.current.Use();
                            }

                            break;
                        }
                        case EventType.MouseUp:
                        {
                            if (isItemUnderMouse && Event.current.button == 0)
                            {
                                if (!Event.current.shift && !Event.current.control)
                                {
                                    _manager.ClearSelected();
                                    _manager.Select(entry);
                                }
                            }

                            break;
                        }
                        case EventType.MouseDown:
                        {
                            if (isItemUnderMouse)
                            {
                                clickedItem = true;
                                _manager.Select(entry);

                                if (Event.current.button == 0)
                                {
                                    DragAndDrop.PrepareStartDrag();

                                    var dragData = new DragData()
                                    {
                                        Entries = _manager.Selected.ToList(),
                                        SourceList = this,
                                    };

                                    DragAndDrop.SetGenericData(DragId, dragData);
                                    DragAndDrop.objectReferences = new UnityEngine.Object[0];
                                }

                                Event.current.Use();
                            }
                            break;
                        }
                    }

                    _manager.DrawItemLabel(labelRect, entry);

                    yPos += Skin.ItemHeight;
                }
            }
            GUI.EndScrollView();

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !clickedItem &&  isListUnderMouse)
            {
                _manager.ClearSelected();
            }
        }

        void OnThing2()
        {
            Debug.Log("TODO - thing2");
        }

        public class DragData
        {
            public List<DraggableListEntry> Entries;
            public DraggableList SourceList;
        }
    }
}
