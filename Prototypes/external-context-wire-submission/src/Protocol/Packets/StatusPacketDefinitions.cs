using System.IO;

namespace Terraria.NetWork.Core.Protocol;

public sealed class NetworkText
{
    public enum NetworkTextMode : byte
    {
        Literal,
        Formattable,
        LocalizationKey
    }

    private NetworkText[] _substitutions = [];

    public NetworkTextMode Mode { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public IReadOnlyList<NetworkText> Substitutions => _substitutions;

    public static NetworkText FromLiteral(string text)
    {
        return new NetworkText
        {
            Mode = NetworkTextMode.Literal,
            Text = text ?? string.Empty,
            _substitutions = []
        };
    }

    public static NetworkText FromKey(string key, params object[] substitutions)
    {
        return new NetworkText
        {
            Mode = NetworkTextMode.LocalizationKey,
            Text = key ?? string.Empty,
            _substitutions = ConvertSubstitutions(substitutions)
        };
    }

    public static NetworkText FromFormattable(string text, params object[] substitutions)
    {
        return new NetworkText
        {
            Mode = NetworkTextMode.Formattable,
            Text = text ?? string.Empty,
            _substitutions = ConvertSubstitutions(substitutions)
        };
    }

    public static NetworkText Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var text = new NetworkText
        {
            Mode = (NetworkTextMode)reader.ReadByte(),
            Text = reader.ReadString()
        };

        if (text.Mode != NetworkTextMode.Literal)
        {
            var count = reader.ReadByte();
            text._substitutions = new NetworkText[count];
            for (var i = 0; i < count; i++)
            {
                text._substitutions[i] = Deserialize(reader);
            }
        }

        return text;
    }

    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.Write((byte)Mode);
        writer.Write(Text ?? string.Empty);

        if (Mode == NetworkTextMode.Literal)
        {
            return;
        }

        writer.Write((byte)_substitutions.Length);
        for (var i = 0; i < _substitutions.Length; i++)
        {
            _substitutions[i].Serialize(writer);
        }
    }

    public override string ToString()
    {
        return Text;
    }

    private static NetworkText[] ConvertSubstitutions(object[]? substitutions)
    {
        if (substitutions is null || substitutions.Length == 0)
        {
            return [];
        }

        var result = new NetworkText[substitutions.Length];
        for (var i = 0; i < substitutions.Length; i++)
        {
            result[i] = substitutions[i] as NetworkText ?? FromLiteral(substitutions[i]?.ToString() ?? string.Empty);
        }

        return result;
    }
}

public sealed class StatusTextSizePacket : INetPacket
{
    public static PacketType MessageId => PacketType.StatusTextSize;

    public int StatusMaxDelta { get; set; }

    public NetworkText StatusText { get; set; } = NetworkText.FromLiteral(string.Empty);

    public BitsByte ConnectionFlags { get; set; }
}

public static class StatusTextSizePacket9Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<StatusTextSizePacket>();

            builder.Int32(
                "StatusMaxDelta",
                packet => packet.StatusMaxDelta,
                (packet, value) => packet.StatusMaxDelta = value);

            builder.Custom(
                "StatusText",
                "NetworkText",
                packet => packet.StatusText,
                (packet, value) => packet.StatusText = value,
                (writer, value) => value.Serialize(writer),
                NetworkText.Deserialize);

            builder.BitsByte(
                "ConnectionFlags",
                [],
                packet => packet.ConnectionFlags,
                (packet, value) => packet.ConnectionFlags = value);

            Definition = builder.Build((byte)StatusTextSizePacket.MessageId);
        }

        public PacketDefinition<StatusTextSizePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<StatusTextSizePacket> Instance { get; } = LayoutData.Definition;
}
