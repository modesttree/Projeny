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

        void OnEnable()
        {
            bool isFirstLoad = false;

            if (!_hasInitialized)
            {
                _hasInitialized = true;
                isFirstLoad = true;

                Assert.IsNull(_model);
                Assert.IsNull(_viewModel);

                _model = new PmModel();
                _viewModel = new PmView.Model();

                for (int i = 0; i < (int)DragListTypes.Count; i++)
                {
                    _viewModel.ListModels.Add(new DragList.Model());
                }
            }

            _root = new PmCompositionRoot(_model, _viewModel, isFirstLoad);
            _root.Initialize();
        }

        void OnDisable()
        {
            Assert.IsNotNull(_root);
            _root.Dispose();
            _root = null;
        }

        void Update()
        {
            _root.Update();

            // Doesn't seem worth trying to detect changes, just redraw every frame
            Repaint();
        }

        public void OnGUI()
        {
            var fullRect = new Rect(0, 0, this.position.width, this.position.height);
            _root.OnGUI(fullRect);
        }
    }
}
