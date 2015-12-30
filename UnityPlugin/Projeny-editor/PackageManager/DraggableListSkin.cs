using System;
using Projeny.Internal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Projeny
{
    [CreateAssetMenu]
    public class DraggableListSkin : ScriptableObject
    {
        public float ItemHeight;

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
            public Color ListColor;
            public Color ListHoverColor;

            public Color ListItemColor;
            public Color ListItemHoverColor;
        }
    }
}
