namespace Terraria.NetWork.Core.Protocol;

public readonly record struct RgbColor(byte R, byte G, byte B);

// Legacy packet 4 has both an inbound raw shape and an outbound normalized
// broadcast shape. Both belong to protocol facts instead of Server state.
public sealed class PlayerInfoPacketRaw : INetPacket
{
    public static PacketType MessageId => PacketType.SyncPlayer;

    public BitsByte DifficultyAndExtraAccessoryFlags { get; set; }

    public BitsByte TorchAndAbilityFlags { get; set; }

    public BitsByte ConsumableFlags { get; set; }

    public byte TargetPlayerId { get; set; }

    public byte SkinVariant { get; set; }

    public byte VoiceVariant { get; set; }

    public float VoicePitchOffset { get; set; }

    public byte Hair { get; set; }

    public string Name { get; set; } = string.Empty;

    public byte HairDye { get; set; }

    public ushort HideVisibleAccessoryMask { get; set; }

    public byte HideMisc { get; set; }

    public RgbColor HairColor { get; set; }

    public RgbColor SkinColor { get; set; }

    public RgbColor EyeColor { get; set; }

    public RgbColor ShirtColor { get; set; }

    public RgbColor UnderShirtColor { get; set; }

    public RgbColor PantsColor { get; set; }

    public RgbColor ShoeColor { get; set; }
}

public sealed class PlayerInfoPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncPlayer;

    public BitsByte DifficultyAndExtraAccessoryFlags { get; set; }

    public BitsByte TorchAndAbilityFlags { get; set; }

    public BitsByte ConsumableFlags { get; set; }

    public byte PlayerId { get; set; }

    public byte SkinVariant { get; set; }

    public byte VoiceVariant { get; set; }

    public float VoicePitchOffset { get; set; }

    public byte Hair { get; set; }

    public string Name { get; set; } = string.Empty;

    public byte HairDye { get; set; }

    public ushort HideVisibleAccessoryMask { get; set; }

    public byte HideMisc { get; set; }

    public RgbColor HairColor { get; set; }

    public RgbColor SkinColor { get; set; }

    public RgbColor EyeColor { get; set; }

    public RgbColor ShirtColor { get; set; }

    public RgbColor UnderShirtColor { get; set; }

    public RgbColor PantsColor { get; set; }

    public RgbColor ShoeColor { get; set; }

}

// 4 号包入站原始形态。
// 它对应旧协议从客户端发来的玩家资料，字段值先忠实保留，再由 Server 做 bind/validate/handle。
public static class PlayerInfoPacket4RawDefinition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerInfoPacketRaw>();

            builder.BitsByte(
                "DifficultyAndExtraAccessoryFlags",
                [],
                packet => packet.DifficultyAndExtraAccessoryFlags,
                (packet, value) => packet.DifficultyAndExtraAccessoryFlags = value);

            builder.BitsByte(
                "TorchAndAbilityFlags",
                [],
                packet => packet.TorchAndAbilityFlags,
                (packet, value) => packet.TorchAndAbilityFlags = value);

            builder.BitsByte(
                "ConsumableFlags",
                [],
                packet => packet.ConsumableFlags,
                (packet, value) => packet.ConsumableFlags = value);

            builder.Byte("TargetPlayerId", packet => packet.TargetPlayerId, (packet, value) => packet.TargetPlayerId = value);
            builder.Byte("SkinVariant", packet => packet.SkinVariant, (packet, value) => packet.SkinVariant = value);
            builder.Byte("VoiceVariant", packet => packet.VoiceVariant, (packet, value) => packet.VoiceVariant = value);
            builder.Single("VoicePitchOffset", packet => packet.VoicePitchOffset, (packet, value) => packet.VoicePitchOffset = value);
            builder.Byte("Hair", packet => packet.Hair, (packet, value) => packet.Hair = value);
            builder.String("Name", packet => packet.Name, (packet, value) => packet.Name = value);
            builder.Byte("HairDye", packet => packet.HairDye, (packet, value) => packet.HairDye = value);
            builder.UInt16("HideVisibleAccessoryMask", packet => packet.HideVisibleAccessoryMask, (packet, value) => packet.HideVisibleAccessoryMask = value);
            builder.Byte("HideMisc", packet => packet.HideMisc, (packet, value) => packet.HideMisc = value);

            AddColor(builder, "HairColor", packet => packet.HairColor, (packet, value) => packet.HairColor = value);
            AddColor(builder, "SkinColor", packet => packet.SkinColor, (packet, value) => packet.SkinColor = value);
            AddColor(builder, "EyeColor", packet => packet.EyeColor, (packet, value) => packet.EyeColor = value);
            AddColor(builder, "ShirtColor", packet => packet.ShirtColor, (packet, value) => packet.ShirtColor = value);
            AddColor(builder, "UnderShirtColor", packet => packet.UnderShirtColor, (packet, value) => packet.UnderShirtColor = value);
            AddColor(builder, "PantsColor", packet => packet.PantsColor, (packet, value) => packet.PantsColor = value);
            AddColor(builder, "ShoeColor", packet => packet.ShoeColor, (packet, value) => packet.ShoeColor = value);

            Definition = builder.Build((byte)PlayerInfoPacketRaw.MessageId);
        }

        public PacketDefinition<PlayerInfoPacketRaw> Definition { get; }

        private static void AddColor(
            PacketDefinitionBuilder<PlayerInfoPacketRaw> builder,
            string name,
            Func<PlayerInfoPacketRaw, RgbColor> getter,
            Action<PlayerInfoPacketRaw, RgbColor> setter)
        {
            builder.Custom(
                name,
                "RgbColor",
                (writer, packet) =>
                {
                    var color = getter(packet);
                    writer.Write(color.R);
                    writer.Write(color.G);
                    writer.Write(color.B);
                },
                (reader, packet) => setter(packet, new RgbColor(reader.ReadByte(), reader.ReadByte(), reader.ReadByte())));
        }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerInfoPacketRaw> Instance { get; } = LayoutData.Definition;
}

