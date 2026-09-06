using UnityEngine;

namespace NOVR.VrUi;

internal static class WorldSpaceCanvasClipGuard
{
    private const float ParkY = -10000f;
    private const float ClipDistanceMeters = 0.4f;

    public static void Apply(Camera? headsetCamera)
    {
        var hideStock = Native.NativeVrUiRoot.ShouldHideStockMenus;
        var headsetPosition = headsetCamera != null ? headsetCamera.transform.position : (Vector3?)null;
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

            if (hideStock || ShouldParkAsBlockingQuad(gameObject, canvas.transform, headsetPosition))
            {
                Hide(canvas);
            }
        }
    }

    private static bool ShouldParkAsBlockingQuad(GameObject gameObject, Transform canvasTransform, Vector3? headsetPosition)
    {
        if (!IsLikelyBlockingQuad(gameObject.name))
        {
            return false;
        }

        if (!headsetPosition.HasValue)
        {
            return IsStockMenuName(gameObject.name);
        }

        return IsClippingHeadset(canvasTransform, headsetPosition.Value);
    }

    private static bool IsClippingHeadset(Transform canvasTransform, Vector3 headsetPosition)
    {
        var toHeadset = headsetPosition - canvasTransform.position;
        if (toHeadset.sqrMagnitude < ClipDistanceMeters * ClipDistanceMeters)
        {
            return true;
        }

        return Mathf.Abs(Vector3.Dot(toHeadset, canvasTransform.forward)) < ClipDistanceMeters;
    }

    private static bool IsProtected(GameObject gameObject)
    {
        var name = gameObject.name;
        return name == "VrUiCursorCanvas" ||
               name == "NOVR_VrFadeQuad" ||
               name == "NOVR Native VR UI" ||
               name == "NOVR Native VR UI Recenter Widget";
    }

    private static bool IsLikelyBlockingQuad(string name)
    {
        return IsStockMenuName(name) ||
               name == "Canvas" ||
               name.IndexOf("Blackout", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Background", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsStockMenuName(string name)
    {
        return name == "MainCanvas" ||
               name == "MenuCanvas" ||
               name == "MaximizedMapCanvas";
    }

    private static void Hide(Canvas canvas)
    {
        canvas.enabled = false;
        Park(canvas.transform);
    }

    private static void Park(Transform transform)
    {
        transform.position = new Vector3(0f, ParkY, 0f);
    }
}
