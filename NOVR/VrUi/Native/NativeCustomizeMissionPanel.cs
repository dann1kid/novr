using System;
using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.UI;

namespace NOVR.VrUi.Native;

public class NativeCustomizeMissionPanel : MonoBehaviour
{
    private static readonly Color BackgroundColor = new(0.025f, 0.035f, 0.045f, 0.96f);
    private static readonly Color PanelColor = new(0.05f, 0.06f, 0.065f, 0.96f);
    private static readonly Color ButtonColor = new(0.24f, 0.29f, 0.31f, 0.96f);
    private static readonly Color BackButtonColor = new(0.62f, 0.12f, 0.14f, 0.96f);
    private static readonly Color ApplyButtonColor = new(0.12f, 0.34f, 0.20f, 0.96f);

    private RectTransform? _container;
    private RectTransform? _contentRoot;
    private Font? _font;
    private Text? _titleText;
    private Text? _statusText;
    private Mission? _mission;
    private Action<Mission>? _onApply;
    private float _nextRowY;
    private bool _visible;

    public bool IsVisible => _visible;

    public void Initialize(RectTransform root)
    {
        _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        BuildLayout(root);
        SetVisible(false, instant: true);
    }

    public void Show(Mission mission, Action<Mission> onApply)
    {
        _mission = mission;
        _onApply = onApply;
        RenderContent();
        if (_container != null)
        {
            _container.SetAsLastSibling();
        }
        SetVisible(true);
    }

    public void Hide()
    {
        _mission = null;
        _onApply = null;
        SetVisible(false);
    }

    private void BuildLayout(RectTransform root)
    {
        _container = CreateContainer("Native Customize Mission", root, root.sizeDelta);
        CreateImage("Background", _container, BackgroundColor, Vector2.zero, _container.sizeDelta);
        _titleText = CreateText("Header", _container, "CUSTOMIZE MISSION", new Vector2(0f, NativeUiLayout.HeaderY), NativeUiLayout.HeaderSize, 22, TextAnchor.MiddleCenter, Color.white);

        var panel = CreatePanel("Settings Panel", _container, PanelColor, new Vector2(0f, -15f), new Vector2(1100f, 950f));
        _contentRoot = CreateContainer("Settings Content", panel, new Vector2(1040f, 880f));
        _contentRoot.anchoredPosition = new Vector2(0f, 10f);
        _statusText = CreateText("Status", _container, "", new Vector2(0f, NativeUiLayout.FooterY + 52f), new Vector2(900f, 36f), 14, TextAnchor.MiddleCenter, new Color(0.84f, 0.90f, 0.92f, 1f));

        CreateMenuButton("BACK", _container, new Vector2(NativeUiLayout.FooterLeftX, NativeUiLayout.FooterY), NativeUiLayout.FooterButtonSize, BackButtonColor, Hide, 15);
        CreateMenuButton("APPLY", _container, new Vector2(NativeUiLayout.FooterRightX, NativeUiLayout.FooterY), NativeUiLayout.FooterButtonSize, ApplyButtonColor, Apply, 15);
    }

    private void RenderContent()
    {
        if (_contentRoot == null || _mission == null)
        {
            return;
        }

        for (var i = _contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_contentRoot.GetChild(i).gameObject);
        }

        _nextRowY = 400f;
        if (_titleText != null)
        {
            _titleText.text = string.IsNullOrWhiteSpace(_mission.Name) ? "CUSTOMIZE MISSION" : _mission.Name.ToUpperInvariant();
        }

