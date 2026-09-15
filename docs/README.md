# NetWork 设计文档

当前设计已按主题拆分到以下文档：

- [01-总体设计](./01-总体设计.md)
- [02-协议层设计](./02-协议层设计.md)
- [03-会话与管线设计](./03-会话与管线设计.md)
- [04-传输适配与验证](./04-传输适配与验证.md)
- [05-协议布局树提案（历史扩展草案）](./05-协议布局树提案.md)
- [06-有向依赖图初步提案（当前起点）](./06-有向依赖图初步提案.md)
- [原型隔离设计](./plans/2026-09-15-prototype-isolation-design.md)：定义十个语义原型的项目级边界，不保留共享 `Core/`。
- [原型隔离执行计划](./plans/2026-09-15-prototype-isolation-implementation.md)：按批次迁移、验证和清理。
- [外部上下文投影与 WireSubmission 设计](./plans/2026-09-09-external-context-wire-submission-design.md)：以 Packet 13/20 验证提交边界，Packet 10 先保留兼容接缝。
- [数据包上下文 API 与固定/可变内存段 GitHub 调研](./research/2026-09-09-packet-context-api-github-research.md)：记录 packet context 源码证据、20-70 段契约和收缩后的接口方向。
- [原型索引](../Prototypes/README.md)：十个独立语义原型的入口、命令和验证状态。
- [Packet 13 Segment Codec 原型](../Prototypes/packet13-segment-codec/README.md)：交互验证固定段、`20..70` 段、拼接失败隔离和 Packet 13 提交物。

建议阅读顺序：

1. 先看总体设计，了解分层和边界。
2. 再看协议层，理解 `PacketDefinition` / `PacketCodec` 的职责。
3. 然后看会话与管线，理解入站门禁和出站投递。
4. 最后看传输适配与验证，确认线上帧格式和验证入口。
5. 开始复杂包依赖建模前，看 06；05 仅保留为后续复杂度的历史参考。
6. 需要理解上下文外移、提交物和内部掩码接缝时，看外部上下文投影设计。
