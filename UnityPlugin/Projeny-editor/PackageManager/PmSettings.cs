using System;
using Projeny.Internal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Projeny.Internal
{
    public class PmSettings : ScriptableObject
    {
        public GUISkin GUISkinDark;
        public GUISkin GUISkinLight;

        public DragListSettings DragList;
        public PmView.Settings View;

        public PackagesPaneProperties PackagesPane;
        public InputDialogProperties InputDialog;
        public SavePromptDialogProperties SavePromptDialog;
        public ReleaseInfoMoreInfoDialogProperties ReleaseMoreInfoDialog;
        public GenericPromptDialogProperties GenericPromptDialog;
        public ReleasesPaneProperties ReleasesPane;
        public AsyncPopupPaneProperties AsyncPopupPane;

        [Serializable]
        public class DragListSettings
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

            public GUIStyle ItemTextStyle
            {
                get
                {
                    return GUI.skin.GetStyle("DraggableListItemStyle");
                }
            }

            [Serializable]
            public class ThemeProperties
            {
                public Color ListColor;
                public Color ListHoverColor;

                public Color FilteredListColor;
                public Color FilteredListHoverColor;

                public Color ListItemColor;
                public Color ListItemHoverColor;
                public Color ListItemSelectedColor;
            }
        }

        [Serializable]
        public class InputDialogProperties
        {
            public float PanelPadding;
            public Vector2 PopupSize;

            public GUIStyle LabelStyle
            {
                get
                {
                    return GUI.skin.GetStyle("InputDialogLabelStyle");
                }
            }
        }

        [Serializable]
        public class SavePromptDialogProperties
        {
            public float PanelPadding;
            public float ButtonPadding;
            public float ButtonTopPadding;
            public Vector2 PopupSize;

            public GUIStyle LabelStyle
            {
                get
                {
                    return GUI.skin.GetStyle("InputDialogLabelStyle");
                }
            }
        }

        [Serializable]
        public class ReleaseInfoMoreInfoDialogProperties
        {
            public float PanelPadding;
            public float LabelColumnWidth;
            public float ValueColumnWidth;
            public float ColumnSpacing;
            public float HeadingBottomPadding;
            public float ListPaddingTop;
            public float RowSpacing;
            public float ListHeight;

            public float MarginBottom;
            public float OkButtonWidth;
            public float OkButtonHeight;
            public float OkButtonTopPadding;
            public Color NotAvailableColor;

            public Vector2 PopupSize;

            public GUIStyle ScrollViewStyle
            {
                get
                {
                    return GUI.skin.GetStyle("MoreInfoScrollViewStyle");
                }
            }

            public GUIStyle LabelStyle
            {
                get
                {
                    return GUI.skin.GetStyle("MoreInfoDialogLabelStyle");
                }
            }

            public GUIStyle ValueStyle
            {
                get
                {
                    return GUI.skin.GetStyle("MoreInfoDialogValueStyle");
                }
            }

            public GUIStyle HeadingStyle
            {
                get
                {
                    return GUI.skin.GetStyle("InputDialogLabelStyle");
                }
            }
        }

        [Serializable]
        public class AsyncPopupPaneProperties
        {
            public float PanelPadding;
            public Vector2 PopupSize;
            public float DotRepeatRate;
            public float HeadingBottomPadding;

            public GUIStyle HeadingTextStyle
            {
                get
                {
                    return GUI.skin.GetStyle("ProcessingPopupTextStyle");
                }
            }

            public GUIStyle StatusMessageTextStyle
            {
                get
                {
                    return GUI.skin.GetStyle("ProcessingPopupStatusTextStyle");
                }
            }
        }

        [Serializable]
        public class ReleasesPaneProperties
        {
            public float ButtonSpacing;
            public float ButtonWidth;

            public Color MouseOverBackgroundColor;
            public Color IconRowBackgroundColor;

            public float IconRowHeight;
            public float SortIconRightPadding;

            public float TextFieldPaddingLeft;
            public float TextFieldPaddingRight;

            public Vector2 IconSize;
            public Vector2 SearchIconOffset;

            public Texture2D SortIcon;
            public Texture2D SortDirDownIcon;
            public Texture2D SortDirUpIcon;
            public Texture2D SearchIcon;

            public Vector2 SearchIconSize;
            public Color IconRowBackgroundColorHover;

            public GUIStyle SortButtonStyle
            {
                get
                {
                    return GUI.skin.GetStyle("ReleasePaneSortButtonStyle");
                }
            }

            public GUIStyle SearchTextStyle
            {
                get
                {
                    return GUI.skin.GetStyle("ReleasePaneSearchTextStyle");
                }
            }
        }

        [Serializable]
        public class GenericPromptDialogProperties
        {
            public float PanelPadding;
            public float ButtonTopPadding;
            public float ButtonWidth;
            public float ButtonSpacing;
            public float TitleBottomPadding;
            public float PopupWidth;

            public GUIStyle TitleStyle
            {
                get
                {
                    return GUI.skin.GetStyle("GenericPromptTitleStyle");
                }
            }

            public GUIStyle LabelStyle
            {
                get
                {
                    return GUI.skin.GetStyle("GenericPromptLabelStyle");
                }
            }

            public GUIStyle BackgroundStyle
            {
                get
                {
                    return GUI.skin.GetStyle("GenericPromptBackgroundStyle");
                }
            }
        }

        [Serializable]
        public class PackagesPaneProperties
        {
            public float ButtonPadding;
        }

        public GUISkin GUISkin
        {
            get
            {
                return EditorGUIUtility.isProSkin ? GUISkinDark : GUISkinLight;
            }
        }
    }
}


