using UnityEngine;

namespace NOVR.VrTogglers;

public class VrTogglerManager
{
    private const float RetryIntervalSeconds = 2f;
    private VrToggler _toggler;
    private float _nextRetryTime;

    public bool IsVrEnabled => _toggler != null && _toggler.IsVrEnabled;
    
    public VrTogglerManager()
    {
        SetUpToggler();
        _toggler?.SetVrEnabled(true);
        _nextRetryTime = Time.unscaledTime + RetryIntervalSeconds;
    }

    private void SetUpToggler()
    {
        if (_toggler != null)
        {
            _toggler.SetVrEnabled(false);
        }
        _toggler = new XrPluginOpenXrToggler();
    }

    public void EnsureVrEnabled()
    {
        if (_toggler == null)
        {
            SetUpToggler();
        }

        if (_toggler.IsVrEnabled)
        {
            return;
        }

        if (Time.unscaledTime < _nextRetryTime)
        {
            return;
        }

        _nextRetryTime = Time.unscaledTime + RetryIntervalSeconds;
        _toggler.SetVrEnabled(true);
        if (_toggler.IsVrEnabled)
        {
            Debug.Log("[NOVR] OpenXR started.");
        }
    }

    public void ToggleVr()
    {
        _toggler.SetVrEnabled(!_toggler.IsVrEnabled);
    }
}
