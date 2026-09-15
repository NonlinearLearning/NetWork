using System.IO;
using System.Numerics;

namespace Terraria.NetWork.Core.Protocol;

// 这一组包共享“动作码/变体载荷”模式。
// 当前只落地旧 TR 与简化读写都能稳定确认的最小线协议结构，
// 不提前把运行时游戏对象完整搬进协议层。

public sealed class SyncEmoteBubblePacket91 : INetPacket
{
    public static PacketType MessageId => PacketType.SyncEmoteBubble;

    public int BubbleId { get; set; }

    public byte AnchorType { get; set; } = byte.MaxValue;

    public ushort AnchorEntityId { get; set; }

    public ushort LifeTime { get; set; }

    public sbyte EmoteId { get; set; }

    public short Metadata { get; set; }
}

public sealed class DevCommandsPacket94 : INetPacket
{
    public static PacketType MessageId => PacketType.DevCommands;

    public string Command { get; set; } = string.Empty;

    public int ReservedValue { get; set; }

    public float Argument1 { get; set; }

    public float Argument2 { get; set; }
}

public sealed class PoofOfSmokePacket106 : INetPacket
{
    public static PacketType MessageId => PacketType.PoofOfSmoke;

    public uint PackedPosition { get; set; }
}

public sealed class SmartTextMessagePacket107 : INetPacket
{
    public static PacketType MessageId => PacketType.SmartTextMessage;

    public byte ColorR { get; set; }

    public byte ColorG { get; set; }

    public byte ColorB { get; set; }

    public NetworkText Text { get; set; } = NetworkText.FromLiteral(string.Empty);

    public short ExtraValue { get; set; }
}

public sealed class ProtocolNetSoundInfo
{
    public Vector2 Position { get; set; }

    public ushort SoundIndex { get; set; }

    public int Style { get; set; } = -1;

    public float Volume { get; set; } = -1f;

    public float PitchOffset { get; set; } = -1f;

    public static ProtocolNetSoundInfo Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var info = new ProtocolNetSoundInfo
        {
            Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            SoundIndex = reader.ReadUInt16()
        };

        var flags = (BitsByte)reader.ReadByte();
        if (flags[0])
        {
            info.Style = reader.ReadInt32();
        }

        if (flags[1])
        {
            info.Volume = reader.ReadSingle();
        }

        if (flags[2])
        {
            info.PitchOffset = reader.ReadSingle();
        }

        return info;
    }

    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(SoundIndex);

        var flags = new BitsByte(Style != -1, Volume != -1f, PitchOffset != -1f);
        writer.Write((byte)flags);

        if (flags[0])
        {
            writer.Write(Style);
        }

        if (flags[1])
        {
            writer.Write(Volume);
        }

        if (flags[2])
        {
            writer.Write(PitchOffset);
        }
    }
}

public sealed class PlayLegacySoundPacket132 : INetPacket
{
    public static PacketType MessageId => PacketType.PlayLegacySound;

    public ProtocolNetSoundInfo SoundInfo { get; set; } = new();
}

public sealed class ShimmerActionsPacket146 : INetPacket
{
    public static PacketType MessageId => PacketType.ShimmerActions;

    public byte ActionType { get; set; }

    public Vector2 Position { get; set; }

    public int CoinAmount { get; set; }
}

internal static class SubtypedActionPacketValidation
{
    public static void EnsureConsumed(byte messageId, MemoryStream stream)
    {
        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException($"Packet {messageId} was not fully consumed. Remaining={stream.Length - stream.Position}");
        }
    }
}

public static class SyncEmoteBubblePacket91Definition
{
    private sealed class Codec : IPacketCustomCodec<SyncEmoteBubblePacket91>
    {
        public SyncEmoteBubblePacket91 Read(PacketDefinition<SyncEmoteBubblePacket91> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new SyncEmoteBubblePacket91
            {
                BubbleId = reader.ReadInt32(),
                AnchorType = reader.ReadByte()
            };

            if (packet.AnchorType != byte.MaxValue)
            {
                packet.AnchorEntityId = reader.ReadUInt16();
                packet.LifeTime = reader.ReadUInt16();
                packet.EmoteId = unchecked((sbyte)reader.ReadByte());
                if (packet.EmoteId < 0)
                {
                    packet.Metadata = reader.ReadInt16();
                }
            }

            SubtypedActionPacketValidation.EnsureConsumed(definition.MessageId, stream);
            return packet;
        }

