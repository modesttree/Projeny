using System;
using System.IO;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Projeny.Internal;
using System.Linq;

namespace Projeny.Internal
{
    public class PmView
    {
        public event Action ViewStateChanged = delegate {};
        public event Action<ProjectConfigTypes> ClickedProjectType = delegate {};
        public event Action ClickedRefreshReleaseList = delegate {};
        public event Action ClickedRefreshPackages = delegate {};
        public event Action ClickedCreateNewPackage = delegate {};
        public event Action ClickedProjectApplyButton = delegate {};
        public event Action ClickedProjectRevertButton = delegate {};
        public event Action ClickedProjectSaveButton = delegate {};
        public event Action ClickedProjectEditButton = delegate {};
        public event Action ClickedUpdateSolution = delegate {};
        public event Action ClickedOpenSolution = delegate {};
        public event Action<DragListTypes, DragListTypes, List<DragListEntry>> DraggedDroppedListEntries = delegate {};
        public event Action ProjectConfigTypeChanged = delegate {};

        readonly Dictionary<DragListTypes, Func<IEnumerable<ContextMenuItem>>> _contextMenuHandlers = new Dictionary<DragListTypes, Func<IEnumerable<ContextMenuItem>>>();

        readonly List<PopupInfo> _popupHandlers = new List<PopupInfo>();

        readonly List<DragList> _lists = new List<DragList>();

        readonly Model _model;
        readonly Settings _settings;
        readonly PmSettings _pmSettings;

        float _split1 = 0;
        float _split2 = 0.5f;
        float _split3 = 1.0f;

        float _lastTime = 0.5f;

        int _popupIdCount;

        public PmView(
            Model model, PmSettings settings)
        {
            _settings = settings.View;
            _pmSettings = settings;
            _model = model;

            for (int i = 0; i < (int)DragListTypes.Count; i++)
            {
                Assert.That(i <= _model.ListModels.Count - 1,
                    "Could not find drag list type '{0}' in model", (DragListTypes)i);

                var list = new DragList(
                    this, (DragListTypes)i, _model.ListModels[i], settings);

                _lists.Add(list);
            }
        }

        public bool IsSaveEnabled
        {
            get;
            set;
        }

        public bool IsRevertEnabled
        {
            get;
            set;
        }

        public bool IsEditEnabled
        {
            get;
            set;
        }

        public PmViewStates ViewState
        {
            get
            {
                return _model.ViewState;
            }
            set
            {
                if (_model.ViewState != value)
                {
                    _model.ViewState = value;
                    ViewStateChanged();
                }
            }
        }

        public IEnumerable<DragList> Lists
        {
            get
            {
                return _lists;
            }
        }

        public ProjectConfigTypes ProjectConfigType
        {
            get
            {
                return _model.ProjectConfigType;
            }
            set
            {
                if (_model.ProjectConfigType != value)
                {
                    _model.ProjectConfigType = value;
                    ProjectConfigTypeChanged();
                }
            }
        }

        public string BlockedStatusMessage
        {
            get;
            set;
        }

        public string BlockedStatusTitle
        {
            get;
            set;
        }

        public void AddContextMenuHandler(DragListTypes listType, Func<IEnumerable<ContextMenuItem>> handler)
        {
            _contextMenuHandlers.Add(listType, handler);
        }

        public void RemoveContextMenuHandler(DragListTypes listType)
        {
            _contextMenuHandlers.RemoveWithConfirm(listType);
        }

        public List<DragListEntry> GetSelected(DragListTypes listType)
        {
            return GetSelected().Where(x => x.ListType == listType).ToList();
        }

        public List<DragListEntry> GetSelected()
        {
            return _lists.SelectMany(x => x.GetSelected()).ToList();
        }

        public void ClearOtherListSelected(DragListTypes type)
        {
            foreach (var list in _lists)
            {
                if (list.ListType != type)
                {
                    list.ClearSelected();
                }
            }
        }

