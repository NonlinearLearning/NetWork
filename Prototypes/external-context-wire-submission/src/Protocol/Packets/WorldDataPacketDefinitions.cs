using System.IO;

namespace Terraria.NetWork.Core.Protocol;

public readonly record struct Int16Point(short X, short Y);

public sealed class ProtocolTreeTops
{
    public const int VariationCount = 13;

    public byte[] Variations { get; set; } = new byte[VariationCount];
}

public sealed class ProtocolExtraSpawnPoints
{
    public Int16Point[] SpawnPoints { get; set; } = [];
}

public sealed class WorldDataPacket : INetPacket
{
    public static PacketType MessageId => PacketType.WorldData;

    public int Time { get; set; }
    public BitsByte TimeFlags { get; set; }
    public byte MoonPhase { get; set; }
    public short MaxTilesX { get; set; }
    public short MaxTilesY { get; set; }
    public short SpawnTileX { get; set; }
    public short SpawnTileY { get; set; }
    public short WorldSurface { get; set; }
    public short RockLayer { get; set; }
    public int WorldId { get; set; }
    public string WorldName { get; set; } = string.Empty;
    public byte GameMode { get; set; }
    public Guid UniqueId { get; set; }
    public ulong WorldGeneratorVersion { get; set; }
    public byte MoonType { get; set; }
    public byte TreeBg1 { get; set; }
    public byte TreeBg2 { get; set; }
    public byte TreeBg3 { get; set; }
    public byte TreeBg4 { get; set; }
    public byte CorruptBg { get; set; }
    public byte JungleBg { get; set; }
    public byte SnowBg { get; set; }
    public byte HallowBg { get; set; }
    public byte CrimsonBg { get; set; }
    public byte DesertBg { get; set; }
    public byte OceanBg { get; set; }
    public byte MushroomBg { get; set; }
    public byte UnderworldBg { get; set; }
    public byte IceBackStyle { get; set; }
    public byte JungleBackStyle { get; set; }
    public byte HellBackStyle { get; set; }
    public float WindSpeedTarget { get; set; }
    public byte NumClouds { get; set; }
    public int[] TreeX { get; set; } = new int[3];
    public byte[] TreeStyle { get; set; } = new byte[4];
    public int[] CaveBackX { get; set; } = new int[3];
    public byte[] CaveBackStyle { get; set; } = new byte[4];
    public ProtocolTreeTops TreeTops { get; set; } = new();
    public float MaxRaining { get; set; }
    public BitsByte WorldFlags1 { get; set; }
    public BitsByte WorldFlags2 { get; set; }
    public BitsByte WorldFlags3 { get; set; }
    public BitsByte WorldFlags4 { get; set; }
    public BitsByte WorldFlags5 { get; set; }
    public BitsByte WorldFlags6 { get; set; }
    public BitsByte WorldFlags7 { get; set; }
    public BitsByte WorldFlags8 { get; set; }
    public BitsByte WorldFlags9 { get; set; }
    public BitsByte WorldFlags10 { get; set; }
    public BitsByte WorldFlags11 { get; set; }
    public byte SundialCooldown { get; set; }
    public byte MoondialCooldown { get; set; }
    public short CopperOreTier { get; set; }
    public short IronOreTier { get; set; }
    public short SilverOreTier { get; set; }
    public short GoldOreTier { get; set; }
    public short CobaltOreTier { get; set; }
    public short MythrilOreTier { get; set; }
    public short AdamantiteOreTier { get; set; }
    public sbyte InvasionType { get; set; }
    public ulong LobbyId { get; set; }
    public float SandstormIntendedSeverity { get; set; }
    public ProtocolExtraSpawnPoints ExtraSpawnPoints { get; set; } = new();
}

public static class WorldDataPacket7Definition
{
    private sealed class WorldDataCodec : IPacketCustomCodec<WorldDataPacket>
    {
        public WorldDataPacket Read(PacketDefinition<WorldDataPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length < 2)
            {
                throw new InvalidDataException($"Packet {definition.MessageId} is shorter than a message body.");
            }

            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            var messageId = reader.ReadByte();
            if (messageId != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {messageId}. Expected {definition.MessageId}");
            }

