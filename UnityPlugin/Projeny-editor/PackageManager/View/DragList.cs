using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Projeny.Internal;

namespace Projeny.Internal
{
    public enum DragListTypes
    {
        Package,
        Release,
        AssetItem,
        PluginItem,
        Count
    }

    public class DragListEntry
    {
        public string Name;
        public object Model;
        public int Index;
        public DragList ListOwner;
        public DragListTypes ListType;
        public bool IsSelected;
    }

    public class DragList
    {
        public event Action SortMethodChanged = delegate {};
        public event Action SortDescendingChanged = delegate {};

        static readonly string DragId = "DragListData";

        readonly List<DragListEntry> _entries = new List<DragListEntry>();
        readonly PmView _manager;

        readonly Model _model;

        PackageManagerWindowSkin _pmSkin;

        readonly DragListTypes _listType;
        static DraggableListSkin _skin;

        readonly List<string> _sortMethodCaptions = new List<string>();

        public DragList(
            PmView manager, DragListTypes listType,
            Model model)
        {
            _model = model;
            _manager = manager;
            _listType = listType;
        }

        public bool ShowSortPane
        {
            get;
            set;
        }

        public List<string> SortMethodCaptions
        {
            set
            {
                Assert.That(_sortMethodCaptions.IsEmpty());
                _sortMethodCaptions.AddRange(value);
            }
        }

        public int SortMethod
        {
            get
            {
                return _model.SortMethod;
            }
            set
            {
                if (_model.SortMethod != value)
                {
                    _model.SortMethod = value;
                    SortMethodChanged();
                }
            }
        }

        public bool SortDescending
        {
            get
            {
                return _model.SortDescending;
            }
            set
            {
                if (_model.SortDescending != value)
                {
                    _model.SortDescending = value;
                    SortDescendingChanged();
                }
            }
        }

        public DragListTypes ListType
        {
            get
            {
                return _listType;
            }
        }

