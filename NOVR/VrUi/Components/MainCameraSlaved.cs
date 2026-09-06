using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class MainCameraSlaved : MonoBehaviour
{
    private void Update()
    {
        FollowHeadset();
    }

    private void LateUpdate()
    {
        FollowHeadset();
    }

    private void FollowHeadset()
    {
        var cam = APIBus.MainCamera;
        if (cam == null)
        {
            return;
        }

        transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
    }
}