            var packet = new WorldDataPacket
            {
                Time = reader.ReadInt32(),
                TimeFlags = reader.ReadByte(),
                MoonPhase = reader.ReadByte(),
                MaxTilesX = reader.ReadInt16(),
                MaxTilesY = reader.ReadInt16(),
                SpawnTileX = reader.ReadInt16(),
                SpawnTileY = reader.ReadInt16(),
                WorldSurface = reader.ReadInt16(),
                RockLayer = reader.ReadInt16(),
                WorldId = reader.ReadInt32(),
                WorldName = reader.ReadString(),
                GameMode = reader.ReadByte(),
                UniqueId = new Guid(reader.ReadBytes(16)),
                WorldGeneratorVersion = reader.ReadUInt64(),
                MoonType = reader.ReadByte(),
                TreeBg1 = reader.ReadByte(),
                TreeBg2 = reader.ReadByte(),
                TreeBg3 = reader.ReadByte(),
                TreeBg4 = reader.ReadByte(),
                CorruptBg = reader.ReadByte(),
                JungleBg = reader.ReadByte(),
                SnowBg = reader.ReadByte(),
                HallowBg = reader.ReadByte(),
                CrimsonBg = reader.ReadByte(),
                DesertBg = reader.ReadByte(),
                OceanBg = reader.ReadByte(),
                MushroomBg = reader.ReadByte(),
                UnderworldBg = reader.ReadByte(),
                IceBackStyle = reader.ReadByte(),
                JungleBackStyle = reader.ReadByte(),
                HellBackStyle = reader.ReadByte(),
                WindSpeedTarget = reader.ReadSingle(),
                NumClouds = reader.ReadByte()
            };

            for (var i = 0; i < packet.TreeX.Length; i++)
            {
                packet.TreeX[i] = reader.ReadInt32();
            }

            for (var i = 0; i < packet.TreeStyle.Length; i++)
            {
                packet.TreeStyle[i] = reader.ReadByte();
            }

            for (var i = 0; i < packet.CaveBackX.Length; i++)
            {
                packet.CaveBackX[i] = reader.ReadInt32();
            }

            for (var i = 0; i < packet.CaveBackStyle.Length; i++)
            {
                packet.CaveBackStyle[i] = reader.ReadByte();
            }

            packet.TreeTops = ReadTreeTops(reader);
            packet.MaxRaining = reader.ReadSingle();
            packet.WorldFlags1 = reader.ReadByte();
            packet.WorldFlags2 = reader.ReadByte();
            packet.WorldFlags3 = reader.ReadByte();
            packet.WorldFlags4 = reader.ReadByte();
            packet.WorldFlags5 = reader.ReadByte();
            packet.WorldFlags6 = reader.ReadByte();
            packet.WorldFlags7 = reader.ReadByte();
            packet.WorldFlags8 = reader.ReadByte();
            packet.WorldFlags9 = reader.ReadByte();
            packet.WorldFlags10 = reader.ReadByte();
            packet.WorldFlags11 = reader.ReadByte();
            packet.SundialCooldown = reader.ReadByte();
            packet.MoondialCooldown = reader.ReadByte();
            packet.CopperOreTier = reader.ReadInt16();
            packet.IronOreTier = reader.ReadInt16();
            packet.SilverOreTier = reader.ReadInt16();
            packet.GoldOreTier = reader.ReadInt16();
            packet.CobaltOreTier = reader.ReadInt16();
            packet.MythrilOreTier = reader.ReadInt16();
            packet.AdamantiteOreTier = reader.ReadInt16();
            packet.InvasionType = reader.ReadSByte();
            packet.LobbyId = reader.ReadUInt64();
            packet.SandstormIntendedSeverity = reader.ReadSingle();
            packet.ExtraSpawnPoints = ReadExtraSpawnPoints(reader);

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException($"Packet was not fully consumed. Remaining={stream.Length - stream.Position}");
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<WorldDataPacket> definition, WorldDataPacket packet)
        {
            packet.WorldName ??= string.Empty;
            packet.TreeX ??= new int[3];
            packet.TreeStyle ??= new byte[4];
            packet.CaveBackX ??= new int[3];
            packet.CaveBackStyle ??= new byte[4];
            packet.TreeTops ??= new ProtocolTreeTops();
            packet.TreeTops.Variations ??= new byte[ProtocolTreeTops.VariationCount];
            packet.ExtraSpawnPoints ??= new ProtocolExtraSpawnPoints();
            packet.ExtraSpawnPoints.SpawnPoints ??= [];

            if (packet.TreeX.Length != 3 || packet.CaveBackX.Length != 3)
            {
                throw new InvalidDataException("WorldData packet expects three tree/cave x coordinates.");
            }

            if (packet.TreeStyle.Length != 4 || packet.CaveBackStyle.Length != 4)
            {
                throw new InvalidDataException("WorldData packet expects four tree/cave styles.");
            }

            if (packet.TreeTops.Variations.Length != ProtocolTreeTops.VariationCount)
            {
                throw new InvalidDataException($"WorldData packet expects {ProtocolTreeTops.VariationCount} tree-top variations.");
            }
        }