        public void ClearSelected()
        {
            foreach (var list in _lists)
            {
                list.ClearSelected();
            }
        }

        public List<DragListEntry> SortList(DragList list, List<DragListEntry> entries)
        {
            return entries.OrderBy(x => x.Name).ToList();
            //switch (list.ListType)
            //{
                //case DragListTypes.Release:
                //{
                //}
                //default:
                //{
                    //return entries.OrderBy(x => x.Name).ToList();
                //}
            //}
        }

        public void DrawItemLabel(Rect rect, DragListEntry entry)
        {
            Assert.Throw("TODO");
            //DrawListItem(rect, entry.Name);

            //switch (entry.ListOwner.ListType)
            //{
                //case DragListTypes.Release:
                //{
                    //var info = (ReleaseInfo)(entry.Tag);

                    //var labelStr = info.Name;

                    //if (_model.IsReleaseInstalled(info))
                    //{
                        //labelStr = ImguiUtil.WrapWithColor(labelStr, _settings.Theme.DraggableItemAlreadyAddedColor);
                    //}

                    //DrawItemLabelWithVersion(rect, labelStr, info.Version);
                    //break;
                //}
                //case DragListTypes.Package:
                //{
                //}
                //case DragListTypes.AssetItem:
                //case DragListTypes.PluginItem:
                //{
                //}
                //default:
                //{
                    //Assert.Throw();
                    //break;
                //}
            //}
        }

        public bool ShowBlockedPopup
        {
            get;
            set;
        }

        public bool IsBlocked
        {
            get;
            set;
        }

        public void SetListItems(
            DragListTypes listType, List<DragList.ItemDescriptor> items)
        {
            GetList(listType).SetItems(items);
        }

        public DragList GetList(DragListTypes listType)
        {
            return _lists[(int)listType];
        }

        public bool IsDragAllowed(DragList.DragData data, DragList list)
        {
            var sourceListType = data.SourceList.ListType;
            var dropListType = list.ListType;

            if (sourceListType == dropListType)
            {
                return true;
            }

            switch (dropListType)
            {
                case DragListTypes.Package:
                {
                    return sourceListType == DragListTypes.Release || sourceListType == DragListTypes.AssetItem || sourceListType == DragListTypes.PluginItem;
                }
                case DragListTypes.Release:
                {
                    return false;
                }
                case DragListTypes.AssetItem:
                {
                    return sourceListType == DragListTypes.Package || sourceListType == DragListTypes.PluginItem;
                }
                case DragListTypes.PluginItem:
                {
                    return sourceListType == DragListTypes.Package || sourceListType == DragListTypes.AssetItem;
                }
                case DragListTypes.VsSolution:
                {
                    return sourceListType == DragListTypes.AssetItem || sourceListType == DragListTypes.PluginItem;
                }
            }

            Assert.Throw();
            return true;
        }

        public void Update()
        {
            var deltaTime = Time.realtimeSinceStartup - _lastTime;
            _lastTime = Time.realtimeSinceStartup;

            var px = Mathf.Clamp(deltaTime * _settings.InterpSpeed, 0, 1);

            _split1 = Mathf.Lerp(_split1, GetDesiredSplit1(), px);
            _split2 = Mathf.Lerp(_split2, GetDesiredSplit2(), px);
            _split3 = Mathf.Lerp(_split3, GetDesiredSplit3(), px);
        }

        float GetDesiredSplit2()
        {
            if (ViewState == PmViewStates.ReleasesAndPackages)
            {
                return 1.0f;
            }

            if (ViewState == PmViewStates.PackagesAndProject)
            {
                return 0.4f;
            }

            Assert.That(ViewState == PmViewStates.Project || ViewState == PmViewStates.ProjectAndVisualStudio);
            return 0;
        }

        float GetDesiredSplit1()
        {
            if (ViewState == PmViewStates.ReleasesAndPackages)
            {
                return 0.5f;
            }

            return 0;
        }

