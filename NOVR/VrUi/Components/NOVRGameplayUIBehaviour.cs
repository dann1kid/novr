using NOVR.VrUi.Native;
using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRGameplayUIBehaviour : UIRenderedCanvasBehavior
{
    private static readonly Vector3 PanelScale = new(0.003f, 0.003f, 0.003f);

    private void LateUpdate()
    {
        if (NativeVrUiRoot.ShouldHideStockMenus)
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
            }

            transform.position = new Vector3(0f, -10000f, 0f);
            return;
        }

        // 0.4.3: world-locked in front of origin, not following headset yaw.
        transform.localScale = PanelScale;
        transform.localPosition = new Vector3(0f, 0f, 3f);
        transform.localRotation = Quaternion.identity;
    }
}
