using System.Numerics;

namespace Terraria.NetWork.Core.Protocol;

// Packet 13 的冻结 schema。
// 它只组装协议事实，不承担业务快照构造、legacy 基准拼装或 demo 入口职责。
public static class PlayerControlsPacket13Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerControlsPacket13>();

            ControlFlags1 = builder.BitsByte(
                "ControlFlags1",
                [
                    new PacketBitDefinition(0, "Up"),
                    new PacketBitDefinition(1, "Down"),
                    new PacketBitDefinition(2, "Left"),
                    new PacketBitDefinition(3, "Right"),
                    new PacketBitDefinition(4, "Jump"),
                    new PacketBitDefinition(5, "UseItem"),
                    new PacketBitDefinition(6, "DirectionRight")
                ],
                packet => packet.ControlFlags1,
                (packet, value) => packet.ControlFlags1 = value);

            ControlFlags2 = builder.BitsByte(
                "ControlFlags2",
                [
                    new PacketBitDefinition(0, "Pulley"),
                    new PacketBitDefinition(1, "PulleyDir2"),
                    new PacketBitDefinition(2, "HasVelocity"),
                    new PacketBitDefinition(3, "VortexStealth"),
                    new PacketBitDefinition(4, "GravDirPositive"),
                    new PacketBitDefinition(5, "ShieldRaised"),
                    new PacketBitDefinition(6, "Ghost"),
                    new PacketBitDefinition(7, "HasMount")
                ],
                packet => packet.ControlFlags2,
                (packet, value) => packet.ControlFlags2 = value);

            ControlFlags3 = builder.BitsByte(
                "ControlFlags3",
                [
                    new PacketBitDefinition(0, "HoverUp"),
                    new PacketBitDefinition(1, "VoidVaultEnabled"),
                    new PacketBitDefinition(2, "Sitting"),
                    new PacketBitDefinition(3, "DownedDD2AnyDifficulty"),
                    new PacketBitDefinition(4, "Petting"),
                    new PacketBitDefinition(5, "PetSmall"),
                    new PacketBitDefinition(6, "HasPotionOfReturn"),
                    new PacketBitDefinition(7, "HoverDown")
                ],
                packet => packet.ControlFlags3,
                (packet, value) => packet.ControlFlags3 = value);

            ControlFlags4 = builder.BitsByte(
                "ControlFlags4",
                [
                    new PacketBitDefinition(0, "Sleeping"),
                    new PacketBitDefinition(1, "AutoReuseAllWeapons"),
                    new PacketBitDefinition(2, "ControlDownHold"),
                    new PacketBitDefinition(3, "OperatingAnotherEntity"),
                    new PacketBitDefinition(4, "ControlUseTile"),
                    new PacketBitDefinition(5, "HasNetCameraTarget"),
                    new PacketBitDefinition(6, "LastItemUseAttemptSuccess")
                ],
                packet => packet.ControlFlags4,
                (packet, value) => packet.ControlFlags4 = value);

            HasVelocity = PacketCondition.Flag(ControlFlags2, 2);
            HasMount = PacketCondition.Flag(ControlFlags2, 7);
            HasPotionOfReturn = PacketCondition.Flag(ControlFlags3, 6);
            HasNetCameraTarget = PacketCondition.Flag(ControlFlags4, 5);

            builder.Byte(
                "PlayerId",
                packet => packet.PlayerId,
                (packet, value) => packet.PlayerId = value);

            builder.Byte(
                "SelectedItem",
                packet => packet.SelectedItem,
                (packet, value) => packet.SelectedItem = value);

            builder.Vector2(
                "Position",
                packet => packet.Position,
                (packet, value) => packet.Position = value);

            Velocity = builder.Vector2(
                "Velocity",
                packet => packet.Velocity!.Value,
                (packet, value) => packet.Velocity = value,
                HasVelocity,
                packet => packet.Velocity.HasValue);

            MountType = builder.UInt16(
                "MountType",
                packet => packet.MountType!.Value,
                (packet, value) => packet.MountType = value,
                HasMount,
                packet => packet.MountType.HasValue);

            PotionOfReturnOriginalUsePosition = builder.Vector2(
                "PotionOfReturnOriginalUsePosition",
                packet => packet.PotionOfReturnOriginalUsePosition!.Value,
                (packet, value) => packet.PotionOfReturnOriginalUsePosition = value,
                HasPotionOfReturn,
                packet => packet.PotionOfReturnOriginalUsePosition.HasValue,
                "PotionOfReturn");

            PotionOfReturnHomePosition = builder.Vector2(
                "PotionOfReturnHomePosition",
                packet => packet.PotionOfReturnHomePosition!.Value,
                (packet, value) => packet.PotionOfReturnHomePosition = value,
                HasPotionOfReturn,
                packet => packet.PotionOfReturnHomePosition.HasValue,
                "PotionOfReturn");

            builder.Vector2(
                "NetCameraTarget",
                packet => packet.NetCameraTarget!.Value,
                (packet, value) => packet.NetCameraTarget = value,
                HasNetCameraTarget,
                packet => packet.NetCameraTarget.HasValue);

            builder.Group(
                "PotionOfReturn",
                HasPotionOfReturn,
                [PotionOfReturnOriginalUsePosition, PotionOfReturnHomePosition],
                packet => packet.PotionOfReturnOriginalUsePosition.HasValue &&
                    packet.PotionOfReturnHomePosition.HasValue,
                packet => packet.PotionOfReturnOriginalUsePosition.HasValue ==
                    packet.PotionOfReturnHomePosition.HasValue,
                "PotionOfReturn positions must both be present or both be absent.");

            Definition = builder.Build((byte)PlayerControlsPacket13.MessageId);
        }

        public PacketFlagByteHandle<PlayerControlsPacket13> ControlFlags1 { get; }

        public PacketFlagByteHandle<PlayerControlsPacket13> ControlFlags2 { get; }

        public PacketFlagByteHandle<PlayerControlsPacket13> ControlFlags3 { get; }

        public PacketFlagByteHandle<PlayerControlsPacket13> ControlFlags4 { get; }

        public PacketConditionDefinition<PlayerControlsPacket13> HasVelocity { get; }

        public PacketConditionDefinition<PlayerControlsPacket13> HasMount { get; }

        public PacketConditionDefinition<PlayerControlsPacket13> HasPotionOfReturn { get; }

        public PacketConditionDefinition<PlayerControlsPacket13> HasNetCameraTarget { get; }

        public PacketFieldHandle<PlayerControlsPacket13, Vector2> Velocity { get; }

        public PacketFieldHandle<PlayerControlsPacket13, ushort> MountType { get; }

        public PacketFieldHandle<PlayerControlsPacket13, Vector2> PotionOfReturnOriginalUsePosition { get; }

        public PacketFieldHandle<PlayerControlsPacket13, Vector2> PotionOfReturnHomePosition { get; }

        public PacketDefinition<PlayerControlsPacket13> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerControlsPacket13> Instance { get; } = LayoutData.Definition;
}
