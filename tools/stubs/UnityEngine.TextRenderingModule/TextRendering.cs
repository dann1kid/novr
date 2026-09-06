namespace UnityEngine
{
    public enum TextAnchor
    {
        UpperLeft,
        UpperCenter,
        UpperRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        LowerLeft,
        LowerCenter,
        LowerRight
    }

    public enum FontStyle
    {
        Normal,
        Bold,
        Italic,
        BoldAndItalic
    }

    public enum HorizontalWrapMode
    {
        Wrap,
        Overflow
    }

    public enum VerticalWrapMode
    {
        Truncate,
        Overflow
    }

    public class Font : Object
    {
        public Font() { }
        public Font(string name) { }

        public int fontSize;
        public FontStyle fontStyle;
        public Material material;
        public string name { get; set; }

        public static string[] GetOSInstalledFontNames() => System.Array.Empty<string>();
        public static Font CreateDynamicFontFromOSFont(string fontname, int size) => null;
        public static Font CreateDynamicFontFromOSFont(string[] fontnames, int size) => null;
        public int lineHeight => 16;
        public int ascent => 12;
        public bool dynamic => true;
        public CharacterInfo[] characterInfo { get; set; }
        public bool HasCharacter(char c) => true;
        public bool RequestCharactersInTexture(string characters, int size = 0, FontStyle style = FontStyle.Normal) => true;
        public void GetCharacterInfo(char ch, out CharacterInfo info, int size = 0, FontStyle style = FontStyle.Normal) => info = default;
    }

    public struct CharacterInfo
    {
        public int index;
        public Rect uv;
        public Rect vert;
        public float width;
        public int size;
        public FontStyle style;
    }
}
