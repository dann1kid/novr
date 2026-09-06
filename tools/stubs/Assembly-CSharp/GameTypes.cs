using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NuclearOption.Effects;
using NuclearOption.ModScripts;
using NuclearOption.Networking;
using NuclearOption.Networking.Lobbies;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using NuclearOption.UIStyleSystem;
using NuclearOption.Workshop;
using Rewired;
using Rewired.UI.ControlMapper;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class SceneSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T i { get; set; }
}

public static partial class UnitRegistry
{
    public static bool TryGetUnit<T>(object id, out T unit) where T : Unit
    {
        unit = default;
        return false;
    }

    public static bool TryGetUnit<T>(PersistentID id, out T unit) where T : Unit
    {
        unit = default;
        return false;
    }
}

public class PersistentID { }

public static partial class GameManager
{
    public static Player playerInput { get; set; }
    public static bool flightControlsEnabled { get; set; } = true;
    public static ControlMapper controlMapper { get; set; }
    public static bool GetLocalAircraft(out Aircraft aircraft)
    {
        aircraft = null;
        return false;
    }
}

public static partial class PlayerSettings
{
    public enum UnitSystem
    {
        Metric = 0,
        Imperial = 1
    }

    public static GraphicsSettings graphics { get; set; } = new GraphicsSettings();
    public static DetailSettings DetailSettings { get; set; } = new DetailSettings();
    public static bool cinematicMode;
    public static bool debugVis;
    public static float cockpitCamInertia;
    public static float defaultFoV = 80f;
    public static float defaultExternalFoV = 80f;
    public static bool zoomOnBoresight;
    public static bool padLockTarget;
    public static bool tacScreenIR;
    public static bool cameraAutoNVG;
    public static bool showHitMarkers;
    public static bool virtualJoystickEnabled;
    public static bool virtualJoystickInvertPitch;
    public static bool viewInvertPitch;
    public static bool throttleUseNegative;
    public static bool throttleUseRelative;
    public static bool controllerMenuNavigation;
    public static bool menuWeaponSafety;
    public static bool invertCollective;
    public static bool useTrackIR;
    public static float virtualJoystickSensitivity = 1f;
    public static float virtualJoystickCentering;
    public static float viewSensitivity = 1f;
    public static float viewSmoothing;
    public static float clickDelay;
    public static float pressDelay;
    public static bool lagPip;
    public static bool rangeCircle;
    public static bool gauges = true;
    public static bool hudWeapons = true;
    public static float hmdWidth = 1920f;
    public static float hmdHeight = 1080f;
    public static float hmdSideDist;
    public static float hmdSideAngle;
    public static float hmdTopHeight;
    public static float hmdHideDist;
    public static float hmdIconSize = 32f;
    public static float hudTextSize = 24f;
    public static float hmdTextSize = 24f;
    public static float overlayTextSize = 24f;
    public static bool chatEnabled = true;
    public static bool chatFilter;
    public static bool chatTts;
    public static int chatTtsSpeed;
    public static int chatTtsVolume = 100;
    public static UnitSystem unitSystem;
    public static string playerName_Unsanitized = "Pilot";

    public static void LoadPrefs() { }
    public static void ApplyPrefs() { }
}

public class GraphicsSettings
{
    public bool Vsync;
    public int AntiAliasing;
    public int MipmapLevel;
    public int AnisotropicFiltering;
    public int ShadowQuality;
    public int ShadowDistance = 2000;
    public bool SoftShadows = true;
    public float LodBias = 1f;
    public float CloudDetail = 1f;
    public void Clear() { }
}

public static partial class GraphicsHelper
{
    public static readonly string[] AAOptions = { "Off", "2x", "4x", "8x" };
    public static readonly string[] MipmapLimitOptions = { "Full", "Half", "Quarter" };
    public static readonly string[] AnisotropicOptions = { "Disable", "Enable", "Force Enable" };
    public static readonly string[] ShadowQualityOptions = { "Off", "Hard", "Soft" };
}

