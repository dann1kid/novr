using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRStatusDisplayBehavior : UIRenderedCanvasBehavior
{
    private const float PanelDistanceMeters = 2.6f;
    private static readonly Vector3 PanelScale = new(0.0022f, 0.0022f, 0.0022f);
    private static readonly Vector3 ViewOffset = new(0.95f, 0.35f, 0f);

    private void LateUpdate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        VrFacingUiPlacement.Apply(transform, PanelDistanceMeters, 0f, PanelScale, ViewOffset);
    }
}