// 4 号包出站广播形态。
public static class PlayerInfoPacket4Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerInfoPacket>();

            builder.BitsByte(
                "DifficultyAndExtraAccessoryFlags",
                [],
                packet => packet.DifficultyAndExtraAccessoryFlags,
                (packet, value) => packet.DifficultyAndExtraAccessoryFlags = value);

            builder.BitsByte(
                "TorchAndAbilityFlags",
                [],
                packet => packet.TorchAndAbilityFlags,
                (packet, value) => packet.TorchAndAbilityFlags = value);

            builder.BitsByte(
                "ConsumableFlags",
                [],
                packet => packet.ConsumableFlags,
                (packet, value) => packet.ConsumableFlags = value);

            builder.Byte("PlayerId", packet => packet.PlayerId, (packet, value) => packet.PlayerId = value);
            builder.Byte("SkinVariant", packet => packet.SkinVariant, (packet, value) => packet.SkinVariant = value);
            builder.Byte("VoiceVariant", packet => packet.VoiceVariant, (packet, value) => packet.VoiceVariant = value);
            builder.Single("VoicePitchOffset", packet => packet.VoicePitchOffset, (packet, value) => packet.VoicePitchOffset = value);
            builder.Byte("Hair", packet => packet.Hair, (packet, value) => packet.Hair = value);
            builder.String("Name", packet => packet.Name, (packet, value) => packet.Name = value);
            builder.Byte("HairDye", packet => packet.HairDye, (packet, value) => packet.HairDye = value);
            builder.UInt16("HideVisibleAccessoryMask", packet => packet.HideVisibleAccessoryMask, (packet, value) => packet.HideVisibleAccessoryMask = value);
            builder.Byte("HideMisc", packet => packet.HideMisc, (packet, value) => packet.HideMisc = value);

            AddColor(builder, "HairColor", packet => packet.HairColor, (packet, value) => packet.HairColor = value);
            AddColor(builder, "SkinColor", packet => packet.SkinColor, (packet, value) => packet.SkinColor = value);
            AddColor(builder, "EyeColor", packet => packet.EyeColor, (packet, value) => packet.EyeColor = value);
            AddColor(builder, "ShirtColor", packet => packet.ShirtColor, (packet, value) => packet.ShirtColor = value);
            AddColor(builder, "UnderShirtColor", packet => packet.UnderShirtColor, (packet, value) => packet.UnderShirtColor = value);
            AddColor(builder, "PantsColor", packet => packet.PantsColor, (packet, value) => packet.PantsColor = value);
            AddColor(builder, "ShoeColor", packet => packet.ShoeColor, (packet, value) => packet.ShoeColor = value);

            Definition = builder.Build((byte)PlayerInfoPacket.MessageId);
        }

        public PacketDefinition<PlayerInfoPacket> Definition { get; }

        private static void AddColor(
            PacketDefinitionBuilder<PlayerInfoPacket> builder,
            string name,
            Func<PlayerInfoPacket, RgbColor> getter,
            Action<PlayerInfoPacket, RgbColor> setter)
        {
            builder.Custom(
                name,
                "RgbColor",
                getter,
                setter,
                (writer, color) =>
                {
                    writer.Write(color.R);
                    writer.Write(color.G);
                    writer.Write(color.B);
                },
                reader => new RgbColor(reader.ReadByte(), reader.ReadByte(), reader.ReadByte()));
        }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerInfoPacket> Instance { get; } = LayoutData.Definition;
}
