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
        // Note here that the model is the only thing that gets serialized,
        // the other classes get re-created every assembly reload
        PmModel _model;

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
                _model = new PmModel();
            }

            _controller = new PmController(_model);
            _controller.Initialize();

            if (isFirstLoad)
            {
                Log.Trace("STEVETODO");
                //AddBackgroundTask(RefreshAll(), "Refreshing Packages");
            }
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