        CreateSection("ENVIRONMENT");
        AddFloatRow("Time of Day", () => _mission.environment.timeOfDay, value => _mission.environment.timeOfDay = value, 0f, 23.75f, 0.25f, FormatTimeOfDay);
        AddFloatRow("Weather", () => _mission.environment.weatherIntensity, value => _mission.environment.weatherIntensity = value, 0f, 1f, 0.1f, value => $"{value * 100f:0}%");
        AddFloatRow("Cloud Altitude", () => _mission.environment.cloudAltitude, value => _mission.environment.cloudAltitude = value, 500f, 8000f, 100f, value => $"{value:0} m");
        AddFloatRow("Wind Speed", () => _mission.environment.windSpeed, value => _mission.environment.windSpeed = value, 0f, 50f, 1f, value => $"{value:0} m/s");
        AddFloatRow("Wind Heading", () => _mission.environment.windHeading, value => _mission.environment.windHeading = value, 0f, 359f, 5f, value => $"{value:0}°");

        CreateSection("GAMEPLAY");
        AddToggleRow("Allow Respawn", () => _mission.missionSettings.allowRespawn, value => _mission.missionSettings.allowRespawn = value);
        AddToggleRow("Event Content", () => _mission.missionSettings.allowEventContent, value => _mission.missionSettings.allowEventContent = value);
        AddIntRow("Starting Rank", () => _mission.missionSettings.playerStartingRank, value => _mission.missionSettings.playerStartingRank = value, 0, 20, 1, value => $"{value}");
        AddFloatRow("Rank Multiplier", () => _mission.missionSettings.rankMultiplier, value => _mission.missionSettings.rankMultiplier = value, 0.25f, 3f, 0.25f, value => $"{value:0.00}x");
        AddFloatRow("Sortie Bonus", () => _mission.missionSettings.successfulSortieBonus, value => _mission.missionSettings.successfulSortieBonus = value, 0f, 1f, 0.05f, value => $"{value * 100f:0}%");
        AddFloatRow("Tactical Escalation", () => _mission.missionSettings.nuclearEscalationThreshold, value => _mission.missionSettings.nuclearEscalationThreshold = value, 0f, 1f, 0.05f, value => $"{value:0.00}");
        AddFloatRow("Strategic Escalation", () => _mission.missionSettings.strategicEscalationThreshold, value => _mission.missionSettings.strategicEscalationThreshold = value, 0f, 2f, 0.05f, value => $"{value:0.00}");

