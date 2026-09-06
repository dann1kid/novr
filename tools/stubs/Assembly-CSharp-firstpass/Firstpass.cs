namespace NaturalPoint.TrackIR
{
    public class TrackIRClient
    {
        public bool IsConnected => false;
        public void Connect() { }
        public void Disconnect() { }
    }

    public static class TrackIR
    {
        public static bool available => false;
        public static bool connected => false;
    }
}

namespace Rewired.UI.ControlMapper
{
    public class ControlMapper : UnityEngine.MonoBehaviour
    {
        public bool isOpen { get; private set; }
        public void Open() { isOpen = true; }
        public void Close(bool save) { isOpen = false; }
        public void Close() { isOpen = false; }
        public UnityEngine.Events.UnityEvent onScreenClosed = new UnityEngine.Events.UnityEvent();
        public UnityEngine.Events.UnityEvent onScreenOpened = new UnityEngine.Events.UnityEvent();
    }
}