        public void ValidatePacket(PacketDefinition<SyncEmoteBubblePacket91> definition, SyncEmoteBubblePacket91 packet)
        {
            if (packet.AnchorType == byte.MaxValue)
            {
                return;
            }
        }

        public byte[] Write(PacketDefinition<SyncEmoteBubblePacket91> definition, SyncEmoteBubblePacket91 packet)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.BubbleId);
            writer.Write(packet.AnchorType);

            if (packet.AnchorType != byte.MaxValue)
            {
                writer.Write(packet.AnchorEntityId);
                writer.Write(packet.LifeTime);
                writer.Write(unchecked((byte)packet.EmoteId));
                if (packet.EmoteId < 0)
                {
                    writer.Write(packet.Metadata);
                }
            }

            return stream.ToArray();
        }
    }

    public static PacketDefinition<SyncEmoteBubblePacket91> Instance { get; } =
        new PacketDefinitionBuilder<SyncEmoteBubblePacket91>().Build((byte)SyncEmoteBubblePacket91.MessageId, new Codec());
}

public static class DevCommandsPacket94Definition
{
    private sealed class Codec : IPacketCustomCodec<DevCommandsPacket94>
    {
        public DevCommandsPacket94 Read(PacketDefinition<DevCommandsPacket94> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new DevCommandsPacket94
            {
                Command = reader.ReadString(),
                ReservedValue = reader.ReadInt32(),
                Argument1 = reader.ReadSingle(),
                Argument2 = reader.ReadSingle()
            };

            SubtypedActionPacketValidation.EnsureConsumed(definition.MessageId, stream);
            return packet;
        }

        public void ValidatePacket(PacketDefinition<DevCommandsPacket94> definition, DevCommandsPacket94 packet)
        {
            packet.Command ??= string.Empty;
        }

        public byte[] Write(PacketDefinition<DevCommandsPacket94> definition, DevCommandsPacket94 packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.Command);
            writer.Write(packet.ReservedValue);
            writer.Write(packet.Argument1);
            writer.Write(packet.Argument2);
            return stream.ToArray();
        }
    }

    public static PacketDefinition<DevCommandsPacket94> Instance { get; } =
        new PacketDefinitionBuilder<DevCommandsPacket94>().Build((byte)DevCommandsPacket94.MessageId, new Codec());
}

public static class PoofOfSmokePacket106Definition
{
    private sealed class Codec : IPacketCustomCodec<PoofOfSmokePacket106>
    {
        public PoofOfSmokePacket106 Read(PacketDefinition<PoofOfSmokePacket106> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new PoofOfSmokePacket106
            {
                PackedPosition = reader.ReadUInt32()
            };

            SubtypedActionPacketValidation.EnsureConsumed(definition.MessageId, stream);
            return packet;
        }

        public void ValidatePacket(PacketDefinition<PoofOfSmokePacket106> definition, PoofOfSmokePacket106 packet)
        {
        }

        public byte[] Write(PacketDefinition<PoofOfSmokePacket106> definition, PoofOfSmokePacket106 packet)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.PackedPosition);
            return stream.ToArray();
        }
    }

    public static PacketDefinition<PoofOfSmokePacket106> Instance { get; } =
        new PacketDefinitionBuilder<PoofOfSmokePacket106>().Build((byte)PoofOfSmokePacket106.MessageId, new Codec());
}

