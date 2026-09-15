# GitHub 类似项目中的内存段、长度提交与拼接模式

日期：2026-09-09  
范围：固定长度段、最小/最大长度段、注册 codec、长度提交、buffer 拼接。  
状态：源码调研与设计输入，不代表已经批准实现。

## 1. 先给结论

这批项目没有把“20 到 70 字节”直接建模成通用 `IBufferWriter<byte>` 能力。实际做法主要分成四类：

1. **索引式 buffer**：DotNetty 维护 `ReaderIndex` / `WriterIndex`，可以 mark/reset writer index，并提供 slice 和 composite buffer。
2. **长度分隔追加**：MessagePack 和 protobuf 先写长度头，或者先测量 payload，再向 `IBufferWriter<byte>` 顺序追加。
3. **反向构建和 offset**：FlatBuffers 从 buffer 尾部反向构建，最后通过 offset 和 `Finish` 冻结完整消息。
4. **分段追加 writer**：.NET `IBufferWriter`、`ArrayBufferWriter`、PipeWriter 和 MemoryPack 都以 `GetSpan/GetMemory -> 写入 -> Advance` 为核心；MemoryPack 还在内部维护多个 buffer segment，最后顺序写入目标 writer。

对 NetWork 最有价值、也最少引入概念的组合是：

```text
SegmentBounds.Fixed(n)       -> 成功提交 written == n
SegmentBounds.Bounded(20,70) -> 成功提交 20 <= written <= 70
codec 写入本段独立 staging buffer
校验 written 后，拼接器追加 [0, written) 到最终输出
```

因此建议保留以下边界：

- `20-70` 是本段一次成功编码的长度约束，不是 `GetSpan(20)` 的语义。
- 注册函数不直接拿最终 `IBufferWriter<byte>`；通用 writer 没有可依赖的任意位置 rollback。
- codec 可以拿到长度恰好为 `MaxBytes` 的同步 `Span<byte>`，返回实际 `written`；拼接器负责验证和提交。
- 读取端必须有长度前缀、父级边界或自描述格式之一，不能仅凭 `[20,70]` 猜出本段结束位置。
- 零拷贝 component 所需的引用计数和所有权转移应与首版 bounded codec 分开，不要在一个 API 中同时解决。

## 2. 证据边界

以下源码均从 GitHub 默认分支读取，并锁定到读取时的 commit。链接中的行号用于定位源码事实；设计判断是本文的推论，不应当当作这些项目的官方建议。

### 检索方法

先用 GitHub repository search 做候选发现：

- `dotnet IBufferWriter serialization`
- `.NET byte buffer networking`

广泛搜索会返回大量与本问题无关的应用仓库，因此不使用搜索排序、star 数或项目名称作为架构证据。最终只保留能直接观察到 buffer writer、长度提交、子消息边界、索引 checkpoint 或多段拼接实现的项目，并把源码链接固定到具体 commit。项目的成熟度和相似性是筛选条件，不是其 API 对 NetWork 的授权或规范。

