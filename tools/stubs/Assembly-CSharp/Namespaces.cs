using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using Steamworks;
using UnityEngine;

namespace NuclearOption.Effects
{
    public partial class DetailSettings
    {
        public bool GrassEnabled = true;
        public float TreeRangeMultiplier = 1f;
        public static float TreeRangeMultiplierMin = 0.1f;
        public static float TreeRangeMultiplierMax = 4f;
        public void Clear() { }
    }
}

namespace NuclearOption.SavedMission
{
    public partial class Mission
    {
        public string Name;
        public MissionSettings missionSettings = new MissionSettings();
        public MapKey MapKey;
        public MissionKey LoadKey;
        public MissionEnvironment environment = new MissionEnvironment();
    }

    public partial class MissionEnvironment
    {
        public float timeOfDay;
        public float weatherIntensity;
        public float cloudAltitude;
        public float windSpeed;
        public float windHeading;
    }

    public partial class MissionSettings
    {
        public string description;
        public bool allowEventContent;
        public bool allowRespawn;
        public int playerStartingRank;
        public float rankMultiplier = 1f;
        public float successfulSortieBonus;
        public float nuclearEscalationThreshold;
        public float strategicEscalationThreshold;
        public float timeOfDay;
        public float weatherIntensity;
        public float cloudAltitude;
        public float windSpeed;
        public float windHeading;
        public IList<MissionTag> Tags { get; set; } = new List<MissionTag>();
    }

    public partial class MissionQuickLoad
    {
        public MissionSettings missionSettings = new MissionSettings();
    }

    public partial class MissionKey
    {
        public string Name;
        public MissionGroup Group;
        public PublishedFileId_t? WorkshopId;
        public bool TryLoad(out Mission mission, out string error)
        {
            mission = new Mission { Name = Name, LoadKey = this };
            error = null;
            return true;
        }
        public override string ToString() => Name;
    }

    public partial class MissionGroup
    {
        public string Name;
        public static MissionGroup All { get; } = new MissionGroup { Name = "All" };
        public static MissionGroup Default { get; } = new MissionGroup { Name = "Default" };
        public static MissionGroup Tutorial { get; } = new MissionGroup { Name = "Tutorial" };
        public static MissionGroup BuiltIn { get; } = new MissionGroup { Name = "BuiltIn" };
        public static MissionGroup User { get; } = new MissionGroup { Name = "User" };
        public static MissionGroup Workshop { get; } = new MissionGroup { Name = "Workshop" };
        public static void Init() { }
        public IEnumerable<MissionKey> GetMissions() => Array.Empty<MissionKey>();
    }

    public partial struct MissionTag : IEquatable<MissionTag>
    {
        public string Tag;
        public static readonly MissionTag Multiplayer = new MissionTag { Tag = "Multiplayer" };
        public static readonly MissionTag SinglePlayer = new MissionTag { Tag = "SinglePlayer" };
        public static readonly MissionTag PVE = new MissionTag { Tag = "PVE" };
        public static readonly MissionTag PVP = new MissionTag { Tag = "PVP" };
        public static readonly MissionTag Dawn = new MissionTag { Tag = "Dawn" };
        public static readonly MissionTag Day = new MissionTag { Tag = "Day" };
        public static readonly MissionTag Dusk = new MissionTag { Tag = "Dusk" };
        public static readonly MissionTag Night = new MissionTag { Tag = "Night" };
        public bool Equals(MissionTag other) => Tag == other.Tag;
        public override bool Equals(object obj) => obj is MissionTag other && Equals(other);
        public override int GetHashCode() => Tag != null ? Tag.GetHashCode() : 0;
        public static bool operator ==(MissionTag left, MissionTag right) => left.Equals(right);
        public static bool operator !=(MissionTag left, MissionTag right) => !left.Equals(right);
        public static string GetPvpTypeLobbyString(Mission mission) => "pve";
    }

    public static partial class MissionSaveLoad
    {
        public static IEnumerable<MissionLoadEntry> QuickLoadMany(IEnumerable<MissionKey> keys)
        {
            return Array.Empty<MissionLoadEntry>();
        }

        public static bool SaveMissionTemp(Mission mission, string name, bool runBeforeSave, out Mission copy, out string error)
        {
            copy = mission;
            error = null;
            return true;
        }
    }

    public class MissionLoadEntry
    {
        public MissionKey key;
        public MissionQuickLoad mission;
    }
}

namespace NuclearOption.SceneLoading
{
    public partial class MapKey
    {
        public string Name;
        public override string ToString() => Name;
    }

