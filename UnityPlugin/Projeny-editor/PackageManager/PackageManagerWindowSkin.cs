
using System;
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
        public GUISkin GUISkinDark;
        public GUISkin GUISkinLight;

        public GUISkin GUISkin
        {
            get
            {
                return EditorGUIUtility.isProSkin ? GUISkinDark : GUISkinLight;
            }
        }

        public float InterpSpeed;

        public float HeaderHeight;
        public float ListVerticalSpacing;
        public float ListHorizontalSpacing;

        public float MarginTop;
        public float MarginBottom;

        public float ArrowWidth;
        public float ArrowHeight;

        public float FileButtonsPadding;
        public float FileButtonsPercentWidth;

        public float ApplyButtonHeight;
        public float ApplyButtonTopPadding;
        public float ProjectButtonsPadding;

        public float ProcessingDotRepeatRate;

        public float FileDropdownHeight;

        public int ButtonFontSize;

        public Texture2D FileDropdownArrow;
        public Texture2D FileDropdownBackground;

        public float ArrowButtonIconWidth;
        public float ArrowButtonIconHeight;
        public Texture2D ArrowLeftTexture;
        public Texture2D ArrowRightTexture;

        public Vector2 ArrowSize;
        public Vector2 ArrowOffset;

        public Vector2 ProcessingPopupSize;

        public GUIStyle ProcessingPopupTextStyle;

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
            public Color LoadingOverlayColor;
            public Color LoadingOverlapPopupColor;
            public GUIStyle DropdownTextStyle;
            public GUIStyle HeaderTextStyle;
        }
    }
}

