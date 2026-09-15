# Packet 20 Repeated Tile Graph Design

旧 TR 的 `NetMessage.SendData(20)` 和 `MessageBuffer.GetData(20)` 使用同一 tile-rectangle wire 形状：固定头 `(StartX, StartY, Width, Height, ChangeType)` 后接 `Width * Height` 个 tile record。每个 record 无条件写入三个 flag byte；颜色、tile 类型、frame、墙和液体字段由 flag bit 控制。

目标是把该重复结构保留在 Graph 可导出的元数据中，而不是把整个 tile 区块降格为不透明 `byte[]`。Graph 描述应具有：重复字段名、二维计数源、element CLR 类型、element wire 字段顺序和 element-local Flags 关系。Exporter 使用该描述生成固定循环；不读取旧 TR 源，也不扫描反射。

为了不改变当前生产注册表的 `AreaTileChangePacket20Definition` 行为，第一阶段只生成独立的 `AreaTileChangePacket20GeneratedCodec` 供字节一致性和吞吐基准调用。固化基线由旧 TR case 20 的字段顺序直接实现。测试使用 16×16 全字段 tile fixture（3,848 B payload，未含外层 length 前缀）以及稀疏 fixture；吞吐报告区分 encode/decode、Mpps 和 MB/s。
