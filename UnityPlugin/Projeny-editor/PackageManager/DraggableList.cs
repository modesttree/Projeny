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
    }

    public class DraggableList
    {
        static readonly string DragId = "DraggableListData";

        readonly List<DraggableListEntry> _entryList = new List<DraggableListEntry>();
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
            _manager.Deselect(entry);
            _entryList.RemoveWithConfirm(entry);
            UpdateIndices();
        }

        public DraggableListEntry GetAtIndex(int index)
        {
            return _entryList[index];
        }

        public void Remove(string name)
        {
            Remove(_entryList.Where(x => x.Name == name).Single());
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

            _entryList.Add(entry);
            UpdateIndices();
        }

        public void Add(string entry)
        {
            Add(entry, null);
        }

        public void Clear()
        {
            foreach (var entry in _entryList)
            {
                _manager.Deselect(entry);
            }
            _entryList.Clear();
        }

        public void UpdateIndices()
        {
            for (int i = 0; i < _entryList.Count; i++)
            {
                _entryList[i].Index = i;
            }
        }

        public void Draw(Rect listRect)
        {
            var searchFilter = _model.SearchFilter.Trim().ToLowerInvariant();
            var visibleEntries = _entryList.Where(x => x.Name.ToLowerInvariant().Contains(searchFilter)).ToList();

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
                                // Unfocus on text field
                                GUI.FocusControl(null);

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
        }
    }
}
