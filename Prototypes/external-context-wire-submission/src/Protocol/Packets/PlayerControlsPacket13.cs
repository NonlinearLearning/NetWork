using System.Numerics;

namespace Terraria.NetWork.Core.Protocol;

// Packet 13 protocol DTO. The Graph bootstrap compiles this separately from
// the packet-definition implementation to avoid a generated-code cycle.
public sealed class PlayerControlsPacket13 : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerControls;
    public BitsByte ControlFlags1 { get; set; }
    public BitsByte ControlFlags2 { get; set; }
    public BitsByte ControlFlags3 { get; set; }
    public BitsByte ControlFlags4 { get; set; }
    public byte PlayerId { get; set; }
    public byte SelectedItem { get; set; }
    public Vector2 Position { get; set; }
    public Vector2? Velocity { get; set; }
    public ushort? MountType { get; set; }
    public Vector2? PotionOfReturnOriginalUsePosition { get; set; }
    public Vector2? PotionOfReturnHomePosition { get; set; }
    public Vector2? NetCameraTarget { get; set; }
}
