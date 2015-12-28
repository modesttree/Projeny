using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Projeny
{
    public class PackageManagerWindow : EditorWindow
    {
        DraggableList _available;
        DraggableList _assets;
        DraggableList _plugins;

        void OnEnable()
        {
            if (_available == null)
            {
                _available = new DraggableList();

                _available.Add("bob");
                _available.Add("joe");
                _available.Add("frank");
                _available.Add("mary");
                _available.Add("mary");
                _available.Add("zxcv");
                _available.Add("wetqsdf");
                _available.Add("dsgfasdgz");
                _available.Add("235325");
                _available.Add("623");
            }

            if (_assets == null)
            {
                _assets = new DraggableList();

                _assets.Add("john");
                _assets.Add("zack");
            }

            if (_plugins == null)
            {
                _plugins = new DraggableList();

                _plugins.Add("asdf");
                _plugins.Add("zxcv");
            }
        }

        void Update()
        {
            // Doesn't seem worth trying to detect changes, just redraw every frame
            Repaint();
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    GUILayout.Label("Project", EditorStyles.boldLabel);

                    _available.ListField(GUILayout.ExpandHeight(true));
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                {
                    GUILayout.Label("Assets", EditorStyles.boldLabel);
                    _assets.ListField(GUILayout.ExpandHeight(true));

                    GUILayout.Label("Plugins", EditorStyles.boldLabel);
                    _plugins.ListField(GUILayout.ExpandHeight(true));
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

}
