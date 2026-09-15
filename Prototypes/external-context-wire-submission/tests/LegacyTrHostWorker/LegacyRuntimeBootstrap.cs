using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Terraria.NetWork.Verification.LegacyTrHostWorker;

internal sealed class LegacyRuntimeBootstrap
{
    private readonly string _legacyTempPath;
    private readonly Assembly _assembly;
    private readonly Type _programType;
    private readonly Type _mainType;
    private readonly Type _messageBufferType;
    private readonly Type _netMessageType;
    private readonly Type _networkTextType;
    private readonly Type _netplayType;
    private readonly Type _remoteClientType;
    private readonly Type _remoteServerType;
    private readonly Type _playerType;
    private readonly Type _worldItemType;
    private readonly Type _tileType;
    private readonly Type _quickStackingType;
    private readonly Type _quickStackSourceInventoryType;
    private readonly Type _slotReferenceType;
    private readonly Type _deathReasonType;
    private readonly Type _revengeMarkerType;
    private readonly MethodInfo _sendDataMethod;
    private readonly FieldInfo _savePathField;
    private readonly FieldInfo _launchParametersField;
    private readonly FieldInfo _dedServField;
    private readonly FieldInfo _netModeField;
    private readonly FieldInfo _netMessageBufferField;
    private readonly FieldInfo _netplayClientsField;
    private readonly FieldInfo _netplayConnectionField;
    private readonly FieldInfo _serverPasswordField;
    private readonly FieldInfo _playerArrayField;
    private readonly FieldInfo _itemArrayField;
    private readonly FieldInfo _tileGridField;
    private readonly FieldInfo _tileFrameImportantField;
    private readonly FieldInfo _currentPlayerDeathReasonField;
    private readonly FieldInfo _currentRevengeMarkerField;

    public LegacyRuntimeBootstrap(string legacyAssemblyPath, string legacyTempPath)
    {
        _legacyTempPath = legacyTempPath;

        _assembly = Assembly.LoadFrom(legacyAssemblyPath);
        _programType = RequireType("Terraria.Program");
        _mainType = RequireType("Terraria.Main");
        _messageBufferType = RequireType("Terraria.MessageBuffer");
        _netMessageType = RequireType("Terraria.NetMessage");
        _networkTextType = RequireType("Terraria.Localization.NetworkText");
        _netplayType = RequireType("Terraria.Netplay");
        _remoteClientType = RequireType("Terraria.RemoteClient");
        _remoteServerType = RequireType("Terraria.RemoteServer");
        _playerType = RequireType("Terraria.Player");
        _worldItemType = RequireType("Terraria.WorldItem");
        _tileType = RequireType("Terraria.Tile");
        _quickStackingType = RequireType("Terraria.GameContent.QuickStacking");
        _quickStackSourceInventoryType = RequireType("Terraria.GameContent.QuickStacking+SourceInventory");
        _slotReferenceType = RequireType("Terraria.ID.PlayerItemSlotID+SlotReference");
        _deathReasonType = RequireType("Terraria.DataStructures.PlayerDeathReason");
        _revengeMarkerType = RequireType("Terraria.GameContent.CoinLossRevengeSystem+RevengeMarker");

        _sendDataMethod = RequireMethod(
            _netMessageType,
            "SendData",
            [typeof(int), typeof(int), typeof(int), _networkTextType, typeof(int), typeof(float), typeof(float), typeof(float), typeof(int), typeof(int), typeof(int)]);

        _savePathField = RequireField(_programType, "SavePath");
        _launchParametersField = RequireField(_programType, "LaunchParameters");
        _dedServField = RequireField(_mainType, "dedServ");
        _netModeField = RequireField(_mainType, "netMode");
        _netMessageBufferField = RequireField(_netMessageType, "buffer");
        _netplayClientsField = RequireField(_netplayType, "Clients");
        _netplayConnectionField = RequireField(_netplayType, "Connection");
        _serverPasswordField = RequireField(_netplayType, "ServerPassword");
        _playerArrayField = RequireField(_mainType, "player");
        _itemArrayField = RequireField(_mainType, "item");
        _tileGridField = RequireField(_mainType, "tile");
        _tileFrameImportantField = RequireField(_mainType, "tileFrameImportant");
        _currentPlayerDeathReasonField = RequireField(_netMessageType, "_currentPlayerDeathReason", BindingFlags.NonPublic | BindingFlags.Static);
        _currentRevengeMarkerField = RequireField(_netMessageType, "_currentRevengeMarker", BindingFlags.NonPublic | BindingFlags.Static);

        InitializeStaticProgramState();
    }

