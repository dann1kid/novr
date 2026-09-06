using HarmonyLib;
using UnityEngine;

namespace NOVR.VrCamera;

/// <summary>
/// URP renders XR views from XRPass. A Harmony postfix on GetProjMatrix blacked the
/// headset under HarmonyX (zero projection matrix). Zoom stays tracked but is not
/// applied to the XR pass until a safe hook is restored.
/// </summary>
internal static class XRPassZoomPatch
{
    internal static void TryApply(Harmony harmony)
    {
        _ = harmony;
        Debug.Log("[NOVR] Stereoscopic XRPass zoom is disabled to keep the HMD image visible.");
    }
}
