using System.Collections.Generic;
using NOVR.VrUi.SpecialBehavior;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
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

    private Texture2D? _texture;
    private const float MaxYawDegrees = 65f;
    private const float MaxPitchDegrees = 45f;
    private const float DefaultProjectionDistance = 2.7f;
    private const float MinProjectionDistance = 1.0f;
    private const float CursorWorldSize = 0.48f;
    private const float MenuCanvasFrontOffset = -48f;
    private const int CursorTextureSize = 64;
    private const float CursorRingRadius = 20f;
    private const float CursorRingThickness = 7f;
    private const float CursorDotRadius = 4f;
    private const float CursorIdlePulseScale = 0.035f;
    private const float CursorIdlePulseSpeed = 5.5f;
    private const float CursorHoverScale = 1.18f;
    private const float CursorPressedScale = 0.84f;
    private const float CursorClickPulseScale = 0.22f;
    private const float CursorClickPulseDuration = 0.18f;
    private const float CursorAnimationLerpSpeed = 24f;
    private static readonly Color CursorNormalColor = new Color32(80, 255, 90, 255);
    private static readonly Color CursorHoverColor = new Color32(170, 255, 180, 255);
    private static readonly Color CursorPressedColor = new Color32(255, 224, 92, 255);
    private GameObject? _cursor;
    private MeshRenderer? _cursorRenderer;
    private Material? _cursorMaterial;
    private RawImage? _cursorImage;
    private Transform? _menuHost;
    private bool _cursorOverInteractive;
    private float _lastCursorClickTime = -100f;
    private bool _hasProjectionReferenceOverride;
    private Quaternion _projectionReferenceRotation = Quaternion.identity;

    private bool _hasInitializedEventSystem = false;
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
        var camera = UiCamera;
        if (_cursor != null && camera != null)
        {
            Vector3 viewportPoint = camera.WorldToViewportPoint(_cursor.transform.position, Camera.MonoOrStereoscopicEye.Mono);
            var pixelRect = camera.pixelRect;
            if (pixelRect.width <= 1f || pixelRect.height <= 1f)
            {
                pixelRect = new Rect(0f, 0f, Screen.width, Screen.height);
            }

            var screenX = pixelRect.x + Mathf.Clamp01(viewportPoint.x) * pixelRect.width;
            var screenY = pixelRect.y + Mathf.Clamp01(viewportPoint.y) * pixelRect.height;
            return new Vector2(screenX, screenY);
        }
        return Vector2.zero;
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
    
    
    private void Start()
    {
        _texture = CreateCursorTexture();
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
        // The game locks the cursor in the cockpit. Do not steal it.
        if (Cursor.lockState == CursorLockMode.Locked && !_owningMouseCapture)
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

        _texture ??= CreateCursorTexture();
        UpdateCursorAngles();

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

        // Drive UI clicks from the hardware mouse in game-window pixels. The
        // visible ring is a separate world quad; reprojecting it back to
        // screen space missed buttons when overlay and hangar cameras differed.
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
    }

    private static Vector2 ClampToScreen(Vector2 mousePos)
    {
        return new Vector2(
            Mathf.Clamp(mousePos.x, 0f, Mathf.Max(1, Screen.width)),
            Mathf.Clamp(mousePos.y, 0f, Mathf.Max(1, Screen.height)));
    }
    

    private void UpdateCursorAngles()
    {
        EnsureCursorVisual();
        if (_cursor == null)
        {
            return;
        }

        if (!_cursor.activeSelf)
        {
            _cursor.SetActive(true);
        }

        var host = FindActiveMenuCanvas();
        if (host != null)
        {
            AttachToMenu(host);
            PlaceOnMenu(host);
            return;
        }

        var camera = UiCamera;
        if (camera == null)
        {
            return;
        }

        AttachToOverlay(camera);
        PlaceInFrontOfCamera(camera);
    }

    private Quaternion GetProjectionReferenceRotation(Camera camera)
    {
        if (_hasProjectionReferenceOverride)
        {
            return _projectionReferenceRotation;
        }

        return camera.transform.rotation;
    }

    private void EnsureCursorVisual()
    {
        if (_cursor != null)
        {
            if (_cursorRenderer != null)
            {
                _cursorRenderer.enabled = true;
            }
            return;
        }

        _texture ??= CreateCursorTexture();
        _cursor = new GameObject("NOVR VrCursor");
        var meshFilter = _cursor.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateFacingQuadMesh();
        _cursorRenderer = _cursor.AddComponent<MeshRenderer>();
        _cursorMaterial = CreateCursorMaterial(_texture);
        _cursorRenderer.sharedMaterial = _cursorMaterial;
        _cursorRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _cursorRenderer.receiveShadows = false;
        _cursorRenderer.lightProbeUsage = LightProbeUsage.Off;
        _cursorRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _cursorRenderer.sortingOrder = short.MaxValue;

        var imageObject = new GameObject("NOVR VrCursorImage");
        imageObject.transform.SetParent(_cursor.transform, false);
        var imageRect = imageObject.AddComponent<RectTransform>();
        imageRect.sizeDelta = Vector2.one;
        imageRect.localPosition = Vector3.zero;
        imageRect.localRotation = Quaternion.identity;
        imageRect.localScale = Vector3.one;
        var imageCanvas = imageObject.AddComponent<Canvas>();
        imageCanvas.renderMode = RenderMode.WorldSpace;
        imageCanvas.overrideSorting = true;
        imageCanvas.sortingOrder = short.MaxValue;
        imageCanvas.pixelPerfect = false;
        _cursorImage = imageObject.AddComponent<RawImage>();
        _cursorImage.raycastTarget = false;
        _cursorImage.texture = _texture;
        _cursorImage.color = CursorNormalColor;

        LayerHelper.SetLayerRecursive(_cursor.transform, LayerHelper.GetVrUiLayer());
        Debug.Log("[NOVR] VR cursor quad created on the VrUi layer.");
    }

    private void DestroyCursorVisual()
    {
        if (_cursor != null)
        {
            Destroy(_cursor);
            _cursor = null;
        }

        _cursorRenderer = null;
        _cursorMaterial = null;
        _cursorImage = null;
        _menuHost = null;
    }

    private void AttachToMenu(Transform host)
    {
        if (_cursor == null)
        {
            return;
        }

        if (_cursor.transform.parent != host)
        {
            _cursor.transform.SetParent(host, false);
            _menuHost = host;
            LayerHelper.SetLayerRecursive(_cursor.transform, LayerHelper.GetVrUiLayer());
            Debug.Log($"[NOVR] VR cursor attached to menu canvas '{host.name}'.");
        }
    }

    private void AttachToOverlay(Camera overlay)
    {
        if (_cursor == null)
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
    }

    private void PlaceOnMenu(Transform host)
    {
        if (_cursor == null)
        {
            return;
        }

        var mousePos = _realMouse != null
            ? ClampToScreen(_realMouse.position.ReadValue())
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        var nx = ScreenWidth > 0 ? Mathf.Clamp01(mousePos.x / ScreenWidth) : 0.5f;
        var ny = ScreenHeight > 0 ? Mathf.Clamp01(mousePos.y / ScreenHeight) : 0.5f;

        var rectTransform = host as RectTransform ?? host.GetComponent<RectTransform>();
        Vector3 localPos;
        if (rectTransform != null)
        {
            var rect = rectTransform.rect;
            localPos = new Vector3(
                Mathf.Lerp(rect.xMin, rect.xMax, nx),
                Mathf.Lerp(rect.yMin, rect.yMax, ny),
                MenuCanvasFrontOffset);
        }
        else
        {
            localPos = new Vector3((nx - 0.5f) * 400f, (ny - 0.5f) * 225f, MenuCanvasFrontOffset);
        }

        _cursor.transform.localPosition = localPos;
        _cursor.transform.localRotation = Quaternion.identity;

        var parentScale = Mathf.Max(Mathf.Abs(host.lossyScale.x), 0.0001f);
        var target = CursorWorldSize / parentScale;
        _cursor.transform.localScale = Vector3.one * target;

        UpdateHoverFromScreen(mousePos);
    }

    private void PlaceInFrontOfCamera(Camera camera)
    {
        if (_cursor == null)
        {
            return;
        }

        Vector3 localEuler;
        var mouse = _realMouse;
        if (mouse == null)
        {
            localEuler = Vector3.forward;
        }
        else
        {
            var mousePos = ClampToScreen(mouse.position.ReadValue());
            localEuler = Quaternion.Euler(-ProjectPitchAngle(mousePos.y), ProjectYawAngle(mousePos.x), 0f) * Vector3.forward;
            UpdateHoverFromScreen(mousePos);
        }

        var worldDirection = GetProjectionReferenceRotation(camera) * localEuler;
        var localDirection = Quaternion.Inverse(camera.transform.rotation) * worldDirection;
        if (localDirection.sqrMagnitude < 0.0001f)
        {
            localDirection = Vector3.forward;
        }

        _cursor.transform.localPosition = localDirection.normalized * DefaultProjectionDistance;
        _cursor.transform.localRotation = Quaternion.LookRotation(localDirection, Vector3.up);
        _cursor.transform.localScale = Vector3.one * CursorWorldSize;
    }

    private void UpdateHoverFromScreen(Vector2 screenPos)
    {
        _cursorOverInteractive = false;
        if (TryGetUiDistanceUnderCursor(screenPos, out _, out var overInteractive))
        {
            _cursorOverInteractive = overInteractive;
        }
    }

    private static Transform? FindActiveMenuCanvas()
    {
        Transform? mainCanvas = null;
        Transform? menuCanvas = null;
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        for (var i = 0; i < canvases.Length; i++)
        {
            var canvas = canvases[i];
            if (canvas == null || !canvas.enabled || canvas.renderMode != RenderMode.WorldSpace)
            {
                continue;
            }

            var gameObject = canvas.gameObject;
            if (gameObject == null || !gameObject.activeInHierarchy || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            {
                continue;
            }

            if (gameObject.transform.position.y < -1000f)
            {
                continue;
            }

            if (gameObject.name == "MainCanvas")
            {
                mainCanvas = gameObject.transform;
            }
            else if (gameObject.name == "MenuCanvas")
            {
                menuCanvas = gameObject.transform;
            }
        }

        return mainCanvas != null ? mainCanvas : menuCanvas;
    }

    private static Mesh CreateFacingQuadMesh()
    {
        var mesh = new Mesh { name = "NOVR VrCursorQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        mesh.normals = new[]
        {
            Vector3.back, Vector3.back, Vector3.back, Vector3.back
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material CreateCursorMaterial(Texture2D texture)
    {
        var shader = Shader.Find("Sprites/Default") ??
                     Shader.Find("UI/Default") ??
                     Shader.Find("Unlit/Transparent") ??
                     Shader.Find("Universal Render Pipeline/Unlit") ??
                     Shader.Find("Standard");
        var material = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
        material.mainTexture = texture;
        material.color = CursorNormalColor;
        material.SetColor("_Color", CursorNormalColor);
        material.SetColor("_BaseColor", CursorNormalColor);
        material.SetTexture("_MainTex", texture);
        material.SetTexture("_BaseMap", texture);
        material.SetFloat("_Surface", 1f);
        material.SetInt("_ZWrite", 0);
        material.SetInt("_ZTest", (int)CompareFunction.Always);
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.renderQueue = 4000;
        material.enableInstancing = false;
        return material;
    }


    private bool TryGetUiDistanceUnderCursor(Vector2 screenPos, out float distance, out bool overInteractive)
    {
        distance = default;
        overInteractive = false;

        if (EventSystem.current == null)
        {
            return false;
        }

        var pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);

        var camera = UiCamera;
        Vector3 cameraPos = camera != null ? camera.transform.position : Vector3.zero;
        RaycastResult? bestInteractive = null;
        RaycastResult? bestAny = null;

        foreach (var result in results)
        {
            if (result.gameObject == _cursor ||
                result.distance < 0f ||
                result.gameObject.GetComponentInParent<NOVRBlackoutCanvasBehavior>() != null ||
                result.gameObject.GetComponentInParent<global::MapIcon>() != null)
            {
                continue;
            }

            if (bestAny == null)
            {
                bestAny = result;
            }

            if (bestInteractive == null && IsInteractiveRaycastTarget(result.gameObject))
            {
                bestInteractive = result;
            }
        }

        var chosen = bestInteractive ?? bestAny;
        if (chosen == null)
        {
            return false;
        }

        overInteractive = IsInteractiveRaycastTarget(chosen.Value.gameObject);
        distance = chosen.Value.worldPosition == Vector3.zero
            ? chosen.Value.distance
            : Vector3.Distance(cameraPos, chosen.Value.worldPosition);
        distance = Mathf.Max(distance, MinProjectionDistance);

        return distance > 0f;
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
        if (_cursor == null || _cursorMaterial == null) return;

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

        ApplyAnimatedScale(targetVisualScale);

        var targetColor = CursorNormalColor;
        if (_cursorOverInteractive)
        {
            targetColor = CursorHoverColor;
        }
        if (isPressed)
        {
            targetColor = CursorPressedColor;
        }

        var color = Color.Lerp(_cursorMaterial.color, targetColor, Time.unscaledDeltaTime * CursorAnimationLerpSpeed);
        _cursorMaterial.color = color;
        _cursorMaterial.SetColor("_Color", color);
        _cursorMaterial.SetColor("_BaseColor", color);
        if (_cursorImage != null)
        {
            _cursorImage.color = color;
        }
    }

    private void ApplyAnimatedScale(float visualScale)
    {
        if (_cursor == null)
        {
            return;
        }

        float layout;
        if (_menuHost != null)
        {
            var parentScale = Mathf.Max(Mathf.Abs(_menuHost.lossyScale.x), 0.0001f);
            layout = CursorWorldSize / parentScale;
        }
        else
        {
            layout = CursorWorldSize;
        }

        var target = Vector3.one * (layout * visualScale);
        _cursor.transform.localScale = Vector3.Lerp(
            _cursor.transform.localScale,
            target,
            Time.unscaledDeltaTime * CursorAnimationLerpSpeed);
    }


    private float ProjectPitchAngle(float y)
    {
        int height = ScreenHeight;
        if (height <= 0)
        {
            return 0f;
        }
        return Mathf.Lerp(-MaxPitchDegrees, MaxPitchDegrees, Mathf.Clamp01(y / height));
    }
    private float ProjectYawAngle(float x)
    {
        int width = ScreenWidth;
        if (width <= 0)
        {
            return 0f;
        }
        return Mathf.Lerp(-MaxYawDegrees, MaxYawDegrees, Mathf.Clamp01(x / width));
    }

    private static Texture2D CreateCursorTexture()
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
        return texture;
    }
    
    private void LogRaycastAtCursor()
    {
        if (EventSystem.current == null) return;
        
        var screenPos = _realMouse != null ? ClampToScreen(_realMouse.position.ReadValue()) : GetScreenPoint();
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
            if (result.gameObject == _cursor) continue;
            
            var canvas = result.gameObject.GetComponentInParent<Canvas>();
            var cg = result.gameObject.GetComponentInParent<CanvasGroup>();
            string cgInfo = cg != null ? $", CanvasGroup(alpha={cg.alpha}, interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts})" : "";
            string rectInfo = "";
            var rt = result.gameObject.GetComponent<RectTransform>();
            if (rt != null)
            {
                rectInfo = $", localPos={rt.localPosition}, size={rt.sizeDelta}";
            }
            Debug.Log($"[VrUiCursor]   Hit[{i}]: name='{result.gameObject.name}', path='{GetGameObjectPath(result.gameObject)}', canvas='{(canvas != null ? canvas.name : "None")}'{rectInfo}{cgInfo}");
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
            if (_virtualMouse == null) return false;
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
            else
            {
                Debug.LogWarning("[NOVR] InputSystemUIInputModule not found in scene yet, retrying next frame...");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NOVR] Exception while restricting UI actions to VirtualMouse: {ex}");
            return false;
        }
    }

    private static void RestrictActionToVirtualMouse(InputAction? action, string devicePath)
    {
        if (action == null) return;
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
