# 数据包上下文 API 与固定/可变内存段 GitHub 调研

日期：2026-09-09  
范围：数据包上下文 API、注册 codec、固定/可变 wire segment、长度边界和最终拼接。  
状态：源码调研与设计输入，不代表已经批准实现。  
研究方式：直接读取 GitHub 一手源码，使用固定 commit 定位；源码事实、设计推论和 NetWork 建议分开记录。

## 1. 直接结论

上下文层不应直接暴露最终 wire memory。推荐的两层关系是：

```text
上下文层
    外部领域状态
        -> packet-specific Projector
        -> immutable WireSubmission

非上下文层
    WireSubmission
        -> EncodePlan / registered segment codec
        -> bounded staging lease
        -> validate written length
        -> append to final IBufferWriter<byte>
```

这里必须区分三种“上下文”：

1. **Domain context**：`Main`、`World`、`Session`、目录和路由状态。它只应被上下文层读取。
2. **Wire execution context**：schema、字段 scope、读取/写入 cursor、segment bounds、错误定位和 framing。它属于非上下文层。
3. **Operation metadata**：取消、截止时间、schema revision、预算和诊断信息。它可以显式传递给 Projector，但不应携带最终 writer 或可逃逸的 raw memory。

真实项目的共同点不是“所有数据包都接收一个大 Context”，而是把 packet 值、wire 状态、连接状态和 buffer 能力拆开。`ProtoDef` 的 context 是字段作用域；`MCProtocolLib` 的 `Session`/`ChannelHandlerContext` 是连接或 pipeline 执行状态；`ByteBuf` 是另一个独立的 wire memory。它们都不能直接证明 NetWork 需要一个万能上下文对象。

## 2. 调研项目与证据范围

下表的 commit 通过 `git ls-remote` 核对，链接中的源码行号用于定位事实。

