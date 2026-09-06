using UnityEngine;

namespace NOVR.VrUi;

internal static class WorldSpaceCanvasClipGuard
{
    private const float ParkY = -10000f;
    private const float ClipDistanceMeters = 0.4f;

    public static void Apply(Camera? headsetCamera)
    {
        if (headsetCamera == null)
        {
            return;
        }

        var headsetPosition = headsetCamera.transform.position;
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

            if (IsProtected(gameObject) || !IsLeftoverOccluder(gameObject.name))
            {
                continue;
            }

            if (IsClippingHeadset(canvas.transform, headsetPosition))
            {
                canvas.enabled = false;
                canvas.transform.position = new Vector3(0f, ParkY, 0f);
            }
        }
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
               name == "NOVR Native VR UI" ||
               name == "NOVR Native VR UI Recenter Widget" ||
               name == "MainCanvas" ||
               name == "MenuCanvas" ||
               name == "MaximizedMapCanvas";
    }

    private static bool IsLeftoverOccluder(string name)
    {
        return name == "NOVR_VrFadeQuad" ||
               name.IndexOf("Blackout", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
