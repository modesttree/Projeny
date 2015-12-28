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

        public Color ListColor;
        public Color ListHoverColor;

        public Color ListItemColor;
        public Color ListItemHoverColor;

        public GUIStyle ItemTextStyle;
        public GUIStyle ListStyle;

        public GUIStyle Scrollbar;
    }
}