public static class AudioMixerVolume
{
    public static readonly string Master = "Master";
    public static readonly string Music = "Music";
    public static readonly string Interface = "Interface";
    public static readonly string Effects = "Effects";
    public static readonly string Menu = "Menu";
    public static readonly string RadarWarning = "RadarWarning";
    public static readonly string MissileAlert = "MissileAlert";
    public static readonly string JammedNoise = "JammedNoise";
    public static float GetPref(string channel) => 1f;
    public static void SetValue(string channel, float value) { }
}

public enum GameState
{
    Menu,
    SinglePlayer,
    Multiplayer
}

public enum FactionMode
{
    Neutral,
    Friendly,
    Hostile
}

public class ControlsFilter
{
    public bool GetAim(Unit target, out GlobalPosition? aimPoint, out GlobalPosition? secondary)
    {
        aimPoint = null;
        secondary = null;
        return false;
    }
}

public partial struct GlobalPosition
{
    public float x, y, z;
    public GlobalPosition(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public Vector3 AsVector3() => new Vector3(x, y, z);
    public Vector3 ToLocalPosition() => AsVector3();
    public Vector3 normalized => AsVector3().normalized;
    public float sqrMagnitude => AsVector3().sqrMagnitude;
    public static GlobalPosition operator -(GlobalPosition a, GlobalPosition b) => new GlobalPosition(a.x - b.x, a.y - b.y, a.z - b.z);
}

public static class GlobalPositionExtensions
{
    public static GlobalPosition GlobalPosition(this Transform transform)
    {
        var p = transform != null ? transform.position : Vector3.zero;
        return new GlobalPosition { x = p.x, y = p.y, z = p.z };
    }

    public static Vector3 ToLocalPosition(this GlobalPosition position) => position.AsVector3();
    public static GlobalPosition ToGlobalPosition(this Vector3 vector) => new GlobalPosition(vector.x, vector.y, vector.z);
    public static GlobalPosition GlobalPosition(this Unit unit) => default;
}

public static partial class StringHelper
{
    public static string SanitizeRichText(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}

public partial class Unit : MonoBehaviour
{
    public bool outdated;
    public bool selected;
    public bool fresh;
    public FactionHQ NetworkHQ;
    public Radar radar;
    public bool disabled;
    public bool HasRadarEmission() => false;
    public GlobalPosition GlobalPosition() => default;
}

public partial class Turret : MonoBehaviour
{
    public Vector3 GetDirection() => Vector3.forward;
    public bool IsOnTarget() => false;
    public void SetVector(Vector3 v) { }
}

public partial class Gun : MonoBehaviour
{
    public float GetReloadProgress() => 0f;
}

public partial class WeaponStation : MonoBehaviour
{
    public int Number;
}

public class FlightInfo
{
    public bool HasTakenOff;
}

public partial class Aircraft : Unit
{
    public bool LocalSim;
    public float radarAlt;
    public Pilot[] pilots = Array.Empty<Pilot>();
    public Rigidbody rb;
    public bool disabled;
    public FlightInfo flightInfo = new FlightInfo();
    public void SetTurretVector(int station, Vector3 vector) { }
}

public partial class Airbase : MonoBehaviour
{
    public partial class Runway
    {
        public struct RunwayUsage
        {
            public Runway Runway;
            public bool Reverse;
            public Transform GetEnd() => Runway != null ? Runway.End : null;
        }

        public Vector3 GetGlideslopeAimpoint() => default;
        public Transform Start;
        public Transform End;
        public float GetWidth() => 0f;
        public Vector3 GetVelocity() => Vector3.zero;
    }

    public Transform center;
}

public partial class FactionHQ : MonoBehaviour
{
    public bool TryGetKnownPosition(Unit unit, out GlobalPosition position)
    {
        position = default;
        return false;
    }

