using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRGameplayUIBehaviour : UIRenderedCanvasBehavior
{
    private const float PanelDistanceMeters = 3.0f;
    private static readonly Vector3 PanelScale = new(0.003f, 0.003f, 0.003f);

    private void LateUpdate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        var canvas = GetComponent<Canvas>();
        if (canvas != null && !canvas.enabled)
        {
            return;
        }

        VrFacingUiPlacement.Apply(transform, PanelDistanceMeters, 0f, PanelScale);
        var cursor = VrUiCursor.I;
        if (cursor != null && cursor.IsActive)
        {
            cursor.SetProjectionReferenceRotation(transform.rotation);
        }
    }

    private void OnDisable()
    {
        VrUiCursor.I?.ClearProjectionReferenceRotation();
    }
}