public static class SmartTextMessagePacket107Definition
{
    private sealed class Codec : IPacketCustomCodec<SmartTextMessagePacket107>
    {
        public SmartTextMessagePacket107 Read(PacketDefinition<SmartTextMessagePacket107> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new SmartTextMessagePacket107
            {
                ColorR = reader.ReadByte(),
                ColorG = reader.ReadByte(),
                ColorB = reader.ReadByte(),
                Text = NetworkText.Deserialize(reader),
                ExtraValue = reader.ReadInt16()
            };

            SubtypedActionPacketValidation.EnsureConsumed(definition.MessageId, stream);
            return packet;
        }

        public void ValidatePacket(PacketDefinition<SmartTextMessagePacket107> definition, SmartTextMessagePacket107 packet)
        {
            packet.Text ??= NetworkText.FromLiteral(string.Empty);
        }

        public byte[] Write(PacketDefinition<SmartTextMessagePacket107> definition, SmartTextMessagePacket107 packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.ColorR);
            writer.Write(packet.ColorG);
            writer.Write(packet.ColorB);
            packet.Text.Serialize(writer);
            writer.Write(packet.ExtraValue);
            return stream.ToArray();
        }
    }

    public static PacketDefinition<SmartTextMessagePacket107> Instance { get; } =
        new PacketDefinitionBuilder<SmartTextMessagePacket107>().Build((byte)SmartTextMessagePacket107.MessageId, new Codec());
}

public static class PlayLegacySoundPacket132Definition
{
    private sealed class Codec : IPacketCustomCodec<PlayLegacySoundPacket132>
    {
        public PlayLegacySoundPacket132 Read(PacketDefinition<PlayLegacySoundPacket132> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new PlayLegacySoundPacket132
            {
                SoundInfo = ProtocolNetSoundInfo.Deserialize(reader)
            };

            SubtypedActionPacketValidation.EnsureConsumed(definition.MessageId, stream);
            return packet;
        }

        public void ValidatePacket(PacketDefinition<PlayLegacySoundPacket132> definition, PlayLegacySoundPacket132 packet)
        {
            packet.SoundInfo ??= new ProtocolNetSoundInfo();
        }

        public byte[] Write(PacketDefinition<PlayLegacySoundPacket132> definition, PlayLegacySoundPacket132 packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            packet.SoundInfo.Serialize(writer);
            return stream.ToArray();
        }
    }

    public static PacketDefinition<PlayLegacySoundPacket132> Instance { get; } =
        new PacketDefinitionBuilder<PlayLegacySoundPacket132>().Build((byte)PlayLegacySoundPacket132.MessageId, new Codec());
}

public static class ShimmerActionsPacket146Definition
{
    private sealed class Codec : IPacketCustomCodec<ShimmerActionsPacket146>
    {
        public ShimmerActionsPacket146 Read(PacketDefinition<ShimmerActionsPacket146> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new ShimmerActionsPacket146
            {
                ActionType = reader.ReadByte()
            };

            switch (packet.ActionType)
            {
                case 0:
                    packet.Position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    break;
                case 1:
                    packet.Position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    packet.CoinAmount = reader.ReadInt32();
                    break;
                default:
                    throw new InvalidDataException($"Unsupported shimmer action type {packet.ActionType}.");
            }

            SubtypedActionPacketValidation.EnsureConsumed(definition.MessageId, stream);
            return packet;
        }

        public void ValidatePacket(PacketDefinition<ShimmerActionsPacket146> definition, ShimmerActionsPacket146 packet)
        {
            if (packet.ActionType is not 0 and not 1)
            {
                throw new InvalidDataException($"Unsupported shimmer action type {packet.ActionType}.");
            }
        }

        public byte[] Write(PacketDefinition<ShimmerActionsPacket146> definition, ShimmerActionsPacket146 packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.ActionType);
            writer.Write(packet.Position.X);
            writer.Write(packet.Position.Y);
            if (packet.ActionType == 1)
            {
                writer.Write(packet.CoinAmount);
            }

            return stream.ToArray();
        }
    }

    public static PacketDefinition<ShimmerActionsPacket146> Instance { get; } =
        new PacketDefinitionBuilder<ShimmerActionsPacket146>().Build((byte)ShimmerActionsPacket146.MessageId, new Codec());
}
