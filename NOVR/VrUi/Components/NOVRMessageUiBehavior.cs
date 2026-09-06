using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRMessageUiBehavior : UIRenderedCanvasBehavior
{
    private const float PanelDistanceMeters = 2.4f;
    private static readonly Vector3 PanelScale = new(0.0024f, 0.0024f, 0.0024f);
    private static readonly Vector3 ViewOffset = new(-0.85f, -0.2f, 0f);

    private void LateUpdate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        VrFacingUiPlacement.Apply(transform, PanelDistanceMeters, 0f, PanelScale, ViewOffset);
    }
}
