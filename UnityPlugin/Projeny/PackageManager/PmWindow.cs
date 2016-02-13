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
    // Pm = Package Manager
    public class PmWindow : EditorWindow
    {
        // These model mclasses are the only things that get serialized,
        // the other classes get re-created every assembly reload
        PmModel _model;
        PmView.Model _viewModel;

        bool _hasInitialized;

        [NonSerialized]
        PmCompositionRoot _root;

        [NonSerialized]
        GUIStyle _errorTextStyle;

        [NonSerialized]
        int _consecutiveUpdateErrorCount;

        [NonSerialized]
        int _consecutiveGuiErrorCount;

        GUIStyle ErrorTextStyle
        {
            get
            {
                if (_errorTextStyle == null)
                {
                    _errorTextStyle = new GUIStyle(GUI.skin.label);
                    _errorTextStyle.fontSize = 18;
                    _errorTextStyle.normal.textColor = Color.red;
                    _errorTextStyle.wordWrap = true;
                    _errorTextStyle.alignment = TextAnchor.MiddleCenter;
                }

                return _errorTextStyle;
            }
        }

        public void ShowCreateNewProjectPopup()
        {
            _root.ShowCreateNewProjectPopup();
        }

        void OnEnable()
        {
            Initialize();
        }

        void Initialize()
        {
            bool isFirstLoad = false;

            if (!_hasInitialized)
            {
                isFirstLoad = true;

                _model = new PmModel();
                _viewModel = new PmView.Model();

                for (int i = 0; i < (int)DragListTypes.Count; i++)
                {
                    _viewModel.ListModels.Add(new DragList.Model());
                }
            }

            _root = new PmCompositionRoot(_model, _viewModel, isFirstLoad);
            _root.Initialize();

            // Put the _hasInitialized here so that if it fails to initialize it will try again next assembly reload
            // Otherwise it might serialize half-initialized data
            _hasInitialized = true;
        }

        void OnDisable()
        {
            if (_root != null)
            {
                _root.Dispose();
                _root = null;
            }
        }

        void Update()
        {
            if (_root != null)
            {
                try
                {
                    _root.Update();
                    _consecutiveUpdateErrorCount = 0;
                }
                catch (Exception e)
                {
                    _consecutiveUpdateErrorCount += 1;
                    OnErrorOccurred(e);
                }
            }

            // Doesn't seem worth trying to detect changes, just redraw every frame
            Repaint();
        }

        void OnErrorOccurred(Exception e)
        {
            Log.ErrorException(e);

            // If we are continually getting errors every frame then just give up completely
            // To avoid freezing the unity editor
            if (_consecutiveUpdateErrorCount > 5 || _consecutiveGuiErrorCount > 5)
            {
                _root = null;
                // Try again next assembly reload
                _hasInitialized = false;
            }

            // If a one-off error occurred that doesn't repeat, display it as a popup
            if (_root != null)
            {
                _root.DisplayError(e);
            }
        }

        public void OnGUI()
        {
            if (_root == null)
            {
                var labelWidth = 600;
                var labelHeight = 200;

                GUI.Label(new Rect(Screen.width / 2 - labelWidth / 2, Screen.height / 3 - labelHeight / 2, labelWidth, labelHeight), "Unrecoverable Projeny error occurred!  \nSee log for details.", ErrorTextStyle);

                var buttonWidth = 100;
                var buttonHeight = 50;
                var offset = new Vector2(0, 100);

                // Try again next assembly reload
                _hasInitialized = false;

                if (GUI.Button(new Rect(Screen.width / 2 - buttonWidth / 2 + offset.x, Screen.height / 3 - buttonHeight / 2 + offset.y, buttonWidth, buttonHeight), "Reload"))
                {
                    Initialize();
                }
            }
            else
            {
                var fullRect = new Rect(0, 0, this.position.width, this.position.height);

                try
                {
                    _root.OnGUI(fullRect);
                    _consecutiveGuiErrorCount = 0;
                }
                catch (Exception e)
                {
                    _consecutiveGuiErrorCount += 1;
                    OnErrorOccurred(e);
                }
            }

            // For debugging
            //if (Event.current != null)
            //{
                //if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F5)
                //{
                    //_root = null;
                    //// Try again next assembly reload
                    //_hasInitialized = false;
                //}
            //}
        }
    }
}
