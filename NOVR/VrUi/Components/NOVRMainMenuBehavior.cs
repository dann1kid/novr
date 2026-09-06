using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRMainMenuBehavior : UIRenderedCanvasBehavior
{
    private const float PanelDistanceMeters = 3.0f;
    private static readonly Vector3 PanelScale = new(0.003f, 0.003f, 0.003f);

    private void Start()
    {
        // 0.4.3 parked the stock menu 3 m along the parent, not on the HMD.
        transform.localScale = PanelScale;
        transform.localPosition = new Vector3(0f, 0f, PanelDistanceMeters);
    }

    private void LateUpdate()
    {
        if (!isActiveAndEnabled)
        {
            ParkOffscreen();
            return;
        }

        var canvas = GetComponent<Canvas>();
        if (canvas != null && !canvas.enabled)
        {
            ParkOffscreen();
            return;
        }

        var group = GetComponent<CanvasGroup>();
        if (group != null && group.alpha <= 0.02f)
        {
            ParkOffscreen();
            return;
        }

        VrFacingUiPlacement.Apply(transform, PanelDistanceMeters, 0f, PanelScale);
        var cursor = VrUiCursor.I;
        if (cursor != null && cursor.IsActive)
        {
            cursor.SetProjectionReferenceRotation(transform.rotation);
        }
    }

    private void ParkOffscreen()
    {
        transform.position = new Vector3(0f, -10000f, 0f);
    }
}
