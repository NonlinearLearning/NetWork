using System.IO;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal sealed class LegacyRuntimeHarness
{
    private const int ReadBufferMax = 131070;

    private readonly object?[] _players = new object?[256];
    private readonly object?[] _npcs = new object?[200];
    private readonly object?[] _projectiles = new object?[1000];
    private readonly object?[] _items = new object?[400];
    private readonly object?[] _chests = new object?[1000];
    private readonly byte[] _readBuffer = new byte[ReadBufferMax];

    private int _totalData;

    public LegacyRuntimeHarness()
    {
        Initialize();
    }

    public bool IsInitialized { get; private set; }

    public int NetMode { get; private set; }

    public byte[] WriteFrame(object packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        EnsureInitialized();

        byte[] messageBytes = packet switch
        {
            HelloPacketRaw hello => WriteHello(hello),
            StatusTextSizePacket status => WriteStatusTextSize(status),
            PlayerSpawnPacket spawn => WritePlayerSpawn(spawn),
            PasswordPacketRaw password => WritePassword(password),
            PingPacket ping => WritePing(ping),
            QuickStackChestsPacket quickStack => WriteQuickStackChests(quickStack),
            ItemTweakerPacket itemTweaker => WriteItemTweaker(itemTweaker),
            PlayerHurtV2Packet playerHurt => WritePlayerHurtV2(playerHurt),
            PlayerDeathV2Packet playerDeath => WritePlayerDeathV2(playerDeath),
            SyncRevengeMarkerPacket revengeMarker => WriteSyncRevengeMarker(revengeMarker),
            SyncNpcPacket23 syncNpc => WriteSyncNpc(syncNpc),
            SyncProjectilePacket27 syncProjectile => WriteSyncProjectile(syncProjectile),
            TeDisplayDollDataSyncPacket displayDoll => WriteDisplayDoll(displayDoll),
            TeHatRackItemSyncPacket hatRack => WriteHatRack(hatRack),
            SyncProjectileTrackersPacket142 projectileTrackers => WriteProjectileTrackers(projectileTrackers),
            _ => throw new InvalidOperationException($"Legacy harness has no serializer for {packet.GetType().FullName}.")
        };

        return BuildFrame(messageBytes);
    }

    public WireObservation ReadFrame(byte[] frameBytes)
    {
        ArgumentNullException.ThrowIfNull(frameBytes);
        EnsureInitialized();

        if (frameBytes.Length < 3)
        {
            throw new InvalidDataException("Legacy frame is shorter than a valid packet.");
        }

        var declaredLength = BitConverter.ToUInt16(frameBytes, 0);
        if (declaredLength != frameBytes.Length)
        {
            throw new InvalidDataException($"Legacy frame length mismatch. Header={declaredLength}, Actual={frameBytes.Length}");
        }

        var messageId = frameBytes[2];
        var payload = frameBytes.AsSpan(3).ToArray();
        return new WireObservation(messageId, payload, frameBytes.ToArray(), DecodeMessageBody(frameBytes.AsSpan(2).ToArray()));
    }

    public StreamObservation ReadStream(byte[] bytes, IReadOnlyList<int> chunkPlan)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(chunkPlan);
        EnsureInitialized();
        ResetReadBuffer();

        var frames = new List<WireObservation>();
        var offset = 0;
        foreach (var rawChunkSize in chunkPlan)
        {
            var chunkSize = Math.Min(rawChunkSize, bytes.Length - offset);
            if (chunkSize <= 0)
            {
                continue;
            }

            AppendChunk(bytes, offset, chunkSize);
            offset += chunkSize;

            while (_totalData >= 2)
            {
                var frameLength = (ushort)(_readBuffer[0] | (_readBuffer[1] << 8));
                if (frameLength < 3)
                {
                    throw new InvalidDataException($"Legacy stream encountered invalid frame length {frameLength}.");
                }

                if (_totalData < frameLength)
                {
                    break;
                }

                var frameBytes = new byte[frameLength];
                Buffer.BlockCopy(_readBuffer, 0, frameBytes, 0, frameLength);
                frames.Add(ReadFrame(frameBytes));

                _totalData -= frameLength;
                if (_totalData > 0)
                {
                    Buffer.BlockCopy(_readBuffer, frameLength, _readBuffer, 0, _totalData);
                }
            }
        }

        return new StreamObservation(frames, _totalData);
    }

    private void Initialize()
    {
        NetMode = 2;
        Array.Clear(_players);
        Array.Clear(_npcs);
        Array.Clear(_projectiles);
        Array.Clear(_items);
        Array.Clear(_chests);
        ResetReadBuffer();
        IsInitialized = true;
    }

    private void EnsureInitialized()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Legacy runtime harness was not initialized.");
        }
    }

    private void ResetReadBuffer()
    {
        Array.Clear(_readBuffer);
        _totalData = 0;
    }

    private void AppendChunk(byte[] bytes, int offset, int count)
    {
        if ((_totalData + count) > _readBuffer.Length)
        {
            throw new InvalidDataException($"Legacy read buffer overflow. Count={count}, TotalData={_totalData}");
        }

        Buffer.BlockCopy(bytes, offset, _readBuffer, _totalData, count);
        _totalData += count;
    }

    private static byte[] BuildFrame(byte[] messageBytes)
    {
        var frameBytes = new byte[messageBytes.Length + 2];
        BitConverter.GetBytes((ushort)frameBytes.Length).CopyTo(frameBytes, 0);
        messageBytes.CopyTo(frameBytes, 2);
        return frameBytes;
    }

    private static byte[] WriteHello(HelloPacketRaw packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)HelloPacketRaw.MessageId);
        writer.Write(packet.ClientVersion ?? string.Empty);
        return stream.ToArray();
    }

    private static byte[] WriteStatusTextSize(StatusTextSizePacket packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)StatusTextSizePacket.MessageId);
        writer.Write(packet.StatusMaxDelta);
        packet.StatusText.Serialize(writer);
        writer.Write((byte)packet.ConnectionFlags);
        return stream.ToArray();
    }

    private static byte[] WritePlayerSpawn(PlayerSpawnPacket packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)PlayerSpawnPacket.MessageId);
        writer.Write(packet.PlayerId);
        writer.Write(packet.SpawnX);
        writer.Write(packet.SpawnY);
        writer.Write(packet.RespawnTimer);
        writer.Write(packet.DeathsPve);
        writer.Write(packet.DeathsPvp);
        writer.Write(packet.Team);
        writer.Write(packet.SpawnContext);
        return stream.ToArray();
    }

    private static byte[] WritePassword(PasswordPacketRaw packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)PasswordPacketRaw.MessageId);
        writer.Write(packet.Password ?? string.Empty);
        return stream.ToArray();
    }

    private static byte[] WritePing(PingPacket packet)
    {
        return [(byte)PingPacket.MessageId];
    }

    private static byte[] WriteQuickStackChests(QuickStackChestsPacket packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)QuickStackChestsPacket.MessageId);
        if (packet.SmartStack.HasValue)
        {
            writer.Write(packet.InventorySlotIds.Length);
            foreach (var slotId in packet.InventorySlotIds)
            {
                writer.Write(slotId);
            }

            writer.Write(packet.SmartStack.Value);
            return stream.ToArray();
        }

        writer.Write(packet.BlockedChestIds.Length);
        foreach (var blockedChestId in packet.BlockedChestIds)
        {
            writer.Write(blockedChestId);
        }

        return stream.ToArray();
    }

    private static byte[] WriteItemTweaker(ItemTweakerPacket packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)ItemTweakerPacket.MessageId);
        writer.Write(packet.ItemId);
        writer.Write((byte)packet.Flags1);

        if (packet.Flags1[0])
        {
            writer.Write(packet.ColorPackedValue ?? 0u);
        }

        if (packet.Flags1[1])
        {
            writer.Write(packet.Damage ?? 0);
        }

        if (packet.Flags1[2])
        {
            writer.Write(packet.KnockBack ?? 0f);
        }

        if (packet.Flags1[3])
        {
            writer.Write(packet.UseAnimation ?? 0);
        }

        if (packet.Flags1[4])
        {
            writer.Write(packet.UseTime ?? 0);
        }

        if (packet.Flags1[5])
        {
            writer.Write(packet.Shoot ?? 0);
        }

        if (packet.Flags1[6])
        {
            writer.Write(packet.ShootSpeed ?? 0f);
        }

        if (packet.Flags1[7])
        {
            writer.Write((byte)packet.Flags2);
            if (packet.Flags2[0])
            {
                writer.Write(packet.Width ?? 0);
            }

            if (packet.Flags2[1])
            {
                writer.Write(packet.Height ?? 0);
            }

            if (packet.Flags2[2])
            {
                writer.Write(packet.Scale ?? 0f);
            }

            if (packet.Flags2[3])
            {
                writer.Write(packet.Ammo ?? 0);
            }

            if (packet.Flags2[4])
            {
                writer.Write(packet.UseAmmo ?? 0);
            }

            if (packet.Flags2[5])
            {
                writer.Write(packet.NotAmmo ?? false);
            }
        }

        return stream.ToArray();
    }

    private static byte[] WritePlayerHurtV2(PlayerHurtV2Packet packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)PlayerHurtV2Packet.MessageId);
        writer.Write(packet.PlayerIndex);
        packet.DeathReason.Serialize(writer);
        writer.Write(packet.Damage);
        writer.Write((byte)(packet.HitDirection + 1));
        writer.Write((byte)packet.Flags);
        writer.Write(packet.CooldownCounter);
        return stream.ToArray();
    }

    private static byte[] WritePlayerDeathV2(PlayerDeathV2Packet packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)PlayerDeathV2Packet.MessageId);
        writer.Write(packet.PlayerIndex);
        packet.DeathReason.Serialize(writer);
        writer.Write(packet.Damage);
        writer.Write((byte)(packet.HitDirection + 1));
        writer.Write((byte)packet.Flags);
        return stream.ToArray();
    }

    private static byte[] WriteSyncRevengeMarker(SyncRevengeMarkerPacket packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)SyncRevengeMarkerPacket.MessageId);
        packet.Marker.Serialize(writer);
        return stream.ToArray();
    }

    private static byte[] WriteSyncNpc(SyncNpcPacket23 packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)SyncNpcPacket23.MessageId);
        writer.Write((short)packet.NpcIndex);
        writer.Write(packet.Position.X);
        writer.Write(packet.Position.Y);
        writer.Write(packet.Velocity.X);
        writer.Write(packet.Velocity.Y);
        writer.Write((ushort)packet.Target);
        writer.Write((byte)packet.Flags1);
        writer.Write((byte)packet.Flags2);

        if (packet.Flags1[2])
        {
            writer.Write(packet.Ai0);
        }

        if (packet.Flags1[3])
        {
            writer.Write(packet.Ai1);
        }

        if (packet.Flags1[4])
        {
            writer.Write(packet.Ai2);
        }

        if (packet.Flags1[5])
        {
            writer.Write(packet.Ai3);
        }

        writer.Write((short)packet.NetId);

        if (packet.Flags2[0])
        {
            writer.Write((byte)packet.StatsScaledForPlayersCount);
        }

        if (packet.Flags2[2])
        {
            writer.Write(packet.Difficulty);
        }

        if (!packet.Flags1[7])
        {
            writer.Write(packet.CurrentLifeSize);
            switch (packet.CurrentLifeSize)
            {
                case 1:
                    writer.Write((sbyte)packet.CurrentLife);
                    break;
                case 2:
                    writer.Write((short)packet.CurrentLife);
                    break;
                case 4:
                    writer.Write(packet.CurrentLife);
                    break;
            }
        }

        if (packet.ReleaseOwner >= 0)
        {
            writer.Write((byte)packet.ReleaseOwner);
        }

        return stream.ToArray();
    }

    private static byte[] WriteSyncProjectile(SyncProjectilePacket27 packet)
    {
        var flags1 = new BitsByte(
            packet.Ai0 != 0f,
            packet.Ai1 != 0f,
            packet.Ai2 != 0f,
            packet.BannerIdToRespondTo != 0,
            packet.Damage != 0,
            packet.KnockBack != 0f,
            packet.OriginalDamage != 0,
            packet.ProjectileUuid != -1);
        var flags2 = new BitsByte(packet.Ai2 != 0f);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)SyncProjectilePacket27.MessageId);
        writer.Write((short)packet.ProjectileIdentity);
        writer.Write(packet.Position.X);
        writer.Write(packet.Position.Y);
        writer.Write(packet.Velocity.X);
        writer.Write(packet.Velocity.Y);
        writer.Write((byte)packet.OwnerIndex);
        writer.Write((short)packet.ProjectileType);
        writer.Write((byte)flags1);
        if (flags1[2])
        {
            writer.Write((byte)flags2);
        }

        if (flags1[0])
        {
            writer.Write(packet.Ai0);
        }

        if (flags1[1])
        {
            writer.Write(packet.Ai1);
        }

        if (flags1[3])
        {
            writer.Write((ushort)packet.BannerIdToRespondTo);
        }

        if (flags1[4])
        {
            writer.Write((short)packet.Damage);
        }

        if (flags1[5])
        {
            writer.Write(packet.KnockBack);
        }

        if (flags1[6])
        {
            writer.Write((short)packet.OriginalDamage);
        }

        if (flags1[7])
        {
            writer.Write((short)packet.ProjectileUuid);
        }

        if (flags2[0])
        {
            writer.Write(packet.Ai2);
        }

        return stream.ToArray();
    }

    private static byte[] WriteDisplayDoll(TeDisplayDollDataSyncPacket packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)TeDisplayDollDataSyncPacket.MessageId);
        writer.Write(packet.PlayerIndex);
        writer.Write(packet.TileEntityId);
        writer.Write(packet.ItemIndex);
        writer.Write(packet.Command);
        if (packet.Command == 2)
        {
            writer.Write(packet.Pose);
        }
        else
        {
            packet.Item!.Serialize(writer);
        }

        return stream.ToArray();
    }

    private static byte[] WriteHatRack(TeHatRackItemSyncPacket packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)TeHatRackItemSyncPacket.MessageId);
        writer.Write(packet.PlayerIndex);
        writer.Write(packet.TileEntityId);
        byte encodedSlot = packet.IsDye ? (byte)(packet.SlotIndex + 2) : packet.SlotIndex;
        writer.Write(encodedSlot);
        packet.Item.Serialize(writer);
        return stream.ToArray();
    }

    private static byte[] WriteProjectileTrackers(SyncProjectileTrackersPacket142 packet)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)SyncProjectileTrackersPacket142.MessageId);
        writer.Write(packet.PlayerIndex);
        packet.PiggyBankProjectileTracker.Serialize(writer);
        packet.VoidLensChestTracker.Serialize(writer);
        return stream.ToArray();
    }

    private static INetPacket DecodeMessageBody(byte[] messageBytes)
    {
        using var stream = new MemoryStream(messageBytes, writable: false);
        using var reader = new BinaryReader(stream);
        var messageId = reader.ReadByte();

        INetPacket packet = (PacketType)messageId switch
        {
            PacketType.Hello => ReadHello(reader),
            PacketType.StatusTextSize => ReadStatusTextSize(reader),
            PacketType.PlayerSpawn => ReadPlayerSpawn(reader),
            PacketType.SendPassword => ReadPassword(reader),
            PacketType.Ping => ReadPing(reader),
            PacketType.QuickStackChests => ReadQuickStackChests(reader, stream),
            PacketType.ItemTweaker => ReadItemTweaker(reader),
            PacketType.PlayerHurtV2 => ReadPlayerHurtV2(reader),
            PacketType.PlayerDeathV2 => ReadPlayerDeathV2(reader),
            PacketType.SyncRevengeMarker => ReadSyncRevengeMarker(reader),
            PacketType.SyncNPC => ReadSyncNpc(reader, stream),
            PacketType.SyncProjectile => ReadSyncProjectile(reader, stream),
            PacketType.TEDisplayDollDataSync => ReadDisplayDoll(reader, stream),
            PacketType.TEHatRackItemSync => ReadHatRack(reader, stream),
            PacketType.SyncProjectileTrackers => ReadProjectileTrackers(reader, stream),
            _ => throw new InvalidOperationException($"Legacy harness has no decoder for packet id {messageId}.")
        };

        EnsureConsumed(messageId, stream);
        return packet;
    }

    private static HelloPacketRaw ReadHello(BinaryReader reader)
    {
        return new HelloPacketRaw
        {
            ClientVersion = reader.ReadString()
        };
    }

    private static StatusTextSizePacket ReadStatusTextSize(BinaryReader reader)
    {
        return new StatusTextSizePacket
        {
            StatusMaxDelta = reader.ReadInt32(),
            StatusText = NetworkText.Deserialize(reader),
            ConnectionFlags = reader.ReadByte()
        };
    }

    private static PlayerSpawnPacket ReadPlayerSpawn(BinaryReader reader)
    {
        return new PlayerSpawnPacket
        {
            PlayerId = reader.ReadByte(),
            SpawnX = reader.ReadInt16(),
            SpawnY = reader.ReadInt16(),
            RespawnTimer = reader.ReadInt32(),
            DeathsPve = reader.ReadInt16(),
            DeathsPvp = reader.ReadInt16(),
            Team = reader.ReadByte(),
            SpawnContext = reader.ReadByte()
        };
    }

    private static PasswordPacketRaw ReadPassword(BinaryReader reader)
    {
        return new PasswordPacketRaw
        {
            Password = reader.ReadString()
        };
    }

    private static PingPacket ReadPing(BinaryReader reader)
    {
        return new PingPacket();
    }

    private static QuickStackChestsPacket ReadQuickStackChests(BinaryReader reader, MemoryStream stream)
    {
        var count = reader.ReadInt32();
        var remainingBytes = checked((int)(stream.Length - stream.Position));
        if (remainingBytes == (count * sizeof(short)) + sizeof(bool))
        {
            var slotIds = new short[count];
            for (var i = 0; i < count; i++)
            {
                slotIds[i] = reader.ReadInt16();
            }

            return new QuickStackChestsPacket
            {
                InventorySlotIds = slotIds,
                SmartStack = reader.ReadBoolean()
            };
        }

        if (remainingBytes == count * sizeof(ushort))
        {
            var blockedChestIds = new ushort[count];
            for (var i = 0; i < count; i++)
            {
                blockedChestIds[i] = reader.ReadUInt16();
            }

            return new QuickStackChestsPacket
            {
                BlockedChestIds = blockedChestIds
            };
        }

        throw new InvalidDataException("Legacy QuickStackChests payload does not match either known wire shape.");
    }

    private static ItemTweakerPacket ReadItemTweaker(BinaryReader reader)
    {
        var packet = new ItemTweakerPacket
        {
            ItemId = reader.ReadInt16(),
            Flags1 = reader.ReadByte()
        };

        if (packet.Flags1[0])
        {
            packet.ColorPackedValue = reader.ReadUInt32();
        }

        if (packet.Flags1[1])
        {
            packet.Damage = reader.ReadUInt16();
        }

        if (packet.Flags1[2])
        {
            packet.KnockBack = reader.ReadSingle();
        }

        if (packet.Flags1[3])
        {
            packet.UseAnimation = reader.ReadUInt16();
        }

        if (packet.Flags1[4])
        {
            packet.UseTime = reader.ReadUInt16();
        }

        if (packet.Flags1[5])
        {
            packet.Shoot = reader.ReadInt16();
        }

        if (packet.Flags1[6])
        {
            packet.ShootSpeed = reader.ReadSingle();
        }

        if (packet.Flags1[7])
        {
            packet.Flags2 = reader.ReadByte();
            if (packet.Flags2[0])
            {
                packet.Width = reader.ReadUInt16();
            }

            if (packet.Flags2[1])
            {
                packet.Height = reader.ReadUInt16();
            }

            if (packet.Flags2[2])
            {
                packet.Scale = reader.ReadSingle();
            }

            if (packet.Flags2[3])
            {
                packet.Ammo = reader.ReadInt16();
            }

            if (packet.Flags2[4])
            {
                packet.UseAmmo = reader.ReadInt16();
            }

            if (packet.Flags2[5])
            {
                packet.NotAmmo = reader.ReadBoolean();
            }
        }

        return packet;
    }

    private static PlayerHurtV2Packet ReadPlayerHurtV2(BinaryReader reader)
    {
        return new PlayerHurtV2Packet
        {
            PlayerIndex = reader.ReadByte(),
            DeathReason = PlayerDeathReason.Deserialize(reader),
            Damage = reader.ReadInt16(),
            HitDirection = checked((sbyte)(reader.ReadByte() - 1)),
            Flags = reader.ReadByte(),
            CooldownCounter = reader.ReadSByte()
        };
    }

    private static PlayerDeathV2Packet ReadPlayerDeathV2(BinaryReader reader)
    {
        return new PlayerDeathV2Packet
        {
            PlayerIndex = reader.ReadByte(),
            DeathReason = PlayerDeathReason.Deserialize(reader),
            Damage = reader.ReadInt16(),
            HitDirection = checked((sbyte)(reader.ReadByte() - 1)),
            Flags = reader.ReadByte()
        };
    }

    private static SyncRevengeMarkerPacket ReadSyncRevengeMarker(BinaryReader reader)
    {
        return new SyncRevengeMarkerPacket
        {
            Marker = RevengeMarkerSnapshot.Deserialize(reader)
        };
    }

    private static SyncNpcPacket23 ReadSyncNpc(BinaryReader reader, MemoryStream stream)
    {
        var packet = new SyncNpcPacket23
        {
            NpcIndex = reader.ReadInt16(),
            Position = new System.Numerics.Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Velocity = new System.Numerics.Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Target = reader.ReadUInt16(),
            Flags1 = reader.ReadByte(),
            Flags2 = reader.ReadByte()
        };

        if (packet.Flags1[2])
        {
            packet.Ai0 = reader.ReadSingle();
        }

        if (packet.Flags1[3])
        {
            packet.Ai1 = reader.ReadSingle();
        }

        if (packet.Flags1[4])
        {
            packet.Ai2 = reader.ReadSingle();
        }

        if (packet.Flags1[5])
        {
            packet.Ai3 = reader.ReadSingle();
        }

        packet.NetId = reader.ReadInt16();
        if (packet.Flags2[0])
        {
            packet.StatsScaledForPlayersCount = reader.ReadByte();
        }

        if (packet.Flags2[2])
        {
            packet.Difficulty = reader.ReadSingle();
        }

        if (!packet.Flags1[7])
        {
            packet.CurrentLifeSize = reader.ReadByte();
            packet.CurrentLife = packet.CurrentLifeSize switch
            {
                1 => reader.ReadSByte(),
                2 => reader.ReadInt16(),
                4 => reader.ReadInt32(),
                _ => throw new InvalidDataException($"Unsupported NPC life size marker {packet.CurrentLifeSize}.")
            };
        }

        if (stream.Position < stream.Length)
        {
            packet.ReleaseOwner = reader.ReadByte();
        }

        return packet;
    }

    private static SyncProjectilePacket27 ReadSyncProjectile(BinaryReader reader, MemoryStream stream)
    {
        var packet = new SyncProjectilePacket27
        {
            ProjectileIdentity = reader.ReadInt16(),
            Position = new System.Numerics.Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Velocity = new System.Numerics.Vector2(reader.ReadSingle(), reader.ReadSingle()),
            OwnerIndex = reader.ReadByte(),
            ProjectileType = reader.ReadInt16()
        };

        var flags1 = (BitsByte)reader.ReadByte();
        var flags2 = flags1[2] ? (BitsByte)reader.ReadByte() : (BitsByte)0;

        packet.Ai0 = flags1[0] ? reader.ReadSingle() : 0f;
        packet.Ai1 = flags1[1] ? reader.ReadSingle() : 0f;
        packet.BannerIdToRespondTo = flags1[3] ? reader.ReadUInt16() : 0;
        packet.Damage = flags1[4] ? reader.ReadInt16() : 0;
        packet.KnockBack = flags1[5] ? reader.ReadSingle() : 0f;
        packet.OriginalDamage = flags1[6] ? reader.ReadInt16() : 0;
        packet.ProjectileUuid = flags1[7] ? reader.ReadInt16() : -1;
        packet.Ai2 = flags2[0] ? reader.ReadSingle() : 0f;

        return packet;
    }

    private static TeDisplayDollDataSyncPacket ReadDisplayDoll(BinaryReader reader, MemoryStream stream)
    {
        var packet = new TeDisplayDollDataSyncPacket
        {
            PlayerIndex = reader.ReadByte(),
            TileEntityId = reader.ReadInt32(),
            ItemIndex = reader.ReadByte(),
            Command = reader.ReadByte()
        };

        if (packet.Command == 2)
        {
            packet.Pose = reader.ReadByte();
        }
        else
        {
            packet.Item = TileEntityItemSlotData.Deserialize(reader);
        }

        return packet;
    }

    private static TeHatRackItemSyncPacket ReadHatRack(BinaryReader reader, MemoryStream stream)
    {
        var packet = new TeHatRackItemSyncPacket
        {
            PlayerIndex = reader.ReadByte(),
            TileEntityId = reader.ReadInt32()
        };
        var encodedSlot = reader.ReadByte();
        packet.IsDye = encodedSlot >= 2;
        packet.SlotIndex = packet.IsDye ? (byte)(encodedSlot - 2) : encodedSlot;
        packet.Item = TileEntityItemSlotData.Deserialize(reader);
        return packet;
    }

    private static SyncProjectileTrackersPacket142 ReadProjectileTrackers(BinaryReader reader, MemoryStream stream)
    {
        return new SyncProjectileTrackersPacket142
        {
            PlayerIndex = reader.ReadByte(),
            PiggyBankProjectileTracker = ProtocolTrackedProjectileReference.Deserialize(reader),
            VoidLensChestTracker = ProtocolTrackedProjectileReference.Deserialize(reader)
        };
    }

    private static void EnsureConsumed(byte messageId, MemoryStream stream)
    {
        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException($"Legacy decoder did not fully consume packet {messageId}. Remaining={stream.Length - stream.Position}");
        }
    }
}
