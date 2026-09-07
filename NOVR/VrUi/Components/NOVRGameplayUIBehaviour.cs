using NOVR.VrUi.Native;
using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRGameplayUIBehaviour : UIRenderedCanvasBehavior
{
    private static readonly Vector3 PanelScale = new(0.003f, 0.003f, 0.003f);

    private void LateUpdate()
    {
        var canvas = GetComponent<Canvas>();
        if (NativeVrUiRoot.ShouldHideStockMenus)
        {
            if (canvas != null)
            {
                canvas.enabled = false;
            }

            transform.position = new Vector3(0f, -10000f, 0f);
            return;
        }

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
}
