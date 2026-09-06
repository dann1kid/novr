namespace UnityEngine
{
    public class AudioListener : Behaviour
    {
        public static float volume { get; set; } = 1f;
        public static bool pause { get; set; }
        public AudioVelocityUpdateMode velocityUpdateMode { get; set; }
    }

    public enum AudioVelocityUpdateMode
    {
        Auto,
        Fixed,
        Dynamic
    }

    public class AudioSource : Behaviour
    {
        public float volume { get; set; }
        public float pitch { get; set; }
        public bool loop { get; set; }
        public bool mute { get; set; }
        public AudioClip clip { get; set; }
        public void Play() { }
        public void Stop() { }
        public void Pause() { }
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position) { }
    }

    public class AudioClip : Object
    {
        public float length => 0f;
        public int samples => 0;
        public int channels => 2;
        public int frequency => 44100;
    }
}
