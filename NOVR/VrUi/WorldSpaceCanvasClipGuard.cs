using UnityEngine;
using UnityEngine.UI;

namespace NOVR.VrUi;

internal static class WorldSpaceCanvasClipGuard
{
    private const float MinPlaneDistanceMeters = 0.9f;
    private const float MaxMenuWidthMeters = 6f;
    private const float ParkY = -10000f;

    public static void Apply(Camera? hudCamera)
    {
        if (hudCamera == null)
        {
            return;
        }

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

            if (IsHeadLockedOverlay(gameObject))
            {
                continue;
            }

            var transform = canvas.transform;
            var cameraPosition = hudCamera.transform.position;
            var toCanvas = transform.position - cameraPosition;
            var planeDistance = Mathf.Abs(Vector3.Dot(toCanvas, transform.forward));
            var positionDistance = toCanvas.magnitude;
            var inHeadset = canvas.enabled && gameObject.activeInHierarchy &&
                            (planeDistance < MinPlaneDistanceMeters || positionDistance < MinPlaneDistanceMeters);

            if (IsSuppressedStockMenu(canvas))
            {
                Park(transform);
                continue;
            }

            if (!inHeadset)
            {
                continue;
            }

            if (IsKnownMenuCanvas(gameObject))
            {
                PushInFrontOfHeadset(transform, hudCamera.transform);
                ClampWorldWidth(transform, MaxMenuWidthMeters);
                continue;
            }

            Debug.Log($"[NOVR] Hiding world-space canvas '{gameObject.name}' sitting in the headset (plane={planeDistance:0.00}m, dist={positionDistance:0.00}m).");
            canvas.enabled = false;
            DisableGraphics(gameObject);
            Park(transform);
        }
    }

    private static bool IsHeadLockedOverlay(GameObject gameObject)
    {
        var name = gameObject.name;
        return name == "VrUiCursorCanvas" ||
               name == "NOVR_VrFadeQuad" ||
               name == "NOVR Native VR UI Recenter Widget";
    }

    private static bool IsKnownMenuCanvas(GameObject gameObject)
    {
        var name = gameObject.name;
        return name == "NOVR Native VR UI" ||
               name == "MainCanvas" ||
               name == "MenuCanvas" ||
               name == "MaximizedMapCanvas";
    }

    private static bool IsSuppressedStockMenu(Canvas canvas)
    {
        if (canvas.gameObject.name != "MainCanvas")
        {
            return false;
        }

        var group = canvas.GetComponent<CanvasGroup>();
        return !canvas.enabled || (group != null && group.alpha <= 0.02f);
    }

    private static void PushInFrontOfHeadset(Transform transform, Transform hud)
    {
        if (transform.parent != null)
        {
            transform.localPosition = new Vector3(0f, 0f, 3f);
            transform.localRotation = Quaternion.identity;
            if (transform.localScale.sqrMagnitude < 0.000001f)
            {
                transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);
            }
            return;
        }

        var forward = Vector3.ProjectOnPlane(hud.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = hud.forward;
        }

        forward.Normalize();
        transform.SetPositionAndRotation(
            hud.position + forward * 3f,
            Quaternion.LookRotation(forward, Vector3.up));
    }

    private static void ClampWorldWidth(Transform transform, float maxWidthMeters)
    {
        if (transform is not RectTransform rect)
        {
            return;
        }

        var width = Mathf.Abs(rect.rect.width * rect.lossyScale.x);
        if (width <= maxWidthMeters || width < 0.01f)
        {
            return;
        }

        transform.localScale *= maxWidthMeters / width;
    }

    private static void DisableGraphics(GameObject gameObject)
    {
        var graphics = gameObject.GetComponentsInChildren<Graphic>(true);
        for (var i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
            {
                graphics[i].enabled = false;
            }
        }
    }

    private static void Park(Transform transform)
    {
        transform.position = new Vector3(0f, ParkY, 0f);
    }
}
