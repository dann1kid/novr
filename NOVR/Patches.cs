using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;

namespace NOVR;

[HarmonyPatch(typeof(Camera), "set_fieldOfView")]
public static class CameraPatches
{
    [HarmonyPrefix]
    // XR/HMD cameras reject FOV writes and spam the log. The previous blanket skip made
    // every Camera.fieldOfView setter a no-op, including TargetCam and HUD overlay cameras.
    private static bool PreventChangingFov(Camera __instance)
    {
        if (__instance == null)
        {
            return false;
        }

        if (__instance.stereoEnabled)
        {
            return false;
        }

        if (XRSettings.isDeviceActive && __instance.stereoTargetEye != StereoTargetEyeMask.None)
        {
            return false;
        }

        return true;
    }
}