    public Type NetworkTextType => _networkTextType;

    public Type PlayerType => _playerType;

    public Type WorldItemType => _worldItemType;

    public Type QuickStackSourceInventoryType => _quickStackSourceInventoryType;

    public Type SlotReferenceType => _slotReferenceType;

    public Type DeathReasonType => _deathReasonType;

    public Type RevengeMarkerType => _revengeMarkerType;

    public Assembly LegacyAssembly => _assembly;

    public Array GetPlayers() => (Array)(_playerArrayField.GetValue(null)
        ?? throw new InvalidOperationException("Legacy Main.player is null."));

    public Array GetItems() => (Array)(_itemArrayField.GetValue(null)
        ?? throw new InvalidOperationException("Legacy Main.item is null."));

    public void SetTileFrameImportant(ushort tileType, bool value)
    {
        var frameImportant = (Array)(_tileFrameImportantField.GetValue(null)
            ?? throw new InvalidOperationException("Legacy Main.tileFrameImportant is null."));
        frameImportant.SetValue(value, tileType);
    }

    public void ConfigureTile(
        int x,
        int y,
        ushort type,
        short frameX,
        short frameY,
        byte color,
        ushort wall,
        byte wallColor,
        byte liquid,
        int liquidType,
        bool wire,
        bool halfBrick,
        bool actuator,
        bool inactive,
        byte slope,
        bool wire2,
        bool wire3,
        bool wire4,
        bool fullbrightBlock,
        bool fullbrightWall,
        bool invisibleBlock,
        bool invisibleWall)
    {
        var tiles = (Array)(_tileGridField.GetValue(null)
            ?? throw new InvalidOperationException("Legacy Main.tile is null."));
        var tile = tiles.GetValue(x, y);
        if (tile is null)
        {
            tile = Activator.CreateInstance(_tileType)
                ?? throw new InvalidOperationException("Failed to create legacy Tile.");
            tiles.SetValue(tile, x, y);
        }

        SetFieldValue(tile, "type", type);
        SetFieldValue(tile, "wall", wall);
        SetFieldValue(tile, "liquid", liquid);
        SetFieldValue(tile, "frameX", frameX);
        SetFieldValue(tile, "frameY", frameY);
        SetFieldValue(tile, "sTileHeader", (ushort)0);
        SetFieldValue(tile, "bTileHeader", (byte)0);
        SetFieldValue(tile, "bTileHeader2", (byte)0);
        SetFieldValue(tile, "bTileHeader3", (byte)0);

        InvokeTileSetter(tile, "active", true);
        InvokeTileSetter(tile, "color", color);
        InvokeTileSetter(tile, "wallColor", wallColor);
        InvokeTileSetter(tile, "liquidType", liquidType);
        InvokeTileSetter(tile, "wire", wire);
        InvokeTileSetter(tile, "wire2", wire2);
        InvokeTileSetter(tile, "wire3", wire3);
        InvokeTileSetter(tile, "wire4", wire4);
        InvokeTileSetter(tile, "halfBrick", halfBrick);
        InvokeTileSetter(tile, "slope", slope);
        InvokeTileSetter(tile, "actuator", actuator);
        InvokeTileSetter(tile, "inActive", inactive);
        InvokeTileSetter(tile, "fullbrightBlock", fullbrightBlock);
        InvokeTileSetter(tile, "fullbrightWall", fullbrightWall);
        InvokeTileSetter(tile, "invisibleBlock", invisibleBlock);
        InvokeTileSetter(tile, "invisibleWall", invisibleWall);
    }