    public bool IsTargetPositionAccurate(Unit unit, GlobalPosition position) => true;
    public bool IsTargetPositionAccurate(Unit unit, float range) => true;
}

public partial class Radar : MonoBehaviour
{
    public bool IsJammed() => false;
}

public partial class Pilot : MonoBehaviour
{
    public FlightInfo flightInfo = new FlightInfo();
}

public partial class PilotDismounted : MonoBehaviour
{
    public PersistentID parentUnit;
    public byte pilotNumber;
}

public partial class TargetCam : MonoBehaviour { }
public partial class CameraBaseState { }
public partial class CameraCockpitState : CameraBaseState { }
public partial class CameraOrbitState : CameraBaseState { }
public partial class CameraSelectionState : CameraBaseState { }

public partial class CameraStateManager : MonoBehaviour
{
    public Unit followingUnit;
    public static bool enableMouseLook;
    public CameraBaseState currentState;
    public CameraCockpitState cockpitState;
    public Camera mainCamera;
    public Transform cameraPivot;
}

public partial class CombatHUD : MonoBehaviour
{
    public Aircraft aircraft;
}

public class Datum
{
    public static Transform origin;
    public static Vector3 originPosition;
}

public partial class FlightHud : MonoBehaviour
{
    public Image velocityVector;
}

public partial class HUDOptions : MonoBehaviour
{
    public void ApplyHUDSettings() { }
}

public partial class MapIcon : MonoBehaviour
{
    public enum ClickSource
    {
        Mouse,
        Map,
        Other
    }

    public Image iconImage;
    public void ClickIcon(ClickSource source) { }
}

public partial class UnitMapIcon : MapIcon
{
    public Unit unit;
}

public partial class Missile : MonoBehaviour
{
    public enum SeekerMode
    {
        activeLock,
        activeSearch,
        infrared,
        none
    }

