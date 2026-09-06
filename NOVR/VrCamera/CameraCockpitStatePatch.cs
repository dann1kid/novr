using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NOVR.VrCamera;

public class CameraCockpitStatePatch
{
    [HarmonyPatch(typeof(CameraCockpitState), "UpdateState")]
    private static class UpdateStatePatch
    {
        private static readonly FieldInfo PanViewField = AccessTools.Field(typeof(CameraCockpitState), "panView");
        private static readonly FieldInfo TiltViewField = AccessTools.Field(typeof(CameraCockpitState), "tiltView");
        
        
        [HarmonyPostfix]
        private static void Postfix(CameraCockpitState __instance, CameraStateManager cam)
        {
            PanViewField.SetValue(__instance, 0.0f);
            TiltViewField.SetValue(__instance, 0.0f);

            if (GameManager.playerInput != null &&
                GameManager.flightControlsEnabled &&
                !DynamicMap.mapMaximized)
            {
                VrZoomController.UpdateZoomInput(GameManager.playerInput.GetAxis("Zoom View"));
            }
        }
    }

    // Optional: 0.4.4 games may not have these method names. Applied from TryApply so a miss
    // cannot abort the rest of Harmony patching (which would leave VR disabled).
    private static void ResetZoomPostfix()
    {
        VrZoomController.ResetZoom();
    }

    internal static void TryApply(Harmony harmony)
    {
        TryPatch(harmony, "EnterState");
        TryPatch(harmony, "LeaveState");
    }

    private static void TryPatch(Harmony harmony, string methodName)
    {
        var method = AccessTools.Method(typeof(CameraCockpitState), methodName);
        if (method == null)
        {
            Debug.Log($"[NOVR] CameraCockpitState.{methodName} not found; zoom will reset from UpdateState only.");
            return;
        }

        harmony.Patch(method, postfix: new HarmonyMethod(typeof(CameraCockpitStatePatch), nameof(ResetZoomPostfix)));
    }
}