using System;
using NOVR.VrCamera;
using NOVR.VrUi.Native;
using NOVR.VrUi.SpecialBehavior;
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
        IncludeVrUiOnHeadsetCamera();
    }

    private void LateUpdate()
    {
        IncludeVrUiOnHeadsetCamera();
        WorldSpaceCanvasClipGuard.Apply(CockpitHudCamera);
    }
    
    private void UpdateSmoothedPosition()
    {
        var smoothedForwardReference = CockpitHudReference;
        var cam = CockpitHudCamera;
        smoothedForwardReference.transform.position = cam.transform.position;
        smoothedForwardReference.transform.localRotation = Quaternion.Lerp(smoothedForwardReference.transform.localRotation, cam.transform.localRotation, Mathf.Clamp(Time.deltaTime * SmoothingFactor, 0, 1));
    }

    private Camera CreateUiCamera(string cameraName, float depth)
    {
        var host = new GameObject(cameraName);
        host.transform.SetParent(transform, false);
        host.AddComponent<MainCameraSlaved>();

        var camera = host.AddComponent<Camera>();
        var additionalCameraData = host.AddComponent<UniversalAdditionalCameraData>();
        VrCameraManager.IgnoredCameras.Add(camera);

        // Do not submit a second XR view. 0.4.9–0.4.12 used stereo Both and the
        // overlay replaced the game with black. VR UI is drawn by the headset camera.
        camera.stereoTargetEye = StereoTargetEyeMask.None;
        additionalCameraData.allowXRRendering = false;
        additionalCameraData.renderType = CameraRenderType.Overlay;
        camera.targetTexture = null;
        camera.clearFlags = CameraClearFlags.Nothing;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.depth = depth;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.cullingMask = 1 << (int)LayerHelper.GetVrUiLayer();
        camera.enabled = true;

        return camera;
    }

    private void OnMainCameraChanged(Camera? previous, Camera? newCam)
    {
        IncludeVrUiOnHeadsetCamera(newCam);
    }

    private void IncludeVrUiOnHeadsetCamera()
    {
        IncludeVrUiOnHeadsetCamera(APIBus.MainCamera ?? Camera.main);
    }

    private static void IncludeVrUiOnHeadsetCamera(Camera? xrCamera)
    {
        if (xrCamera == null)
        {
            return;
        }

        xrCamera.cullingMask |= 1 << (int)LayerHelper.GetVrUiLayer();
    }

    private void ConfigureUiCameras()
    {
        ConfigureUiCamera(CockpitHudCamera);
    }

    private static void ConfigureUiCamera(Camera camera)
    {
        camera.stereoTargetEye = StereoTargetEyeMask.None;
        camera.clearFlags = CameraClearFlags.Nothing;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.targetTexture = null;
        camera.nearClipPlane = 0.05f;
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
