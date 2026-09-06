namespace UnityEngine
{
    public static class JsonUtility
    {
        public static string ToJson(object obj) => "{}";
        public static string ToJson(object obj, bool prettyPrint) => "{}";
        public static T FromJson<T>(string json) => default;
        public static object FromJson(string json, System.Type type) => null;
        public static void FromJsonOverwrite(string json, object objectToOverwrite) { }
    }
}
