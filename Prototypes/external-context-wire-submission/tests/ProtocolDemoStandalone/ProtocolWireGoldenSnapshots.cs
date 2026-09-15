namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class ProtocolWireGoldenSnapshots
{
    private static readonly IReadOnlyDictionary<string, string> FrameHexByName = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Hello"] = "0F00010B5465727261726961333138",
        ["StatusTextSize"] = "1D000964000000010B4C6F6164696E67207B307D010005776F726C6405",
        ["PlayerSpawn"] = "12000C076400C8002C010000040005000607",
        ["SendPassword"] = "0A002606736563726574",
        ["Ping"] = "03009A",
        ["QuickStackChests"] = "0E00550300000001000500090001",
        ["ItemTweaker"] = "20005858008FDDCCBBAA30000000D04014003F0E001600CDCCAC3F0300280001",
        ["PlayerHurtV2"] = "0F0075046102005E01054200020502",
        ["PlayerDeathV2"] = "1B00760492120037000D776173207661706F72697A65647800000B",
        ["SyncRevengeMarker"] = "28007E4D0000000000C94200404843370000000000403F0300000004000000DC0500000000614401",
        ["SyncNPC"] = "2C00170C00002096430000FB420000C0BF0000403F06000C050000A03F000020C03200020000C03F025E0104",
        ["SyncProjectile"] = "32001B18000000424200808042000070400000A0BF096200FF010000C03F000000BF0B002C000000B0403000130000001040",
        ["TEDisplayDollDataSync"] = "0F007903D10700000501F502010002",
        ["TEHatRackItemSync"] = "0E007C03D2070000036400010003",
        ["SyncProjectileTrackers"] = "0C008E08080078005902FFFF"
    };

    public static int Count => FrameHexByName.Count;

    public static byte[] GetFrameBytes(string caseName)
    {
        if (!FrameHexByName.TryGetValue(caseName, out var hex))
        {
            throw new InvalidOperationException($"Missing golden wire snapshot for {caseName}.");
        }

        return Convert.FromHexString(hex);
    }
}