        public byte[] Write(PacketDefinition<WorldDataPacket> definition, WorldDataPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.Time);
            writer.Write((byte)packet.TimeFlags);
            writer.Write(packet.MoonPhase);
            writer.Write(packet.MaxTilesX);
            writer.Write(packet.MaxTilesY);
            writer.Write(packet.SpawnTileX);
            writer.Write(packet.SpawnTileY);
            writer.Write(packet.WorldSurface);
            writer.Write(packet.RockLayer);
            writer.Write(packet.WorldId);
            writer.Write(packet.WorldName);
            writer.Write(packet.GameMode);
            writer.Write(packet.UniqueId.ToByteArray());
            writer.Write(packet.WorldGeneratorVersion);
            writer.Write(packet.MoonType);
            writer.Write(packet.TreeBg1);
            writer.Write(packet.TreeBg2);
            writer.Write(packet.TreeBg3);
            writer.Write(packet.TreeBg4);
            writer.Write(packet.CorruptBg);
            writer.Write(packet.JungleBg);
            writer.Write(packet.SnowBg);
            writer.Write(packet.HallowBg);
            writer.Write(packet.CrimsonBg);
            writer.Write(packet.DesertBg);
            writer.Write(packet.OceanBg);
            writer.Write(packet.MushroomBg);
            writer.Write(packet.UnderworldBg);
            writer.Write(packet.IceBackStyle);
            writer.Write(packet.JungleBackStyle);
            writer.Write(packet.HellBackStyle);
            writer.Write(packet.WindSpeedTarget);
            writer.Write(packet.NumClouds);

            for (var i = 0; i < packet.TreeX.Length; i++)
            {
                writer.Write(packet.TreeX[i]);
            }

            for (var i = 0; i < packet.TreeStyle.Length; i++)
            {
                writer.Write(packet.TreeStyle[i]);
            }

            for (var i = 0; i < packet.CaveBackX.Length; i++)
            {
                writer.Write(packet.CaveBackX[i]);
            }

            for (var i = 0; i < packet.CaveBackStyle.Length; i++)
            {
                writer.Write(packet.CaveBackStyle[i]);
            }

            WriteTreeTops(writer, packet.TreeTops);
            writer.Write(packet.MaxRaining);
            writer.Write((byte)packet.WorldFlags1);
            writer.Write((byte)packet.WorldFlags2);
            writer.Write((byte)packet.WorldFlags3);
            writer.Write((byte)packet.WorldFlags4);
            writer.Write((byte)packet.WorldFlags5);
            writer.Write((byte)packet.WorldFlags6);
            writer.Write((byte)packet.WorldFlags7);
            writer.Write((byte)packet.WorldFlags8);
            writer.Write((byte)packet.WorldFlags9);
            writer.Write((byte)packet.WorldFlags10);
            writer.Write((byte)packet.WorldFlags11);
            writer.Write(packet.SundialCooldown);
            writer.Write(packet.MoondialCooldown);
            writer.Write(packet.CopperOreTier);
            writer.Write(packet.IronOreTier);
            writer.Write(packet.SilverOreTier);
            writer.Write(packet.GoldOreTier);
            writer.Write(packet.CobaltOreTier);
            writer.Write(packet.MythrilOreTier);
            writer.Write(packet.AdamantiteOreTier);
            writer.Write(packet.InvasionType);
            writer.Write(packet.LobbyId);
            writer.Write(packet.SandstormIntendedSeverity);
            WriteExtraSpawnPoints(writer, packet.ExtraSpawnPoints);

            return stream.ToArray();
        }

        private static ProtocolTreeTops ReadTreeTops(BinaryReader reader)
        {
            var result = new ProtocolTreeTops();
            for (var i = 0; i < result.Variations.Length; i++)
            {
                result.Variations[i] = reader.ReadByte();
            }

            return result;
        }

        private static void WriteTreeTops(BinaryWriter writer, ProtocolTreeTops treeTops)
        {
            for (var i = 0; i < treeTops.Variations.Length; i++)
            {
                writer.Write(treeTops.Variations[i]);
            }
        }

        private static ProtocolExtraSpawnPoints ReadExtraSpawnPoints(BinaryReader reader)
        {
            var count = reader.ReadByte();
            var result = new ProtocolExtraSpawnPoints
            {
                SpawnPoints = new Int16Point[count]
            };

            for (var i = 0; i < count; i++)
            {
                result.SpawnPoints[i] = new Int16Point(reader.ReadInt16(), reader.ReadInt16());
            }

            return result;
        }

        private static void WriteExtraSpawnPoints(BinaryWriter writer, ProtocolExtraSpawnPoints extraSpawnPoints)
        {
            writer.Write((byte)extraSpawnPoints.SpawnPoints.Length);
            for (var i = 0; i < extraSpawnPoints.SpawnPoints.Length; i++)
            {
                writer.Write(extraSpawnPoints.SpawnPoints[i].X);
                writer.Write(extraSpawnPoints.SpawnPoints[i].Y);
            }
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<WorldDataPacket>();
            Definition = builder.Build((byte)WorldDataPacket.MessageId, new WorldDataCodec());
        }

        public PacketDefinition<WorldDataPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<WorldDataPacket> Instance { get; } = LayoutData.Definition;
}
