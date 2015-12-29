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
        public float InterpSpeed;

        public float ViewToggleHeight;

        public float HeaderHeight;
        public float ListVerticalSpacing;
        public float ListHorizontalSpacing;

        public float MarginTop;
        public float MarginRight;
        public float MarginLeft;
        public float MarginBottom;

        public float ApplyButtonHeight;
        public float ApplyButtonTopPadding;
        public float ProjectButtonsPadding;

        public float FileSelectLabelWidth;

        public float FileDropdownHeight;
        public float FileDropdownTopPadding;

        public int ButtonFontSize;
        public int FilePopupFontSize;

        public float FileDropdownReloadFileButtonWidth;
        public float FileDropdownSaveFileButtonWidth;
        public float FileDropdownSaveFileButtonLeftPadding;

        public float FileDropdownOpenFileButtonWidth;
        public float FileDropdownOpenFileButtonLeftPadding;

        public Color FileDropdownBackgroundColor;
        public Texture2D FileDropdownArrow;
        public Texture2D FileDropdownBackground;

        public Vector2 ArrowSize;
        public Vector2 ArrowOffset;

        public int FileDropdownBorder;

        public float ViewSelectSpacing;

        public GUIStyle HeaderTextStyle;
        public GUIStyle DropdownTextStyle;
        public GUIStyle FileDropdownLabelTextStyle;
        public GUIStyle FileDropdownEditFileButtonTextStyle;
    }
}
