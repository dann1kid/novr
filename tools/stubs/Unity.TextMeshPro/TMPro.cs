using UnityEngine;
using UnityEngine.UI;

namespace TMPro
{
    public enum TextOverflowModes
    {
        Overflow,
        Ellipsis,
        Masking,
        Truncate,
        ScrollRect,
        Page,
        Linked
    }

    public enum TextAlignmentOptions
    {
        TopLeft,
        Top,
        TopRight,
        Left,
        Center,
        Right,
        BottomLeft,
        Bottom,
        BottomRight,
        BaselineLeft,
        Baseline,
        BaselineRight,
        MidlineLeft,
        Midline,
        MidlineRight,
        CaplineLeft,
        Capline,
        CaplineRight
    }

    public class TMP_Text : MaskableGraphic
    {
        public string text { get; set; }
        public float fontSize { get; set; } = 24f;
        public bool enableWordWrapping { get; set; } = true;
        public bool enableAutoSizing { get; set; }
        public TextOverflowModes overflowMode { get; set; }
        public TextAlignmentOptions alignment { get; set; }
        public Color color { get; set; } = Color.white;
        public bool richText { get; set; } = true;
        public float lineSpacing { get; set; }
        public TMP_FontAsset font { get; set; }
        public virtual void SetText(string sourceText) => text = sourceText;
        public virtual void ForceMeshUpdate(bool ignoreActiveState = false, bool forceTextWrapping = false) { }
        public Bounds textBounds => default;
        public int textInfoCount => 0;
    }

    public class TextMeshProUGUI : TMP_Text
    {
        public bool raycastTarget { get; set; } = true;
    }

    public class TextMeshPro : TMP_Text
    {
    }

    public class TMP_FontAsset : ScriptableObject
    {
        public static TMP_FontAsset defaultFontAsset { get; set; }
    }

    public class TMP_Dropdown : Selectable
    {
        public RectTransform template;
        public TMP_Text captionText;
        public Image captionImage;
        public TMP_Text itemText;
        public Image itemImage;
        public int value { get; set; }
        public bool IsExpanded => false;
        public DropdownEvent onValueChanged { get; } = new DropdownEvent();
        public void Show() { }
        public void Hide() { }
        public void RefreshShownValue() { }
        public class DropdownEvent : UnityEngine.Events.UnityEvent<int> { }
        public class OptionData
        {
            public string text;
            public Sprite image;
        }
    }

    public class TMP_InputField : Selectable
    {
        public string text { get; set; }
        public TMP_Text textComponent;
        public bool isFocused => false;
        public OnChangeEvent onValueChanged { get; } = new OnChangeEvent();
        public class OnChangeEvent : UnityEngine.Events.UnityEvent<string> { }
    }
}
