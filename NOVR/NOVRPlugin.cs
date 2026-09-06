using System;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using NOVR.VrCamera;
using UnityEngine;
using UnityEngine.XR;

#if CPP
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
#endif

namespace NOVR;

[BepInPlugin(
    "deltawing.novr",
    "NOVR",
    "0.4.12")]
public class NOVRPlugin : BaseUnityPlugin
{
    
    private static NOVRPlugin _instance;
    public static string ModFolderPath { get; private set; }

    public NOVRPlugin()
    {
        _instance = this;
        ModFolderPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(NOVRPlugin)).Location);

        try
        {
            InputTracking.trackingAcquired += TrackingAcquired;
        }
        catch (Exception ex)
        {
            Debug.LogError("[NOVR] Could not subscribe to trackingAcquired: " + ex);
        }

        try
        {
            new ModConfiguration(Config);
            var harmony = HarmonyPatchApplier.Apply(Assembly.GetExecutingAssembly());
            CameraCockpitStatePatch.TryApply(harmony);
            // XRPassZoomPatch is disabled: HarmonyX postfix on XRPass.GetProjMatrix can
            // replace the OpenXR projection with a zero matrix and produce a black HMD.
        }
        catch (Exception ex)
        {
            Debug.LogError("[NOVR] Plugin setup error: " + ex);
        }

        try
        {
            Core.Create();
        }
        catch (Exception ex)
        {
            Debug.LogError("[NOVR] Core.Create failed: " + ex);
        }
    }

    private void TrackingAcquired(XRNodeState obj)
    {
        // Match 0.4.4: only seat height/position when tracking appears.
        // Yaw recenter on this event used a default pose and made VR feel like it never started.
        NOVRHeadsetData.CalibrateTranslation();
    }
     
    private void Awake()
    {

    }
    
    
}
