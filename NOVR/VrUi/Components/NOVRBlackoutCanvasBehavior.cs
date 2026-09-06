using System;
using UnityEngine;
using UnityEngine.UI;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRBlackoutCanvasBehavior : MonoBehaviour
{
    private Canvas? _canvas;
    private CanvasGroup? _canvasGroup;
    private GraphicRaycaster? _raycaster;

    protected virtual void Awake()
    {
        _canvas = gameObject.GetComponent<Canvas>();
        if (_canvas == null) throw new Exception($"{typeof(NOVRBlackoutCanvasBehavior)} attached to {typeof(GameObject)} without {typeof(Canvas)} component.");

        _canvasGroup = gameObject.GetComponent<CanvasGroup>();
        _raycaster = gameObject.GetComponent<GraphicRaycaster>();

        ApplyVrUiLayerRecursive(_canvas.transform);
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = APIBus.CockpitHudCamera;
        _canvas.planeDistance = 1f;
    }

    private void Update()
    {
        var hud = APIBus.CockpitHudCamera;
        if (hud == null)
        {
            return;
        }

        if (!IsFadeVisible())
        {
            if (_raycaster != null)
            {
                _raycaster.enabled = false;
            }
            return;
        }

        if (_raycaster != null)
        {
            _raycaster.enabled = true;
        }

        transform.rotation = hud.transform.rotation;
        transform.position = hud.transform.position + hud.transform.forward * 1.25f;
    }

    private bool IsFadeVisible()
    {
        if (_canvas == null || !_canvas.enabled || !gameObject.activeInHierarchy)
        {
            return false;
        }

        if (_canvasGroup != null)
        {
            return _canvasGroup.alpha > 0.02f;
        }

        var graphics = GetComponentsInChildren<Graphic>(false);
        for (var i = 0; i < graphics.Length; i++)
        {
            var graphic = graphics[i];
            if (graphic != null && graphic.enabled && graphic.color.a > 0.02f)
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplyVrUiLayerRecursive(Transform root)
    {
        LayerHelper.SetLayerRecursive(root, LayerHelper.GetVrUiLayer());
    }
}
