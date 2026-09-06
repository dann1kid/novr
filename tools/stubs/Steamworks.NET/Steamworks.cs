namespace Steamworks
{
    public enum ELobbyType
    {
        k_ELobbyTypePrivate = 0,
        k_ELobbyTypeFriendsOnly = 1,
        k_ELobbyTypePublic = 2,
        k_ELobbyTypeInvisible = 3
    }

    public struct PublishedFileId_t
    {
        public ulong m_PublishedFileId;
        public PublishedFileId_t(ulong value) { m_PublishedFileId = value; }
        public static implicit operator ulong(PublishedFileId_t id) => id.m_PublishedFileId;
        public override string ToString() => m_PublishedFileId.ToString();
    }

    public struct CSteamID
    {
        public ulong m_SteamID;
        public CSteamID(ulong id) { m_SteamID = id; }
        public static implicit operator ulong(CSteamID id) => id.m_SteamID;
        public override string ToString() => m_SteamID.ToString();
    }

    public struct SteamId
    {
        public ulong Value;
    }
}
