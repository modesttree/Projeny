using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Projeny.Internal;

namespace Projeny.Internal
{
    public class DraggableListEntry
    {
        public string Name;
        public object Tag;
        public int Index;
        public DraggableList ListOwner;
        public ListTypes ListType;
        public bool IsSelected;
    }

    public class DraggableList
    {
        static readonly string DragId = "DraggableListData";

        readonly List<DraggableListEntry> _entries = new List<DraggableListEntry>();
        readonly PmView _manager;

        readonly Model _model;

        readonly ListTypes _listType;
        static DraggableListSkin _skin;

        public DraggableList(
            PmView manager, ListTypes listType,
            Model model)
        {
            _model = model;
            _manager = manager;
            _listType = listType;
        }

        public ListTypes ListType
        {
            get
            {
                return _listType;
            }
        }

        DraggableListSkin Skin
        {
            get
            {
                return _skin ?? (_skin = Resources.Load<DraggableListSkin>("Projeny/DraggableListSkin"));
            }
        }

        public string SearchFilter
        {
            get
            {
                return _model.SearchFilter;
            }
            set
            {
                Assert.IsNotNull(value);
                _model.SearchFilter = value;
            }
        }

        public IEnumerable<DraggableListEntry> Values
        {
            get
            {
                return _entries;
            }
        }

        public IEnumerable<string> DisplayValues
        {
            get
            {
                return _entries.Select(x => x.Name);
            }
        }

        public void ClearSelected()
        {
            foreach (var entry in _entries)
            {
                entry.IsSelected = false;
            }
        }

        public void Remove(DraggableListEntry entry)
        {
            _entries.RemoveWithConfirm(entry);
            UpdateIndices();
        }

        public DraggableListEntry GetAtIndex(int index)
        {
            return _entries[index];
        }

        public void Remove(string name)
        {
            Remove(_entries.Where(x => x.Name == name).Single());
        }

        public void Add(string name, object tag)
        {
            var entry = new DraggableListEntry()
            {
                Name = name,
                Tag = tag,
                ListOwner = this,
                ListType = _listType,
            };

            _entries.Add(entry);
            UpdateIndices();
        }

        public void Add(string entry)
        {
            Add(entry, null);
        }

        public void Clear()
        {
            _entries.Clear();
        }

        public void UpdateIndices()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Index = i;
            }
        }

        void Select(DraggableListEntry newEntry)
        {
            if (newEntry.IsSelected)
            {
                if (Event.current.control)
                {
                    newEntry.IsSelected = false;
                }

                return;
            }

            if (!Event.current.control && !Event.current.shift)
            {
                _manager.ClearSelected();
            }

            // The selection entry list should all be from the same list
            _manager.ClearOtherListSelected(_listType);

            var selected = GetSelected();

            if (Event.current.shift && !selected.IsEmpty())
            {
                var closestEntry = selected
                    .Select(x => new { Distance = Mathf.Abs(x.Index - newEntry.Index), Entry = x })
                    .OrderBy(x => x.Distance)
                    .Select(x => x.Entry).First();

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

                    inBetweenEntry.IsSelected = true;
                }
            }

            newEntry.IsSelected = true;
        }

        public List<DraggableListEntry> GetSelected()
        {
            return _entries.Where(x => x.IsSelected).ToList();
        }

        public void Draw(Rect listRect)
        {
            var searchFilter = _model.SearchFilter.Trim().ToLowerInvariant();
            var visibleEntries = _entries.Where(x => x.Name.ToLowerInvariant().Contains(searchFilter)).ToList();

            var viewRect = new Rect(0, 0, listRect.width - 30.0f, visibleEntries.Count * Skin.ItemHeight);

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
            _model.ScrollPos = GUI.BeginScrollView(listRect, _model.ScrollPos, viewRect);
            {
                foreach (var entry in visibleEntries)
                {
                    var labelRect = new Rect(0, yPos, listRect.width, Skin.ItemHeight);

                    bool isItemUnderMouse = labelRect.Contains(Event.current.mousePosition);

                    Color itemColor;

                    if (entry.IsSelected)
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
                                    Select(entry);
                                }
                            }

                            break;
                        }
                        case EventType.MouseDown:
                        {
                            if (isItemUnderMouse)
                            {
                                // Unfocus on text field
                                GUI.FocusControl(null);

                                clickedItem = true;
                                Select(entry);

                                if (Event.current.button == 0)
                                {
                                    DragAndDrop.PrepareStartDrag();

                                    var dragData = new DragData()
                                    {
                                        Entries = GetSelected(),
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

                    GUI.Label(labelRect, entry.Name, Skin.ItemTextStyle);

                    yPos += Skin.ItemHeight;
                }
            }
            GUI.EndScrollView();

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !clickedItem &&  isListUnderMouse)
            {
                // Unfocus on text field
                GUI.FocusControl(null);

                _manager.ClearSelected();
            }
        }

        public class DragData
        {
            public List<DraggableListEntry> Entries;
            public DraggableList SourceList;
        }

        // View data that needs to be saved and restored
        [Serializable]
        public class Model
        {
            public Vector2 ScrollPos;
            public string SearchFilter = "";
            public List<int> SelectedIndices = new List<int>();
        }
    }
}
