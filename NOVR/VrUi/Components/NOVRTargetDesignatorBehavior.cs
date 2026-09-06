using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRTargetDesignatorBehavior : UIRenderedCanvasBehavior
{
    private const float DesignatorDistance = 1000f;

    private void Update()
    {
        var uiCam = APIBus.CockpitHudReference;
        if (uiCam == null)
        {
            return;
        }

        var overshoot = ModConfiguration.Instance?.TargetDesignatorOvershoot.Value ?? 1.2f;
        transform.rotation = Quaternion.SlerpUnclamped(Quaternion.identity, uiCam.transform.rotation, overshoot);
        transform.position = uiCam.transform.position + transform.forward * DesignatorDistance;
    }
}
