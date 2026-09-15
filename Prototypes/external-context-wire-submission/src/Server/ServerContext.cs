namespace Terraria.NetWork.Core.Server;

public sealed class ServerPlayer
{
    public int PlayerId { get; init; }

    public string Name { get; set; } = string.Empty;

    public byte SkinVariant { get; set; }

    public byte VoiceVariant { get; set; }

    public float VoicePitchOffset { get; set; }

    public byte Hair { get; set; }

    public byte HairDye { get; set; }

    public bool ExtraAccessory { get; set; }

    public int Difficulty { get; set; }
}

//服务器状态
public sealed class ServerContext
{
    public Dictionary<int, ServerPlayer> Players { get; } = [];

    public string ProtocolVersion { get; set; } = "Terraria318";

    public string ProtocolPassword { get; set; } = string.Empty;

    public bool IsJourneyMode { get; set; }

    public int MaxPlayerNameLength { get; set; } = 20;

    public bool IsNameDuplicate(string name, int exceptPlayerId)
    {
        return Players.Values.Any(player => player.PlayerId != exceptPlayerId && string.Equals(player.Name, name, StringComparison.Ordinal));
    }
}


