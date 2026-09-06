namespace UnityEngine.UIElements
{
    public class VisualElement
    {
        public string name { get; set; }
        public bool visible { get; set; } = true;
    }

    public class UIDocument : Behaviour
    {
        public VisualElement rootVisualElement { get; set; }
    }
}
