using Projeny.Internal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Projeny
{
    [CreateAssetMenu]
    public class PackageManagerWindowSkin : ScriptableObject
    {
        public float HeaderHeight;
        public float ListVerticalSpacing;
        public float ListHorizontalSpacing;
        public float MarginRight;
        public float MarginLeft;
        public float MarginBottom;

        public GUIStyle HeaderTextStyle;
    }
}