        SetStatus("Adjust environment and economy settings, then APPLY.");
    }

    private void Apply()
    {
        if (_mission == null)
        {
            return;
        }

        if (MissionSaveLoad.SaveMissionTemp(_mission, "CurrentMission", runBeforeSave: false, out var missionCopy, out var loadErrors))
        {
            _onApply?.Invoke(missionCopy);
        }
        else
        {
            Debug.LogWarning($"[NOVR] Customize mission temp save failed: {loadErrors}");
            _onApply?.Invoke(_mission);
        }

        Hide();
    }

    private void SetVisible(bool visible, bool instant = false)
    {
        _visible = visible;
        if (_container != null)
        {
            NativePanelTransition.SetVisible(_container, visible, instant);
        }
    }

    private void CreateSection(string title)
    {
        if (_contentRoot == null) return;

        CreateText(title, _contentRoot, title, new Vector2(0f, _nextRowY), new Vector2(960f, 28f), 16, TextAnchor.MiddleCenter, new Color(0.82f, 0.86f, 0.72f, 1f));
        _nextRowY -= 42f;
    }

    private void AddToggleRow(string label, Func<bool> getValue, Action<bool> setValue)
    {
        if (_contentRoot == null) return;

        var y = ConsumeRowY();
        CreateText($"{label} Label", _contentRoot, label, new Vector2(-310f, y), new Vector2(420f, 32f), 15, TextAnchor.MiddleLeft, Color.white);
        CreateMenuButton(
            getValue() ? "ON" : "OFF",
            _contentRoot,
            new Vector2(310f, y),
            new Vector2(170f, 32f),
            getValue() ? ApplyButtonColor : ButtonColor,
            () =>
            {
                setValue(!getValue());
                RenderContent();
            },
            14);
    }

    private void AddFloatRow(string label, Func<float> getValue, Action<float> setValue, float min, float max, float step, Func<float, string> format)
    {
        if (_contentRoot == null) return;

        var y = ConsumeRowY();
        CreateText($"{label} Label", _contentRoot, label, new Vector2(-310f, y), new Vector2(420f, 32f), 15, TextAnchor.MiddleLeft, Color.white);
        CreateMenuButton("-", _contentRoot, new Vector2(150f, y), new Vector2(48f, 32f), ButtonColor, () =>
        {
            setValue(Mathf.Clamp(getValue() - step, min, max));
            RenderContent();
        }, 18);
        CreateText($"{label} Value", _contentRoot, format(getValue()), new Vector2(310f, y), new Vector2(230f, 32f), 14, TextAnchor.MiddleCenter, Color.white);
        CreateMenuButton("+", _contentRoot, new Vector2(470f, y), new Vector2(48f, 32f), ButtonColor, () =>
        {
            setValue(Mathf.Clamp(getValue() + step, min, max));
            RenderContent();
        }, 18);
    }

    private void AddIntRow(string label, Func<int> getValue, Action<int> setValue, int min, int max, int step, Func<int, string> format)
    {
        if (_contentRoot == null) return;

        var y = ConsumeRowY();
        CreateText($"{label} Label", _contentRoot, label, new Vector2(-310f, y), new Vector2(420f, 32f), 15, TextAnchor.MiddleLeft, Color.white);
        CreateMenuButton("-", _contentRoot, new Vector2(150f, y), new Vector2(48f, 32f), ButtonColor, () =>
        {
            setValue(Mathf.Clamp(getValue() - step, min, max));
            RenderContent();
        }, 18);
        CreateText($"{label} Value", _contentRoot, format(getValue()), new Vector2(310f, y), new Vector2(230f, 32f), 14, TextAnchor.MiddleCenter, Color.white);
        CreateMenuButton("+", _contentRoot, new Vector2(470f, y), new Vector2(48f, 32f), ButtonColor, () =>
        {
            setValue(Mathf.Clamp(getValue() + step, min, max));
            RenderContent();
        }, 18);
    }

    private float ConsumeRowY()
    {
        var y = _nextRowY;
        _nextRowY -= 40f;
        return y;
    }

    private void SetStatus(string status)
    {
        if (_statusText != null)
        {
            _statusText.text = status;
        }
    }

    private static string FormatTimeOfDay(float hours)
    {
        hours = Mathf.Repeat(hours, 24f);
        var hour = Mathf.FloorToInt(hours);
        var minute = Mathf.FloorToInt((hours - hour) * 60f);
        return $"{hour:00}:{minute:00}";
    }

    private RectTransform CreateContainer(string name, RectTransform parent, Vector2 size)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        LayerHelper.SetLayerRecursive(gameObject.transform, LayerHelper.GetVrUiLayer());

        var rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;
        return rectTransform;
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        return CreateImage(name, parent, color, anchoredPosition, size);
    }

    private RectTransform CreateImage(string name, RectTransform parent, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        LayerHelper.SetLayerRecursive(gameObject.transform, LayerHelper.GetVrUiLayer());

        var rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        var image = gameObject.AddComponent<Image>();
        image.color = color;
        return rectTransform;
    }

    private Text CreateText(string name, RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, Color color)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        LayerHelper.SetLayerRecursive(gameObject.transform, LayerHelper.GetVrUiLayer());

        var rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        var textComponent = gameObject.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = _font;
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = color;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        return textComponent;
    }

    private Button CreateMenuButton(string label, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick, int fontSize = 15)
    {
        var rectTransform = CreateImage(label, parent, color, anchoredPosition, size);
        var button = rectTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = rectTransform.GetComponent<Image>();
        button.onClick.AddListener(onClick);
        NativeButtonFeedback.Configure(button, color);
        CreateText($"{label} Text", rectTransform, label, Vector2.zero, size, fontSize, TextAnchor.MiddleCenter, Color.white);
        return button;
    }
}
