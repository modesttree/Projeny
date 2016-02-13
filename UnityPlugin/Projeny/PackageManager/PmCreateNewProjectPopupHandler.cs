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
    public class PmCreateNewProjectPopupHandler
    {
        readonly Settings _settings;
        readonly PmSettings _pmSettings;
        readonly PmWindowInitializer _windowInitializer;
        readonly PrjCommandHandler _commandHandler;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmView _view;

        public PmCreateNewProjectPopupHandler(
            PmView view,
            AsyncProcessor asyncProcessor,
            PrjCommandHandler commandHandler,
            PmWindowInitializer windowInitializer,
            PmSettings pmSettings)
        {
            _settings = pmSettings.CreateNewPopup;
            _pmSettings = pmSettings;
            _windowInitializer = windowInitializer;
            _commandHandler = commandHandler;
            _asyncProcessor = asyncProcessor;
            _view = view;
        }

        public void ShowCreateNewProjectPopup()
        {
            _asyncProcessor.Process(ShowCreateNewProjectPopupAsync(), true, "Creating Project");
        }

        IEnumerator<PopupChoices> ShowPopup()
        {
            var label = "Enter new project name:";

            var choices = new PopupChoices();

            choices.NewProjectName = "Untitled";
            PmView.InputDialogStates state = PmView.InputDialogStates.None;

            bool isFirst = true;

            var popupId = _view.AddPopup(delegate(Rect fullRect)
            {
                if (Event.current.type == EventType.KeyDown)
                {
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.Return:
                        {
                            state = PmView.InputDialogStates.Submitted;
                            break;
                        }
                        case KeyCode.Escape:
                        {
                            state = PmView.InputDialogStates.Cancelled;
                            break;
                        }
                    }
                }

                var popupRect = ImguiUtil.CenterRectInRect(fullRect, _settings.PopupSize);

                _view.DrawPopupCommon(fullRect, popupRect);

                var contentRect = ImguiUtil.CreateContentRectWithPadding(
                    popupRect, _pmSettings.InputDialog.PanelPadding);

                GUILayout.BeginArea(contentRect);
                {
                    GUILayout.Label(label, _pmSettings.InputDialog.LabelStyle);

                    GUI.SetNextControlName("PopupTextField");
                    choices.NewProjectName = GUILayout.TextField(choices.NewProjectName, 100);
                    GUI.SetNextControlName("");

                    GUILayout.Space(5);

                    GUILayout.BeginHorizontal();
                    {
                        choices.DuplicateSettings = GUILayout.Toggle(choices.DuplicateSettings, "", GUILayout.Height(_settings.CheckboxHeight));
                        GUILayout.Label("Share Project Settings with '{0}'".Fmt(ProjenyEditorUtil.GetCurrentProjectName()));
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(5);

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Submit", GUILayout.MaxWidth(100)))
                        {
                            state = PmView.InputDialogStates.Submitted;
                        }

                        if (GUILayout.Button("Cancel", GUILayout.MaxWidth(100)))
                        {
                            state = PmView.InputDialogStates.Cancelled;
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

            while (state == PmView.InputDialogStates.None)
            {
                yield return null;
            }

            _view.RemovePopup(popupId);

            if (state == PmView.InputDialogStates.Submitted)
            {
                yield return choices;
            }
            else
            {
                // Just return null
            }
        }

        IEnumerator ShowCreateNewProjectPopupAsync()
        {
            while (!_windowInitializer.IsInitialized)
            {
                yield return null;
            }

            var choices = ShowPopup();

            yield return choices;

            if (choices.Current == null)
            {
                // User Cancelled
                yield break;
            }

            var projName = choices.Current.NewProjectName;
            var duplicateSettings = choices.Current.DuplicateSettings;

            yield return _commandHandler.ProcessPrjCommand(
                "Creating Project '{0}'".Fmt(projName), PrjHelper.CreateProjectAsync(projName, duplicateSettings));

            yield return _commandHandler.ProcessPrjCommand(
                "Initializing directory links", PrjHelper.UpdateLinksAsyncForProject(projName));

            yield return _commandHandler.ProcessPrjCommand(
                "Opening Unity", PrjHelper.OpenUnityForProjectAsync(projName));

            EditorApplication.Exit(0);
        }

        class PopupChoices
        {
            public string NewProjectName;
            public bool DuplicateSettings;
        }

        [Serializable]
        public class Settings
        {
            public Vector2 PopupSize;
            public float CheckboxHeight;
        }
    }
}


