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

        GUIStyle _errorTextStyle;

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
                }
                catch (Exception e)
                {
                    Log.ErrorException(e);
                    _root = null;
                }
            }

            // Doesn't seem worth trying to detect changes, just redraw every frame
            Repaint();
        }

        public void OnGUI()
        {
            if (_root == null)
            {
                var width = 600;
                var height = 200;

                GUI.Label(new Rect(Screen.width / 2 - width / 2, Screen.height / 3 - height / 2, width, height), "Projeny error occurred!  \nSee log for details.", ErrorTextStyle);
            }
            else
            {
                var fullRect = new Rect(0, 0, this.position.width, this.position.height);

                try
                {
                    _root.OnGUI(fullRect);
                }
                catch (Exception e)
                {
                    Log.ErrorException(e);
                    _root = null;
                }
            }
        }
    }
}
