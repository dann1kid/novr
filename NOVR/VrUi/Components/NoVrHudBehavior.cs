namespace NOVR.VrUi.SpecialBehavior;

public class NoVrHudBehavior : UIRenderedCanvasBehavior
{
    private void Update()
    {
        transform.localPosition = UnityEngine.Vector3.zero;
        transform.localRotation = UnityEngine.Quaternion.identity;
    }
}
