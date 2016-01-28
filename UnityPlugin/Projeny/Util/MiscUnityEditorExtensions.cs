using System;
using UnityEditor;
using UnityEngine;

namespace ModestTree.Util
{
    public static class MiscUnityEditorExtensions
    {
        public static void AddOptionalItem(this GenericMenu menu, bool isEnabled, GUIContent content, bool isOn, GenericMenu.MenuFunction handler)
        {
            if (isEnabled)
            {
                menu.AddItem(content, isOn, handler);
            }
            else
            {
                menu.AddDisabledItem(content);
            }
        }
    }
}
