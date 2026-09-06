using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NOVR.VrUi.HarmonyPatches;

internal static class GameplayUiSelectAircraftPatch
{
    private static readonly FieldInfo SelectAircraftButtonField =
        AccessTools.Field(typeof(global::GameplayUI), "selectAircraftButton");
    private static readonly FieldInfo SelectAirbasePanelField =
        AccessTools.Field(typeof(global::GameplayUI), "selectAirbasePanel");

    [HarmonyPatch(typeof(global::GameplayUI), nameof(global::GameplayUI.SelectAirbase))]
    private static class SelectAirbasePatch
    {
        [HarmonyPostfix]
        private static void Postfix(global::GameplayUI __instance)
        {
            EnsureSelectAircraftClickable(__instance);
        }
    }

    [HarmonyPatch(typeof(global::GameplayUI), nameof(global::GameplayUI.ShowSelectAirbase))]
    private static class ShowSelectAirbasePatch
    {
        [HarmonyPostfix]
        private static void Postfix(global::GameplayUI __instance)
        {
            EnsureSelectAircraftClickable(__instance);
        }
    }

    private static void EnsureSelectAircraftClickable(global::GameplayUI ui)
    {
        var button = SelectAircraftButtonField.GetValue(ui) as Button;
        if (button == null)
        {
            return;
        }

        button.interactable = true;
        if (button.targetGraphic != null)
        {
            button.targetGraphic.raycastTarget = true;
        }

        foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = true;
        }

        var panel = SelectAirbasePanelField.GetValue(ui) as GameObject;
        if (panel != null)
        {
            panel.transform.SetAsLastSibling();
        }

        var canvas = button.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 200);
        }
    }
}
