using System.Collections.Generic;
using NOVR.VrUi.SpecialBehavior;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace NOVR.VrUi;

[DefaultExecutionOrder(10000)]
public class VrUiCursor: NOVRBehaviour
{
    public static VrUiCursor? Instance { get; private set; }
    public static VrUiCursor? I => Instance;

    public bool IsActive => _cursor != null && _cursor.activeSelf;
    public Vector3 CursorPosition => _cursor != null ? _cursor.transform.position : Vector3.zero;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        ReleaseMouseCapture();
        DestroyCursorVisual();
        if (_virtualMouse != null)
        {
            try
            {
                InputSystem.RemoveDevice(_virtualMouse);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[{nameof(VrUiCursor)}] Failed to remove VirtualMouse during OnDestroy: {ex}");
            }
            _virtualMouse = null;
        }
    }

    private const float OverlayCanvasScale = 0.003f;
    private const float OverlayCanvasDistance = 900f;
    private const float MenuCanvasFrontOffset = -24f;
    private const int CursorTextureSize = 64;
    private const float CursorRingRadius = 22f;
    private const float CursorRingThickness = 8f;
    private const float CursorDotRadius = 5f;
    private const float CursorIdlePulseScale = 0.04f;
    private const float CursorIdlePulseSpeed = 5.5f;
    private const float CursorHoverScale = 1.18f;
    private const float CursorPressedScale = 0.84f;
    private const float CursorClickPulseScale = 0.18f;
    private const float CursorClickPulseDuration = 0.18f;
    private const float CursorAnimationLerpSpeed = 24f;
    private static readonly Vector2 CrossBarSize = new Vector2(260f, 32f);
    private static readonly Vector2 CrossStemSize = new Vector2(32f, 260f);
    private static readonly Vector2 DotSize = new Vector2(28f, 28f);
    private static readonly Vector2 RingSize = new Vector2(180f, 180f);
    private static readonly Color CursorNormalColor = new Color32(40, 255, 70, 255);
    private static readonly Color CursorHoverColor = new Color32(180, 255, 190, 255);
    private static readonly Color CursorPressedColor = new Color32(255, 224, 92, 255);

    private GameObject? _cursor;
    private RectTransform? _cursorRect;
    private GameObject? _centerMarker;
    private RectTransform? _centerRect;
    private Canvas? _ownedCanvas;
    private Image? _crossBar;
    private Image? _crossStem;
    private Image? _dot;
    private Image? _ring;
    private Text? _marker;
    private readonly List<Image> _images = new();
    private Sprite? _whiteSprite;
    private Sprite? _ringSprite;
    private Transform? _boundHost;
    private Transform? _menuHost;
    private bool _loggedHost;
    private bool _cursorOverInteractive;
    private float _lastCursorClickTime = -100f;
    private bool _hasProjectionReferenceOverride;
    private Quaternion _projectionReferenceRotation = Quaternion.identity;
    private Color _displayColor = CursorNormalColor;

    private bool _hasInitializedEventSystem;
    private bool _owningMouseCapture;
    private Mouse? _virtualMouse;
    private Mouse? _realMouse;

    private int ScreenWidth => Screen.width;
    private int ScreenHeight => Screen.height;

    public Camera? UiCamera
    {
        get
        {
            if (NOUIManager.I != null)
            {
                var overlay = APIBus.CockpitHudCamera;
                if (overlay != null)
                {
                    return overlay;
                }
            }

            return APIBus.HeadsetCamera ?? Camera.main;
        }
    }

    public Vector2 GetScreenPoint()
    {
        if (_realMouse != null)
        {
            return ClampToScreen(_realMouse.position.ReadValue());
        }

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    public void SetProjectionReferenceRotation(Quaternion referenceRotation)
    {
        _projectionReferenceRotation = referenceRotation;
        _hasProjectionReferenceOverride = true;
    }

    public void ClearProjectionReferenceRotation()
    {
        _hasProjectionReferenceOverride = false;
    }

    public void BindMenuHost(Transform host)
    {
        if (host == null || host.position.y < -1000f)
        {
            return;
        }

        _boundHost = host;
    }

    private void Update()
    {
        TickCursor();
    }

    private void LateUpdate()
    {
        TickCursor();
    }

    protected override void OnBeforeRender()
    {
        base.OnBeforeRender();
        TickCursor();
    }

    protected override void OnDisable()
    {
        ReleaseMouseCapture();
        base.OnDisable();
    }

    private void TickCursor()
    {
        var menuHost = FindActiveMenuCanvas();
        // The game often sets Cursor.lockState = Locked in the hangar. That used
        // to hide the VR pointer every frame before it was drawn. Only hide it
        // in the cockpit, when no menu canvas is up.
        if (menuHost == null && Cursor.lockState == CursorLockMode.Locked)
        {
            WindowsCursorClip.Release();
            HideCursor();
            return;
        }

        if (Application.isFocused)
        {
            CaptureMouseToGameWindow();
        }
        else
        {
            ReleaseMouseCapture();
        }

        _realMouse ??= Mouse.current;
        if (_virtualMouse == null)
        {
            _virtualMouse = InputSystem.AddDevice<Mouse>("VirtualMouse");
            Debug.Log($"[NOVR] Added VirtualMouse device: name='{_virtualMouse.name}', path='{_virtualMouse.path}', displayName='{_virtualMouse.displayName}'");
        }

        if (!_hasInitializedEventSystem && RestrictUIModuleToVirtualMouse())
        {
            _hasInitializedEventSystem = true;
        }

        UpdateCursorAngles(menuHost);

        var realMouse = _realMouse;
        if (realMouse == null || _virtualMouse == null)
        {
            return;
        }

        UpdateCursorAnimation(realMouse);

        ushort buttons = 0;
        if (realMouse.leftButton.isPressed) buttons |= 1;
        if (realMouse.rightButton.isPressed) buttons |= 2;
        if (realMouse.middleButton.isPressed) buttons |= 4;

        InputState.Change(_virtualMouse, new MouseState
        {
            position = ClampToScreen(realMouse.position.ReadValue()),
            delta = realMouse.delta.ReadValue(),
            scroll = realMouse.scroll.ReadValue(),
            buttons = buttons
        });

        if (realMouse.leftButton.wasPressedThisFrame)
        {
            LogRaycastAtCursor();
        }
    }

    private void CaptureMouseToGameWindow()
    {
        _owningMouseCapture = true;
        Cursor.visible = false;
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Confined;
        }

        WindowsCursorClip.ConfineToGameWindow();
    }

    private void ReleaseMouseCapture()
    {
        if (_owningMouseCapture && Cursor.lockState == CursorLockMode.Confined)
        {
            Cursor.lockState = CursorLockMode.None;
        }

        _owningMouseCapture = false;
        WindowsCursorClip.Release();
    }

    private void HideCursor()
    {
        if (_cursor != null && _cursor.activeSelf)
        {
            _cursor.SetActive(false);
        }

        if (_centerMarker != null && _centerMarker.activeSelf)
        {
            _centerMarker.SetActive(false);
        }
    }

    private static Vector2 ClampToScreen(Vector2 mousePos)
    {
        return new Vector2(
            Mathf.Clamp(mousePos.x, 0f, Mathf.Max(1, Screen.width)),
            Mathf.Clamp(mousePos.y, 0f, Mathf.Max(1, Screen.height)));
    }

    private void UpdateCursorAngles(Transform? host)
    {
        EnsureCursorVisual();
        if (_cursor == null || _cursorRect == null)
        {
            return;
        }

        if (!_cursor.activeSelf)
        {
            _cursor.SetActive(true);
        }

        if (host != null)
        {
            AttachToMenu(host);
            PlaceOnMenu(host);
            PlaceCenterMarker(host);
            return;
        }

        if (_centerMarker != null)
        {
            _centerMarker.SetActive(false);
        }

        var camera = UiCamera;
        if (camera == null)
        {
            return;
        }

        AttachToOverlay(camera);
        PlaceOnOverlayCanvas();
    }

    private void EnsureCursorVisual()
    {
        if (_cursor != null)
        {
            return;
        }

        _whiteSprite ??= CreateWhiteSprite();
        _ringSprite ??= CreateRingSprite();

        _cursor = new GameObject("NOVR VrCursor");
        _cursorRect = _cursor.AddComponent<RectTransform>();
        _cursorRect.sizeDelta = Vector2.zero;
        _cursorRect.anchorMin = new Vector2(0.5f, 0.5f);
        _cursorRect.anchorMax = new Vector2(0.5f, 0.5f);
        _cursorRect.pivot = new Vector2(0.5f, 0.5f);

        _crossBar = CreateSolidImage("CrossBar", _cursorRect, CrossBarSize);
        _crossStem = CreateSolidImage("CrossStem", _cursorRect, CrossStemSize);
        _dot = CreateSolidImage("Dot", _cursorRect, DotSize);
        _ring = CreateSolidImage("Ring", _cursorRect, RingSize);
        _ring.sprite = _ringSprite;
        _ring.type = Image.Type.Simple;
        _ring.preserveAspect = true;

        var markerObject = new GameObject("Marker");
        markerObject.transform.SetParent(_cursorRect, false);
        var markerRect = markerObject.AddComponent<RectTransform>();
        markerRect.sizeDelta = new Vector2(220f, 220f);
        _marker = markerObject.AddComponent<Text>();
        _marker.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _marker.fontSize = 140;
        _marker.alignment = TextAnchor.MiddleCenter;
        _marker.color = CursorNormalColor;
        _marker.raycastTarget = false;
        _marker.text = "+";
        _marker.horizontalOverflow = HorizontalWrapMode.Overflow;
        _marker.verticalOverflow = VerticalWrapMode.Overflow;

        LayerHelper.SetLayerRecursive(_cursor.transform, LayerHelper.GetVrUiLayer());

        _centerMarker = new GameObject("NOVR VrCursorCenter");
        _centerRect = _centerMarker.AddComponent<RectTransform>();
        _centerRect.sizeDelta = Vector2.zero;
        _centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        _centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        _centerRect.pivot = new Vector2(0.5f, 0.5f);
        CreateSolidImage("CenterBar", _centerRect, new Vector2(420f, 40f));
        CreateSolidImage("CenterStem", _centerRect, new Vector2(40f, 420f));
        LayerHelper.SetLayerRecursive(_centerMarker.transform, LayerHelper.GetVrUiLayer());

        Debug.Log("[NOVR] VR cursor created as Unity UI graphics on the menu canvas.");
    }

    private Image CreateSolidImage(string name, RectTransform parent, Vector2 size)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        var rect = gameObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        var image = gameObject.AddComponent<Image>();
        image.sprite = _whiteSprite;
        image.color = CursorNormalColor;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        _images.Add(image);
        return image;
    }

    private void DestroyCursorVisual()
    {
        if (_cursor != null)
        {
            Destroy(_cursor);
            _cursor = null;
        }

        if (_centerMarker != null)
        {
            Destroy(_centerMarker);
            _centerMarker = null;
        }

        _cursorRect = null;
        _centerRect = null;
        _ownedCanvas = null;
        _crossBar = null;
        _crossStem = null;
        _dot = null;
        _ring = null;
        _marker = null;
        _images.Clear();
        _menuHost = null;
    }

    private void AttachToMenu(Transform host)
    {
        if (_cursor == null || _cursorRect == null)
        {
            return;
        }

        if (_ownedCanvas != null)
        {
            Destroy(_ownedCanvas);
            _ownedCanvas = null;
        }

        var canvasHost = GetBestCanvasTransform(host);
        if (_cursor.transform.parent != canvasHost)
        {
            _cursor.transform.SetParent(canvasHost, false);
            _menuHost = canvasHost;
            LayerHelper.SetLayerRecursive(_cursor.transform, LayerHelper.GetVrUiLayer());
            AdoptWorkingSprite(canvasHost);
            Canvas.ForceUpdateCanvases();
            Debug.Log($"[NOVR] VR cursor attached to '{canvasHost.name}' (lockState={Cursor.lockState}, focused={Application.isFocused}).");
        }

        _cursorRect.localScale = Vector3.one;
        _cursorRect.SetAsLastSibling();
    }

    private void AttachToOverlay(Camera overlay)
    {
        if (_cursor == null || _cursorRect == null)
        {
            return;
        }

        if (_cursor.transform.parent != overlay.transform)
        {
            _cursor.transform.SetParent(overlay.transform, false);
            _menuHost = null;
            LayerHelper.SetLayerRecursive(_cursor.transform, LayerHelper.GetVrUiLayer());
            Debug.Log("[NOVR] VR cursor attached to the VR UI overlay camera.");
        }

        if (_ownedCanvas == null)
        {
            _ownedCanvas = _cursor.AddComponent<Canvas>();
            _ownedCanvas.renderMode = RenderMode.WorldSpace;
            _ownedCanvas.overrideSorting = true;
            _ownedCanvas.sortingOrder = short.MaxValue;
            _ownedCanvas.pixelPerfect = false;
        }

        _ownedCanvas.worldCamera = overlay;
        _cursorRect.localScale = new Vector3(OverlayCanvasScale, OverlayCanvasScale, OverlayCanvasScale);
        _cursorRect.localRotation = Quaternion.identity;
    }

    private void PlaceOnMenu(Transform host)
    {
        if (_cursorRect == null)
        {
            return;
        }

        var canvasHost = _menuHost != null ? _menuHost : GetBestCanvasTransform(host);
        var hostRect = canvasHost as RectTransform ?? canvasHost.GetComponent<RectTransform>();
        var mousePos = GetScreenPoint();
        var canvas = canvasHost.GetComponent<Canvas>();
        var eventCamera = canvas != null ? canvas.worldCamera : null;
        eventCamera ??= UiCamera;

        _cursorRect.anchorMin = new Vector2(0.5f, 0.5f);
        _cursorRect.anchorMax = new Vector2(0.5f, 0.5f);
        _cursorRect.pivot = new Vector2(0.5f, 0.5f);
        _cursorRect.localRotation = Quaternion.identity;
        _cursorRect.localScale = Vector3.one;

        if (hostRect != null &&
            eventCamera != null &&
            RectTransformUtility.ScreenPointToWorldPointInRectangle(hostRect, mousePos, eventCamera, out var world))
        {
            _cursorRect.position = world;
        }
        else if (hostRect != null)
        {
            var nx = ScreenWidth > 0 ? Mathf.Clamp01(mousePos.x / ScreenWidth) : 0.5f;
            var ny = ScreenHeight > 0 ? Mathf.Clamp01(mousePos.y / ScreenHeight) : 0.5f;
            _cursorRect.anchoredPosition = new Vector2(
                Mathf.Lerp(hostRect.rect.xMin, hostRect.rect.xMax, nx),
                Mathf.Lerp(hostRect.rect.yMin, hostRect.rect.yMax, ny));
        }

        var local = _cursorRect.localPosition;
        _cursorRect.localPosition = new Vector3(local.x, local.y, MenuCanvasFrontOffset);
        _cursorRect.SetAsLastSibling();
        UpdateHoverFromScreen(mousePos);
    }

    private void PlaceCenterMarker(Transform host)
    {
        if (_centerMarker == null || _centerRect == null)
        {
            return;
        }

        var canvasHost = _menuHost != null ? _menuHost : GetBestCanvasTransform(host);
        if (_centerMarker.transform.parent != canvasHost)
        {
            _centerMarker.transform.SetParent(canvasHost, false);
            LayerHelper.SetLayerRecursive(_centerMarker.transform, LayerHelper.GetVrUiLayer());
        }

        if (!_centerMarker.activeSelf)
        {
            _centerMarker.SetActive(true);
        }

        _centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        _centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        _centerRect.pivot = new Vector2(0.5f, 0.5f);
        _centerRect.anchoredPosition = Vector2.zero;
        _centerRect.localRotation = Quaternion.identity;
        _centerRect.localScale = Vector3.one;
        _centerRect.localPosition = new Vector3(0f, 0f, MenuCanvasFrontOffset);
        _centerRect.SetAsLastSibling();
        if (_cursorRect != null)
        {
            _cursorRect.SetAsLastSibling();
        }

        if (!_loggedHost)
        {
            _loggedHost = true;
            var graphics = canvasHost.GetComponentsInChildren<Graphic>(true).Length;
            Debug.Log($"[NOVR] Center cursor marker on '{canvasHost.name}', graphics={graphics}, lockState={Cursor.lockState}.");
        }
    }

    private static Transform GetBestCanvasTransform(Transform host)
    {
        var canvases = host.GetComponentsInChildren<Canvas>(true);
        Canvas? best = host.GetComponent<Canvas>();
        var bestCount = -1;
        for (var i = 0; i < canvases.Length; i++)
        {
            var canvas = canvases[i];
            if (canvas == null || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (canvas.renderMode != RenderMode.WorldSpace || canvas.transform.position.y < -1000f)
            {
                continue;
            }

            var count = canvas.GetComponentsInChildren<Graphic>(true).Length;
            if (count > bestCount)
            {
                bestCount = count;
                best = canvas;
            }
        }

        return best != null ? best.transform : host;
    }

    private void AdoptWorkingSprite(Transform host)
    {
        var images = host.GetComponentsInChildren<Image>(true);
        Sprite? working = null;
        for (var i = 0; i < images.Length; i++)
        {
            var image = images[i];
            if (image == null || image.sprite == null || _images.Contains(image))
            {
                continue;
            }

            working = image.sprite;
            break;
        }

        if (working == null)
        {
            return;
        }

        for (var i = 0; i < _images.Count; i++)
        {
            if (_images[i] != null && _images[i] != _ring)
            {
                _images[i].sprite = working;
            }
        }
    }

    private void PlaceOnOverlayCanvas()
    {
        if (_cursorRect == null)
        {
            return;
        }

        var mousePos = GetScreenPoint();
        var nx = ScreenWidth > 0 ? Mathf.Clamp01(mousePos.x / ScreenWidth) : 0.5f;
        var ny = ScreenHeight > 0 ? Mathf.Clamp01(mousePos.y / ScreenHeight) : 0.5f;
        _cursorRect.localPosition = new Vector3((nx - 0.5f) * 1920f, (ny - 0.5f) * 1080f, OverlayCanvasDistance);
        _cursorRect.localRotation = Quaternion.identity;
        _cursorRect.localScale = new Vector3(OverlayCanvasScale, OverlayCanvasScale, OverlayCanvasScale);
        UpdateHoverFromScreen(mousePos);
        _ = _hasProjectionReferenceOverride;
        _ = _projectionReferenceRotation;
    }

    private void UpdateHoverFromScreen(Vector2 screenPos)
    {
        _cursorOverInteractive = false;
        if (EventSystem.current == null)
        {
            return;
        }

        var pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);
        foreach (var result in results)
        {
            if (result.gameObject == _cursor ||
                result.gameObject.GetComponentInParent<NOVRBlackoutCanvasBehavior>() != null ||
                result.gameObject.GetComponentInParent<global::MapIcon>() != null)
            {
                continue;
            }

            if (IsInteractiveRaycastTarget(result.gameObject))
            {
                _cursorOverInteractive = true;
                return;
            }
        }
    }

    private Transform? FindActiveMenuCanvas()
    {
        var main = FindObjectOfType<NOVRMainMenuBehavior>();
        if (IsUsableHost(main != null ? main.transform : null))
        {
            return main!.transform;
        }

        if (IsUsableHost(_boundHost))
        {
            return _boundHost;
        }

        var gameplay = FindObjectOfType<NOVRGameplayUIBehaviour>();
        if (IsUsableHost(gameplay != null ? gameplay.transform : null))
        {
            return gameplay!.transform;
        }

        return null;
    }

    private static bool IsUsableHost(Transform? host)
    {
        if (host == null || !host.gameObject.activeInHierarchy || host.position.y < -1000f)
        {
            return false;
        }

        var canvas = host.GetComponent<Canvas>();
        return canvas == null || canvas.enabled;
    }

    private static bool IsInteractiveRaycastTarget(GameObject gameObject)
    {
        var selectable = gameObject.GetComponentInParent<Selectable>();
        if (selectable != null)
        {
            return selectable.IsInteractable();
        }

        return ExecuteEvents.GetEventHandler<IPointerClickHandler>(gameObject) != null ||
               ExecuteEvents.GetEventHandler<IPointerDownHandler>(gameObject) != null ||
               ExecuteEvents.GetEventHandler<ISubmitHandler>(gameObject) != null ||
               ExecuteEvents.GetEventHandler<IDragHandler>(gameObject) != null;
    }

    private void UpdateCursorAnimation(Mouse realMouse)
    {
        if (_cursorRect == null)
        {
            return;
        }

        if (realMouse.leftButton.wasPressedThisFrame)
        {
            _lastCursorClickTime = Time.unscaledTime;
        }

        var isPressed = realMouse.leftButton.isPressed;
        var idlePulse = Mathf.Sin(Time.unscaledTime * CursorIdlePulseSpeed) * CursorIdlePulseScale;
        var clickProgress = Mathf.Clamp01((Time.unscaledTime - _lastCursorClickTime) / CursorClickPulseDuration);
        var clickPulse = clickProgress < 1f
            ? Mathf.Sin((1f - clickProgress) * Mathf.PI) * CursorClickPulseScale
            : 0f;

        var targetVisualScale = 1f + idlePulse + clickPulse;
        if (_cursorOverInteractive)
        {
            targetVisualScale *= CursorHoverScale;
        }
        if (isPressed)
        {
            targetVisualScale *= CursorPressedScale;
        }

        var layout = _menuHost != null ? Vector3.one : new Vector3(OverlayCanvasScale, OverlayCanvasScale, OverlayCanvasScale);
        var target = layout * targetVisualScale;
        _cursorRect.localScale = Vector3.Lerp(_cursorRect.localScale, target, Time.unscaledDeltaTime * CursorAnimationLerpSpeed);

        var targetColor = CursorNormalColor;
        if (_cursorOverInteractive)
        {
            targetColor = CursorHoverColor;
        }
        if (isPressed)
        {
            targetColor = CursorPressedColor;
        }

        _displayColor = Color.Lerp(_displayColor, targetColor, Time.unscaledDeltaTime * CursorAnimationLerpSpeed);
        for (var i = 0; i < _images.Count; i++)
        {
            if (_images[i] != null)
            {
                _images[i].color = _displayColor;
            }
        }

        if (_marker != null)
        {
            _marker.color = _displayColor;
        }
    }

    private static Sprite CreateWhiteSprite()
    {
        var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[64];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(255, 255, 255, 255);
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateRingSprite()
    {
        var texture = new Texture2D(CursorTextureSize, CursorTextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var colors = new Color32[CursorTextureSize * CursorTextureSize];
        var center = new Vector2((CursorTextureSize - 1) * 0.5f, (CursorTextureSize - 1) * 0.5f);
        var innerRadius = CursorRingRadius - CursorRingThickness * 0.5f;
        var outerRadius = CursorRingRadius + CursorRingThickness * 0.5f;
        var transparent = new Color32(0, 0, 0, 0);

        for (var y = 0; y < CursorTextureSize; y++)
        {
            for (var x = 0; x < CursorTextureSize; x++)
            {
                var distanceFromCenter = Vector2.Distance(new Vector2(x, y), center);
                var isRing = distanceFromCenter >= innerRadius && distanceFromCenter <= outerRadius;
                var isDot = distanceFromCenter <= CursorDotRadius;
                colors[y * CursorTextureSize + x] = isRing || isDot ? Color.white : transparent;
            }
        }

        texture.SetPixels32(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, CursorTextureSize, CursorTextureSize), new Vector2(0.5f, 0.5f), 100f);
    }

    private void LogRaycastAtCursor()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        var screenPos = GetScreenPoint();
        var pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);

        Debug.Log($"[VrUiCursor] Click Raycast at screenPos={screenPos}: found {results.Count} results");
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            if (result.gameObject == _cursor)
            {
                continue;
            }

            var canvas = result.gameObject.GetComponentInParent<Canvas>();
            var cg = result.gameObject.GetComponentInParent<CanvasGroup>();
            string cgInfo = cg != null ? $", CanvasGroup(alpha={cg.alpha}, interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts})" : "";
            Debug.Log($"[VrUiCursor]   Hit[{i}]: name='{result.gameObject.name}', path='{GetGameObjectPath(result.gameObject)}', canvas='{(canvas != null ? canvas.name : "None")}'{cgInfo}");
        }
    }

    private static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform p = go.transform.parent;
        while (p != null)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }
        return path;
    }

    private bool RestrictUIModuleToVirtualMouse()
    {
        try
        {
            if (_virtualMouse == null)
            {
                return false;
            }
            var uiModule = FindObjectOfType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (uiModule != null)
            {
                Debug.Log($"[NOVR] Restricting InputSystemUIInputModule actions to VirtualMouse (path: {_virtualMouse.path})");
                RestrictActionToVirtualMouse(uiModule.point?.action, _virtualMouse.path);
                RestrictActionToVirtualMouse(uiModule.leftClick?.action, _virtualMouse.path);
                RestrictActionToVirtualMouse(uiModule.middleClick?.action, _virtualMouse.path);
                RestrictActionToVirtualMouse(uiModule.rightClick?.action, _virtualMouse.path);
                RestrictActionToVirtualMouse(uiModule.scrollWheel?.action, _virtualMouse.path);
                return true;
            }

            Debug.LogWarning("[NOVR] InputSystemUIInputModule not found in scene yet, retrying next frame...");
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NOVR] Exception while restricting UI actions to VirtualMouse: {ex}");
            return false;
        }
    }

    private static void RestrictActionToVirtualMouse(InputAction? action, string devicePath)
    {
        if (action == null)
        {
            return;
        }
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (binding.path.Contains("<Mouse>"))
            {
                var newPath = binding.path.Replace("<Mouse>", devicePath);
                action.ApplyBindingOverride(i, newPath);
                Debug.Log($"[NOVR] Overriding UI binding path: '{binding.path}' -> '{newPath}' for action '{action.name}'");
            }
        }
    }
}
