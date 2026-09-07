using NOVR.VrUi.SpecialBehavior;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;

namespace NOVR.VrUi;

[DefaultExecutionOrder(10000)]
public class VrUiCursor: NOVRBehaviour
{
    public static VrUiCursor? Instance { get; private set; }
    public static VrUiCursor? I => Instance;

    public bool IsActive => _hudRoot != null && _hudRoot.activeSelf;
    public Vector3 CursorPosition => _hudRoot != null ? _hudRoot.transform.position : Vector3.zero;

    private const float MinHudDistance = 0.75f;
    private const float DefaultHudDistance = 0.95f;
    private const float MaxHudDistance = 2.8f;
    private const float HudFollowWidth = 0.85f;
    private const float HudFollowHeight = 0.5f;
    private const float CrossLength = 0.22f;
    private const float CrossThickness = 0.022f;
    private static readonly Color CursorColor = new Color(0.1f, 1f, 0.2f, 1f);

    private GameObject? _hudRoot;
    private Material? _unlitMaterial;
    private Transform? _boundHost;
    private bool _loggedHud;
    private bool _hideHud;
    private bool _hasInitializedEventSystem;
    private bool _owningMouseCapture;
    private Mouse? _virtualMouse;
    private Mouse? _realMouse;
    private bool _hasProjectionReferenceOverride;
    private Quaternion _projectionReferenceRotation = Quaternion.identity;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void OnDisable()
    {
        ReleaseMouseCapture();
        base.OnDisable();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        ReleaseMouseCapture();
        DestroyHud();
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

    public Camera? UiCamera => GetHangarCamera();

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

    private void TickCursor()
    {
        _hideHud = ShouldHideInCockpit();
        if (_hideHud)
        {
            WindowsCursorClip.Release();
            SetHudVisible(false);
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
            Debug.Log($"[NOVR] Added VirtualMouse device: name='{_virtualMouse.name}', path='{_virtualMouse.path}'");
        }

        if (!_hasInitializedEventSystem && RestrictUIModuleToVirtualMouse())
        {
            _hasInitializedEventSystem = true;
        }

        UpdateHangarHud();

        var realMouse = _realMouse;
        if (realMouse == null || _virtualMouse == null)
        {
            return;
        }

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

        _ = _hasProjectionReferenceOverride;
        _ = _projectionReferenceRotation;
        _ = _boundHost;
    }

    private static bool ShouldHideInCockpit()
    {
        if (FindActiveMenuCanvas() != null)
        {
            return false;
        }

        return GameManager.GetLocalAircraft(out _);
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

    private static Vector2 ClampToScreen(Vector2 mousePos)
    {
        return new Vector2(
            Mathf.Clamp(mousePos.x, 0f, Mathf.Max(1, Screen.width)),
            Mathf.Clamp(mousePos.y, 0f, Mathf.Max(1, Screen.height)));
    }

    private static Camera? GetHangarCamera()
    {
        var headset = APIBus.HeadsetCamera;
        if (IsHangarCamera(headset))
        {
            return headset;
        }

        var main = Camera.main;
        if (IsHangarCamera(main))
        {
            return main;
        }

        var cameras = Camera.allCameras;
        for (var i = 0; i < cameras.Length; i++)
        {
            if (IsHangarCamera(cameras[i]))
            {
                return cameras[i];
            }
        }

        return headset ?? main;
    }

    private static bool IsHangarCamera(Camera? camera)
    {
        if (camera == null || !camera.enabled)
        {
            return false;
        }

        var name = camera.gameObject.name;
        if (name.Contains("VrCockpitHud") || name.Contains("VrUi"))
        {
            return false;
        }

        return camera.stereoTargetEye != StereoTargetEyeMask.None ||
               name.Contains("Menu Camera") ||
               name.Contains("NOVR Main Camera") ||
               name.Contains("Main Camera");
    }

    private void UpdateHangarHud()
    {
        var camera = GetHangarCamera();
        if (camera == null)
        {
            return;
        }

        EnsureHud();
        if (_hudRoot == null)
        {
            return;
        }

        var mouse = GetScreenPoint();
        var nx = Screen.width > 0 ? Mathf.Clamp01(mouse.x / Screen.width) : 0.5f;
        var ny = Screen.height > 0 ? Mathf.Clamp01(mouse.y / Screen.height) : 0.5f;
        var localDirection = new Vector3((nx - 0.5f) * HudFollowWidth, (ny - 0.5f) * HudFollowHeight, 1f);
        var distance = GetHudDistance(camera, localDirection);
        AttachHud(_hudRoot, camera, localDirection.normalized * distance);
        SetHudVisible(true);

        if (!_loggedHud)
        {
            _loggedHud = true;
            Debug.Log($"[NOVR] Hangar HUD cursor on '{camera.gameObject.name}', layer=Default, lockState={Cursor.lockState}.");
        }
    }

    private static float GetHudDistance(Camera camera, Vector3 localDirection)
    {
        var origin = camera.transform.position;
        var direction = camera.transform.TransformDirection(localDirection.normalized);
        if (Physics.Raycast(origin, direction, out var hit, MaxHudDistance + 1f, camera.cullingMask, QueryTriggerInteraction.Ignore))
        {
            return Mathf.Clamp(hit.distance - 0.12f, MinHudDistance, MaxHudDistance);
        }

        return DefaultHudDistance;
    }

    private void AttachHud(GameObject root, Camera camera, Vector3 localPosition)
    {
        if (root.transform.parent != camera.transform)
        {
            root.transform.SetParent(camera.transform, false);
            SetLayerDefault(root.transform);
        }

        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
    }

    private void EnsureHud()
    {
        _unlitMaterial ??= CreateUnlitMaterial();
        if (_unlitMaterial == null)
        {
            return;
        }

        if (_hudRoot == null)
        {
            _hudRoot = CreateCrossRoot("NOVR HangarCursor", CrossLength, CrossThickness);
        }
    }

    private GameObject CreateCrossRoot(string name, float length, float thickness)
    {
        var root = new GameObject(name);
        CreateBar(root.transform, "BarH", new Vector3(length, thickness, thickness));
        CreateBar(root.transform, "BarV", new Vector3(thickness, length, thickness));
        SetLayerDefault(root.transform);
        return root;
    }

    private void CreateBar(Transform parent, string name, Vector3 scale)
    {
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = name;
        bar.transform.SetParent(parent, false);
        bar.transform.localPosition = Vector3.zero;
        bar.transform.localRotation = Quaternion.identity;
        bar.transform.localScale = scale;
        var collider = bar.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        var renderer = bar.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _unlitMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private static Material? CreateUnlitMaterial()
    {
        var shader = Shader.Find("Hidden/Internal-Colored") ??
                     Shader.Find("Sprites/Default") ??
                     Shader.Find("Unlit/Color") ??
                     Shader.Find("GUI/Text Shader") ??
                     Shader.Find("Universal Render Pipeline/Unlit") ??
                     Shader.Find("UI/Default");
        if (shader == null)
        {
            Debug.LogError("[NOVR] No unlit shader found for hangar cursor.");
            return null;
        }

        var material = new Material(shader);
        material.color = CursorColor;
        material.SetColor("_Color", CursorColor);
        material.SetColor("_BaseColor", CursorColor);
        material.SetInt("_ZTest", (int)CompareFunction.Always);
        material.SetInt("_ZWrite", 0);
        material.SetFloat("_Surface", 1f);
        material.renderQueue = 5000;
        Debug.Log($"[NOVR] Hangar cursor shader '{shader.name}'.");
        return material;
    }

    private static void SetLayerDefault(Transform transform)
    {
        transform.gameObject.layer = (int)LayerHelper.Layers.Default;
        for (var i = 0; i < transform.childCount; i++)
        {
            SetLayerDefault(transform.GetChild(i));
        }
    }

    private void SetHudVisible(bool visible)
    {
        if (_hudRoot != null && _hudRoot.activeSelf != visible)
        {
            _hudRoot.SetActive(visible);
        }
    }

    private void DestroyHud()
    {
        if (_hudRoot != null)
        {
            Destroy(_hudRoot);
            _hudRoot = null;
        }

        _unlitMaterial = null;
    }

    private static Transform? FindActiveMenuCanvas()
    {
        var main = FindObjectOfType<NOVRMainMenuBehavior>();
        if (IsUsableHost(main != null ? main.transform : null))
        {
            return main!.transform;
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
        return host != null && host.gameObject.activeInHierarchy && host.position.y > -1000f;
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
            if (uiModule == null)
            {
                return false;
            }

            RestrictActionToVirtualMouse(uiModule.point?.action, _virtualMouse.path);
            RestrictActionToVirtualMouse(uiModule.leftClick?.action, _virtualMouse.path);
            RestrictActionToVirtualMouse(uiModule.middleClick?.action, _virtualMouse.path);
            RestrictActionToVirtualMouse(uiModule.rightClick?.action, _virtualMouse.path);
            RestrictActionToVirtualMouse(uiModule.scrollWheel?.action, _virtualMouse.path);
            return true;
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
                action.ApplyBindingOverride(i, binding.path.Replace("<Mouse>", devicePath));
            }
        }
    }
}
