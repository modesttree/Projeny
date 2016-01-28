using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ModestTree.Util
{
    public class ContextMenuItem
    {
        public readonly bool IsEnabled;
        public readonly string Caption;
        public readonly Action Handler;
        public readonly bool IsChecked;

        public ContextMenuItem(
            bool isEnabled, string caption, bool isChecked, Action handler)
        {
            IsEnabled = isEnabled;
            Caption = caption;
            Handler = handler;
            IsChecked = isChecked;
        }
    }

    public static class ImguiUtil
    {
        public static void DrawColoredQuad(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public static string WrapWithColor(string text, Color color)
        {
            return "<color=#{0}>{1}</color>".Fmt(ColorToHex(color), text);
        }

        static string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }

        public static Rect CreateContentRectWithPadding(Rect rect, float padding)
        {
            return Rect.MinMaxRect(rect.xMin + padding, rect.yMin + padding, rect.xMax - padding, rect.yMax - padding);
        }

        public static Rect CenterRectInRect(Rect parentRect, Vector2 size)
        {
            return new Rect(parentRect.width * 0.5f - 0.5f * size.x, 0.5f * parentRect.height - 0.5f * size.y, size.x, size.y);
        }

        public static void OpenContextMenu(Vector2 startPos, IEnumerable<ContextMenuItem> items)
        {
            GenericMenu contextMenu = new GenericMenu();

            foreach (var item in items)
            {
                var handler = item.Handler;
                contextMenu.AddOptionalItem(
                    item.IsEnabled, new GUIContent(item.Caption), item.IsChecked, () => handler());
            }

            contextMenu.DropDown(new Rect(startPos.x, startPos.y, 0, 0));
        }

        public static void OpenContextMenu(IEnumerable<ContextMenuItem> items)
        {
            GenericMenu contextMenu = new GenericMenu();

            foreach (var item in items)
            {
                var handler = item.Handler;
                contextMenu.AddOptionalItem(
                    item.IsEnabled, new GUIContent(item.Caption), item.IsChecked, () => handler());
            }

            contextMenu.ShowAsContext();
        }
    }
}
