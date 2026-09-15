# Packet 13 验证笔记

## 目的

这份文档只记录 `13` 号包验证样例的范围和结论，不再承担总设计文档角色。

`13` 号包被选为第一批样例，是因为它同时覆盖：

- 前置 `BitsByte`
- 条件字段
- 多字段共享同一个控制位
- 固定字段和可选字段混排

## 当前样例边界

当前 demo 分成两层。

### 生产骨架

位于 [`Prototypes/external-context-wire-submission/src/Protocol`](../../src/Protocol)：

- [`BitsByte.cs`](../../src/Protocol/BitsByte.cs)
- [`PacketFramework.cs`](../../src/Protocol/PacketFramework.cs)
- [`PlayerControlsPacket13Definition.cs`](../../src/Protocol/Packets/PlayerControlsPacket13Definition.cs)

### 验证辅助

位于当前原型的 `tests/ProtocolDemoStandalone`：

- [`PlayerControlsPacket13TestSupport.cs`](./PlayerControlsPacket13TestSupport.cs)
- [`Program.cs`](./Program.cs)

## 已验证的点

- `PacketDefinition + PacketCodec` 可以表达旧 `13` 号包的字节布局。
- 条件字段读取只依赖前序 flag byte，不依赖业务上下文。
- `PotionOfReturn` 这类"多个字段共享同一控制位"的情况可以通过 `FieldGroup` 表达。
- 字段依赖已经改为字段句柄 / 标志句柄，不再靠裸字符串。
- schema 已经改成 `Build()` 一次性冻结产物。

## 还没有解决的高层问题

这份样例没有覆盖这些内容：

- 会话阶段门禁
- 连接状态机
- `4` 号包那类"读完立即进入上下文解释"的处理链
- 广播 / 回应 / 投递策略
- 新旧协议全量迁移顺序

## 结论

`13` 号包样例足以证明：

- 包体层协议骨架方向可行
- 可以进入高层设计阶段

但它还不能代表完整网络框架设计已经完成。
