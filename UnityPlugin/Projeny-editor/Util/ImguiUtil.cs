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
    }
}
