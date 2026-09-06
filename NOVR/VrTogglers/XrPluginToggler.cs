#if MODERN
using System;
using UnityEngine;
using UnityEngine.XR.Management;

namespace NOVR.VrTogglers;

public abstract class XrPluginToggler: VrToggler
{
    protected XRGeneralSettings _generalSettings;
    protected XRManagerSettings _managerSetings;
    
    protected override bool SetUp()
    {
        try
        {
            if (_generalSettings == null)
            {
                _generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                _managerSetings = ScriptableObject.CreateInstance<XRManagerSettings>();
                _generalSettings.Manager = _managerSetings;

                #pragma warning disable CS0618
                /*
                 * ManagerSettings.loaders is deprecated but very useful, allows me to add the xr loader without reflection.
                 * Should be fine unless the game's Unity version gets majorly updated, in which case the whole mod will be
                 * broken, so I'll have to update it anyway.
                 */
                _managerSetings.loaders.Add(CreateLoader());
                #pragma warning restore CS0618
            }

            _managerSetings.InitializeLoaderSync();
            if (_managerSetings.activeLoader == null)
            {
                Debug.LogError("[NOVR] OpenXR loader did not become active. Wake the headset and wait — NOVR will retry.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[NOVR] OpenXR setup failed: " + ex);
            return false;
        }
    }

    protected override bool EnableVr()
    {
        if (_managerSetings == null)
        {
            return false;
        }

        _managerSetings.StartSubsystems();
        return _managerSetings.activeLoader != null;
    }

    protected override bool DisableVr()
    {
        if (_managerSetings.activeLoader == null) return true;

        _managerSetings.StopSubsystems();
        _managerSetings.DeinitializeLoader();
        return _managerSetings.activeLoader == null;
    }

    protected abstract XRLoader CreateLoader();
}
#endif
