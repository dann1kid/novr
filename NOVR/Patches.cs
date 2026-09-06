using HarmonyLib;
using UnityEngine;

namespace NOVR;

[HarmonyPatch(typeof(Camera), "set_fieldOfView")]
public static class CameraPatches
{
    [HarmonyPrefix]
    // Unity XR rejects FOV writes on HMD cameras and nags every frame. 0.4.3 blocked every
    // setter; the 0.4.5 selective skip coincided with a black headset view.
    private static bool PreventChangingFov()
    {
        return false;
    }
}