| 项目 | 锁定提交 | 主要用途 |
| --- | --- | --- |
| [Azure/DotNetty](https://github.com/Azure/DotNetty/tree/379d8cc1d32d2b557347aad1f7b6c48656212a4b) | `379d8cc1d32d2b557347aad1f7b6c48656212a4b` | 可读/可写索引、slice、composite buffer |
| [MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp/tree/b0e5cee3e30e75004ea95e154ec3832027a46090) | `b0e5cee3e30e75004ea95e154ec3832027a46090` | 长度头、`IBufferWriter` 追加 |
| [protobuf-net](https://github.com/protobuf-net/protobuf-net/tree/7eab91f1d326a388bd7c4b2998f6697709d9c77c) | `7eab91f1d326a388bd7c4b2998f6697709d9c77c` | length-delimited 子消息、测量后写入 |
| [google/flatbuffers](https://github.com/google/flatbuffers/tree/5761d6e67af841d15ee21bc1ce9a78ffa9cf939e) | `5761d6e67af841d15ee21bc1ce9a78ffa9cf939e` | 反向构建、offset、最终冻结 |
| [dotnet/runtime](https://github.com/dotnet/runtime/tree/0d5b6c0b6ecc39cd272912814c71e898929cbec6) | `0d5b6c0b6ecc39cd272912814c71e898929cbec6` | 官方 `IBufferWriter`、`ArrayBufferWriter`、`PipeWriter` 基线 |
| [Cysharp/MemoryPack](https://github.com/Cysharp/MemoryPack/tree/85ab9ad76c380aca48c09ff3a0ad955ee5a2902b) | `85ab9ad76c380aca48c09ff3a0ad955ee5a2902b` | 高性能 span writer、内部多段 buffer |

## 3. DotNetty：索引 checkpoint 和 component 组合

### 源码事实

`IByteBuffer` 把 reader 和 writer 位置分开，并明确保持：

```text
ReaderIndex <= WriterIndex <= Capacity
```

接口提供 `MarkWriterIndex()` / `ResetWriterIndex()`，也提供 `EnsureWritable`、`Slice` 和 `RetainedSlice`。`EnsureWritable` 的约束是当前 buffer 的可写容量，不是协议字段的最小/最大长度。

来源：

- [`IByteBuffer.cs#L121-L185`](https://github.com/Azure/DotNetty/blob/379d8cc1d32d2b557347aad1f7b6c48656212a4b/src/DotNetty.Buffers/IByteBuffer.cs#L121-L185)
- [`IByteBuffer.cs#L1176-L1188`](https://github.com/Azure/DotNetty/blob/379d8cc1d32d2b557347aad1f7b6c48656212a4b/src/DotNetty.Buffers/IByteBuffer.cs#L1176-L1188)

`AbstractByteBuffer` 中 mark/reset 实际保存和恢复的是一个整数 writer index；`EnsureWritable` 在空间不足时按照 allocator 的规则扩容到 `MaxCapacity` 范围内；普通 `WriteBytes` 则先确保空间、写入、再增加 writer index。

来源：

- [`AbstractByteBuffer.cs#L100-L153`](https://github.com/Azure/DotNetty/blob/379d8cc1d32d2b557347aad1f7b6c48656212a4b/src/DotNetty.Buffers/AbstractByteBuffer.cs#L100-L153)
- [`AbstractByteBuffer.cs#L234-L264`](https://github.com/Azure/DotNetty/blob/379d8cc1d32d2b557347aad1f7b6c48656212a4b/src/DotNetty.Buffers/AbstractByteBuffer.cs#L234-L264)
- [`AbstractByteBuffer.cs#L1168-L1204`](https://github.com/Azure/DotNetty/blob/379d8cc1d32d2b557347aad1f7b6c48656212a4b/src/DotNetty.Buffers/AbstractByteBuffer.cs#L1168-L1204)

`CompositeByteBuffer.AddComponent` 默认不增加 composite 的 writer index；带 `increaseWriterIndex` 的重载才会增加。添加时使用 component 的 slice，超过最大 component 数量后，`ConsolidateIfNeeded` 会分配一个新 buffer 并把各 component 复制进去。

来源：

- [`CompositeByteBuffer.cs#L111-L211`](https://github.com/Azure/DotNetty/blob/379d8cc1d32d2b557347aad1f7b6c48656212a4b/src/DotNetty.Buffers/CompositeByteBuffer.cs#L111-L211)
- [`CompositeByteBuffer.cs#L318-L340`](https://github.com/Azure/DotNetty/blob/379d8cc1d32d2b557347aad1f7b6c48656212a4b/src/DotNetty.Buffers/CompositeByteBuffer.cs#L318-L340)

### 对 NetWork 的启发

- **适合借鉴**：本段 writer 可以有一个 checkpoint；失败时恢复本段起点，成功时把 writer index 增量作为 `written`。
- **不应直接照搬**：DotNetty 的 buffer 同时包含容量、读写索引、slice、引用计数和池化生命周期。把这些全部放入 NetWork 的 segment API 会扩大公共面。
- `CompositeByteBuffer` 说明“拼接”可以是 component 列表，也可以在必要时物理合并；但这依赖明确的 component ownership。首版 NetWork 可以先做顺序 copy，等零拷贝需求被验证后再独立增加 component 方案。

## 4. MessagePack-CSharp：显式长度头加顺序追加

### 源码事实

`MessagePackWriter` 接收 `IBufferWriter<byte>`。它的基础流程是从 writer 获取 span、写入实际字节数、调用 `Advance(written)`；`Flush` 将 writer 内部暂存的数据提交到底层 writer。它还暴露了 raw span/sequence 的直接复制，以及 `GetSpan` / `Advance` 的低层入口。

来源：

- [`MessagePackWriter.cs#L25-L105`](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/b0e5cee3e30e75004ea95e154ec3832027a46090/src/MessagePack/MessagePackWriter.cs#L25-L105)
- [`MessagePackWriter.cs#L693-L713`](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/b0e5cee3e30e75004ea95e154ec3832027a46090/src/MessagePack/MessagePackWriter.cs#L693-L713)

二进制值的写入先计算输入 span 的长度，先写 bin header，再复制 payload 并 `Advance(length)`。单独写 header 时，源码注释也明确要求调用者随后写入相应长度的 raw content。header 请求的 span 可能同时覆盖 header 和 payload，以减少一次分配，但这不是 rollback 机制。

来源：

- [`MessagePackWriter.cs#L450-L517`](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/b0e5cee3e30e75004ea95e154ec3832027a46090/src/MessagePack/MessagePackWriter.cs#L450-L517)
- [`MessagePackWriter.cs#L560-L592`](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/b0e5cee3e30e75004ea95e154ec3832027a46090/src/MessagePack/MessagePackWriter.cs#L560-L592)

读取 raw 数据时可以按指定长度切 `ReadOnlySequence<byte>`，然后推进 reader；读取完整 primitive 时则先记录起始位置，执行 skip，再切出整个 primitive。

来源：

- [`MessagePackReader.cs#L305-L336`](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/b0e5cee3e30e75004ea95e154ec3832027a46090/src/MessagePack/MessagePackReader.cs#L305-L336)
- [`MessagePackReader.cs#L1011-L1055`](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/b0e5cee3e30e75004ea95e154ec3832027a46090/src/MessagePack/MessagePackReader.cs#L1011-L1055)

### 对 NetWork 的启发

- `written` 必须是显式提交数量；span 的实际长度可能大于请求值。
- 读端要先取得长度，再切出 payload；`20-70` 只能作为校验范围，不能替代 framing。
- MessagePack 的公开 writer 没有通用 checkpoint/rollback。codec 一旦对最终 writer `Advance` 后失败，不能假设所有 writer 都可以撤销，因此 NetWork 仍应把本段写入 staging。

## 5. protobuf-net：测量后写 length-delimited 子消息

### 源码事实

protobuf-net 的 buffer-writer 实现从 `IBufferWriter<byte>` 获取 `Memory<byte>`，用内部 `State` 记录当前 buffer 中已经写入的数量；flush 时先取得 `ConsiderWritten()`，再对底层 writer 调用 `Advance(bytes)`。当当前 buffer 不够时，它先 flush，再申请新的 memory。

来源：

- [`ProtoWriter.BufferWriter.cs#L17-L85`](https://github.com/protobuf-net/protobuf-net/blob/7eab91f1d326a388bd7c4b2998f6697709d9c77c/src/protobuf-net.Core/ProtoWriter.BufferWriter.cs#L17-L85)
- [`ProtoWriter.BufferWriter.cs#L143-L215`](https://github.com/protobuf-net/protobuf-net/blob/7eab91f1d326a388bd7c4b2998f6697709d9c77c/src/protobuf-net.Core/ProtoWriter.BufferWriter.cs#L143-L215)

对于 length-delimited 的嵌套对象，源码使用 `_nullWriter` 先 `Measure` 得到 `calculatedLength`，写入 varint 或 fixed32 长度前缀，然后真正写 payload，最后比较实际写入长度和测量值；长度不一致会抛错。

来源：

- [`ProtoWriter.BufferWriter.cs#L248-L270`](https://github.com/protobuf-net/protobuf-net/blob/7eab91f1d326a388bd7c4b2998f6697709d9c77c/src/protobuf-net.Core/ProtoWriter.BufferWriter.cs#L248-L270)
- [`ProtoWriter.BufferWriter.cs#L370-L405`](https://github.com/protobuf-net/protobuf-net/blob/7eab91f1d326a388bd7c4b2998f6697709d9c77c/src/protobuf-net.Core/ProtoWriter.BufferWriter.cs#L370-L405)

旧式 `StartSubItem` / `EndSubItem` 仍存在于 `State` API，但 buffer-writer 实现对 length-prefixed 的 `ImplStartLengthPrefixedSubItem` / `ImplEndLengthPrefixedSubItem` 明确抛出“不支持，必须使用 WriteMessage API”。这表明“支持任意 seek/backpatch 的子项 API”和“只支持前向 `IBufferWriter` 的 API”需要分开处理。

来源：

- [`ProtoWriter.State.WriteMethods.cs#L812-L877`](https://github.com/protobuf-net/protobuf-net/blob/7eab91f1d326a388bd7c4b2998f6697709d9c77c/src/protobuf-net.Core/ProtoWriter.State.WriteMethods.cs#L812-L877)
- [`ProtoWriter.BufferWriter.cs#L408-L430`](https://github.com/protobuf-net/protobuf-net/blob/7eab91f1d326a388bd7c4b2998f6697709d9c77c/src/protobuf-net.Core/ProtoWriter.BufferWriter.cs#L408-L430)

### 对 NetWork 的启发

- 如果 wire format 需要在 payload 前写长度，最稳的方案是 `Measure -> Write -> Verify`，而不是假设最终 writer 支持回填。
- 如果 NetWork 的 `20-70` 只是本段允许范围、前面已有父级边界，那么不必引入完整的两遍序列化；本段 staging 后用 `written` 校验即可。
- 这也是收缩设计的边界：只有真实协议需要长度前缀时，才增加 measure 或 backpatch；不要因为“未来可能需要”预先引入通用 token 栈。

## 6. FlatBuffers：反向构建、offset 和最终冻结

### 源码事实

`FlatBufferBuilder` 维护 `_space` 和 `ByteBuffer`，数据从尾部向前写。`Offset` 由 buffer 长度减去剩余空间得到；`Prep` 负责对齐、检查额外空间并在需要时增长 buffer。vector 先记录元素数量、准备空间，元素按反向顺序写入，`EndVector` 最后写入元素数量并返回 offset。

来源：

- [`FlatBufferBuilder.cs#L30-L149`](https://github.com/google/flatbuffers/blob/5761d6e67af841d15ee21bc1ce9a78ffa9cf939e/net/FlatBuffers/FlatBufferBuilder.cs#L30-L149)
- [`FlatBufferBuilder.cs#L430-L462`](https://github.com/google/flatbuffers/blob/5761d6e67af841d15ee21bc1ce9a78ffa9cf939e/net/FlatBuffers/FlatBufferBuilder.cs#L430-L462)

table 的 `EndTable` 会写 vtable、比较并复用已有 vtable，随后通过 offset 关联数据。最终 `Finish` 写 root offset，可选写 size prefix，然后把 `ByteBuffer.Position` 移到最终数据起点；`DataBuffer` 的文档也说明通常要在 `Finish` 后读取。

来源：

- [`FlatBufferBuilder.cs#L804-L876`](https://github.com/google/flatbuffers/blob/5761d6e67af841d15ee21bc1ce9a78ffa9cf939e/net/FlatBuffers/FlatBufferBuilder.cs#L804-L876)
- [`FlatBufferBuilder.cs#L893-L970`](https://github.com/google/flatbuffers/blob/5761d6e67af841d15ee21bc1ce9a78ffa9cf939e/net/FlatBuffers/FlatBufferBuilder.cs#L893-L970)

### 对 NetWork 的启发

- FlatBuffers 证明“先构建组件、最后冻结完整消息”是可行的，但它解决的是 offset、对齐、随机访问和零拷贝读取，不是普通协议字段的简单拼接。
- 对 NetWork 当前的固定段/有界段 API，采用 FlatBuffers 的反向 builder 会引入 offset、对齐、nested 状态和最终 finish 等大量概念，属于过度设计。
- 仅当未来的协议布局明确需要前向引用、随机访问或大量零拷贝嵌套对象时，才应单独评估这种 builder。

## 7. .NET 官方 writer：只有追加提交契约

### 源码事实

`ArrayBufferWriter<T>` 通过 `_index` 记录已写数量，暴露 `WrittenMemory`、`WrittenSpan` 和 `WrittenCount`。`Advance(count)` 只增加 `_index`，并检查不能越过底层 buffer 末尾。`GetMemory/GetSpan(sizeHint)` 保证至少达到请求大小，但 successive calls 可能返回不同大小的 buffer，并且调用 `Advance` 后必须重新请求。

它提供的 `ResetWrittenCount()` 只把整个 writer 的 index 归零，不是“恢复到任意 segment 起点”的公共事务 API。

来源：

- [`ArrayBufferWriter.cs#L55-L135`](https://github.com/dotnet/runtime/blob/0d5b6c0b6ecc39cd272912814c71e898929cbec6/src/libraries/Common/src/System/Buffers/ArrayBufferWriter.cs#L55-L135)
- [`ArrayBufferWriter.cs#L137-L197`](https://github.com/dotnet/runtime/blob/0d5b6c0b6ecc39cd272912814c71e898929cbec6/src/libraries/Common/src/System/Buffers/ArrayBufferWriter.cs#L137-L197)

`PipeWriter` 的抽象契约同样是 `GetMemory/GetSpan -> Advance -> FlushAsync`，增加的是异步 flush、完成和背压；它没有 checkpoint 或 rollback 方法。

来源：

- [`PipeWriter.cs#L55-L81`](https://github.com/dotnet/runtime/blob/0d5b6c0b6ecc39cd272912814c71e898929cbec6/src/libraries/System.IO.Pipelines/src/System/IO/Pipelines/PipeWriter.cs#L55-L81)
- [`PipeWriter.cs#L109-L148`](https://github.com/dotnet/runtime/blob/0d5b6c0b6ecc39cd272912814c71e898929cbec6/src/libraries/System.IO.Pipelines/src/System/IO/Pipelines/PipeWriter.cs#L109-L148)

### 对 NetWork 的启发

- `IBufferWriter<byte>` 应继续作为最终输出的低层追加接口，而不是承担 segment schema。
- `sizeHint` 是本次请求的最小空间，不是 `[minBytes,maxBytes]`；必须由 NetWork 自己对 `written` 做 bounds 校验。
- 不能因为 `ArrayBufferWriter` 有 `ResetWrittenCount` 就把 rollback 当成 `IBufferWriter` 的通用能力。
- `PipeWriter` 只在 packet 输出已经进入异步管线并且确有背压需求时使用；不应为了 segment 拼接提前引入 `FlushAsync`。

## 8. MemoryPack：内部延迟 Advance 和多段收集

### 源码事实

`MemoryPackWriter<TBufferWriter>` 以 `IBufferWriter<byte>` 为目标，但在内部维护当前 buffer 的引用、剩余长度、`advancedCount` 和 `writtenCount`。写入时先在当前 span 内移动引用并累积 `advancedCount`；需要新 buffer 或调用 `Flush` 时，才把累计数量一次性传给底层 writer 的 `Advance`。

来源：

- [`MemoryPackWriter.cs#L17-L61`](https://github.com/Cysharp/MemoryPack/blob/85ab9ad76c380aca48c09ff3a0ad955ee5a2902b/src/MemoryPack.Core/MemoryPackWriter.cs#L17-L61)
- [`MemoryPackWriter.cs#L98-L165`](https://github.com/Cysharp/MemoryPack/blob/85ab9ad76c380aca48c09ff3a0ad955ee5a2902b/src/MemoryPack.Core/MemoryPackWriter.cs#L98-L165)

MemoryPack 的格式会直接写 collection length 或 UTF-8 实际字节数，随后一次性 `Advance` header 加 payload 的实际长度。它也提供不带 length header 的 span 写入，说明“是否有边界/长度头”是格式层选择，不应混入通用 writer。

来源：

- [`MemoryPackWriter.cs#L262-L315`](https://github.com/Cysharp/MemoryPack/blob/85ab9ad76c380aca48c09ff3a0ad955ee5a2902b/src/MemoryPack.Core/MemoryPackWriter.cs#L262-L315)
- [`MemoryPackWriter.cs#L350-L397`](https://github.com/Cysharp/MemoryPack/blob/85ab9ad76c380aca48c09ff3a0ad955ee5a2902b/src/MemoryPack.Core/MemoryPackWriter.cs#L350-L397)
- [`MemoryPackWriter.cs#L679-L702`](https://github.com/Cysharp/MemoryPack/blob/85ab9ad76c380aca48c09ff3a0ad955ee5a2902b/src/MemoryPack.Core/MemoryPackWriter.cs#L679-L702)

`ReusableLinkedArrayBufferWriter` 将已写满的 buffer segment 放入列表，最后可以转成一个数组，或者按各段顺序申请目标 writer 的 span、复制、`Advance`，然后 reset。它的 `GetMemory` 明确不支持，内部生成代码使用 `GetSpan`。

来源：

- [`ReusableLinkedArrayBufferWriter.cs#L38-L128`](https://github.com/Cysharp/MemoryPack/blob/85ab9ad76c380aca48c09ff3a0ad955ee5a2902b/src/MemoryPack.Core/Internal/ReusableLinkedArrayBufferWriter.cs#L38-L128)
- [`ReusableLinkedArrayBufferWriter.cs#L130-L207`](https://github.com/Cysharp/MemoryPack/blob/85ab9ad76c380aca48c09ff3a0ad955ee5a2902b/src/MemoryPack.Core/Internal/ReusableLinkedArrayBufferWriter.cs#L130-L207)

### 对 NetWork 的启发

- “本段先独立写，之后顺序提交到最终 writer”在成熟高性能 serializer 中有直接对应物。
- 延迟 `Advance` 可以减少底层 writer 的提交次数，但它不是失败后的任意 rollback；一旦把数据提交到底层，仍要遵守底层 writer 的生命周期。
- 首版不需要公开 linked writer。内部 staging 是否采用单个 `ArrayPool<byte>` buffer 或多个 segment，可以留作实现细节。

## 9. 方案比较

| 机制 | 长度如何确定 | 失败/回退 | 拼接/所有权 | 对 NetWork 的取舍 |
| --- | --- | --- | --- | --- |
| DotNetty index buffer | writer index 增量 | 同一 buffer 可 mark/reset | slice、引用计数、component | 可借鉴 checkpoint；不要引入完整生命周期模型 |
| MessagePack | 先知道 payload 长度，写长度头 | writer 本身不提供通用 rollback | 顺序追加到 `IBufferWriter` | 借鉴 `written` 和 framing；本段先 staging |
| protobuf-net | `Measure` 后写 prefix，再验证实际长度 | 前向 buffer-writer 不依赖 backpatch | `GetMemory` 分段、逐段 `Advance` | 需要长度头时借鉴两遍；否则不必测量 |
| FlatBuffers | builder 维护 offset 和反向位置 | 以最终 `Finish` 为边界，非普通 segment rollback | 反向 buffer、offset、对齐 | 当前明显过度设计 |
| ArrayBufferWriter/PipeWriter | caller 决定 `Advance(count)` | 接口没有 checkpoint | 顺序追加；PipeWriter 另有背压 | 作为最终 writer 基线 |
| MemoryPack | 格式头和实际 bytes 一起计算 | 内部延迟提交，不是任意 rollback | 多段 buffer 后顺序写入目标 | 最接近 staging + 拼接，但实现保持内部化 |

## 10. 收缩后的 NetWork API 方向

这只是基于调研的最小候选，不是要求马上实现的公共 API：

```csharp
public readonly record struct SegmentBounds(int MinBytes, int MaxBytes)
{
    public static SegmentBounds Fixed(int bytes) => new(bytes, bytes);
    public static SegmentBounds Bounded(int minBytes, int maxBytes) => new(minBytes, maxBytes);
}

public interface IWireSegmentCodec<TValue>
{
    SegmentBounds Bounds { get; }

    // destination 的长度由拼接器限制为 Bounds.MaxBytes；返回实际写入量。
    int Write(in TValue value, Span<byte> destination);

    // source 已由 framing/父级边界切出，codec 只负责校验和解码。
    TValue Read(ReadOnlySpan<byte> source);
}
```

建议的执行顺序：

1. 注册时验证 `MinBytes >= 0`、`MaxBytes >= MinBytes`，固定段用 `min == max` 表达。
2. 拼接器为当前段提供长度恰好为 `MaxBytes` 的 staging span；codec 不能访问最终输出 writer。
3. codec 返回 `written` 后，拼接器检查 `MinBytes <= written <= MaxBytes`，固定段额外检查 `written == MaxBytes`。
4. 校验成功后，拼接器只把 `[0, written)` 追加到最终 `IBufferWriter<byte>`。
5. 读取时由外层先取得本段实际长度；不满足 bounds、超过父级剩余数据或无法完成 framing 时直接拒绝。

这个 API 有意不包含：

- 通用 `Reserve/Backpatch/Transaction` token 栈；
- 暴露最终 `IBufferWriter<byte>` 给注册函数；
- 异步持有 `Span` 或跨 callback 保存 `Memory`；
- 自动从 `20-70` 推断输入边界；
- FlatBuffers 式 offset 图和全局 finish 状态。

如果未来某个协议字段确实需要前置长度，单独增加 `MeasuredSegmentCodec` 或测量阶段即可；如果未来确实需要零拷贝拼接，单独增加带 ownership 的 component batch。不要把两类需求提前合并到第一版 segment API。

## 11. 对当前问题的直接答复

上层上下文层不应默认变成“受注册函数直接写最终 wire memory”的层。更稳的分层是：

```text
上下文层：产生强类型 snapshot / WireSubmission
非上下文 codec 层：按 slot 选择 IWireSegmentCodec
拼接器：提供 bounded staging、校验 written、追加最终 wire
传输层：只接收已经完成的 wire output
```

固定内存段可以始终暴露固定数字，但这个数字应是 `Fixed(n)` 的 schema 约束；可变内存段暴露的是 `[min,max]` 合约，实际提交长度由 codec 返回。这样既支持注册函数读写本段，又避免把最终输出的可回退性、异步生命周期和协议 framing 混成一个接口。