        // Temporary
        PackageManagerWindowSkin PmSkin
        {
            get
            {
                return _pmSkin ?? (_pmSkin = Resources.Load<PackageManagerWindowSkin>("Projeny/PackageManagerSkin"));
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

        public IEnumerable<DragListEntry> Values
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

        public void SelectAll()
        {
            foreach (var entry in _entries)
            {
                entry.IsSelected = true;
            }
        }

        public void Remove(DragListEntry entry)
        {
            _entries.RemoveWithConfirm(entry);
            UpdateIndices();
        }

        public void SetItems(List<ItemDescriptor> newItems)
        {
            var oldEntries = _entries.ToDictionary(x => x.Model, x => x);

            _entries.Clear();

            for (int i = 0; i < newItems.Count; i++)
            {
                var item = newItems[i];

                var entry = new DragListEntry()
                {
                    Name = item.Caption,
                    Model = item.Model,
                    ListOwner = this,
                    ListType = _listType,
                    Index = i,
                };

                var oldEntry = oldEntries.TryGetValue(item.Model);

                if (oldEntry != null)
                {
                    entry.IsSelected = oldEntry.IsSelected;
                }

                _entries.Add(entry);
            }
        }

        public DragListEntry GetAtIndex(int index)
        {
            return _entries[index];
        }

        public void Remove(string name)
        {
            Remove(_entries.Where(x => x.Name == name).Single());
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

        void ClickSelect(DragListEntry newEntry)
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

        public List<DragListEntry> GetSelected()
        {
            return _entries.Where(x => x.IsSelected).ToList();
        }

        void DrawSearchPane(Rect rect)
        {
            Assert.That(ShowSortPane);

            var startX = rect.xMin;
            var endX = rect.xMax;
            var startY = rect.yMin;
            var endY = rect.yMax;

            var skin = PmSkin.ReleasesPane;

            ImguiUtil.DrawColoredQuad(rect, skin.IconRowBackgroundColor);

            endX = rect.xMax - 2 * skin.ButtonWidth;

            var searchBarRect = Rect.MinMaxRect(startX, startY, endX, endY);
            if (GUI.enabled && searchBarRect.Contains(Event.current.mousePosition))
            {
                ImguiUtil.DrawColoredQuad(searchBarRect, skin.MouseOverBackgroundColor);
            }

            GUI.Label(new Rect(startX + skin.SearchIconOffset.x, startY + skin.SearchIconOffset.y, skin.SearchIconSize.x, skin.SearchIconSize.y), skin.SearchIcon);

            this.SearchFilter = GUI.TextField(
                searchBarRect, this.SearchFilter, skin.SearchTextStyle);

            startX = endX;
            endX = startX + skin.ButtonWidth;

            Rect buttonRect;

            buttonRect = Rect.MinMaxRect(startX, startY, endX, endY);
            if (buttonRect.Contains(Event.current.mousePosition))
            {
                ImguiUtil.DrawColoredQuad(buttonRect, skin.MouseOverBackgroundColor);

                if (Event.current.type == EventType.MouseDown)
                {
                    SortDescending = !SortDescending;
                    this.UpdateIndices();
                }
            }
            GUI.DrawTexture(buttonRect, SortDescending ? skin.SortDirUpIcon : skin.SortDirDownIcon);

            startX = endX;
            endX = startX + skin.ButtonWidth;

            buttonRect = Rect.MinMaxRect(startX, startY, endX, endY);
            if (buttonRect.Contains(Event.current.mousePosition))
            {
                ImguiUtil.DrawColoredQuad(buttonRect, skin.MouseOverBackgroundColor);

                if (Event.current.type == EventType.MouseDown && !_sortMethodCaptions.IsEmpty())
                {
                    var startPos = new Vector2(buttonRect.xMin, buttonRect.yMax);
                    ImguiUtil.OpenContextMenu(startPos, CreateSortMethodContextMenuItems());
                }
            }
            GUI.DrawTexture(buttonRect, skin.SortIcon);
        }

        List<ContextMenuItem> CreateSortMethodContextMenuItems()
        {
            var result = new List<ContextMenuItem>();

            for (int i = 0; i < _sortMethodCaptions.Count; i++)
            {
                var closedI = i;
                result.Add(new ContextMenuItem(
                    true, _sortMethodCaptions[i], _model.SortMethod == i, () => SortMethod = closedI));
            }

            return result;
        }

        public void Draw(Rect fullRect)
        {
            Rect listRect;
            if (ShowSortPane)
            {
                var releaseSkin = PmSkin.ReleasesPane;
                var searchRect = new Rect(fullRect.xMin, fullRect.yMin, fullRect.width, releaseSkin.IconRowHeight);
                DrawSearchPane(searchRect);

                listRect = Rect.MinMaxRect(
                    fullRect.xMin, fullRect.yMin + releaseSkin.IconRowHeight, fullRect.xMax, fullRect.yMax);
            }
            else
            {
                listRect = fullRect;
            }

            var searchFilter = _model.SearchFilter.Trim().ToLowerInvariant();
            var visibleEntries = _entries.Where(x => x.Name.ToLowerInvariant().Contains(searchFilter)).ToList();

            var viewRect = new Rect(0, 0, listRect.width - 30.0f, visibleEntries.Count * Skin.ItemHeight);

            var isListUnderMouse = listRect.Contains(Event.current.mousePosition);

            ImguiUtil.DrawColoredQuad(listRect, GetListBackgroundColor(isListUnderMouse));

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
                                    ClickSelect(entry);
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
                                ClickSelect(entry);

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

        Color GetListBackgroundColor(bool isHover)
        {
            if (!GUI.enabled)
            {
                return Skin.Theme.ListColor;
            }

            if (_model.SearchFilter.Trim().Count() > 0)
            {
                return isHover ? Skin.Theme.FilteredListHoverColor : Skin.Theme.FilteredListColor;
            }

            return isHover ? Skin.Theme.ListHoverColor : Skin.Theme.ListColor;
        }

        public class DragData
        {
            public List<DragListEntry> Entries;
            public DragList SourceList;
        }

        // View data that needs to be saved and restored
        [Serializable]
        public class Model
        {
            public Vector2 ScrollPos;
            public string SearchFilter = "";

            public int SortMethod;
            public bool SortDescending;
        }

        public class ItemDescriptor
        {
            public string Caption;
            public object Model;
        }
    }
}
