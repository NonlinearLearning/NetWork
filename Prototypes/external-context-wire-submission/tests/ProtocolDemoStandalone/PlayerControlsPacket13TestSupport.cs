using System.Numerics;
using CoreBitsByte = Terraria.NetWork.Core.Protocol.BitsByte;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

// 这是测试阶段的业务快照。
// 它只用于把运行时状态整理成构包需要的输入，不进入生产协议骨架。
public sealed class PlayerControlsStateSnapshot
{
    public CoreBitsByte ControlFlags1 { get; set; }

    public CoreBitsByte ControlFlags2 { get; set; }

    public CoreBitsByte ControlFlags3 { get; set; }

    public CoreBitsByte ControlFlags4 { get; set; }

    public byte PlayerId { get; set; }

    public byte SelectedItem { get; set; }

    public Vector2 Position { get; set; }

    public Vector2? Velocity { get; set; }

    public ushort? MountType { get; set; }

    public Vector2? PotionOfReturnOriginalUsePosition { get; set; }

    public Vector2? PotionOfReturnHomePosition { get; set; }

    public Vector2? NetCameraTarget { get; set; }
}

// 这是测试阶段的构包辅助。
// 它把样本快照转换成一个合法的 13 号包，方便做字节级对照。
public static class PlayerControlsPacket13Builder
{
    public static PlayerControlsPacket13 FromSnapshot(PlayerControlsStateSnapshot snapshot)
    {
        var controlFlags1 = snapshot.ControlFlags1;
        var controlFlags2 = snapshot.ControlFlags2;
        var controlFlags3 = snapshot.ControlFlags3;
        var controlFlags4 = snapshot.ControlFlags4;

        controlFlags2[2] = snapshot.Velocity.HasValue;
        controlFlags2[7] = snapshot.MountType.HasValue;

        var hasPotionOfReturn = snapshot.PotionOfReturnOriginalUsePosition.HasValue &&
            snapshot.PotionOfReturnHomePosition.HasValue;
        controlFlags3[6] = hasPotionOfReturn;

        controlFlags4[5] = snapshot.NetCameraTarget.HasValue;

        return new PlayerControlsPacket13
        {
            ControlFlags1 = controlFlags1,
            ControlFlags2 = controlFlags2,
            ControlFlags3 = controlFlags3,
            ControlFlags4 = controlFlags4,
            PlayerId = snapshot.PlayerId,
            SelectedItem = snapshot.SelectedItem,
            Position = snapshot.Position,
            Velocity = snapshot.Velocity,
            MountType = snapshot.MountType,
            PotionOfReturnOriginalUsePosition = snapshot.PotionOfReturnOriginalUsePosition,
            PotionOfReturnHomePosition = snapshot.PotionOfReturnHomePosition,
            NetCameraTarget = snapshot.NetCameraTarget
        };
    }
}

// 这是测试阶段的旧协议基准。
// 它手工拼出 legacy 字节流，用来验证新 codec 的输出是否完全一致。
public static class PlayerControlsPacket13TestSupport
{
    public static PlayerControlsStateSnapshot CreateSampleSnapshot()
    {
        return new PlayerControlsStateSnapshot
        {
            ControlFlags1 = new CoreBitsByte(true, false, true, false, true, false, true, false),
            ControlFlags2 = new CoreBitsByte(true, false, true, false, true, false, false, true),
            ControlFlags3 = new CoreBitsByte(true, false, true, false, true, false, true, false),
            ControlFlags4 = new CoreBitsByte(true, false, true, false, true, true, false, false),
            PlayerId = 15,
            SelectedItem = 3,
            Position = new Vector2(100f, 200f),
            Velocity = new Vector2(5f, -5f),
            MountType = 1,
            PotionOfReturnOriginalUsePosition = new Vector2(50f, 50f),
            PotionOfReturnHomePosition = new Vector2(10f, 10f),
            NetCameraTarget = new Vector2(300f, 400f)
        };
    }

    public static byte[] BuildLegacyPacketBytes(PlayerControlsPacket13 packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((ushort)0);
        writer.Write((byte)13);
        writer.Write((byte)packet.ControlFlags1);
        writer.Write((byte)packet.ControlFlags2);
        writer.Write((byte)packet.ControlFlags3);
        writer.Write((byte)packet.ControlFlags4);
        writer.Write(packet.PlayerId);
        writer.Write(packet.SelectedItem);
        writer.Write(packet.Position.X);
        writer.Write(packet.Position.Y);

        if (packet.ControlFlags2[2])
        {
            writer.Write(packet.Velocity!.Value.X);
            writer.Write(packet.Velocity.Value.Y);
        }

        if (packet.ControlFlags2[7])
        {
            writer.Write(packet.MountType!.Value);
        }

        if (packet.ControlFlags3[6])
        {
            writer.Write(packet.PotionOfReturnOriginalUsePosition!.Value.X);
            writer.Write(packet.PotionOfReturnOriginalUsePosition.Value.Y);
            writer.Write(packet.PotionOfReturnHomePosition!.Value.X);
            writer.Write(packet.PotionOfReturnHomePosition.Value.Y);
        }

        if (packet.ControlFlags4[5])
        {
            writer.Write(packet.NetCameraTarget!.Value.X);
            writer.Write(packet.NetCameraTarget.Value.Y);
        }

        var bytes = stream.ToArray();
        BitConverter.GetBytes((ushort)bytes.Length).CopyTo(bytes, 0);
        return bytes;
    }
}
