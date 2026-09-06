using NOVR.VrCamera;
using NOVR.VrUi.Native;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NOVR.VrUi;

public class NOUIManager : NOVRBehaviour
{
    private const float SmoothingFactor = 10f;
    private Camera? _cockpitHudCamera;
    private GameObject? _smoothedForwardReference;

    public static NOUIManager I { get; private set; }

    public Camera CockpitHudCamera => _cockpitHudCamera ??= CreateUiCamera("VrCockpitHudCamera", 100);
    public GameObject CockpitHudReference => _smoothedForwardReference ??= CreateSmoothedForwardReference();

    private GameObject CreateSmoothedForwardReference()
    {
        var go = new GameObject("SmoothedForwardReference");
        go.transform.SetParent(transform);

        var cam = CockpitHudCamera;
        go.transform.position = cam.transform.position;
        go.transform.localRotation = cam.transform.localRotation;
        return go;
    }

    private new void Awake()
    {
        base.Awake();
        APIBus.OnMainCameraChanged += OnMainCameraChanged;
    }

    private void OnDestroy()
    {
        APIBus.OnMainCameraChanged -= OnMainCameraChanged;
    }

    private void Start()
    {
        I = this;
        Create<UIBehaviorPatcher>(transform);
        UIBehaviorPatcher.DoPatching();
        Create<VrUiCursor>(transform);
        Create<NativeVrUiRoot>(transform);
        ConfigureUiCameras();
    }

    protected override void OnSettingChanged()
    {
        base.OnSettingChanged();
        ConfigureUiCameras();
    }

    private void Update()
    {
        ConfigureUiCameras();
        UpdateSmoothedPosition();
        EnsureHudInCameraStacks();
    }

    private void LateUpdate()
    {
        EnsureHudInCameraStacks();
        WorldSpaceCanvasClipGuard.Apply(APIBus.HeadsetCamera ?? CockpitHudCamera);
    }

    private void UpdateSmoothedPosition()
    {
        var smoothedForwardReference = CockpitHudReference;
        var cam = CockpitHudCamera;
        smoothedForwardReference.transform.position = cam.transform.position;
        smoothedForwardReference.transform.localRotation = Quaternion.Lerp(
            smoothedForwardReference.transform.localRotation,
            cam.transform.localRotation,
            Mathf.Clamp(Time.deltaTime * SmoothingFactor, 0, 1));
    }

    private Camera CreateUiCamera(string cameraName, float depth)
    {
        // 0.4.3 / 0.4.8: independent pose driver. Do not slave this to the game
        // camera or it duplicates the hangar view as a giant world-space quad.
        var poseDriver = Create<NOVRPoseDriver>(transform);
        poseDriver.name = cameraName;

        var camera = poseDriver.gameObject.AddComponent<Camera>();
        var additionalCameraData = poseDriver.gameObject.AddComponent<UniversalAdditionalCameraData>();
        VrCameraManager.IgnoredCameras.Add(camera);

        // stereo Both on this overlay replaced the HMD with black on PSVR2.
        // stereo None + Overlay-in-stack was the last setup where the hangar was visible.
        camera.stereoTargetEye = StereoTargetEyeMask.None;
        additionalCameraData.allowXRRendering = false;
        additionalCameraData.renderType = CameraRenderType.Overlay;
        camera.targetTexture = null;
        camera.clearFlags = CameraClearFlags.Depth;
        camera.backgroundColor = Color.clear;
        camera.depth = depth;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.cullingMask = 1 << (int)LayerHelper.GetVrUiLayer();
        camera.enabled = true;

        return camera;
    }

    private void OnMainCameraChanged(Camera? previous, Camera? newCam)
    {
        AttachHudToStack(newCam);
    }

    private void EnsureHudInCameraStacks()
    {
        var hud = _cockpitHudCamera;
        if (hud == null)
        {
            return;
        }

        AttachHudToStack(APIBus.HeadsetCamera);
        AttachHudToStack(APIBus.MainCamera);
        AttachHudToStack(Camera.main);

        var cameras = Camera.allCameras;
        for (var i = 0; i < cameras.Length; i++)
        {
            AttachHudToStack(cameras[i]);
        }
    }

    private void AttachHudToStack(Camera? xrCamera)
    {
        var hud = _cockpitHudCamera;
        if (xrCamera == null || hud == null || xrCamera == hud)
        {
            return;
        }

        var additional = xrCamera.GetComponent<UniversalAdditionalCameraData>();
        if (additional == null || additional.renderType != CameraRenderType.Base)
        {
            return;
        }

        var stack = additional.cameraStack;
        if (stack != null && !stack.Contains(hud))
        {
            stack.Add(hud);
        }
    }

    private void ConfigureUiCameras()
    {
        ConfigureUiCamera(CockpitHudCamera);
    }

    private static void ConfigureUiCamera(Camera camera)
    {
        camera.enabled = true;
        camera.stereoTargetEye = StereoTargetEyeMask.None;
        camera.clearFlags = CameraClearFlags.Depth;
        camera.backgroundColor = Color.clear;
        camera.targetTexture = null;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 10000f;
        camera.rect = new Rect(0f, 0f, 1f, 1f);

        var additional = camera.GetComponent<UniversalAdditionalCameraData>();
        if (additional != null)
        {
            additional.renderType = CameraRenderType.Overlay;
            additional.allowXRRendering = false;
        }
    }
}
