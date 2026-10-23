using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Camp Expedition Menu Style")]
    public sealed class CampExpeditionMenuStyle : ScriptableObject
    {
        [Serializable] public struct Glyph { public string key; public Sprite sprite; }
        public Texture2D backdrop;
        public Sprite grain, iron, character;
        public Font font;
        private static Font platformFont;
        public Font ResolveFont()
        {
            if(platformFont!=null)return platformFont;
            foreach(var name in Font.GetOSInstalledFontNames())
                if(name=="Segoe UI"){platformFont=Font.CreateDynamicFontFromOSFont(name,18);return platformFont;}
            return font;
        }
        public Glyph[] glyphs = Array.Empty<Glyph>();
        public Color background = new Color32(24, 27, 26, 255);
        public Color surface = new Color32(34, 38, 36, 255);
        public Color raised = new Color32(42, 46, 43, 255);
        public Color ink = new Color32(222, 216, 199, 255);
        public Color muted = new Color32(166, 169, 155, 255);
        public Color line = new Color32(65, 69, 62, 255);
        public Color accent = new Color32(207, 155, 107, 255);
        public Color gold = new Color32(220, 178, 103, 255);

        public Sprite FindGlyph(string key)
        { foreach (var glyph in glyphs) if (glyph.key == key) return glyph.sprite; return null; }
    }
}
