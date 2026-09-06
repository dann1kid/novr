using UnityEngine;

namespace NOVR.VrUi;

internal static class VrFacingUiPlacement
{
    public static bool TryGetPose(float distance, float heightOffset, out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;

        if (NOUIManager.I == null)
        {
            return false;
        }

        var camera = APIBus.CockpitHudCamera;
        if (camera == null)
        {
            return false;
        }

        var reference = APIBus.CockpitHudReference != null
            ? APIBus.CockpitHudReference.transform
            : camera.transform;

        var forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = reference.forward;
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        forward.Normalize();
        rotation = Quaternion.LookRotation(forward, Vector3.up);
        position = reference.position + forward * distance + Vector3.up * heightOffset;
        return true;
    }

    public static void Apply(Transform transform, float distance, float heightOffset, Vector3 localScale, Vector3 viewOffset = default)
    {
        if (!TryGetPose(distance, heightOffset, out var position, out var rotation))
        {
            return;
        }

        if (viewOffset.sqrMagnitude > 0f)
        {
            position += rotation * viewOffset;
        }

        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = localScale;
    }

    public static void ApplyHudPlane(Transform transform, float distance)
    {
        if (NOUIManager.I == null)
        {
            return;
        }

        var camera = APIBus.CockpitHudCamera;
        if (camera == null)
        {
            return;
        }

        transform.SetPositionAndRotation(
            camera.transform.position + camera.transform.forward * distance,
            camera.transform.rotation);
    }

    public static void ApplyCockpitStabilizedHudPlane(Transform transform, float distance)
    {
        if (NOUIManager.I == null)
        {
            return;
        }

        var camera = APIBus.CockpitHudCamera;
        if (camera == null)
        {
            return;
        }

        var gameCamera = APIBus.MainCamera;
        var aircraftForward = gameCamera != null && gameCamera.transform.parent != null
            ? gameCamera.transform.parent.forward
            : camera.transform.forward;
        var planarAircraft = Vector3.ProjectOnPlane(aircraftForward, Vector3.up);
        if (planarAircraft.sqrMagnitude < 0.0001f)
        {
            planarAircraft = aircraftForward;
        }

        planarAircraft.Normalize();
        var forward = Vector3.Slerp(planarAircraft, camera.transform.forward, 0.18f).normalized;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = camera.transform.forward;
        }

        var rotation = Quaternion.LookRotation(forward, Vector3.up);
        transform.SetPositionAndRotation(camera.transform.position + forward * distance, rotation);
    }
}
