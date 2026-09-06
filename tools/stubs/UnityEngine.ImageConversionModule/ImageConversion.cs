namespace UnityEngine
{
    public static class ImageConversion
    {
        public static byte[] EncodeToPNG(this Texture2D tex) => System.Array.Empty<byte>();
        public static byte[] EncodeToJPG(this Texture2D tex) => System.Array.Empty<byte>();
        public static byte[] EncodeToJPG(this Texture2D tex, int quality) => System.Array.Empty<byte>();
        public static bool LoadImage(this Texture2D tex, byte[] data) => true;
        public static bool LoadImage(this Texture2D tex, byte[] data, bool markNonReadable) => true;
    }
}
