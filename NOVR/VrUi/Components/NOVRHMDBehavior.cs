using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRHMDBehavior : UIRenderedCanvasBehavior
{
    private const float HudDistance = 1000f;

    private void Update()
    {
        var camera = APIBus.CockpitHudCamera;
        if (camera == null) return;

        transform.position = camera.transform.position + camera.transform.forward * HudDistance;
        transform.rotation = camera.transform.rotation;

        SetLocalPosition("Speed", new Vector3(-68f, 82f, 0f));
        SetLocalPosition("Altitude", new Vector3(68f, 82f, 0f));
        SetLocalPosition("Bearing", new Vector3(0f, 112f, 0f));
        SetLocalPosition("Artificial Horizon", new Vector3(0f, 78f, 0f));
    }

    private void SetLocalPosition(string childName, Vector3 localPosition)
    {
        var child = FindChildRecursive(transform, childName);
        if (child == null)
        {
            return;
        }

        child.localPosition = localPosition;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            var nestedChild = FindChildRecursive(child, childName);
            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }
}
