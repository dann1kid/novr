using System;
using UnityEngine;
using UnityEngine.UI;

namespace NOVR.VrUi.Native;

public class NativeMainMenuShell : MonoBehaviour
{
    private static readonly Color PanelColor = new(0.05f, 0.065f, 0.075f, 0.92f);
    private static readonly Color ButtonColor = new(0.24f, 0.29f, 0.31f, 0.96f);
    private static readonly Color ExitButtonColor = new(0.62f, 0.12f, 0.14f, 0.96f);
    private const float PrimaryButtonStartY = 250f;
    private const float PrimaryButtonSpacingY = 68f;

    private NativeGameActionAdapter? _actions;
    private RectTransform? _rectTransform;
    private RectTransform? _containerTransform;
    private GameObject? _container;
    private Font? _font;
    private Action? _openVrUiSettings;

    public void Initialize(NativeGameActionAdapter actions, RectTransform rectTransform, Action openVrUiSettings)
    {
        _actions = actions;
        _rectTransform = rectTransform;
        _openVrUiSettings = openVrUiSettings;
        _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        BuildLayout();
    }

    private void BuildLayout()
    {
        if (_rectTransform == null) return;

        var container = CreateContainer("Native Main Menu Shell", _rectTransform, _rectTransform.sizeDelta);
        _containerTransform = container;
        _container = container.gameObject;

        CreateText("Header", container, "MAIN MENU", new Vector2(0f, NativeUiLayout.HeaderY), NativeUiLayout.HeaderSize, 22, TextAnchor.MiddleCenter, Color.white);
        CreateText("Game Title", container, "NUCLEAR OPTION", new Vector2(760f, 470f), new Vector2(440f, 60f), 40, TextAnchor.MiddleRight, Color.white);

        var rail = CreatePanel("Main Menu Rail", container, PanelColor, new Vector2(-850f, -15f), new Vector2(250f, 950f));
        CreateLogo("Menu Header Logo", rail, new Vector2(0f, 405f), new Vector2(118f, 118f));
        CreateText("Menu Header", rail, "NOVR", new Vector2(0f, 325f), new Vector2(210f, 30f), 20, TextAnchor.MiddleCenter, Color.white);

        var primaryButtons = new[]
        {
            new MenuButton("SINGLE PLAYER", () => InvokeAction(NativeGameAction.SinglePlayer)),
            new MenuButton("MULTIPLAYER", () => InvokeAction(NativeGameAction.Multiplayer)),
            new MenuButton("MISSION EDITOR", () => InvokeAction(NativeGameAction.MissionEditor)),
            new MenuButton("SETTINGS", () => InvokeAction(NativeGameAction.Settings)),
            new MenuButton("VR UI SETTINGS", OpenVrUiSettings),
            new MenuButton("ENCYCLOPEDIA", () => InvokeAction(NativeGameAction.Encyclopedia)),
            new MenuButton("WORKSHOP", () => InvokeAction(NativeGameAction.Workshop))
        };

        for (var index = 0; index < primaryButtons.Length; index++)
        {
            var button = primaryButtons[index];
            CreateMenuButton(
                button.Label,
                rail,
                new Vector2(0f, PrimaryButtonStartY - index * PrimaryButtonSpacingY),
                new Vector2(205f, 44f),
                ButtonColor,
                () => button.Action.Invoke(),
                button.Label.Length > 12 ? 14 : 16);
        }

        CreateMenuButton(
            "EXIT GAME",
            rail,
            new Vector2(0f, -420f),
            new Vector2(205f, 44f),
            ExitButtonColor,
            () => _actions?.QuitGame(),
            16);

        var linkPanel = CreatePanel("Secondary Links", container, PanelColor, new Vector2(-610f, -365f), new Vector2(280f, 165f));
        var secondaryButtons = new[]
        {
            new MenuAction("Change Log", NativeGameAction.ChangeLog),
            new MenuAction("Control Changes", NativeGameAction.ControlChanges),
            new MenuAction("Development Roadmap", NativeGameAction.DevelopmentRoadmap),
            new MenuAction("Join our Community", NativeGameAction.Community)
        };

        for (var index = 0; index < secondaryButtons.Length; index++)
        {
            var action = secondaryButtons[index];
            CreateMenuButton(
                action.Label,
                linkPanel,
                new Vector2(0f, 55f - index * 35f),
                new Vector2(235f, 28f),
                ButtonColor,
                () => InvokeAction(action.Action),
                13);
        }

        var tipPanel = CreatePanel("Menu Tip", container, new Color(0.02f, 0.025f, 0.032f, 0.88f), new Vector2(320f, -430f), new Vector2(650f, 92f));
        CreateText("Tip Title", tipPanel, "Did you know?", new Vector2(0f, 24f), new Vector2(600f, 24f), 15, TextAnchor.MiddleCenter, Color.white);
        CreateText("Tip Body", tipPanel, "The SAH-46 Chicane is much better protected against machine gun fire than other aircraft.", new Vector2(0f, -12f), new Vector2(590f, 40f), 14, TextAnchor.MiddleCenter, new Color(0.8f, 0.86f, 0.88f, 1f));
        NativePanelTransition.SetVisible(container, false, instant: true);
    }

    public void SetOriginalMainCanvas(GameObject? sourceMainCanvas)
    {
        // Intentionally unused. Copying the stock MainCanvas background created a
        // 2 m × 4 m world-space wall that the HMD spawned inside of.
    }

    public void SetVisible(bool visible)
    {
        if (_containerTransform != null)
        {
            NativePanelTransition.SetVisible(_containerTransform, visible);
        }
    }

    private void InvokeAction(NativeGameAction action)
    {
        _actions?.TryInvoke(action);
    }

    private void OpenVrUiSettings()
    {
        _openVrUiSettings?.Invoke();
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        return CreateImage(name, parent, color, anchoredPosition, size);
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

    private void CreateLogo(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
    {
        var texture = NativeMainMenuLogo.GetTexture();
        if (texture == null) return;

        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        LayerHelper.SetLayerRecursive(gameObject.transform, LayerHelper.GetVrUiLayer());

        var rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        var image = gameObject.AddComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private void CreateText(string name, RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, Color color)
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
    }

    private void CreateMenuButton(string label, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick, int fontSize = 15)
    {
        var rectTransform = CreateImage(label, parent, color, anchoredPosition, size);
        var button = rectTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = rectTransform.GetComponent<Image>();
        button.onClick.AddListener(onClick);

        NativeButtonFeedback.Configure(button, color);

        CreateText($"{label} Text", rectTransform, label, Vector2.zero, size, fontSize, TextAnchor.MiddleCenter, Color.white);
    }

    private readonly struct MenuAction
    {
        public MenuAction(string label, NativeGameAction action)
        {
            Label = label;
            Action = action;
        }

        public string Label { get; }
        public NativeGameAction Action { get; }
    }

    private readonly struct MenuButton
    {
        public MenuButton(string label, Action action)
        {
            Label = label;
            Action = action;
        }

        public string Label { get; }
        public Action Action { get; }
    }
}