    public LegacyRuntimeContext CreateRuntime(int netMode, bool dedServ)
    {
        _dedServField.SetValue(null, dedServ);
        _netModeField.SetValue(null, netMode);
        _serverPasswordField.SetValue(null, null);
        _currentPlayerDeathReasonField.SetValue(null, null);
        _currentRevengeMarkerField.SetValue(null, null);

        var buffers = (Array?)_netMessageBufferField.GetValue(null)
            ?? throw new InvalidOperationException("Legacy NetMessage.buffer is null.");
        for (var i = 0; i < buffers.Length; i++)
        {
            if (buffers.GetValue(i) is null)
            {
                buffers.SetValue(Activator.CreateInstance(_messageBufferType), i);
            }

            ResetMessageBuffer(buffers.GetValue(i)!);
        }

        var clients = Array.CreateInstance(_remoteClientType, 257);
        for (var i = 0; i < clients.Length; i++)
        {
            clients.SetValue(Activator.CreateInstance(_remoteClientType), i);
        }

        _netplayClientsField.SetValue(null, clients);

        var connection = Activator.CreateInstance(_remoteServerType)
            ?? throw new InvalidOperationException("Failed to create legacy RemoteServer.");
        RequireField(_remoteServerType, "ReadBuffer").SetValue(connection, new byte[1024]);
        _netplayConnectionField.SetValue(null, connection);

        EnsureArrayInitialized(_playerArrayField, _playerType);
        EnsureArrayInitialized(_itemArrayField, _worldItemType);

        return new LegacyRuntimeContext(this, buffers, clients, connection);
    }

    public void InvokeSendData(params object[] args) => _sendDataMethod.Invoke(null, args);

    public object CreateNetworkTextFromFormattable(string format, params object[] substitutions)
    {
        var method = RequireMethod(_networkTextType, "FromFormattable", [typeof(string), typeof(object[])]);
        return method.Invoke(null, [format, substitutions])
            ?? throw new InvalidOperationException("Failed to create legacy NetworkText.");
    }

    public object CreateSlotReference(object player, int slotId)
    {
        var ctor = _slotReferenceType.GetConstructor([_playerType, typeof(int)])
            ?? throw new InvalidOperationException("Legacy SlotReference constructor not found.");
        return ctor.Invoke([player, slotId]);
    }

    public object CreateColorFromPacked(uint packedValue)
    {
        var itemType = RequireField(_worldItemType, "inner").FieldType;
        var colorField = RequireField(itemType, "color");
        var colorType = colorField.FieldType;
        var uintCtor = colorType.GetConstructor([typeof(uint)]);
        if (uintCtor is not null)
        {
            return uintCtor.Invoke([packedValue]);
        }

        var color = Activator.CreateInstance(colorType)
            ?? throw new InvalidOperationException("Failed to create legacy Color.");
        var packedProperty = colorType.GetProperty("PackedValue");
        if (packedProperty is null || !packedProperty.CanWrite)
        {
            throw new InvalidOperationException("Legacy Color.PackedValue is not writable.");
        }

        packedProperty.SetValue(color, packedValue, null);
        return color;
    }

    public object CreateVector2(float x, float y, Type vector2Type)
    {
        return Activator.CreateInstance(vector2Type, x, y)
            ?? throw new InvalidOperationException("Failed to create legacy Vector2.");
    }

    public ConstructorInfo GetRevengeMarkerConstructor()
    {
        return _revengeMarkerType.GetConstructors().FirstOrDefault(ctor => ctor.GetParameters().Length == 10)
            ?? throw new InvalidOperationException("Legacy RevengeMarker constructor not found.");
    }

    public void SetCurrentPlayerDeathReason(object? value) => _currentPlayerDeathReasonField.SetValue(null, value);

    public void SetCurrentRevengeMarker(object? value) => _currentRevengeMarkerField.SetValue(null, value);

    public void SetServerPassword(string? password) => _serverPasswordField.SetValue(null, password);

