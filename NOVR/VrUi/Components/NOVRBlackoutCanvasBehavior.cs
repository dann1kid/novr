using System;
using UnityEngine;
using UnityEngine.UI;

namespace NOVR.VrUi.SpecialBehavior;

public class NOVRBlackoutCanvasBehavior : MonoBehaviour
{
    private const float FadeQuadDistanceMeters = 1.6f;
    private const float FadeQuadSizeMeters = 8f;
    private const float FadeAlphaThreshold = 0.02f;

    private Canvas? _sourceCanvas;
    private CanvasGroup? _canvasGroup;
    private GraphicRaycaster? _sourceRaycaster;
    private Graphic[] _sourceGraphics = Array.Empty<Graphic>();
    private bool _startedInactive;
    private GameObject? _fadeRoot;
    private RawImage? _fadeImage;

    protected virtual void Awake()
    {
        _sourceCanvas = gameObject.GetComponent<Canvas>();
        if (_sourceCanvas == null)
        {
            throw new Exception($"{typeof(NOVRBlackoutCanvasBehavior)} attached to {gameObject.name} without {typeof(Canvas)} component.");
        }

        _startedInactive = !gameObject.activeInHierarchy;
        _canvasGroup = gameObject.GetComponent<CanvasGroup>();
        _sourceRaycaster = gameObject.GetComponent<GraphicRaycaster>();
        _sourceGraphics = GetComponentsInChildren<Graphic>(true);

        // The game's fade canvas is a full-screen overlay. In world space that becomes a
        // huge black quad that clips the HMD. Keep the original canvas disabled and draw a
        // dedicated VR fade only while the game is actually fading.
        HideSourceCanvas();
        CreateFadeQuad();
        Debug.Log("[NOVR] BlackoutCanvas is hidden in VR; a headset fade quad is used only during fades.");
    }

    private void OnEnable()
    {
        HideSourceCanvas();
        RefreshFade();
    }

    private void OnDisable()
    {
        SetFadeVisible(false);
    }

    private void OnDestroy()
    {
        if (_fadeRoot != null)
        {
            Destroy(_fadeRoot);
            _fadeRoot = null;
        }
    }

    private void LateUpdate()
    {
        HideSourceCanvas();
        RefreshFade();
    }

    private void RefreshFade()
    {
        var hud = APIBus.CockpitHudCamera;
        var alpha = ReadFadeAlpha();
        if (hud == null || alpha <= FadeAlphaThreshold)
        {
            SetFadeVisible(false);
            return;
        }

        EnsureFadeQuad();
        if (_fadeRoot == null || _fadeImage == null)
        {
            return;
        }

        _fadeRoot.SetActive(true);
        _fadeRoot.transform.SetParent(hud.transform, false);
        _fadeRoot.transform.localPosition = new Vector3(0f, 0f, FadeQuadDistanceMeters);
        _fadeRoot.transform.localRotation = Quaternion.identity;
        _fadeRoot.transform.localScale = Vector3.one;
        _fadeImage.color = new Color(0f, 0f, 0f, alpha);
    }

    private float ReadFadeAlpha()
    {
        if (!isActiveAndEnabled)
        {
            return 0f;
        }

        if (_canvasGroup != null)
        {
            return _canvasGroup.alpha;
        }

        // Always-on menu leftovers stay hidden. A canvas that starts disabled is a real fade.
        return _startedInactive ? 1f : 0f;
    }

    private void HideSourceCanvas()
    {
        if (_sourceCanvas != null)
        {
            _sourceCanvas.enabled = false;
        }

        if (_sourceRaycaster != null)
        {
            _sourceRaycaster.enabled = false;
        }

        for (var i = 0; i < _sourceGraphics.Length; i++)
        {
            var graphic = _sourceGraphics[i];
            if (graphic != null)
            {
                graphic.enabled = false;
            }
        }
    }

    private void SetFadeVisible(bool visible)
    {
        if (_fadeRoot != null && _fadeRoot.activeSelf != visible)
        {
            _fadeRoot.SetActive(visible);
        }
    }

    private void EnsureFadeQuad()
    {
        if (_fadeRoot != null)
        {
            return;
        }

        CreateFadeQuad();
    }

    private void CreateFadeQuad()
    {
        if (_fadeRoot != null)
        {
            return;
        }

        _fadeRoot = new GameObject("NOVR_VrFadeQuad");
        DontDestroyOnLoad(_fadeRoot);

        var rect = _fadeRoot.AddComponent<RectTransform>();
        rect.sizeDelta = Vector2.one * (FadeQuadSizeMeters / 0.001f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var canvas = _fadeRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue - 10;
        canvas.worldCamera = APIBus.CockpitHudCamera;
        canvas.planeDistance = FadeQuadDistanceMeters;

        _fadeRoot.transform.localScale = Vector3.one * 0.001f;

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        _fadeImage = _fadeRoot.AddComponent<RawImage>();
        _fadeImage.texture = texture;
        _fadeImage.raycastTarget = false;
        _fadeImage.color = Color.black;

        LayerHelper.SetLayerRecursive(_fadeRoot.transform, LayerHelper.GetVrUiLayer());
        _fadeRoot.SetActive(false);
    }
}
