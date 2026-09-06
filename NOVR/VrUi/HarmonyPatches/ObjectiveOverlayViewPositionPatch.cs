using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NOVR.VrUi.HarmonyPatches;

internal static class ObjectiveOverlayViewPositionPatch
{
    private static readonly FieldInfo HiddenField = AccessTools.Field(typeof(global::ObjectiveOverlay), "hidden");
    private static readonly FieldInfo ObjectivePointerField = AccessTools.Field(typeof(global::ObjectiveOverlay), "objectivePointer");
    private static readonly FieldInfo ObjectiveDotField = AccessTools.Field(typeof(global::ObjectiveOverlay), "objectiveDot");
    private static readonly FieldInfo SizeIndicatorField = AccessTools.Field(typeof(global::ObjectiveOverlay), "sizeIndicator");
    private static readonly FieldInfo ObjectiveInfoField = AccessTools.Field(typeof(global::ObjectiveOverlay), "objectiveInfo");
    private static readonly FieldInfo PointerTailField = AccessTools.Field(typeof(global::ObjectiveOverlay), "pointerTail");

    [HarmonyPatch(typeof(global::ObjectiveOverlay), nameof(global::ObjectiveOverlay.UpdateOverlay))]
    private static class UpdateOverlayPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(global::ObjectiveOverlay __instance, MissionPosition.PositionResult result)
        {
            var mainCamera = APIBus.MainCamera;
            var cockpitHudCamera = APIBus.CockpitHudCamera;
            if (mainCamera == null || cockpitHudCamera == null)
            {
                return true;
            }

            HiddenField.SetValue(__instance, false);

            var objectivePointer = ObjectivePointerField.GetValue(__instance) as Image;
            var objectiveDot = ObjectiveDotField.GetValue(__instance) as Image;
            var sizeIndicator = SizeIndicatorField.GetValue(__instance) as Image;
            var objectiveInfo = ObjectiveInfoField.GetValue(__instance) as TMP_Text;
            var pointerTail = PointerTailField.GetValue(__instance) as Transform;
            if (objectivePointer == null || objectiveDot == null || sizeIndicator == null || objectiveInfo == null)
            {
                return true;
            }

            var worldPosition = result.Position.ToLocalPosition();
            var rangeMeters = Mathf.Max(result.Distance, 0.01f);
            var offScreen = VrHudProjection.PinToScreenEdge(worldPosition, out var hudPosition, out _);
            if (!offScreen && !VrHudProjection.TryProjectToCockpitHud(worldPosition, out hudPosition))
            {
                offScreen = true;
                VrHudProjection.PinToScreenEdge(worldPosition, out hudPosition, out _);
            }

            var hudRotation = cockpitHudCamera.transform.rotation;
            var proximityScale = VrHudProjection.ProximityScale(rangeMeters);
            objectivePointer.transform.position = hudPosition;
            objectivePointer.transform.rotation = hudRotation;
            objectivePointer.transform.localScale = Vector3.one * proximityScale;
            objectiveDot.transform.position = hudPosition;
            objectiveDot.transform.rotation = hudRotation;
            objectiveDot.transform.localScale = Vector3.one * proximityScale;
            sizeIndicator.transform.position = hudPosition;
            sizeIndicator.transform.rotation = hudRotation;

            var lookingAway = Vector3.Angle(mainCamera.transform.forward, result.Direction) > 10f || offScreen;
            objectivePointer.enabled = lookingAway;
            objectiveDot.enabled = !lookingAway;
            objectiveInfo.enabled = true;

            if (lookingAway)
            {
                sizeIndicator.enabled = false;
                var textTarget = pointerTail != null ? pointerTail.position : hudPosition;
                __instance.TextNoOverlap?.SetTarget(textTarget);
            }
            else
            {
                sizeIndicator.enabled = true;
                __instance.TextNoOverlap?.SetTarget(hudPosition - cockpitHudCamera.transform.up * 25f);
            }

            var range = result.Range.GetValueOrDefault();
            var distance = Mathf.Max(result.Distance, 0.01f);
            if (range > 0f && sizeIndicator.enabled)
            {
                var fov = GetUsableVerticalFov(mainCamera);
                var canvasHeight = ((RectTransform)sizeIndicator.canvas.transform).rect.height;
                var indicatorHeight = Mathf.Max(sizeIndicator.rectTransform.rect.height, 1f);
                var scale = canvasHeight / indicatorHeight / Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f) * (range / distance);
                sizeIndicator.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
                var indicatorColor = sizeIndicator.color;
                indicatorColor.a = Mathf.Clamp01(range * 20f / distance - 0.5f);
                sizeIndicator.color = indicatorColor;
            }

            var label = result.Objective != null ? result.Objective.SavedObjective.DisplayName : "Waypoint";
            objectiveInfo.text = label + " " + UnitConverter.DistanceReading(result.Distance);
            objectiveInfo.fontSize = (int)PlayerSettings.overlayTextSize;
            objectiveInfo.transform.rotation = hudRotation;
            objectiveInfo.color = VrHudProjection.ApplyProximityTint(objectiveInfo.color, rangeMeters);
            objectivePointer.color = VrHudProjection.ApplyProximityTint(objectivePointer.color, rangeMeters);
            objectiveDot.color = VrHudProjection.ApplyProximityTint(objectiveDot.color, rangeMeters);
            return false;
        }
    }

    [HarmonyPatch(typeof(global::ObjectiveOverlayManager), "StopTextOverlap")]
    private static class StopTextOverlapPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            // Original logic writes screen-space Vector2 positions, which lands labels at
            // world-origin in the VR HUD. Labels stay on the projected overlay instead.
            return false;
        }
    }

    private static float GetUsableVerticalFov(Camera camera)
    {
        var fov = camera.fieldOfView;
        if (fov < 5f || fov > 170f)
        {
            return 90f;
        }

        return fov;
    }
}