    public static void SetFieldValue(object target, string fieldName, object? value)
    {
        RequireField(target.GetType(), fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .SetValue(target, value);
    }

    public static T GetFieldValue<T>(object target, string fieldName)
    {
        var value = RequireField(target.GetType(), fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .GetValue(target);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"Field {fieldName} on {target.GetType().FullName} is not {typeof(T).FullName}.");
    }

    public static byte[] ConvertHexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    public void CopyFrameToReadBuffer(object bufferObject, byte[] frameBytes, int whoAmI)
    {
        var readBuffer = GetFieldValue<byte[]>(bufferObject, "readBuffer");
        Array.Clear(readBuffer, 0, readBuffer.Length);
        Array.Copy(frameBytes, 0, readBuffer, 0, frameBytes.Length);
        SetFieldValue(bufferObject, "whoAmI", whoAmI);
        SetFieldValue(bufferObject, "totalData", frameBytes.Length);
    }

    public string GetFrameHex(object bufferObject)
    {
        var writeBuffer = GetFieldValue<byte[]>(bufferObject, "writeBuffer");
        var frameLength = BitConverter.ToUInt16(writeBuffer, 0);
        var frameBytes = new byte[frameLength];
        Array.Copy(writeBuffer, 0, frameBytes, 0, frameLength);
        return BitConverter.ToString(frameBytes).Replace("-", string.Empty);
    }

    public byte InvokeGetData(object bufferObject, int start, int length)
    {
        var args = new object[] { start, length, 0 };
        RequireMethod(_messageBufferType, "GetData", [typeof(int), typeof(int), typeof(int).MakeByRefType()]).Invoke(bufferObject, args);
        return checked((byte)(int)args[2]);
    }

    private void InitializeStaticProgramState()
    {
        Directory.CreateDirectory(_legacyTempPath);
        _savePathField.SetValue(null, _legacyTempPath);
        _launchParametersField.SetValue(null, new Dictionary<string, string>());
    }

    private void EnsureArrayInitialized(FieldInfo field, Type elementType)
    {
        var array = (Array?)field.GetValue(null)
            ?? throw new InvalidOperationException($"{field.Name} is null on legacy Main.");
        for (var i = 0; i < array.Length; i++)
        {
            if (array.GetValue(i) is null)
            {
                array.SetValue(Activator.CreateInstance(elementType), i);
            }
        }
    }

    private void ResetMessageBuffer(object bufferObject)
    {
        SetFieldValue(bufferObject, "checkBytes", false);
        SetFieldValue(bufferObject, "totalData", 0);
        SetFieldValue(bufferObject, "messageLength", 0);
        SetFieldValue(bufferObject, "whoAmI", 0);

        var writeBuffer = GetFieldValue<byte[]>(bufferObject, "writeBuffer");
        var readBuffer = GetFieldValue<byte[]>(bufferObject, "readBuffer");
        Array.Clear(writeBuffer, 0, writeBuffer.Length);
        Array.Clear(readBuffer, 0, readBuffer.Length);
    }

    private static void InvokeTileSetter(object tile, string methodName, object value)
    {
        var method = tile.GetType().GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            new[] { value.GetType() },
            modifiers: null)
            ?? throw new InvalidOperationException($"Legacy Tile setter not found: {methodName}({value.GetType().Name}).");
        method.Invoke(tile, new[] { value });
    }

    private Type RequireType(string fullName)
    {
        return _assembly.GetType(fullName)
            ?? throw new InvalidOperationException($"Legacy type not found: {fullName}");
    }

    private static FieldInfo RequireField(Type type, string name, BindingFlags? flags = null)
    {
        var actualFlags = flags ?? (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        return type.GetField(name, actualFlags)
            ?? throw new InvalidOperationException($"Field not found: {type.FullName}.{name}");
    }

    private static MethodInfo RequireMethod(Type type, string name, Type[] parameterTypes)
    {
        return type.GetMethod(name, parameterTypes)
            ?? throw new InvalidOperationException($"Method not found: {type.FullName}.{name}");
    }

    internal sealed class LegacyRuntimeContext
    {
        public LegacyRuntimeContext(LegacyRuntimeBootstrap bootstrap, Array buffers, Array clients, object connection)
        {
            Bootstrap = bootstrap;
            Buffers = buffers;
            Clients = clients;
            Connection = connection;
        }

        public LegacyRuntimeBootstrap Bootstrap { get; }

        public Array Buffers { get; }

        public Array Clients { get; }

        public object Connection { get; }
    }
}
