using UnityEngine;
using UnityEngine.UI;

namespace NOVR.VrUi;

internal static class WorldSpaceCanvasClipGuard
{
    private const float ParkY = -10000f;

    public static void Apply(Camera? hudCamera)
    {
        if (hudCamera == null)
        {
            return;
        }

        var hideStock = Native.NativeVrUiRoot.ShouldHideStockMenus;
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        for (var i = 0; i < canvases.Length; i++)
        {
            var canvas = canvases[i];
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
            {
                continue;
            }

            var gameObject = canvas.gameObject;
            if (gameObject == null || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            {
                continue;
            }

            if (IsProtected(gameObject))
            {
                continue;
            }

            if (hideStock && IsStockMenuName(gameObject.name))
            {
                canvas.enabled = false;
                Park(canvas.transform);
            }
        }
    }

    private static bool IsProtected(GameObject gameObject)
    {
        var name = gameObject.name;
        return name == "VrUiCursorCanvas" ||
               name == "NOVR_VrFadeQuad" ||
               name == "NOVR Native VR UI" ||
               name == "NOVR Native VR UI Recenter Widget";
    }

    private static bool IsStockMenuName(string name)
    {
        return name == "MainCanvas" ||
               name == "MenuCanvas" ||
               name == "MaximizedMapCanvas";
    }

    private static void Park(Transform transform)
    {
        transform.position = new Vector3(0f, ParkY, 0f);
    }
}
