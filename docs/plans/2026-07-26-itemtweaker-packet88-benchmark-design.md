# ItemTweaker 88 固化基线性能对比设计

## 目标

为 `ItemTweakerPacket88GraphConcept` 建立一个可复现的性能对照：使用从旧 TR
`NetMessage.SendData(88, ...)` 提取的固定字段顺序，配对一个无反射、无
`Main`/`Netplay` 等全局状态的 88 号包直接读写基线。

## 范围与非目标

- 基线只处理 88 号包，不实现通用消息分发、TCP 帧、`MessageBuffer` 生命周期或
  旧运行时状态。
- 基线输入和输出使用现有的 `ItemTweakerPacket` 与包含 `0x58` 消息号的字节数组；
  因而和 `ItemTweakerPacket88GraphConcept.Serialize/Deserialize` 的公开 API 等价。
- 不把图构造、反射桥接、真实旧程序集加载、`NetMessage.buffer` 或网络传输成本
  纳入每次操作计时。
- 指定备份中的 `MessageBuffer.cs` 目前没有 88 号分支（仅保留至 20 号）；读取侧将
  根据同一旧 wire 顺序固化，并以现有 88 号图测试和完整字段往返测试作为正确性约束。

## 两个实现的共同 wire 合约

每条消息为 `messageId (0x58) + itemId + flags1 + flags1 控制的字段 + flags2 +
flags2 控制的字段`。`flags2` 只在 `flags1.bit7` 为真时出现。字段顺序、原始整数
宽度、little-endian 编码和布尔编码必须与旧 `NetMessage.SendData(88)` 一致。

## 测量方案

分别对序列化和反序列化测量：

1. 建立两个固定输入：稀疏 flags（只含第一层和第二层代表字段）与全字段 flags。
2. 预热两种实现，避免把 JIT 首次编译混入采样。
3. 在同一进程中交替执行基线与图实现，按多个独立批次采样。
4. 每项记录总耗时、每操作纳秒、吞吐量和每操作分配字节；用校验和保留结果，避免
   JIT 删除循环。
5. 结果只解释该公开 API 的端到端分配与编解码开销，不将单次百分比差异视为稳定结论；
   报告批次最小值、中位数与最大值。

## 正确性与验收

- 固化基线和图实现对稀疏、全字段样本产生完全相同的字节。
- 两者对各自字节都能还原全部受 flags 启用的字段。
- 原有 `ProtocolDemoStandalone` 验证项目保持不变，仍可用其原命令独立运行全部验证；
  新项目的 `--verify` 只覆盖 Packet 88 固化基线与报告契约。
- 新项目的 `--benchmark` 打印四个维度（稀疏/全字段 × 序列化/反序列化）的对照结果。
