namespace Terraria.NetWork.Core.Protocol;

public sealed class ItemTweakerPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ItemTweaker;

    public short ItemId { get; set; }
    public BitsByte Flags1 { get; set; }
    public uint? ColorPackedValue { get; set; }
    public ushort? Damage { get; set; }
    public float? KnockBack { get; set; }
    public ushort? UseAnimation { get; set; }
    public ushort? UseTime { get; set; }
    public short? Shoot { get; set; }
    public float? ShootSpeed { get; set; }
    public BitsByte Flags2 { get; set; }
    public ushort? Width { get; set; }
    public ushort? Height { get; set; }
    public float? Scale { get; set; }
    public short? Ammo { get; set; }
    public short? UseAmmo { get; set; }
    public bool? NotAmmo { get; set; }
}
