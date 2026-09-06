using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class PositionZeroBehavior : MonoBehaviour
{
    private void Update()
    {
        if (ContainsWorldSpaceCanvas(transform))
        {
            return;
        }

        transform.position = Vector3.zero;
    }

    private static bool ContainsWorldSpaceCanvas(Transform root)
    {
        if (root.gameObject.layer == (int)LayerHelper.Layers.VrUi)
        {
            return true;
        }

        var canvases = root.GetComponentsInChildren<Canvas>(true);
        for (var i = 0; i < canvases.Length; i++)
        {
            var canvas = canvases[i];
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                return true;
            }
        }

        return false;
    }
}
