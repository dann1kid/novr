using System;
using NOVR.VrUi;
using UnityEngine;

namespace NOVR;

public class APIBus : MonoBehaviour
{
    #region API Properties
    public static Camera MainCamera => _previousMainCamera;
    public static Camera CockpitHudCamera => NOUIManager.I.CockpitHudCamera;
    public static GameObject CockpitHudReference => NOUIManager.I.CockpitHudReference;

    /// <summary>
    /// Camera that actually submits the HMD view. Prefer this for canvas.worldCamera
    /// and cursor projection so a disabled HUD overlay cannot steal XR.
    /// </summary>
    public static Camera? HeadsetCamera
    {
        get
        {
            if (_previousMainCamera != null)
            {
                return _previousMainCamera;
            }

            return Camera.main;
        }
    }
    public static double AngleFromZero;
    //public static Vector3 TrackingCalibrationOffset => NOVRPoseDriver.TranslationCalibrationOffset;
    #endregion
    
    
    public static Action<Camera?, Camera?> OnMainCameraChanged;       
    private static Camera? _previousMainCamera; 
    
    

    
    private static APIBus? _current;


    private bool _loggedExtraEventsError = false;
    private HandoffState _state = HandoffState.NoState;
    

    private APIBus()
    {
        if (CheckExtraDispatchers())
        {
            string trace = StackTraceUtility.ExtractStackTrace();
            Debug.Log($"{typeof(APIBus)} ctor stack: " + trace);
        }
    }
    
    public void Update()
    {
        if (!CheckExtraDispatchers()) return;


        var newMainCamera = Camera.main;                                                         
        if (newMainCamera != _previousMainCamera)                                                
        {                                                                                        
            OnMainCameraChanged(_previousMainCamera, newMainCamera);                             
            _previousMainCamera = newMainCamera;                                                 
        }                                                                                        
        
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>Should continue executing</returns>
    /// <exception cref="Exception">If internal state is deemed impossible</exception>
    private bool CheckExtraDispatchers()
    {
        var oldState = _state;
        _state = GetDispatcherHandoffState(_state);
        switch (_state)
        {
            case HandoffState.NoState:
            case HandoffState.GodFuckingKnows:
                throw new Exception($"Invalid state in {typeof(APIBus)}! State: {_state}");
            case HandoffState.InitialDispatcher:
                _current = this;
                return true;
            case HandoffState.AwaitingPreviousDisposal:
                LogExtraDispatcher();
                return false;
            case HandoffState.ProperHandoff:
                _current = this;
                return true;
            case HandoffState.ImproperHandoff:
                _current = this;
                if (oldState == HandoffState.AwaitingPreviousDisposal)
                    Debug.LogWarning($"{typeof(APIBus)}: Successful handoff after suspicious state");
                return true;
        }
        throw new Exception("How did we get here?");
    }

    private HandoffState GetDispatcherHandoffState(HandoffState current)
    {
        // Unity's overloaded == treats destroyed objects as null.
        if (_current == null)
        {
            return HandoffState.InitialDispatcher;
        }

        if (_current == this)
        {
            return current;
        }

        return current == HandoffState.NoState
            ? HandoffState.ProperHandoff
            : HandoffState.ImproperHandoff;
    }

    private void LogExtraDispatcher()
    {
        if (_loggedExtraEventsError)
        {
            return;
        }

        Debug.LogError($"Additional instances of {typeof(APIBus)}. This should not happen!");
        _loggedExtraEventsError = true;
    }



    private enum HandoffState
    {
        NoState,
        InitialDispatcher,          
        AwaitingPreviousDisposal,   
        ProperHandoff,                            
        ImproperHandoff,                   
        GodFuckingKnows = -1
    }
 
 
}