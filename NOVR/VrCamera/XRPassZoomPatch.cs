using HarmonyLib;
using UnityEngine;

namespace NOVR.VrCamera;

/// <summary>
/// URP renders XR views from XRPass rather than Camera's stereo projection properties.
/// Magnify the matrix at that final pipeline boundary without modifying the stored
/// OpenXR matrix, avoiding both silent Camera API no-ops and cumulative scaling.
/// </summary>
internal static class XRPassZoomPatch
{
    internal static void TryApply(Harmony harmony)
    {
        var xrPassType = AccessTools.TypeByName("UnityEngine.Rendering.Universal.XRPass")
                         ?? AccessTools.TypeByName("UnityEngine.Experimental.Rendering.XRPass")
                         ?? AccessTools.TypeByName("UnityEngine.Rendering.XRPass");
        if (xrPassType == null)
        {
            Debug.LogWarning("[NOVR] XRPass type not found; stereoscopic VR zoom is unavailable.");
            return;
        }

        var method = AccessTools.Method(xrPassType, "GetProjMatrix")
                     ?? AccessTools.Method(xrPassType, "GetProjectionMatrix");
        if (method == null)
        {
            Debug.LogWarning("[NOVR] XRPass.GetProjMatrix not found; stereoscopic VR zoom is unavailable.");
            return;
        }

        harmony.Patch(method, postfix: new HarmonyMethod(typeof(XRPassZoomPatch), nameof(Postfix)));
        Debug.Log($"[NOVR] VR zoom hooked {xrPassType.FullName}.{method.Name}.");
    }

    private static void Postfix(ref Matrix4x4 __result)
    {
        var magnification = VrZoomController.Magnification;
        if (magnification <= 1f)
        {
            return;
        }

        // Preserve the asymmetric optical centre (m02/m12) supplied by OpenXR.
        __result.m00 *= magnification;
        __result.m11 *= magnification;
    }
}
