# 固定/可变内存段 API 调研

日期：2026-09-09  
范围：C#/.NET 网络协议编码中，固定长度段、最小/最大长度段、注册 codec 和最终拼接。  
研究方式：读取 Microsoft Learn 官方 API/指南页面，并对照本仓库现有 `PacketCodec`、生成 codec 和 WireSubmission 设计。  
状态：资料调研，不代表已经批准实现。

## 1. 结论摘要

1. `IBufferWriter<byte>` 适合表达“编码器向最终输出顺序追加数据”，不直接表达“这个逻辑段允许 20 到 70 字节”。`GetSpan(sizeHint)` / `GetMemory(sizeHint)` 只承诺至少有请求大小的可写区域，调用方必须通过 `Advance(count)` 声明实际写入量。
2. 固定段和有界可变段是协议 schema 的额外约束，应由本项目自己的 `SegmentBounds` / `SegmentCodec` 表达，而不是寄希望于 `IBufferWriter` 推断。
3. `Span<byte>` 适合在同步 callback 的短租期内暴露固定或临时写入区域；`Memory<byte>` 可以跨异步边界，但同时需要明确 owner、consumer 和 lease。首版 segment codec 应限制为同步调用，避免把内存所有权扩大到注册函数。
4. `IBufferWriter` 没有通用 rollback 契约。若注册函数写入了一部分后失败，已经 `Advance` 的最终 writer 通常不能被抽象地回滚。因此需要先写入每段自己的 bounded staging buffer，验证长度后再提交，或把 codec 设计成“测量/尝试写入/成功后提交”的明确协议。
5. `PipeWriter` 适合异步流和背压，不是当前 packet 内部 segment 拼接的必要抽象。`ReadOnlySequence<byte>` / `SequenceReader<byte>` 适合读取可能跨多个底层块的输入，不应被当作固定/可变段的写入 API。
6. 这支持一个边界判断：原始可写内存段应属于非上下文层的注册 codec/拼接器；上下文层仍然产生 typed submission。若确实需要 segment callback，也只能接受同步、有界、不可逃逸的 lease，而不是把最终输出 buffer 直接暴露给领域 Projector。

## 2. 官方事实

### 2.1 `IBufferWriter<T>`

官方 API 文档列出三个核心成员：

- `GetSpan(int sizeHint)`：返回至少满足 `sizeHint` 的 `Span<T>`；
- `GetMemory(int sizeHint)`：返回至少满足 `sizeHint` 的 `Memory<T>`；
- `Advance(int count)`：通知 writer 已经向取得的 span/memory 写入了 `count` 个元素。

官方页面没有声明 `minBytes`、`maxBytes`、事务、checkpoint 或 rollback。`sizeHint` 是一次取得写入区域时的最小请求，不是协议字段的最大长度，也不是实际提交长度。

来源：

- [IBufferWriter<T> API](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.ibufferwriter-1?view=net-10.0)

### 2.2 `ArrayBufferWriter<T>`

官方文档将 `ArrayBufferWriter<T>` 描述为顺序写入的 buffer writer，并提供：

- `WrittenMemory` / `WrittenSpan`：到目前为止已经写入的数据；
- `GetMemory` / `GetSpan`：取得至少达到请求大小的写入区域；
- `Advance`：提交已经写入的数量；
- `Clear` / `ResetWrittenCount`：重置已写计数，但不是一个通用的任意位置事务 rollback API。

它适合作为测试和简单最终拼接器，但不能据此假定所有 `IBufferWriter` 都支持回退到任意 segment 起点。

来源：

- [ArrayBufferWriter<T> API](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraybufferwriter-1?view=net-10.0)

### 2.3 `Span<T>`、`Memory<T>` 和生命周期

Microsoft 的 memory 使用指南区分 owner、consumer 和 lease：

- 一个 buffer 具有 owner，owner 负责生命周期；
- consumer 在租期内读写 buffer；没有外部同步时，一次通常只能有一个 active consumer；
- 同步 `void` 方法接收 `Memory<T>` 时，使用租期应在方法返回时结束；
- 如果需要跨异步边界，应使用能表达堆上存活和所有权的 `Memory<T>` / `IMemoryOwner<T>` 模型；
- `Span<T>` / `ReadOnlySpan<T>` 只能存放在栈上，不适合跨 `await` 保存。

这些规则直接影响“暴露 20-70 byte 内存段给注册函数”的设计：注册函数不能保存 span/memory，也不能在 callback 返回后继续使用 segment，除非 API 明确转移了 owner 和 lease。

来源：

