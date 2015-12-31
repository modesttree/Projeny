using UnityEngine;

namespace Projeny.Internal
{
    public static class ImguiUtil
    {
        public static void DrawColoredQuad(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public static Rect CreateContentRectWithPadding(Rect rect, float padding)
        {
            return Rect.MinMaxRect(rect.xMin + padding, rect.yMin + padding, rect.xMax - padding, rect.yMax - padding);
        }

        public static Rect CenterRectInRect(Rect parentRect, Vector2 size)
        {
            return new Rect(parentRect.width * 0.5f - 0.5f * size.x, 0.5f * parentRect.height - 0.5f * size.y, size.x, size.y);
        }
    }
}
