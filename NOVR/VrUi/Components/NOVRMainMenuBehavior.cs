using NOVR.VrUi.Native;
using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRMainMenuBehavior : UIRenderedCanvasBehavior
{
    private static readonly Vector3 PanelScale = new(0.003f, 0.003f, 0.003f);

    private void LateUpdate()
    {
        if (NativeVrUiRoot.ShouldHideStockMenus)
        {
            ParkHidden();
            return;
        }

        // 0.4.3: world-locked 3 m along the parent, not glued to the headset.
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = true;
            var eventCamera = APIBus.CockpitHudCamera ?? APIBus.HeadsetCamera;
            if (eventCamera != null)
            {
                canvas.worldCamera = eventCamera;
            }
        }

        transform.localScale = PanelScale;
        transform.localPosition = new Vector3(0f, 0f, 3f);
        transform.localRotation = Quaternion.identity;
    }

    private void ParkHidden()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = false;
        }

        transform.position = new Vector3(0f, -10000f, 0f);
    }
}
