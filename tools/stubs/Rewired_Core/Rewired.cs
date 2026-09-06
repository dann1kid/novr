using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rewired
{
    public enum AxisRange
    {
        Full = 0,
        Positive = 1,
        Negative = 2
    }

    public enum InputActionType
    {
        Axis = 0,
        Button = 1
    }

    public enum ControllerElementType
    {
        Axis = 0,
        Button = 1,
        CompoundElement = 2
    }

    public enum Pole
    {
        Positive = 0,
        Negative = 1
    }

    public enum ControllerType
    {
        Keyboard = 0,
        Mouse = 1,
        Joystick = 2,
        Custom = 3
    }

    public partial class InputAction
    {
        public int id;
        public string name;
        public string descriptiveName;
        public string positiveDescriptiveName;
        public string negativeDescriptiveName;
        public int categoryId;
        public InputActionType type;
        public bool userAssignable = true;
    }

    public class InputCategory
    {
        public int id;
        public string name;
        public string descriptiveName;
    }

    public class InputMapCategory
    {
        public int id;
        public string name;
        public string descriptiveName;
        public bool userAssignable = true;
    }

    public partial class ActionElementMap
    {
        public int id;
        public int actionId;
        public int elementIdentifierId;
        public ControllerElementType elementType;
        public AxisRange axisRange;
        public Pole axisContribution;
        public bool invert;
        public bool enabled = true;
        public KeyCode keyCode;
        public int keyboardKeyCode;
        public string elementIdentifierName;
        public AxisRange axisRangeContribution;
        public string actionDescriptiveName;
    }

    public partial class ControllerMap
    {
        public int id;
        public int categoryId;
        public int layoutId;
        public bool enabled = true;
        public string name;
        public IList<ActionElementMap> AllMaps => _maps;
        public IEnumerable<ActionElementMap> ElementMapsWithAction(int actionId) => _maps.FindAll(m => m.actionId == actionId);
        public bool ContainsAction(int actionId) => _maps.Exists(m => m.actionId == actionId);
        public bool DeleteElementMap(int elementMapId) => _maps.RemoveAll(m => m.id == elementMapId) > 0;
        public ActionElementMap GetFirstElementMapWithAction(int actionId) => _maps.Find(m => m.actionId == actionId);
        public int elementMapCount => _maps.Count;
        private readonly List<ActionElementMap> _maps = new List<ActionElementMap>();
    }

    public class KeyboardMap : ControllerMap { }
    public class MouseMap : ControllerMap { }
    public class JoystickMap : ControllerMap { }

    public partial class Controller
    {
        public int id;
        public string name;
        public string hardwareName;
        public ControllerType type;
        public bool isConnected => true;
        public Guid deviceInstanceGuid;
        public int elementCount => 0;
        public float GetAxis(int index) => 0f;
        public float GetAxisPrev(int index) => 0f;
        public bool GetButton(int index) => false;
        public bool GetButtonDown(int index) => false;
        public bool GetButtonUp(int index) => false;
        public double GetAxisTimeActive(int index) => 0d;
        public double GetButtonTimePressed(int index) => 0d;
    }

    public class Joystick : Controller
    {
        public Joystick() { type = ControllerType.Joystick; }
    }

    public partial class Player
    {
        public int id;
        public string name;
        public readonly ControllerHelper controllers = new ControllerHelper();
        public bool GetButton(string actionName) => false;
        public bool GetButtonDown(string actionName) => false;
        public bool GetButtonUp(string actionName) => false;
        public bool GetButton(int actionId) => false;
        public bool GetButtonDown(int actionId) => false;
        public float GetAxis(string actionName) => 0f;
        public float GetAxis(int actionId) => 0f;
        public float GetAxisRaw(string actionName) => 0f;
        public double GetAxisTimeActive(string actionName) => 0d;
        public double GetButtonTimePressed(string actionName) => 0d;
        public class ControllerHelper
        {
            public readonly MapHelper maps = new MapHelper();
            public IList<Joystick> Joysticks { get; } = new List<Joystick>();
            public Controller GetController(ControllerType type, int id) => null;
            public class MapHelper
            {
                public IEnumerable<TMap> GetMaps<TMap>(int controllerId) where TMap : ControllerMap
                    => new List<TMap>();
                public IEnumerable<ControllerMap> GetAllMaps() => new List<ControllerMap>();
                public TMap GetMap<TMap>(int controllerId, int categoryId, int layoutId) where TMap : ControllerMap
                    => null;
            }
        }
    }

    public static partial class ReInput
    {
        public static bool isReady => true;
        public static readonly MappingHelper mapping = new MappingHelper();
        public static Interfaces.IUserDataStore userDataStore { get; set; } = new NullUserDataStore();
        public static readonly PlayerHelper players = new PlayerHelper();
        public static readonly ControllerHelper controllers = new ControllerHelper();

        public class MappingHelper
        {
            public IEnumerable<InputAction> UserAssignableActions { get; } = new List<InputAction>();
            public IEnumerable<InputAction> Actions { get; } = new List<InputAction>();
            public InputAction GetAction(int actionId) => new InputAction { id = actionId, name = "Action" + actionId, type = InputActionType.Button };
            public InputAction GetAction(string name) => new InputAction { name = name, type = InputActionType.Button };
            public InputCategory GetActionCategory(int categoryId) => new InputCategory { id = categoryId, name = "Category" };
            public InputMapCategory GetMapCategory(int categoryId) => new InputMapCategory { id = categoryId, name = "Map", userAssignable = true };
        }

        public class PlayerHelper
        {
            public Player GetPlayer(int playerId) => new Player { id = playerId };
            public Player GetPlayer(string name) => new Player { name = name };
            public IList<Player> AllPlayers { get; } = new List<Player>();
        }

        public class ControllerHelper
        {
            public IList<Joystick> Joysticks { get; } = new List<Joystick>();
        }

        private class NullUserDataStore : Interfaces.IUserDataStore
        {
            public void Save() { }
            public void Load() { }
        }
    }

    public partial class InputMapper
    {
        public Options options = new Options();
        public bool isRunning { get; private set; }
        public event Action<InputMappedEventData> InputMappedEvent;
        public event Action<CanceledEventData> CanceledEvent;
        public event Action<ErrorEventData> ErrorEvent;
        public event Action<TimedOutEventData> TimedOutEvent;
        public event Action<ConflictFoundEventData> ConflictFoundEvent;

        public bool Start(Context context) { isRunning = true; return true; }
        public void Stop() { isRunning = false; }
        public void Clear() { Stop(); }
        public void RemoveAllEventListeners() { }

        public class Options
        {
            public bool allowAxes = true;
            public bool allowButtons = true;
            public bool allowKeyboardKeysWithModifiers = true;
            public bool allowKeyboardModifierKeyAsPrimary;
            public bool allowButtonsOnFullAxisAssignment;
            public bool ignoreMouseXAxis;
            public bool ignoreMouseYAxis;
            public bool checkForConflicts = true;
            public bool checkForConflictsWithAllControllers;
            public bool checkForConflictsWithSelf = true;
            public bool checkForConflictsWithOtherMaps = true;
            public bool checkForConflictsWithAllPlayers;
            public bool checkForConflictsWithSystemPlayer;
            public bool honorInputBehaviorSettings = true;
            public float timeout = 0;
            public ConflictResponse defaultActionWhenConflictFound = ConflictResponse.Replace;
        }

        public class Context
        {
            public int actionId;
            public AxisRange actionRange;
            public ControllerMap controllerMap;
            public ActionElementMap actionElementMapToReplace;
            public string actionName;
        }

        public enum ConflictResponse
        {
            Cancel,
            Replace,
            Add,
            Ignore
        }

        public class InputMappedEventData
        {
            public ActionElementMap actionElementMap;
            public InputMapper inputMapper;
        }

        public class CanceledEventData
        {
            public InputMapper inputMapper;
            public string message;
        }

        public class ErrorEventData
        {
            public string errorMessage;
            public string message;
            public InputMapper inputMapper;
        }

        public class TimedOutEventData
        {
            public InputMapper inputMapper;
        }

        public class ConflictFoundEventData
        {
            public Action<ConflictResponse> responseCallback;
            public bool isProtected;
            public InputMapper inputMapper;
            public IList<ActionElementMap> conflicts;
        }
    }
}

namespace Rewired.Interfaces
{
    public interface IUserDataStore
    {
        void Save();
        void Load();
    }
}
