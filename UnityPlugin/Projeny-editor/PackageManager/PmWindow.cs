using System;
using System.IO;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Projeny.Internal;
using System.Linq;

namespace Projeny
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
        PmController _controller;

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

                for (int i = 0; i < (int)ListTypes.Count; i++)
                {
                    _viewModel.ListModels.Add(new DraggableList.Model());
                }
            }

            _controller = new PmController(_model, _viewModel, isFirstLoad);
            _controller.Initialize();
        }

        void OnDisable()
        {
            Assert.IsNotNull(_controller);
            _controller.Dispose();
            _controller = null;
        }

        void Update()
        {
            _controller.Update();

            // Doesn't seem worth trying to detect changes, just redraw every frame
            Repaint();
        }

        public void OnGUI()
        {
            var fullRect = new Rect(0, 0, this.position.width, this.position.height);
            _controller.OnGUI(fullRect);
        }
    }
}