| 项目 | 锁定 commit | 主要证据 |
| --- | --- | --- |
| [ProtoDef-io/node-protodef](https://github.com/ProtoDef-io/node-protodef/tree/173105d05cdb7102c9b69b5abe35718f3d4b5421) | `173105d05cdb7102c9b69b5abe35718f3d4b5421` | 字段 scope、父级字段、count、switch、read size |
| [PrismarineJS/node-minecraft-protocol](https://github.com/PrismarineJS/node-minecraft-protocol/tree/ac7854f2da82d11a5bddc0ee61aa8b120380b4de) | `ac7854f2da82d11a5bddc0ee61aa8b120380b4de` | 协议状态/版本选择、编译 schema、packet framing |
| [GeyserMC/MCProtocolLib](https://github.com/GeyserMC/MCProtocolLib/tree/19783c29ece24bc3f07f8ff08628549527e3de20) | `19783c29ece24bc3f07f8ff08628549527e3de20` | packet registry、`ByteBuf` codec、writer index 回退 |
| [Azure/DotNetty](https://github.com/Azure/DotNetty/tree/379d8cc1d32d2b557347aad1f7b6c48656212a4b) | `379d8cc1d32d2b557347aad1f7b6c48656212a4b` | handler context 与 byte buffer 的职责分离 |
| [MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp/tree/b0e5cee3e30e75004ea95e154ec3832027a46090) | `b0e5cee3e30e75004ea95e154ec3832027a46090` | `IBufferWriter`、raw payload、显式长度 |
| [protobuf-net](https://github.com/protobuf-net/protobuf-net/tree/7eab91f1d326a388bd7c4b2998f6697709d9c77c) | `7eab91f1d326a388bd7c4b2998f6697709d9c77c` | measure、length-delimited 子消息、buffer-writer 限制 |
| [Cysharp/MemoryPack](https://github.com/Cysharp/MemoryPack/tree/85ab9ad76c380aca48c09ff3a0ad955ee5a2902b) | `85ab9ad76c380aca48c09ff3a0ad955ee5a2902b` | 延迟 `Advance`、内部多段 buffer、最终顺序写入 |
| [google/flatbuffers](https://github.com/google/flatbuffers/tree/5761d6e67af841d15ee21bc1ce9a78ffa9cf939e) | `5761d6e67af841d15ee21bc1ce9a78ffa9cf939e` | 反向 builder、offset、table/vector 边界、`Finish` |
| [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore/tree/de3b5fb31f067e447a7a035679a6851179cd34bb) | `de3b5fb31f067e447a7a035679a6851179cd34bb` | 业务 `HttpContext`、feature scope、PipeReader 生命周期 |

关于 `IBufferWriter<T>`、`PipeWriter`、`ReadOnlySequence<T>` 和上述后三个 serializer 的更完整内存证据，见 [固定/可变内存段 API 调研](./2026-09-09-memory-segment-api-research.md) 与 [GitHub 类似项目中的内存段模式](./2026-09-09-github-memory-segment-patterns.md)。本报告只补充数据包上下文和它们对分层的影响。

## 3. 数据包上下文的源码事实

### 3.1 ProtoDef：context 是字段 scope，不是业务 context

`ProtoDef.read`、`write` 和 `sizeOf` 都接收 `buffer`、`offset`、字段描述和 `rootNode`，并把 `rootNode` 传给具体类型函数。`createPacketBuffer` 先计算完整大小，再分配精确长度的 buffer，随后从 offset 0 写入；解析则返回已消费的 `size`。

来源：

- [`src/protodef.js#L109-L164`](https://github.com/ProtoDef-io/node-protodef/blob/173105d05cdb7102c9b69b5abe35718f3d4b5421/src/protodef.js#L109-L164)
- [`doc/api.md#L39-L58`](https://github.com/ProtoDef-io/node-protodef/blob/173105d05cdb7102c9b69b5abe35718f3d4b5421/doc/api.md#L39-L58)

`readContainer` 为每个嵌套对象建立当前值，并暂时保存 `..` 指向父级 context；子字段结束后删除这个内部链接。`readArray` 通过 count 读取固定数量的元素并累加每个元素的 size；`writeArray` 先写 count，再按元素顺序递增 offset。

来源：

- [`src/datatypes/structures.js#L9-L35`](https://github.com/ProtoDef-io/node-protodef/blob/173105d05cdb7102c9b69b5abe35718f3d4b5421/src/datatypes/structures.js#L9-L35)
- [`src/datatypes/structures.js#L38-L90`](https://github.com/ProtoDef-io/node-protodef/blob/173105d05cdb7102c9b69b5abe35718f3d4b5421/src/datatypes/structures.js#L38-L90)

`switch` 根据当前 scope 中的前置字段或 root variable 选择类型；`option` 读取一个 presence byte 后决定是否继续读取 payload。也就是说，条件字段和可变长度由 wire schema 的字段上下文表达，而不是由外部业务对象在 codec 内临时查询。

来源：

- [`src/datatypes/conditional.js#L8-L48`](https://github.com/ProtoDef-io/node-protodef/blob/173105d05cdb7102c9b69b5abe35718f3d4b5421/src/datatypes/conditional.js#L8-L48)
- [`src/datatypes/conditional.js#L51-L70`](https://github.com/ProtoDef-io/node-protodef/blob/173105d05cdb7102c9b69b5abe35718f3d4b5421/src/datatypes/conditional.js#L51-L70)
- [`src/utils.js#L1-L42`](https://github.com/ProtoDef-io/node-protodef/blob/173105d05cdb7102c9b69b5abe35718f3d4b5421/src/utils.js#L1-L42)

**对 NetWork 的借鉴：**保留一个内部 wire scope，用于前置字段、局部 mask、count 和 parent scope 的寻址。**不应借鉴：**把领域 context 作为 `rootNode` 继续传入每个 codec，或让动态字段对象成为公共的 `Dictionary<string, object>` 契约。

### 3.2 PrismarineJS：状态、方向和版本属于 schema 选择

`node-minecraft-protocol` 的 `createProtocol` 以 `state`、`direction`、`version` 和 custom packets 选择或缓存协议定义；编译路径将 minecraft 类型、协议 schema 和 NBT 类型交给 `ProtoDefCompiler`，解释路径则建立 `ProtoDef`。对外的 serializer/deserializer 只接收这些协议参数和 packet 值。

来源：

- [`src/transforms/serializer.js#L14-L44`](https://github.com/PrismarineJS/node-minecraft-protocol/blob/ac7854f2da82d11a5bddc0ee61aa8b120380b4de/src/transforms/serializer.js#L14-L44)
- [`src/transforms/serializer.js#L47-L52`](https://github.com/PrismarineJS/node-minecraft-protocol/blob/ac7854f2da82d11a5bddc0ee61aa8b120380b4de/src/transforms/serializer.js#L47-L52)

其自定义类型的 compiled read/write 仍是 `(buffer, offset)` 和 `(value, buffer, offset)` 形状，并返回新的 offset 或 `{ value, size }`。例如终止数组按 sentinel 返回消费 size，`restBuffer` 明确消费到当前 buffer 末尾。

来源：

- [`src/datatypes/compiler-minecraft.js#L14-L43`](https://github.com/PrismarineJS/node-minecraft-protocol/blob/ac7854f2da82d11a5bddc0ee61aa8b120380b4de/src/datatypes/compiler-minecraft.js#L14-L43)
- [`src/datatypes/compiler-minecraft.js#L88-L105`](https://github.com/PrismarineJS/node-minecraft-protocol/blob/ac7854f2da82d11a5bddc0ee61aa8b120380b4de/src/datatypes/compiler-minecraft.js#L88-L105)

**对 NetWork 的借鉴：**packet mode、schema revision、direction 和版本可以是不可变的 wire profile；read 必须返回消费长度。**不应借鉴：**把 `buffer/offset` 低层入口直接给上下文 Projector。Projector 应先产生强类型 submission，只有非上下文 codec 才接触 wire memory。

### 3.3 MCProtocolLib：registry、packet object、transport context 分离

`MinecraftPacketRegistry` 注册 packet class 和 `PacketFactory`，构建后得到按方向索引的 packet registry；`MinecraftPacketSerializer` 负责调用 packet 的 `serialize(ByteBuf)`，入站则用 factory 从 `ByteBuf` 构造 packet。

来源：

- [`MinecraftPacketRegistry.java#L8-L41`](https://github.com/GeyserMC/MCProtocolLib/blob/19783c29ece24bc3f07f8ff08628549527e3de20/protocol/src/main/java/org/geysermc/mcprotocollib/protocol/codec/MinecraftPacketRegistry.java#L8-L41)
- [`MinecraftPacketSerializer.java#L8-L20`](https://github.com/GeyserMC/MCProtocolLib/blob/19783c29ece24bc3f07f8ff08628549527e3de20/protocol/src/main/java/org/geysermc/mcprotocollib/protocol/codec/MinecraftPacketSerializer.java#L8-L20)

外层 `network.netty.PacketCodec` 持有 `Session` 和方向，选择当前协议的 registry，写入 packet id 和 packet payload。编码前记录 `out.writerIndex()`，发生异常时恢复到起点；解码则记录 reader index，packet 读完后要求没有剩余字节，部分读取时可以恢复起点等待更多输入。

来源：

- [`PacketCodec.java#L21-L64`](https://github.com/GeyserMC/MCProtocolLib/blob/19783c29ece24bc3f07f8ff08628549527e3de20/protocol/src/main/java/org/geysermc/mcprotocollib/network/netty/PacketCodec.java#L21-L64)
- [`PacketCodec.java#L67-L115`](https://github.com/GeyserMC/MCProtocolLib/blob/19783c29ece24bc3f07f8ff08628527e3de20/protocol/src/main/java/org/geysermc/mcprotocollib/network/netty/PacketCodec.java#L67-L115)

具体 packet 可以直接接收/写入 `ByteBuf`。例如 custom payload 在读取时用 `readableBytes` 把剩余输入作为数据，在写入时直接 `writeBytes`。

来源：

- [`ServerboundCustomPayloadPacket.java#L15-L28`](https://github.com/GeyserMC/MCProtocolLib/blob/19783c29ece24bc3f07f8ff08628549527e3de20/protocol/src/main/java/org/geysermc/mcprotocollib/protocol/packet/common/serverbound/ServerboundCustomPayloadPacket.java#L15-L28)
- [`MinecraftTypes.java#L276-L300`](https://github.com/GeyserMC/MCProtocolLib/blob/19783c29ece24bc3f07f8ff08628549527e3de20/protocol/src/main/java/org/geysermc/mcprotocollib/protocol/codec/MinecraftTypes.java#L276-L300)

这里的 `ByteBuf` 直出模式说明了自由度和代价：它很适合成熟的 packet-specific codec，但 packet 对象同时知道 wire layout 和 buffer API，难以形成当前 NetWork 所需的上下文/非上下文隔离。值得借鉴的是“外层保存起点、失败恢复”的原子性；首版 NetWork 可以通过独立 staging 实现同样的失败隔离，不必把 rollback 要求扩散到所有 `IBufferWriter`。

### 3.4 DotNetty：handler execution context 不是 payload buffer

DotNetty 的 `IChannelHandlerContext` 提供 channel、allocator、executor、pipeline 事件传播和写出能力；`IByteBuffer` 则独立提供 reader/writer index、capacity、slice、引用计数以及 mark/reset writer index。现有内存段报告已记录这些接口和 `CompositeByteBuffer` 的证据。

来源：

- [`IChannelHandlerContext.cs#L13-L74`](https://github.com/Azure/DotNetty/blob/379d8cc1d32d2b557347aad1f7b6c48656212a4b/src/DotNetty.Transport/Channels/IChannelHandlerContext.cs#L13-L74)
- [`IByteBuffer.cs#L121-L185`](https://github.com/Azure/DotNetty/blob/379d8cc1d32d2b557347aad1f7b6c48656212a4b/src/DotNetty.Buffers/IByteBuffer.cs#L121-L185)

**对 NetWork 的借鉴：**若需要调度、诊断或取消，应传递 operation context；若需要写 wire bytes，应另外取得 slot 级 write capability。不要把两者合并为一个注册回调参数。

### 3.5 MessagePack、protobuf-net、MemoryPack：wire execution state 的三种形态

这些项目与 Terraria packet schema 不完全相同，但直接证明了三个内存原则：

- MessagePack 的 writer 以 `IBufferWriter<byte>` 追加并显式 `Advance(written)`；长度前缀先写 header，再写指定长度 payload。
- protobuf-net 对 length-delimited 子消息采用 measure 后写长度，再验证实际 payload 长度；前向 buffer-writer 路径不假定任意 backpatch。
- MemoryPack 在内部累积当前 buffer 的写入量，必要时才向底层 writer `Advance`，并能把内部多段按顺序复制到目标 writer。

来源：

- [MessagePackWriter.cs#L25-L105](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/b0e5cee3e30e75004ea95e154ec3832027a46090/src/MessagePack/MessagePackWriter.cs#L25-L105)
- [MessagePackWriter.cs#L450-L517](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/b0e5cee3e30e75004ea95e154ec3832027a46090/src/MessagePack/MessagePackWriter.cs#L450-L517)
- [ProtoWriter.BufferWriter.cs#L248-L270](https://github.com/protobuf-net/protobuf-net/blob/7eab91f1d326a388bd7c4b2998f6697709d9c77c/src/protobuf-net.Core/ProtoWriter.BufferWriter.cs#L248-L270)
- [ProtoWriter.BufferWriter.cs#L408-L430](https://github.com/protobuf-net/protobuf-net/blob/7eab91f1d326a388bd7c4b2998f6697709d9c77c/src/protobuf-net.Core/ProtoWriter.BufferWriter.cs#L408-L430)
- [MemoryPackWriter.cs#L17-L61](https://github.com/Cysharp/MemoryPack/blob/85ab9ad76c380aca48c09ff3a0ad955ee5a2902b/src/MemoryPack.Core/MemoryPackWriter.cs#L17-L61)
- [ReusableLinkedArrayBufferWriter.cs#L130-L207](https://github.com/Cysharp/MemoryPack/blob/85ab9ad76c380aca48c09ff3a0ad955ee5a2902b/src/MemoryPack.Core/Internal/ReusableLinkedArrayBufferWriter.cs#L130-L207)

**对 NetWork 的借鉴：**把“可写容量”“实际写入量”“最终提交量”分成三个状态；需要嵌套边界时单独引入 measure 或 length prefix。不要把这些项目的完整 writer state、异步 flush、offset builder 一次性搬进 packet 上下文。

### 3.6 google/flatbuffers：builder state 是 wire construction state，不是业务 context

`FlatBufferBuilder` 保存 `_space`、`_minAlign`、当前 object 起点、vtable 和 vector 元数据；它从 buffer 尾部向前构建，并通过 `Prep` 计算对齐和必要扩容。`StartTable`/`Slot`/`EndTable` 用 vtable 和 offset 组织可选字段，`StartVector`/`EndVector` 用元素数量和反向写入表达变长数组，`Finish` 才写 root offset 并冻结可读起点。

来源：

- [`FlatBufferBuilder.cs#L32-L52`](https://github.com/google/flatbuffers/blob/5761d6e67af841d15ee21bc1ce9a78ffa9cf939e/net/FlatBuffers/FlatBufferBuilder.cs#L32-L52)
- [`FlatBufferBuilder.cs#L124-L149`](https://github.com/google/flatbuffers/blob/5761d6e67af841d15ee21bc1ce9a78ffa9cf939e/net/FlatBuffers/FlatBufferBuilder.cs#L124-L149)
- [`FlatBufferBuilder.cs#L446-L473`](https://github.com/google/flatbuffers/blob/5761d6e67af841d15ee21bc1ce9a78ffa9cf939e/net/FlatBuffers/FlatBufferBuilder.cs#L446-L473)
- [`FlatBufferBuilder.cs#L487-L519`](https://github.com/google/flatbuffers/blob/5761d6e67af841d15ee21bc1ce9a78ffa9cf939e/net/FlatBuffers/FlatBufferBuilder.cs#L487-L519)
- [`FlatBufferBuilder.cs#L804-L845`](https://github.com/google/flatbuffers/blob/5761d6e67af841d15ee21bc1ce9a78ffa9cf939e/net/FlatBuffers/FlatBufferBuilder.cs#L804-L845)
- [`FlatBufferBuilder.cs#L902-L945`](https://github.com/google/flatbuffers/blob/5761d6e67af841d15ee21bc1ce9a78ffa9cf939e/net/FlatBuffers/FlatBufferBuilder.cs#L902-L945)

这套 API 没有把业务对象或连接会话塞进 builder；可选字段由是否调用 `Slot`/默认值决定，嵌套对象必须先构建成 offset，vector 以 count 和 `EndVector` 结束。它的“回退”不是通用 transaction，而是通过 `_space`、offset 和最终 `Finish` 管理一个专用构建器。

**对 NetWork 的借鉴：**schema slot、显式 nested boundary、最终 finalize 的概念有参考价值。**不应照搬：**反向 builder、对齐/vtable/offset 图会把 packet segment API 变成全局对象构建器；当前 bounded segment 只需要正向 staging、`written` 和明确 framing。

### 3.7 dotnet/aspnetcore：`HttpContext` 是业务请求上下文，Pipe/feature 是独立 wire-like capability

ASP.NET Core 的 `HttpContext` 明确封装单个 HTTP 请求的 HTTP-specific information，公开 `Request`、`Response`、`Connection`、`Items`、`RequestServices`、`RequestAborted`、`TraceIdentifier` 和 `Session`。这些是业务/请求生命周期上下文，不是字节读取 cursor；`IHttpContextAccessor` 还明确警告 ambient state 会增加 async 成本并降低可测试性。

来源：

- [`HttpContext.cs#L12-L78`](https://github.com/dotnet/aspnetcore/blob/de3b5fb31f067e447a7a035679a6851179cd34bb/src/Http/Http.Abstractions/src/HttpContext.cs#L12-L78)
- [`IHttpContextAccessor.cs#L6-L18`](https://github.com/dotnet/aspnetcore/blob/de3b5fb31f067e447a7a035679a6851179cd34bb/src/Http/Http.Abstractions/src/IHttpContextAccessor.cs#L6-L18)
- [`DefaultHttpContext.cs#L59-L99`](https://github.com/dotnet/aspnetcore/blob/de3b5fb31f067e447a7a035679a6851179cd34bb/src/Http/Http/src/DefaultHttpContext.cs#L59-L99)

请求 body 的低层能力另由 `IRequestBodyPipeFeature.Reader` 暴露。默认实现按需把 `Request.Body` 包成 `PipeReader`，在 response completed 时调用 `Complete()`；这说明 buffer/stream 的 lifetime 是 feature 的显式责任，不应隐含在业务 context 字典中。

来源：

- [`IRequestBodyPipeFeature.cs#L8-L16`](https://github.com/dotnet/aspnetcore/blob/de3b5fb31f067e447a7a035679a6851179cd34bb/src/Http/Http.Features/src/IRequestBodyPipeFeature.cs#L8-L16)
- [`RequestBodyPipeFeature.cs#L11-L49`](https://github.com/dotnet/aspnetcore/blob/de3b5fb31f067e447a7a035679a6851179cd34bb/src/Http/Http/src/Features/RequestBodyPipeFeature.cs#L11-L49)

**对 NetWork 的借鉴：**把领域/request context 与 `PipeReader`/writer capability 分离；把 cancellation、trace、预算和生命周期作为显式 operation metadata。**不应照搬：**`HttpContext` 的 ambient service/Items/Session 模型不适合 packet codec，也不应让 codec 通过 context 反查外部服务。ASP.NET Core 解决的是异步 HTTP 请求生命周期，不是 segment rollback；它不能替代 `SegmentBounds` 或 packet framing。

## 4. 对 20-70 段的精确定义

“20-70 可变内存范围”不应表示存在一个长度不确定的 `Span<byte>`。它应表示一个 schema contract：

```text
max writable capacity = 70 bytes
successful committed length is in [20, 70]
assembler appends only [0, written)
```

固定段不是“不可变的内存”；如果注册 codec 要向其中写入，它在 callback 期间必然是可变的。应该把“不可变”放在静态描述上：

```text
SegmentSpec       immutable schema description
WriteLease        synchronous, mutable, non-escaping view
CommittedBytes    immutable output range after validation
```

固定和可变不需要两套 codec interface：

```csharp
public readonly record struct SegmentBounds
{
    public SegmentBounds(int minBytes, int maxBytes)
    {
        if (minBytes < 0) throw new ArgumentOutOfRangeException(nameof(minBytes));
        if (maxBytes < minBytes) throw new ArgumentOutOfRangeException(nameof(maxBytes));

        MinBytes = minBytes;
        MaxBytes = maxBytes;
    }

    public int MinBytes { get; }
    public int MaxBytes { get; }
    public bool IsFixed => MinBytes == MaxBytes;

    public static SegmentBounds Fixed(int bytes) => new(bytes, bytes);
    public static SegmentBounds Bounded(int minBytes, int maxBytes) => new(minBytes, maxBytes);

    public bool Accepts(int written) =>
        written >= MinBytes && written <= MaxBytes;
}
```

示例：

```csharp
var fixed20 = SegmentBounds.Fixed(20);
var variable20to70 = SegmentBounds.Bounded(20, 70);
```

两者共用同一个 `Accepts`，但 schema 仍能通过 `IsFixed` 给出更准确的错误信息：固定段报告“期望 20，实际 19”，范围段报告“期望 [20,70]，实际 19”。

## 5. 推荐的 codec 与注册原型

以下是首版候选接口。它只描述 wire 层能力，不是 Projector 的输入接口：

```csharp
public enum SegmentBoundary
{
    Exact,
    ParentDelimited,
    LengthPrefixed,
    SelfDelimited,
}

public readonly record struct SegmentSpec(
    SegmentId Id,
    SegmentBounds Bounds,
    SegmentBoundary Boundary);

public readonly record struct SegmentId(string Value);

public interface IWireSegmentCodec<TValue>
{
    SegmentSpec Spec { get; }

    // destination.Length 由 assembler 限制为 Spec.Bounds.MaxBytes。
    // codec 只能在本次同步调用期间使用 destination。
    bool TryWrite(
        in TValue value,
        Span<byte> destination,
        out int written,
        out WireSegmentError error);

    // available 是当前父级/帧剩余输入；consumed 必须落在 Spec.Bounds 内。
    // 对 Exact/ParentDelimited，assembler 还要求 consumed 等于已切出的范围。
    bool TryRead(
        ReadOnlySpan<byte> available,
        out TValue value,
        out int consumed,
        out WireSegmentError error);
}
```

注册应绑定 schema slot，而不是把字符串 lookup 和业务 context 放进 callback：

```csharp
public readonly record struct SegmentHandle<TValue>(
    SegmentId Id,
    IWireSegmentCodec<TValue> Codec);

SegmentHandle<TilePayload> payload = schema.Register(
    new TilePayloadCodec(
        new SegmentSpec(
            new SegmentId("tile-payload"),
            SegmentBounds.Bounded(20, 70),
            SegmentBoundary.ParentDelimited)));
```

实际项目中 registration 应发生在 schema build 阶段并冻结。`SegmentHandle<TValue>` 是编译期/构造期绑定；运行期不应对 `Dictionary<string, object>` 做任意查找，也不应允许 codec 从注册表取得 `Main` 或 `Session`。

## 6. 拼接器和 lease 的语义

拼接器拥有最终输出 writer 和本段原子性。codec 不直接取得最终 `IBufferWriter<byte>`：

```csharp
public static bool TryAppend<TValue>(
    SegmentHandle<TValue> handle,
    in TValue value,
    IBufferWriter<byte> output,
    out int committed,
    out WireSegmentError error)
{
    var bounds = handle.Codec.Spec.Bounds;
    Span<byte> staging = RentStaging(bounds.MaxBytes);

    if (!handle.Codec.TryWrite(value, staging, out var written, out error))
    {
        committed = 0;
        return false;
    }

    if (!bounds.Accepts(written))
    {
        committed = 0;
        error = WireSegmentError.InvalidLength(handle.Codec.Spec.Id, bounds, written);
        return false;
    }

    staging[..written].CopyTo(output.GetSpan(written));
    output.Advance(written);
    committed = written;
    error = default;
    return true;
}
```

上面的 `RentStaging` 只是表达语义，不是要求马上公开的 allocator API。实现可以使用栈上小 buffer、`ArrayPool<byte>` 或内部 `ArrayBufferWriter<byte>`，但必须把传给 codec 的 view 限制为 `MaxBytes`，并在成功前不调用最终 writer 的 `Advance`。

失败时 staging 被丢弃，最终 writer 不需要支持任意位置 rollback。这是首版选择 staging 的核心原因：通用 `IBufferWriter<byte>` 只有 `GetSpan/GetMemory/Advance`，没有可以依赖的事务或 rollback contract。

## 7. 读取边界不能由 20-70 推断

写侧可以申请最大容量，读侧却不能只看到 `[20,70]` 就知道下一段从哪里开始。注册时 `SegmentBoundary` 至少要落入以下一种真实机制：

| Boundary | 读取方式 | 适用情况 |
| --- | --- | --- |
| `Exact` | 外层切出固定长度 | 固定段，例如 `[20,20]` |
| `ParentDelimited` | 父 scope/record 已提供精确 range | Packet 20 的 record、父级表项 |
| `LengthPrefixed` | 先读长度，再校验并切出 payload | 可变 blob、嵌套消息 |
| `SelfDelimited` | codec 根据 sentinel/内部结构返回 `consumed` | 终止数组、具备明确结束符的格式 |

只声明 `Bounded(20,70)` 而没有 boundary 的 schema 应在注册或验证阶段拒绝。`TryRead` 返回 `consumed` 是必要的，但它不能替代 framing；它只能报告 codec 在已有输入范围内消费了多少。

## 8. 与当前 NetWork 设计的接合

当前 `docs/plans/2026-09-09-external-context-wire-submission-design.md` 的边界可以直接保留：

```text
ContextAdapter
    -> Packet-specific PreparationInput
    -> Projector
    -> PreparedPacket13 / PreparedPacket20
    -> SubmissionValidator
    -> EncodePlan
    -> optional IWireSegmentCodec
    -> SegmentComposer
    -> MessageFrame / transport
```

具体分工：

| 位置 | 可以做什么 | 不可以做什么 |
| --- | --- | --- |
| `ContextAdapter` | 读取 `Main`/`World`/目录，形成只读准备输入 | 写 wire buffer、发送、修改世界 |
| `Projector` | 计算字段存在性、局部 mask、记录顺序和 typed values | 调用 segment writer、延迟回读领域状态 |
| `WireSubmission` | 保存不可变 wire 事实和拥有的数据 | 保存 `Main`、委托、writer、cursor |
| `EncodePlan` | 固定字段顺序、消费 mask、调用已注册 codec | 重新判断领域条件 |
| `IWireSegmentCodec` | 在本段同步 staging 中读写 bytes，返回 `written/consumed` | 访问 `Session`、最终 writer 或全局注册服务 |
| `SegmentComposer` | 校验 bounds、framing、预算并追加最终 bytes | 自动截断、补零、重新投影 |

现有 `IPacketCustomCodec<TPacket>` 和 `PacketDefinitionRegistry` 仍是 `byte[]` 兼容接缝。首版不应把 segment registration 直接扩张成新的全局动态 registry；可以先在 Packet 13/20 的具体 EncodePlan 或 Packet 10 的 legacy adapter 内部使用 composer，最后再由兼容层生成 `byte[]`。这能避免把尚未验证的 segment 抽象扩散到全部 packet。

推荐迁移顺序：

1. Packet 13/20 继续以 packet-specific typed submission 证明上下文外移和局部 mask；普通固定字段不要为了统一而全部包装成 segment callback。
2. 只有压缩块、RLE、尾表或 custom payload 这类天然具有独立 framing/size contract 的部分，才接入 `IWireSegmentCodec`。
3. Packet 10 保留 `LegacyPacket10CodecAdapter`，等真实 RLE/Deflate/尾表证据确认后再决定是否拆成多个 segment。

## 9. 方案取舍与删除清单

### 9.1 方案比较

| 方案 | 优点 | 主要风险 | 结论 |
| --- | --- | --- | --- |
| 上下文 + 最终 `IBufferWriter` 直接暴露 | 迁移初期调用方便 | 领域/wire 混层、失败污染、生命周期不可见、难测试 | 不采用 |
| typed submission + bounded staging lease | 失败隔离、边界清楚、固定/可变共用 contract | 成功时有一次 copy | 首选 |
| owned segment chain / zero-copy | 可能减少大 payload copy | owner、refcount、异步、释放、transport scatter/gather 全要定义 | 后续性能分支 |

### 9.2 应删除或暂不引入的设计

1. 万能 `IContext`：同时放领域对象、取消、目录、writer、registry 和任意服务。
2. `Dictionary<string, object>` 或 `Get("field")` 式动态上下文。
3. 上下文长期持有 `Span<byte>`、`Memory<byte>`、最终 writer 或 staging lease。
4. 一个对象同时承担读 view、写 lease、最终 output owner。
5. 默认的 `Reserve/Backpatch/Transaction` token 栈；只有真实协议需要前置长度回填时才单独设计。
6. 首版的 `PipeWriter`、跨 async `Memory<byte>` 和零拷贝 component chain。
7. 超出范围时截断、补零、删除字段或静默改变 schema。
8. 仅凭 `[20,70]` 推断入站段边界。
9. 把 `SegmentBounds`、`WriteLease` 和 `WireSubmission` 合成一个“大而全”的 context 对象。

## 10. 验收不变量

第一版实现或原型至少应能证明：

- `Projector`/`EncodePlan`/segment codec 无法取得 `Main`、`World`、`Session` 或路由对象；
- `Fixed(n)` 只能提交 `written == n`；
- `Bounded(20,70)` 只能提交 `20 <= written <= 70`；
- codec 失败或返回越界长度时，最终 writer 没有本段残留；
- 读端拥有明确的 `Exact`、父级、长度前缀或自描述 boundary；
- lease 只在同步 callback 内有效，codec 不能保存 span/memory；
- 相同 `WireSubmission` 可以重复编码而不重新读取外部上下文；
- registration 在 schema 冻结时检查 bounds、boundary 和 codec 类型；
- 只有 profile/transport 的真实性能证据证明 copy 是瓶颈后，才进入 owned segment chain 设计。

## 11. 最终建议

对用户提出的两种方向，建议这样落位：

1. **上下文层不提供 20-70 raw memory API。**它提供 packet-specific preparation input 和只读 operation metadata，输出不可变 `WireSubmission`。
2. **非上下文层提供受限 segment codec API。**注册函数可以在一次同步调用期间读写当前 segment 的 `Span<byte>`，但只能使用长度为 `MaxBytes` 的 staging view，并必须返回 `written`。
3. **固定段和范围段共用 `SegmentBounds`。**固定段用 `Fixed(n)`，范围段用 `Bounded(min,max)`；“固定”描述 schema 约束，不描述 buffer 不可写。
4. **拼接器拥有最终输出。**只有通过 bounds、framing 和预算检查后，才把 `[0, written)` 追加到最终 `IBufferWriter<byte>`。
5. **读取端单独设计边界。**`20-70` 是合法长度检查，不是分包规则；必须有父级范围、长度前缀、固定长度或自描述结束条件。
6. **首版不引入 `Memory<byte>`/`PipeWriter`/zero-copy owner。**同步 `Span<byte>` 足够表达当前需求；跨异步或零拷贝只有在明确性能和生命周期证据后再增加。

这套设计保留了注册 codec 的扩展能力，但把它放在正确的 seam：领域上下文只负责形成事实，wire codec 只负责编码事实，拼接器只负责原子提交和顺序。这比把 raw segment API 放进上层上下文更深、更容易验证，也符合当前 Packet 13/20 先收缩、Packet 10 延后拆分的策略。