    public SeekerMode seekerMode;
    public GlobalPosition GetEvasionPoint() => default;
    public string GetSeekerType() => seekerMode.ToString();
    public static string GetSeekerType(SeekerMode mode) => mode.ToString();
}
public partial class GameplayUI : MonoBehaviour
{
    public void SelectAirbase() { }
    public void ShowSelectAirbase() { }
}

public partial class MessageUI : MonoBehaviour
{
    public static MessageUI i => SceneSingleton<MessageUI>.i;
    public void SetDynamicBoxSize() { }
    public void GameMessage(string message) { }
    public void KillFeed(string message) { }
    public void DelayedGameMessage(string message) { }
}

public partial class StatusDisplay : MonoBehaviour { }
public partial class DynamicMap : MonoBehaviour
{
    public static bool mapMaximized;
    public Image mapBackground;
    public Image mapImage;
    public Transform iconLayer;
    public float mapDimension = 900f;
    public float mapDisplayFactor = 1f;
    public RectTransform viewIndicator;
    public static FactionMode GetFactionMode(FactionHQ hq) => FactionMode.Neutral;
    public static bool TryGetMapIcon(Unit unit, out MapIcon icon)
    {
        icon = null;
        return false;
    }
}

public partial class MapWaypoint : MonoBehaviour
{
    public MapWaypoint(Vector3 a, Vector3 b, GameObject c, GameObject d) { }
}

public partial class AirbaseOverlay : MonoBehaviour { }
public partial class ObjectiveOverlay : MonoBehaviour
{
    public NoOverlapHelper TextNoOverlap;
    public void UpdateOverlay() { }
}

public partial class ObjectiveOverlayManager : MonoBehaviour
{
}

public partial class HUDBombingState : MonoBehaviour
{
    public void UpdateWeaponDisplay() { }
}

public partial class HUDBoresightState : MonoBehaviour
{
    public void UpdateWeaponDisplay() { }
}

public partial class HUDTurretCrosshair : MonoBehaviour { }
public partial class HUDUnitMarker : MonoBehaviour
{
    public Unit unit;
    public Image image;
    public bool outdated;
    public bool selected;
    public bool fresh;
    public void UpdatePosition() { }
    public void UpdatePosition(FactionHQ hq, GlobalPosition viewPosition, Vector3 cameraForward) { }
}

public partial class HUDOptions : MonoBehaviour { }
public partial class JammedMarker : MonoBehaviour { }
public partial class ThreatItem : MonoBehaviour { }
public partial class Turret : MonoBehaviour { }
public partial class FloatingOrigin : MonoBehaviour
{
    public void OriginShift() { }
    public static void OriginShift(Vector3 offset) { }
}

public partial class GameAssets : MonoBehaviour
{
    public static GameAssets i { get; set; }
    public Sprite targetUnitSpriteJammed;
    public Sprite targetUnitSpriteFriendly;
}

public partial class MissionManager : MonoBehaviour
{
    public static void SetMission(Mission mission, bool checkIfSame = true) { }
}

public partial class MissionSelectListItem : MonoBehaviour { }
public partial class MissionTagListItem : MonoBehaviour { }
public partial class TagFilterListItem : MonoBehaviour
{
    public partial class Item { }
}
public partial class MissionsPicker : MonoBehaviour
{
    public void SelectMission(MissionKey key) { }
}

public partial class SettingsMenu : MonoBehaviour
{
    public void CloseSettingsMenu() { }
}

public partial class TargetListSelector : MonoBehaviour
{
    public bool CheckExclusions(Unit unit) => false;
}
public partial class TargetDetector : MonoBehaviour { }
public partial class Weapon : MonoBehaviour { }
public partial class DetailRenderer : MonoBehaviour
{
    public static DetailRenderer i { get; set; }
}

public static partial class FastMath
{
    public static float Repeat(float x, float length) => x;
    public static float Distance(GlobalPosition a, GlobalPosition b) => 0f;
    public static float Distance(Vector3 a, Vector3 b) => 0f;
    public static bool InRange(GlobalPosition a, GlobalPosition b, float range) => false;
    public static bool InRange(Vector3 a, Vector3 b, float range) => false;
}

public static partial class UnitConverter
{
    public static string DistanceReading(float meters) => $"{meters:0} m";
    public static string Distance(float meters) => DistanceReading(meters);
}

public class NoOverlapHelper
{
    public void SetTarget(Vector3 target) { }
}

public partial class Objective : MonoBehaviour
{
    public SavedObjective SavedObjective = new SavedObjective();
}

public class SavedObjective
{
    public string DisplayName = "Objective";
}

public partial class MissionPosition
{
    public partial class PositionResult
    {
        public float Distance;
        public float? Range;
        public Objective Objective;
        public GlobalPosition Position;
        public Vector3 Direction;
    }
}

public partial class NetworkManagerNuclearOption : MonoBehaviour
{
    public static NetworkManagerNuclearOption i { get; set; }
    public static bool? ModdedServer { get; set; }
    public void StartHost(HostOptions options) { }
    public UniTask StartHostAsync(HostOptions options) => default;
}

public partial class SteamLobby : MonoBehaviour
{
    public static SteamLobby instance { get; set; }
    public string CurrentLobbyName { get; set; }
    public void TryJoinLobby(LobbyInstance lobby, string password, bool promptIfPasswordNeeded) { }
    public void CheckRelayLocationTask() { }
    public UniTask<HostedLobbyInstance> HostLobby(int maxPlayers, ELobbyType type) =>
        new UniTask<HostedLobbyInstance>(new HostedLobbyInstance());
}

public static partial class LobbyPassword
{
    public static string GetShortPassword(string password) => password;
    public static bool TestShortPassword(string full, string shortened) => true;
    public static bool TestShortPassword(LobbyInstance lobby, string password) => true;
}