- [Memory<T> usage guidelines](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines)
- [Span<T> API](https://learn.microsoft.com/en-us/dotnet/api/system.span-1?view=net-10.0)
- [Memory<T> API](https://learn.microsoft.com/en-us/dotnet/api/system.memory-1?view=net-10.0)

### 2.4 `PipeWriter`

Microsoft 的 pipelines 示例使用以下顺序：

```text
writer.GetMemory(minimumBufferSize)
    -> 写入实际收到的数据
    -> writer.Advance(bytesRead)
    -> writer.FlushAsync()
```

`PipeWriter` 是 `IBufferWriter<T>` 的派生类型，额外提供 flush、completion、取消和背压语义。它解决的是异步生产者/消费者之间的流动，不是单个 packet 内部字段的长度约束或回滚。

来源：

- [System.IO.Pipelines overview](https://learn.microsoft.com/en-us/dotnet/standard/io/pipelines)
- [PipeWriter API](https://learn.microsoft.com/en-us/dotnet/api/system.io.pipelines.pipewriter?view=net-10.0)

### 2.5 `ReadOnlySequence<T>` 和 `SequenceReader<T>`

官方 API 将 `ReadOnlySequence<T>` 用于表示一个可能由多个 segment 组成的只读序列；`SequenceReader<T>` 是面向该序列的高效读取器，提供当前 span、未读序列和推进读取位置等操作。

它们适合：

- 解码一个可能跨底层 buffer 边界的输入；
- 不复制地消费 pipeline 或多段输入；
- 将“剩余输入”与“已消费位置”分开。

它们不提供：

- 可变 segment 的写入提交；
- `minBytes/maxBytes` 协议约束；
- 事务回滚；
- 对注册 callback 的 ownership 管理。

来源：

- [ReadOnlySequence<T> API](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.readonlysequence-1?view=net-10.0)
- [SequenceReader<T> API](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.sequencereader-1?view=net-10.0)

## 3. 对本仓库的直接证据

当前 `Core/Protocol/PacketFramework.cs` 的默认 `PacketCodec.Write` 返回 `byte[]`，通过 `MemoryStream` / `BinaryWriter` 写入；复杂包通过 `IPacketCustomCodec<TPacket>` 自己实现 `Write`、`Read` 和验证。`PacketDefinitionRegistry` 也仍然把 writer 暴露为 `Func<object, byte[]>`。

另一方面，现有生成/验证代码已经使用 `IBufferWriter<byte>`、`Span<byte>` 和 `ReadOnlySpan<byte>`，例如：

- `Tools/NetWork.Concept.GraphExporter/GraphCodecEmitter.cs` 生成 `Serialize(..., IBufferWriter<byte>)` 和 span decode；
- `Verification/Packet88PerformanceStandalone/Program.cs` 使用 `ArrayBufferWriter<byte>` 和 tracking writer 验证 `Advance` 后的写入结果。

因此新 API 可以与现有 .NET buffer writer 对齐，但不应假设当前 registry 已经提供 segment registration 或可回滚 writer。

来源：

- `Core/Protocol/PacketFramework.cs`
- `Core/Protocol/PacketDefinitionRegistry.cs`
- `Tools/NetWork.Concept.GraphExporter/GraphCodecEmitter.cs`
- `Verification/Packet88PerformanceStandalone/Program.cs`

## 4. 设计推论

### 4.1 “20-70 byte”应是 segment contract

应表达为：

```text
SegmentBounds(minBytes = 20, maxBytes = 70)
```

它表示：一次成功编码必须提交 `[20, 70]` 内的实际字节数。它不表示：

- `GetSpan(20)` 会只给 20 到 70 字节；
- callback 可以永久持有这段内存；
- 编码器拥有任意 rollback；
- 20 到 70 的空洞区域会自动被拼接器识别。

### 4.2 固定段和可变段应分离

固定段的成功条件是 `written == exactBytes`。可变段的成功条件是 `minBytes <= written <= maxBytes`。二者可以共享一个 bounded writer，但 schema 应保留 `Fixed` 和 `Bounded` 两种声明，便于静态检查和错误定位。

### 4.3 “拼接”应是追加提交，不是随意合并数组

拼接器应拥有最终输出顺序和所有权。segment callback 只在租期内写入本段 staging area；callback 返回并通过验证后，拼接器才将本段的 `[0, written)` 追加到最终 writer。这样能避免 callback 写到 70 但只提交 20 时污染下一个段，也避免失败后半段字节已经进入最终输出。

### 4.4 读和写是两个不同 contract

写入 API 需要 `Writable bounded lease + Commit(written)`；读取 API 需要 `ReadOnlySpan/ReadOnlySequence + consumed/validated length`。不能把同一个可写 `Memory<byte>` 同时当作读 API 和写 API 的通用上下文对象。

### 4.5 注册函数应是 codec，不是上下文函数

注册函数如果接收 raw memory 并决定 wire bytes，它已经是 codec。它应注册到非上下文层的 `SegmentCodecRegistry`，上下文层只传入强类型 snapshot/submission。这样不会破坏现有设计中“Projector 不直接生成 bytes”的边界。

## 5. 待讨论的 API 骨架

以下仅作为下一轮设计讨论的候选原型，不是已经批准的公共 API：

```csharp
public readonly record struct SegmentBounds(int MinBytes, int MaxBytes)
{
    public bool IsFixed => MinBytes == MaxBytes;
}

public interface IWireSegmentCodec<TValue>
{
    SegmentBounds Bounds { get; }

    // 同步、不可逃逸的写入租期。
    void Write(in TValue value, Span<byte> destination, out int written);

    // 输入已经由拼接器按 segment 边界切出。
    TValue Read(ReadOnlySpan<byte> source);
}
```

这个原型仍需要在下一轮决定：segment 是否先写 staging buffer、如何做变长输入的边界切分、是否允许 codec 返回结构化错误，以及 registration 如何绑定 schema slot。它的价值在于明确了最小原则：bounds 是 schema 约束，span 是同步租期，written 是显式提交结果。