    public partial class MapLoader : MonoBehaviour
    {
        public MapKey mapKey;
        public string mapName;
        public bool TryGetMapName(MapKey key, out string mapName)
        {
            mapName = this.mapName;
            return !string.IsNullOrEmpty(mapName);
        }
    }
}

namespace NuclearOption.Networking
{
    public enum SocketType
    {
        Offline,
        Steam,
        Dedicated
    }

    public partial class HostOptions
    {
        public SocketType Socket;
        public GameState State;
        public MapKey Map;
        public int MaxConnections;
        public string Password;

        public HostOptions(SocketType socket, GameState state, MapKey map)
        {
            Socket = socket;
            State = state;
            Map = map;
        }
    }
}

namespace NuclearOption.Networking.Lobbies
{
    public enum MissionPvpType
    {
        Pve,
        Pvp,
        Mixed
    }

    public partial class LobbyInstance
    {
        public string Name;
        public int PlayerCount;
        public int MaxPlayers;
        public bool HasPassword;
        public bool IsDedicated;
        public int Ping;
        public bool DedicatedServer;
        public bool ModdedServer;
        public MissionPvpType MissionPvpType;
        public string MissionDescriptionSanitized;
        public void SetData(string key, string value) { }
        public string GetData(string key) => "";
        public bool IsPasswordProtected(out string password)
        {
            password = null;
            return HasPassword;
        }
        public bool GetPlayerCounts(out int current, out int max)
        {
            current = PlayerCount;
            max = MaxPlayers;
            return true;
        }
        public static string BoolToTag(bool value) => value ? "true" : "false";
    }

    public partial class LobbyList : MonoBehaviour
    {
        public int TooManyPlayerLimit = 16;
        public void GetListOfLobbies() { }
        public void ShowLobbyPopup(bool show) { }
        public void ShowLobbyPopup(LobbyInstance lobby) { }
    }

    public partial class LobbyListItem : MonoBehaviour
    {
        public LobbyInstance lobby;
        public string LobbyName;
        public string MissionName;
        public string MapName;
        public int? Ping;
        public bool IsFull;
        public bool IsServer;
        public int PlayerCount;
    }

    public partial class LobbyDetailsModal : MonoBehaviour { }

    public partial class HostedLobbyInstance
    {
        public bool HasValue;
        public LobbyInstance Value;
    }
}

namespace NuclearOption.Workshop
{
    public enum OrderBy
    {
        Trend30Days,
        Votes,
        Recent,
        Subscribed,
        TopAllTime,
        New
    }

    public partial class SteamWorkshop : MonoBehaviour
    {
        public static void OpenWorkshopPage() { }
        public static UniTask UpdateAllSubscribedItems() => default;
        public UniTask<bool> RefreshItems(OrderBy order, string tag, List<SteamWorkshopItem> items, uint page) => new UniTask<bool>(false);
        public UniTask Unsubscribe(SteamWorkshopItem item) => default;
        public UniTask DownloadItem(SteamWorkshopItem item) => default;
    }

    public partial class SteamWorkshopItem
    {
        public string Name;
        public string OwnerName;
        public string Description;
        public bool Subscribed;
        public event Action OwnerNameChanged;
        public PublishedFileId_t fileId;
        public UniTask Subscribe() => default;
        public UniTask Unsubscribe() => default;
        public UniTask<Texture2D> GetPreview() => default;
        public UniTask SetPreviewImageAsync(UnityEngine.UI.Image image, ref CancellationTokenSource previewCancellation) => default;
        public void OpenSteamPage() { }
        public void OpenLocalContent() { }
    }

    public partial class WorkshopMenu : MonoBehaviour
    {
        public void CloseMenu() { }
    }
}

namespace NuclearOption.ModScripts
{
    public enum ModType
    {
        None,
        Mission,
        Aircraft
    }

    public class ModTypeInfo
    {
        public string Tag;
        public ModTypeInfo(string tag) { Tag = tag; }
    }

    public static partial class ModTypes
    {
        public static bool AnyLoaded => false;
        public static readonly ModTypeInfo AircraftLivery = new ModTypeInfo("livery");
        public static readonly ModTypeInfo Missions = new ModTypeInfo("missions");
    }
}

namespace NuclearOption.UIStyleSystem
{
    public partial class ThemeManager
    {
        public static ThemeManager Active { get; set; } = new ThemeManager();
        public ColorTheme ColorTheme { get; set; } = new ColorTheme();
    }

    public partial class ColorTheme
    {
        public Color Warning = Color.red;
        public Color Friendly = Color.green;
        public Color Hostile = Color.red;
        public Color Neutral = Color.white;
    }

    public partial class ThemeGroup { }
}