        float GetDesiredSplit3()
        {
            if (ViewState == PmViewStates.ProjectAndVisualStudio)
            {
                return 0.5f;
            }

            return 1.0f;
        }

        public void DrawPopupCommon(Rect fullRect, Rect popupRect)
        {
            ImguiUtil.DrawColoredQuad(popupRect, _settings.Theme.LoadingOverlapPopupColor);
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

        public void OnDragDrop(DragList.DragData data, DragList dropList)
        {
            if (data.SourceList == dropList || !IsDragAllowed(data, dropList))
            {
                return;
            }

            var sourceListType = data.SourceList.ListType;
            var dropListType = dropList.ListType;

            DraggedDroppedListEntries(sourceListType, dropListType, data.Entries);
        }

        public void OpenContextMenu(DragList dropList)
        {
            var itemGetter = _contextMenuHandlers.TryGetValue(dropList.ListType);

            if (itemGetter != null)
            {
                ImguiUtil.OpenContextMenu(itemGetter());
            }
        }

        void DrawFileDropdown(Rect rect)
        {
            var dropDownRect = Rect.MinMaxRect(
                rect.xMin,
                rect.yMin,
                rect.xMax - _settings.FileButtonsPercentWidth * rect.width,
                rect.yMax);

            var displayValues = GetConfigTypesDisplayValues();
            var desiredConfigType = (ProjectConfigTypes)EditorGUI.Popup(dropDownRect, (int)_model.ProjectConfigType, displayValues, _settings.DropdownTextStyle);

            GUI.Button(dropDownRect, displayValues[(int)desiredConfigType], _settings.DropdownTextButtonStyle);

            if (desiredConfigType != _model.ProjectConfigType)
            {
                ClickedProjectType(desiredConfigType);
            }

            GUI.DrawTexture(new Rect(dropDownRect.xMax - _settings.ArrowSize.x + _settings.ArrowOffset.x, dropDownRect.yMin + _settings.ArrowOffset.y, _settings.ArrowSize.x, _settings.ArrowSize.y), _settings.FileDropdownArrow);

            var startX = rect.xMax - _settings.FileButtonsPercentWidth * rect.width;
            var startY = rect.yMin;
            var endX = rect.xMax;
            var endY = rect.yMax;

            var buttonPadding = _settings.FileButtonsPadding;
            var buttonWidth = ((endX - startX) - 3 * buttonPadding) / 3.0f;
            var buttonHeight = endY - startY;

            startX = startX + buttonPadding;

            bool wasEnabled;
            wasEnabled = GUI.enabled;
            GUI.enabled = IsRevertEnabled;
            if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Revert"))
            {
                ClickedProjectRevertButton();
            }
            GUI.enabled = wasEnabled;

            startX = startX + buttonWidth + buttonPadding;

            wasEnabled = GUI.enabled;
            GUI.enabled = IsSaveEnabled;
            if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Save"))
            {
                ClickedProjectSaveButton();
            }
            GUI.enabled = wasEnabled;

            startX = startX + buttonWidth + buttonPadding;

            wasEnabled = GUI.enabled;
            GUI.enabled = IsEditEnabled;
            if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Edit"))
            {
                ClickedProjectEditButton();
            }
            GUI.enabled = wasEnabled;
        }

        public IEnumerator AlertUser(string message, string title = null)
        {
            return PromptForUserChoice(message, new[] { "Ok" }, title, null, 0, 0);
        }

        public IEnumerator<int> PromptForUserChoice(
            string question, string[] choices, string title = null, string styleOverride = null, int enterChoice = -1, int exitChoice = -1)
        {
            return CoRoutine.Wrap<int>(
                PromptForUserChoiceInternal(question, choices, title, styleOverride, enterChoice, exitChoice));
        }

        public int AddPopup(Action<Rect> handler)
        {
            _popupIdCount++;
            int newId = _popupIdCount;

            Assert.That(_popupHandlers.Where(x => x.Id == newId).IsEmpty());

            _popupHandlers.Add(new PopupInfo(newId, handler));
            return newId;
        }

        public void RemovePopup(int id)
        {
            var info = _popupHandlers.Where(x => x.Id == id).Single();
            _popupHandlers.RemoveWithConfirm(info);
        }

        public IEnumerator PromptForUserChoiceInternal(
            string question, string[] choices, string title = null, string styleOverride = null, int enterChoice = -1, int escapeChoice = -1)
        {
            int choice = -1;

            var skin = _pmSettings.GenericPromptDialog;

            var popupId = AddPopup(delegate(Rect fullRect)
            {
                GUILayout.BeginArea(fullRect);
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.BeginVertical(skin.BackgroundStyle, GUILayout.Width(skin.PopupWidth));
                        {
                            GUILayout.Space(skin.PanelPadding);

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Space(skin.PanelPadding);

                                GUILayout.BeginVertical();
                                {
                                    if (title != null)
                                    {
                                        GUILayout.Label(title, skin.TitleStyle);
                                        GUILayout.Space(skin.TitleBottomPadding);
                                    }

                                    GUILayout.Label(question, styleOverride == null ? skin.LabelStyle : GUI.skin.GetStyle(styleOverride));

                                    GUILayout.Space(skin.ButtonTopPadding);

                                    GUILayout.BeginHorizontal();
                                    {
                                        GUILayout.FlexibleSpace();

                                        for (int i = 0; i < choices.Length; i++)
                                        {
                                            if (i > 0)
                                            {
                                                GUILayout.Space(skin.ButtonSpacing);
                                            }

                                            if (GUILayout.Button(choices[i], GUILayout.Width(skin.ButtonWidth)))
                                            {
                                                choice = i;
                                            }
                                        }

                                        GUILayout.FlexibleSpace();
                                    }
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.EndVertical();
                                GUILayout.Space(skin.PanelPadding);
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.Space(skin.PanelPadding);
                        }
                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndArea();

                if (Event.current.type == EventType.KeyDown)
                {
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.Return:
                        {
                            if (enterChoice >= 0)
                            {
                                Assert.That(enterChoice <= choices.Length-1);
                                choice = enterChoice;
                            }
                            break;
                        }
                        case KeyCode.Escape:
                        {
                            if (escapeChoice >= 0)
                            {
                                Assert.That(escapeChoice <= choices.Length-1);
                                choice = escapeChoice;
                            }
                            break;
                        }
                    }
                }
            });

            while (choice == -1)
            {
                yield return null;
            }

            RemovePopup(popupId);

            yield return choice;
        }

        public IEnumerator<string> PromptForInput(string label, string defaultValue)
        {
            string userInput = defaultValue;
            InputDialogStates state = InputDialogStates.None;

            bool isFirst = true;

            var popupId = AddPopup(delegate(Rect fullRect)
            {
                if (Event.current.type == EventType.KeyDown)
                {
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.Return:
                        {
                            state = InputDialogStates.Submitted;
                            break;
                        }
                        case KeyCode.Escape:
                        {
                            state = InputDialogStates.Cancelled;
                            break;
                        }
                    }
                }

                var popupRect = ImguiUtil.CenterRectInRect(fullRect, _pmSettings.InputDialog.PopupSize);

                DrawPopupCommon(fullRect, popupRect);

                var contentRect = ImguiUtil.CreateContentRectWithPadding(
                    popupRect, _pmSettings.InputDialog.PanelPadding);

                GUILayout.BeginArea(contentRect);
                {
                    GUILayout.Label(label, _pmSettings.InputDialog.LabelStyle);

                    GUI.SetNextControlName("PopupTextField");
                    userInput = GUILayout.TextField(userInput, 100);
                    GUI.SetNextControlName("");

                    GUILayout.Space(5);

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Submit", GUILayout.MaxWidth(100)))
                        {
                            state = InputDialogStates.Submitted;
                        }

                        if (GUILayout.Button("Cancel", GUILayout.MaxWidth(100)))
                        {
                            state = InputDialogStates.Cancelled;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndArea();

                if (isFirst)
                {
                    isFirst = false;
                    // Need to remove focus then regain focus on the text box for it to select the whole contents
                    GUI.FocusControl("");
                }
                else if (string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
                {
                    GUI.FocusControl("PopupTextField");
                }
            });

            while (state == InputDialogStates.None)
            {
                yield return null;
            }

            RemovePopup(popupId);

            if (state == InputDialogStates.Submitted)
            {
                yield return userInput;
            }
            else
            {
                // Just return null
            }
        }

        public void OnGUI(Rect fullRect)
        {
            GUI.skin = _pmSettings.GUISkin;

            if (IsBlocked)
            {
                // Do not allow any input processing when running an async task
                GUI.enabled = false;
            }

            DrawArrowColumns(fullRect);

            var windowRect = Rect.MinMaxRect(
                _settings.ListVerticalSpacing + _settings.ArrowWidth,
                _settings.MarginTop,
                fullRect.width - _settings.ListVerticalSpacing - _settings.ArrowWidth,
                fullRect.height - _settings.MarginBottom);

            if (_split1 >= 0.1f)
            {
                DrawReleasePane(windowRect);
            }

            if (_split2 >= 0.1f)
            {
                DrawPackagesPane(windowRect);
            }

            if (_split2 <= 0.92f)
            {
                DrawProjectPane(windowRect);
            }

            GUI.enabled = true;

            if (IsBlocked)
            {
                if (ShowBlockedPopup || !_popupHandlers.IsEmpty())
                {
                    ImguiUtil.DrawColoredQuad(fullRect, _settings.Theme.LoadingOverlayColor);

                    if (_popupHandlers.IsEmpty())
                    {
                        DisplayGenericProcessingDialog(fullRect);
                    }
                    else
                    {
                        foreach (var info in _popupHandlers)
                        {
                            info.Handler(fullRect);
                        }
                    }
                }
            }
        }

        void DisplayGenericProcessingDialog(Rect fullRect)
        {
            var skin = _pmSettings.AsyncPopupPane;
            var popupRect = ImguiUtil.CenterRectInRect(fullRect, skin.PopupSize);

            DrawPopupCommon(fullRect, popupRect);

            var contentRect = ImguiUtil.CreateContentRectWithPadding(
                popupRect, skin.PanelPadding);

            GUILayout.BeginArea(contentRect);
            {
                string title;

                if (string.IsNullOrEmpty(BlockedStatusTitle))
                {
                    title = "Processing";
                }
                else
                {
                    title = BlockedStatusTitle;
                }

                GUILayout.Label(title, skin.HeadingTextStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(skin.HeadingBottomPadding);

                string statusMessage = "";

                if (!string.IsNullOrEmpty(BlockedStatusMessage))
                {
                    statusMessage = BlockedStatusMessage;

                    int numExtraDots = (int)(Time.realtimeSinceStartup * skin.DotRepeatRate) % 4;

                    statusMessage += new String('.', numExtraDots);

                    // This is very hacky but the only way I can figure out how to keep the message a fixed length
                    // so that the text doesn't jump around as the number of dots change
                    // I tried using spaces instead of _ but that didn't work
                    statusMessage += ImguiUtil.WrapWithColor(new String('_', 3 - numExtraDots), _settings.Theme.LoadingOverlapPopupColor);
                }

                GUILayout.Label(statusMessage, skin.StatusMessageTextStyle, GUILayout.ExpandWidth(true));
            }

            GUILayout.EndArea();
        }

        void DrawArrowColumns(Rect fullRect)
        {
            var halfHeight = 0.5f * fullRect.height;

            var rect1 = new Rect(
                _settings.ListVerticalSpacing, halfHeight - 0.5f * _settings.ArrowHeight, _settings.ArrowWidth, _settings.ArrowHeight);

            if ((int)ViewState > 0)
            {
                if (GUI.Button(rect1, ""))
                {
                    ViewState = (PmViewStates)((int)ViewState - 1);
                }

                if (_settings.ArrowLeftTexture != null)
                {
                    GUI.DrawTexture(new Rect(rect1.xMin + 0.5f * rect1.width - 0.5f * _settings.ArrowButtonIconWidth, rect1.yMin + 0.5f * rect1.height - 0.5f * _settings.ArrowButtonIconHeight, _settings.ArrowButtonIconWidth, _settings.ArrowButtonIconHeight), _settings.ArrowLeftTexture);
                }
            }

            var rect2 = new Rect(fullRect.xMax - _settings.ListVerticalSpacing - _settings.ArrowWidth, halfHeight - 0.5f * _settings.ArrowHeight, _settings.ArrowWidth, _settings.ArrowHeight);

            var numValues = Enum.GetValues(typeof(PmViewStates)).Length;

            if ((int)ViewState < numValues-1)
            {
                if (GUI.Button(rect2, ""))
                {
                    ViewState = (PmViewStates)((int)ViewState + 1);
                }

                if (_settings.ArrowRightTexture != null)
                {
                    GUI.DrawTexture(new Rect(rect2.xMin + 0.5f * rect2.width - 0.5f * _settings.ArrowButtonIconWidth, rect2.yMin + 0.5f * rect2.height - 0.5f * _settings.ArrowButtonIconHeight, _settings.ArrowButtonIconWidth, _settings.ArrowButtonIconHeight), _settings.ArrowRightTexture);
                }
            }
        }

        void DrawReleasePane(Rect windowRect)
        {
            var startX = windowRect.xMin;
            var endX = windowRect.xMin + _split1 * windowRect.width - _settings.ListVerticalSpacing;
            var startY = windowRect.yMin;
            var endY = windowRect.yMax;

            DrawReleasePane2(Rect.MinMaxRect(startX, startY, endX, endY));
        }

        void DrawReleasePane2(Rect rect)
        {
            var startX = rect.xMin;
            var endX = rect.xMax;
            var startY = rect.yMin;
            var endY = startY + _settings.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Releases", _settings.HeaderTextStyle);

            var skin = _pmSettings.ReleasesPane;

            startY = endY;
            endY = rect.yMax - _settings.ApplyButtonHeight - _settings.ApplyButtonTopPadding;

            GetList(DragListTypes.Release).Draw(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + _settings.ApplyButtonTopPadding;
            endY = rect.yMax;

            if (GUI.Button(Rect.MinMaxRect(startX, startY, endX, endY), "Refresh"))
            {
                ClickedRefreshReleaseList();
            }
        }

        void DrawVisualStudioPane(Rect windowRect)
        {
            var startX = windowRect.xMin + _split3 * windowRect.width + _settings.ListVerticalSpacing;
            var endX = windowRect.xMax - _settings.ListVerticalSpacing;
            var startY = windowRect.yMin + _settings.HeaderHeight + _settings.FileDropdownHeight + _settings.FileDropDownBottomPadding;
            var endY = windowRect.yMax;

            var rect = Rect.MinMaxRect(startX, startY, endX, endY);

            DrawVisualStudioPane2(rect);
        }

        void DrawVisualStudioPane2(Rect rect)
        {
            var startX = rect.xMin;
            var endX = rect.xMax;
            var startY = rect.yMin;
            var endY = startY + _settings.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Visual Studio Solution", _settings.HeaderTextStyle);

            startY = endY;
            endY = rect.yMax - _settings.ApplyButtonHeight - _settings.ApplyButtonTopPadding;

            GetList(DragListTypes.VsSolution).Draw(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + _settings.ApplyButtonTopPadding;
            endY = rect.yMax;

            var horizMiddle = 0.5f * (rect.xMax + rect.xMin);

            endX = horizMiddle - 0.5f * _pmSettings.PackagesPane.ButtonPadding;

            if (GUI.Button(Rect.MinMaxRect(startX, startY, endX, endY), "Update Solution"))
            {
                ClickedUpdateSolution();
            }

            startX = endX + _pmSettings.PackagesPane.ButtonPadding;
            endX = rect.xMax;

            if (GUI.Button(Rect.MinMaxRect(startX, startY, endX, endY), "Open Solution"))
            {
                ClickedOpenSolution();
            }
        }

        void DrawProjectPane(Rect windowRect)
        {
            var startX = windowRect.xMin + _split2 * windowRect.width + _settings.ListVerticalSpacing;
            var endX = windowRect.xMax - _settings.ListVerticalSpacing;
            var startY = windowRect.yMin;
            var endY = windowRect.yMax;

            var headerRect = Rect.MinMaxRect(startX, startY, endX, endY);

            endX = windowRect.xMin + _split3 * windowRect.width - _settings.ListVerticalSpacing;

            var contentRect = Rect.MinMaxRect(startX, startY, endX, endY);

            DrawProjectPane2(headerRect, contentRect);

            if (_split3 <= 0.92f)
            {
                DrawVisualStudioPane(windowRect);
            }
        }

        void DrawProjectPane2(Rect headerRect, Rect contentRect)
        {
            var startY = headerRect.yMin;
            var endY = startY + _settings.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(headerRect.xMin, startY, headerRect.xMax, endY), "Project", _settings.HeaderTextStyle);

            startY = endY;
            endY = startY + _settings.FileDropdownHeight;

            DrawFileDropdown(Rect.MinMaxRect(headerRect.xMin, startY, headerRect.xMax, endY));

            startY = endY + _settings.FileDropDownBottomPadding;
            endY = startY + _settings.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(contentRect.xMin, startY, contentRect.xMax, endY), "Assets Folder", _settings.HeaderTextStyle);

            startY = endY;
            endY = contentRect.yMax - _settings.ApplyButtonHeight - _settings.ApplyButtonTopPadding;

            DrawProjectPane3(Rect.MinMaxRect(contentRect.xMin, startY, contentRect.xMax, endY));

            startY = endY + _settings.ApplyButtonTopPadding;
            endY = contentRect.yMax;

            DrawProjectButtons(Rect.MinMaxRect(contentRect.xMin, startY, contentRect.xMax, endY));
        }

        void DrawProjectPane3(Rect listRect)
        {
            var halfHeight = 0.5f * listRect.height;

            var rect1 = new Rect(listRect.x, listRect.y, listRect.width, halfHeight - 0.5f * _settings.ListHorizontalSpacing);
            var rect2 = new Rect(listRect.x, listRect.y + halfHeight + 0.5f * _settings.ListHorizontalSpacing, listRect.width, listRect.height - halfHeight - 0.5f * _settings.ListHorizontalSpacing);

            GetList(DragListTypes.AssetItem).Draw(rect1);
            GetList(DragListTypes.PluginItem).Draw(rect2);

            GUI.Label(Rect.MinMaxRect(rect1.xMin, rect1.yMax, rect1.xMax, rect2.yMin), "Plugins Folder", _settings.HeaderTextStyle);
        }

        void DrawPackagesPane(Rect windowRect)
        {
            var startX = windowRect.xMin + _split1 * windowRect.width + _settings.ListVerticalSpacing;
            var endX = windowRect.xMin + _split2 * windowRect.width - _settings.ListVerticalSpacing;
            var startY = windowRect.yMin;
            var endY = windowRect.yMax;

            DrawPackagesPane2(Rect.MinMaxRect(startX, startY, endX, endY));
        }

        void DrawPackagesPane2(Rect rect)
        {
            var startX = rect.xMin;
            var endX = rect.xMax;
            var startY = rect.yMin;
            var endY = startY + _settings.HeaderHeight;

            GUI.Label(Rect.MinMaxRect(startX, startY, endX, endY), "Packages", _settings.HeaderTextStyle);

            startY = endY;
            endY = rect.yMax - _settings.ApplyButtonHeight - _settings.ApplyButtonTopPadding;

            GetList(DragListTypes.Package).Draw(Rect.MinMaxRect(startX, startY, endX, endY));

            startY = endY + _settings.ApplyButtonTopPadding;
            endY = rect.yMax;

            var horizMiddle = 0.5f * (rect.xMax + rect.xMin);

            endX = horizMiddle - 0.5f * _pmSettings.PackagesPane.ButtonPadding;

            if (GUI.Button(Rect.MinMaxRect(startX, startY, endX, endY), "Refresh"))
            {
                ClickedRefreshPackages();
            }

            startX = endX + _pmSettings.PackagesPane.ButtonPadding;
            endX = rect.xMax;

            if (GUI.Button(Rect.MinMaxRect(startX, startY, endX, endY), "New"))
            {
                ClickedCreateNewPackage();
            }
        }

        void DrawProjectButtons(Rect rect)
        {
            var halfWidth = rect.width * 0.5f;
            var padding = 0.5f * _settings.ProjectButtonsPadding;
            var buttonWidth = 0.7f * rect.width;

            if (GUI.Button(new Rect(rect.x + halfWidth - 0.5f * buttonWidth, rect.y, buttonWidth, rect.height), "Update Directories"))
            {
                ClickedProjectApplyButton();
            }
        }

        enum InputDialogStates
        {
            None,
            Cancelled,
            Submitted
        }

        class PopupInfo
        {
            public readonly int Id;
            public readonly Action<Rect> Handler;

            public PopupInfo(int id, Action<Rect> handler)
            {
                Id = id;
                Handler = handler;
            }
        }

        // View data that needs to be saved and restored
        [Serializable]
        public class Model
        {
            public PmViewStates ViewState = PmViewStates.Project;
            public ProjectConfigTypes ProjectConfigType = ProjectConfigTypes.LocalProject;
            public List<DragList.Model> ListModels = new List<DragList.Model>();
        }

        [Serializable]
        public class Settings
        {
            public float InterpSpeed;
            public float ProcessingPopupDelayTime;

            public float HeaderHeight;
            public float ListVerticalSpacing;
            public float ListHorizontalSpacing;

            public float MarginTop;
            public float MarginBottom;

            public float ArrowWidth;
            public float ArrowHeight;

            public float FileButtonsPadding;
            public float FileButtonsPercentWidth;

            public float ApplyButtonHeight;
            public float ApplyButtonTopPadding;
            public float ProjectButtonsPadding;

            public float FileDropdownHeight;
            public float FileDropDownBottomPadding;

            public Texture2D FileDropdownArrow;

            public float ArrowButtonIconWidth;
            public float ArrowButtonIconHeight;
            public Texture2D ArrowLeftTexture;
            public Texture2D ArrowRightTexture;

            public Vector2 ArrowSize;
            public Vector2 ArrowOffset;

            public ThemeProperties Light;
            public ThemeProperties Dark;

            public ThemeProperties Theme
            {
                get
                {
                    return EditorGUIUtility.isProSkin ? Dark : Light;
                }
            }

            [Serializable]
            public class ThemeProperties
            {
                public Color VersionColor;
                public Color LoadingOverlayColor;
                public Color LoadingOverlapPopupColor;
                public Color DraggableItemAlreadyAddedColor;

                public GUIStyle DropdownTextStyle;
                public GUIStyle HeaderTextStyle;
            }

            public GUIStyle HeaderTextStyle
            {
                get
                {
                    return GUI.skin.GetStyle("HeaderTextStyle");
                }
            }

            public GUIStyle DropdownTextStyle
            {
                get
                {
                    return GUI.skin.GetStyle("DropdownTextStyle");
                }
            }

            public GUIStyle DropdownTextButtonStyle
            {
                get
                {
                    return GUI.skin.GetStyle("DropdownTextButtonStyle");
                }
            }
        }
    }
}
