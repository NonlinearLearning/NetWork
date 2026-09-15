# 强类型 Span 与协议源码生成设计

## 目标

保持当前 Graph 定义作为协议事实、校验和可视化来源，同时为 Packet 13 与 Packet 88
生成无反射、无 `object` 转型的静态编解码器。通用布局执行器改为 `Span<byte>` /
`ReadOnlySpan<byte>` 与 `IBufferWriter<byte>`，避免每包创建 stream/reader/writer。

## 边界

- 首批只覆盖 `PlayerControlsPacket13` 和 `ItemTweakerPacket`；不推断或迁移其他包。
- `PacketDefinitionGraph` 保留非泛型元数据节点和 QuikGraph 拓扑；不把执行状态放进图。
- `PacketNode<TPacket>` 持有强类型写入、读取和长度逻辑。它不保留 `MemberInfo`，也不在
  每字段执行 `OwnerType` 比较或 `object` 转型。
- 现有 `Serialize(TPacket): byte[]`、`Deserialize(byte[])` 保持为兼容包装；新增 span/buffer
  API 承担热路径。
- 源码生成器只消费当前 `ConceptPacket*` 属性，不能迁移旧 `Terraria.NetworkLayer` 生成器：
  后者的目标模型、BinaryReader/Writer API 与当前协议不兼容，且旧 88 输出不完整。

## 运行时结构

```text
Concept specification
  -> Graph layout (validation / dependency graph)
  -> source generator
  -> static Packet13 / Packet88 codec
  -> Span or IBufferWriter execution
```

Graph 解释器和生成 codec 必须共享同一 wire 合约，但生成 codec 不在每包调用 Graph、
表达式、反射或 object 委托。生成代码通过 `BinaryPrimitives` 进行 little-endian 基础值
读写，并对 Packet 13 的 `Vector2` 与 Packet 88 的两级 flag 条件生成直接分支。

## API 形状

通用布局提供：

- `int GetEncodedLength(TPacket packet)`
- `void Serialize(TPacket packet, Span<byte> destination, out int written)`
- `void Serialize(TPacket packet, IBufferWriter<byte> destination)`
- `TPacket Deserialize(ReadOnlySpan<byte> source, out int consumed)`

旧数组 API 通过上述方法分配恰当大小的数组并调用 span 版本。调用方提供空间不足、消息号
不匹配、截断输入或未消费尾部数据时均抛出明确异常。

## 正确性与性能验收

- Packet 13/88 的 Graph、生成 codec 与既有 golden/legacy fixture 产生相同字节。
- 三者均能对 sparse/full flag 组合往返，且非法长度/消息号失败方式一致。
- 生成 codec 的测试证明编译输出被实际调用，而非只检查生成文件存在。
- 基准把建图和生成器执行排除在热路径外，报告 Graph span、生成 codec、固化基线的
  序列化/反序列化时间与分配；不得只凭单次百分比声明稳定收益。
