# PRD: SlotGraph IR 协议框架

状态：提案，供后续实现拆分使用  
日期：2026-09-08  
范围：NetWork 协议层上层框架；不直接修改 Terraria 参考源码。

## 1. Introduction / Overview

为 NetWork 增加一个受限的 SlotGraph IR 协议框架，用于把 Terraria legacy 网络协议中混在 `NetMessage.cs` 和 `MessageBuffer.cs` 里的 wire layout、字段存在性、局部作用域、循环状态、压缩转换、上下文依赖和业务副作用拆开。

当前 `PacketFramework` 已经能够描述固定字段、条件字段、字段组和 custom codec，但它不能完整表达：

```text
Packet 20 的每 Tile 独立 scope；
Packet 10 的 Deflate、RLE、previousTile、pendingRun 和 side table；
Packet 23/27 的稀疏字段、嵌套 flags 和宽度选择；
Packet 8 的 wire body 与 join effect 分离；
Packet 56/69 的收发 profile 和消费边界差异。
```

本 PRD 定义一个编译器内部/上层运行时框架：

```text
受限 Legacy IDL 或 C# definition
    -> SlotGraph IR
    -> 静态校验
    -> EncodePlan / DecodePlan
    -> PacketFramework adapter
```

本 PRD 不要求一次重写全部 legacy packet，也不要求改变现有 framing、session gate 或 message registry。

## 2. Goals

- 将 wire layout、wire sequence、scope、presence、loop state、transform、side table 和 effect 分成可审计的结构。
- 首批用 Packet 13、Packet 20、Packet 10 验证 IR 的表达能力。
- 内部使用 `bool`、mask、scope mask 和 variant state 表达一次 packet 实例的字段存在性；这些内部状态不得自动写入 legacy wire。
- 保留线上 `BitsByte`/flag 字段，并显式表达其到内部 PresencePlan 的投影关系。
- 生成或解释稳定的 `EncodePlan` 和 `DecodePlan`，而不是在运行时遍历不可查询的通用图对象。
- 让 `DecodePlan` 产生结构化 decoded result 和 layout diagnostics，不直接修改 Terraria 世界状态。
- 通过兼容 adapter 接入现有 `PacketFramework`、`MessageFrame`、`SessionGate` 和 `PacketDefinitionRegistry`。
- 对循环上限、最大字节数、完整消费、scope 隔离和 custom codec 消费行为进行静态检查。
- 保留 legacy wire 的真实差异，包括 flag/payload 条件不一致和 mode-dependent profile。

## 3. Non-Goals

- 不修改 `D:\TRbackup\无任何删减通过编译\Terraria` 下的参考源码。
- 不在第一阶段迁移全部 160 个接收侧 message ID。
- 不将 Packet 8 的 join-world section 发送循环建模为 wire payload loop。
- 不将 Packet 34 的 action effect 分支建模为 wire union，除非未来发现其 wire shape 实际不同。
- 不把 `MessageFrame` framing、`SessionGate` 门禁或 transport buffer 重写为 SlotGraph 节点。
- 不把业务副作用、权限、spam 检查、实体 slot 分配或广播路由隐藏到 FieldSlot 内部。
- 不把 Kaitai Struct 或 Construct 直接作为当前 .NET 生产 codec；它们可以作为 read-side verifier 或外部实验工具。
- 不通过 PRD 直接实现代码、修改 csproj 或修复现有 Concept 目录状态。

## 4. Users and Stakeholders

### Protocol designer

需要声明 legacy packet 的字段、顺序、条件、profile、loop 和 scope，并能看到静态校验错误。

### Codec implementer

需要从已编译的 IR 获取小而稳定的 EncodePlan/DecodePlan 接口，不需要直接处理全局 Terraria 状态。

### Runtime integrator

需要把旧 DTO、session context 和 domain state 转换为 snapshot，并在 decode 后显式运行 ApplyEffect/ReplicationEffect。

### Protocol auditor or AI tooling

需要查询某字段存在的条件、所属 scope、最大字节数、依赖来源、Effect 边界和消息 profile，而不是阅读一整个大型 switch。

## 5. User Stories

### US-001: 定义不可变 PacketSchema 和 SlotGraph

**Description:** 作为协议设计者，我希望定义 packet、slot、scope 和关系，使 wire layout 不再依赖业务代码中的隐式全局状态。

**Acceptance Criteria:**

- [ ] 可定义 `PacketSchema`、`MessageProfile`、`FieldSlot`、`ScopeSlot` 和 `WireSequence`。
- [ ] 每个 slot 有稳定 ID、wire type、logical type、scope 和局部顺序。
- [ ] 静态定义冻结后不能被运行时 packet value 修改。
- [ ] graph 可以导出字段、关系域、scope 和线序信息。

### US-002: 表达内部 PresencePlan 和线上 flag projection

**Description:** 作为 codec 实现者，我希望分别处理内部字段存在性和 legacy flags，避免把内部 mask 写进旧协议或丢失线上 flag 语义。

**Acceptance Criteria:**

- [ ] 支持 packet-level `RootMask`、record-level `ScopeMasks`、group state 和 selected variant。
- [ ] `FlagProjection`、`PayloadPresence`、`ValueAvailability` 是三个可区分的 IR 概念。
- [ ] 内部 PresencePlan 不会作为隐式字段写入 packet payload。
- [ ] legacy exception 可以声明两个条件不等价，并生成诊断。

### US-003: 编译 DependencyDAG 和 WireSequence

**Description:** 作为协议审计者，我希望依赖关系和线上顺序分别可见，避免用拓扑排序错误地改变 legacy bytes。

**Acceptance Criteria:**

- [ ] 依赖子图出现环时，错误信息包含完整环路径。
- [ ] `Sequence` 保留同一 scope 内的真实字段顺序。
- [ ] 条件字段只能引用已声明的前置字段、selector 或 ContextBinding。
- [ ] 两个没有依赖关系的字段仍能按明确 wire order 排列。

### US-004: 支持局部 Scope 和 AreaLoop

**Description:** 作为 Packet 20 的 codec 作者，我希望每个重复 Tile 拥有自己的局部状态，避免多个 Tile 共用一个 flat mask。

**Acceptance Criteria:**

- [ ] `AreaLoop` 声明 width source、height source 和 iteration order。
- [ ] Packet 20 可以声明 `x outer / y inner`。
- [ ] 每个 `TileScope` 拥有独立的 flags、presence 和 layout path。
- [ ] 一个 Tile 的 optional color、wall、frame 或 liquid 状态不会污染相邻 Tile。
- [ ] AreaLoop 具有最大迭代数预算。

### US-005: 支持 GroupSlot 和 selector variant

**Description:** 作为 Packet 13/23/65 的 codec 作者，我希望表达字段组、稀疏字段和宽度选择，而不是用多个互不一致的 optional field 拼接。

**Acceptance Criteria:**

- [ ] Packet 13 的 PotionOfReturn 可以声明 all-or-none group。
- [ ] group 成员部分存在时，静态或运行时校验会拒绝布局。
- [ ] 支持 profile select、selector select 和 width select。
- [ ] Packet 23 的生命值可以表达 `sbyte`、`int16`、`int32` variant。
- [ ] Packet 65 的 target-kind 分支可以与 effect-level teleport 逻辑分开。

### US-006: 生成 Packet 13 的 EncodePlan 和 DecodePlan

**Description:** 作为第一批验证者，我希望 Packet 13 能从 IR 降低为直接的条件读写计划。

**Acceptance Criteria:**

- [ ] 固定字段和四个 wire flag 的顺序与 legacy 源码一致。
- [ ] `Flags2.bit2` 只控制 Velocity。
- [ ] `Flags2.bit7` 只控制 MountType。
- [ ] `Flags3.bit6` 同时控制两个 PotionOfReturn 字段。
- [ ] `Flags4.bit5` 控制 NetCameraTarget。
- [ ] 计划输出不包含 player identity override、mount apply 或 server rebroadcast effect。

### US-007: 生成 Packet 20 的 scoped EncodePlan 和 DecodePlan

**Description:** 作为 Tile packet codec 作者，我希望每个 Tile 的 flags 和条件字段按照 legacy 二维顺序处理。

**Acceptance Criteria:**

- [ ] header 顺序为 `startX/startY/width/height/changeType`。
- [ ] iteration order 固定为 x outer/y inner。
- [ ] 每个 Tile 的 `Flags1/Flags2/Flags3` 记录在自己的 scope 中。
- [ ] type 读取后可以通过 catalog predicate决定是否读取 frame。
- [ ] liquid flag projection 与 payload presence 的 legacy 差异得到保留和诊断。
- [ ] `WorldGen.RangeFrame`、tile mutation 和 server rebroadcast 不在 DecodePlan 中执行。

### US-008: 支持 Packet 10 的 Transform、StatefulLoop 和 SideTable

**Description:** 作为压缩 Tile packet codec 作者，我希望表达 Deflate、RLE 和正文后 side table，而不是把它们伪装成普通字段。

**Acceptance Criteria:**

- [ ] 可以声明 Deflate transform 的输入边界、输出子流和完整消费要求。
- [ ] loop 声明 `y outer / x inner`。
- [ ] `previousTile` 和 `pendingRun` 是显式 StateSlot。
- [ ] run transition、短/长 run 编码和展开上限可查询。
- [ ] `Flags1 -> Flags2 -> Flags3 -> Flags4` 的存在关系可查询。
- [ ] chest、sign、tile entity 三个 side table 保持正文之后的顺序。
- [ ] side table 有 count、record type 和最大记录数。

### US-009: 将 DecodePlan 与 EffectPlan 分离

**Description:** 作为运行时集成者，我希望 decode 先产生数据记录，再由显式 effect 应用到世界状态。

**Acceptance Criteria:**

- [ ] DecodePlan 的公开结果包含 decoded values、presence、selected variant 和 layout diagnostics。
- [ ] DecodePlan 不直接调用 `WorldGen`、`Main.*` mutation、`NetMessage.TrySendData` 或实体 slot allocation。
- [ ] IdentityPolicy、ValidationPolicy、ApplyEffect 和 ReplicationEffect 有独立接口/记录。
- [ ] EffectBoundary 声明 authority、writes、emits 和 failure mode。

### US-010: 接入现有 PacketFramework

**Description:** 作为现有网络框架维护者，我希望新计划通过兼容 adapter 接入，而不改变 framing、session gate 和 registry 的外部职责。

**Acceptance Criteria:**

- [ ] `MessageFrame` 继续处理 `ushort length + messageId + payload`。
- [ ] `SessionGate` 在 DecodePlan 前继续执行。
- [ ] `PacketDefinitionRegistry` 可以注册或查找 compiled plan adapter。
- [ ] 旧 `PacketCodec` 和 `IPacketCustomCodec` 可以作为过渡实现继续存在。
- [ ] Packet 13/20 可以接入新计划而不要求 Packet 10 同时迁移。
- [ ] adapter 能明确区分 snapshot、decoded result 和 effect context。

### US-011: 记录布局预算和诊断

**Description:** 作为协议审计者，我希望知道每个 slot 实际写了多少字节、位于哪个 scope，以及是否超过静态预算。

**Acceptance Criteria:**

- [ ] 每个 slot 可声明 `minBytes` 和 `maxBytes`。
- [ ] 每次执行可记录 `offsetBegin`、`offsetEnd`、`actualBytes` 和 presence。
- [ ] 运行时实际大小超过静态预算时产生结构化错误。
- [ ] layout record 不会被写入 legacy payload。
- [ ] error 能定位到 packet、profile、scope path 和 slot。

### US-012: 为 custom codec 建立可审计 contract

**Description:** 作为 legacy codec 维护者，我希望 custom codec 的输入类型、消费长度和 fallback 行为明确可见。

**Acceptance Criteria:**

- [ ] custom codec 声明 input/output logical type、读写能力和 purity。
- [ ] 声明 min/max bytes、消费边界和是否完整消费 payload。
- [ ] dummy/fallback decode path 有独立 shape 和消费预算。
- [ ] 声明 `MayApplyEffect` 的 codec 不能作为纯 FieldSlot 内联执行。

## 6. Functional Requirements

### IR 和 schema

- **FR-1:** 系统必须支持不可变 `PacketSchema`、`MessageProfile`、`ScopeSlot` 和 `FieldSlot`。
- **FR-2:** 每个 FieldSlot 必须包含稳定 ID、scope、wire type、logical type、局部 wire order 和大小契约。
- **FR-3:** 系统必须支持 `PackedFlagSlot`，并记录 bit offset、bit width、语义名称和 encode/decode projection。
- **FR-4:** 系统必须支持 `GateSlot`、`GroupSlot`、`SelectSlot` 和 `CustomCodecSlot`。
- **FR-5:** 系统必须支持 `Contains`、`Sequence`、`Presence`、`Shape`、`Value`、`Context` 和 `Transform` 等 wire relations。
- **FR-6:** 系统必须将 `Applies`、`Allocates`、`Rebroadcasts`、`Emits`、`Policy` 和 `SessionTransition` 标记为 runtime/effect relations，不得参与 wire dependency ordering。

### Presence 和上下文

- **FR-7:** 系统必须提供 packet-level RootMask、scoped mask、group state、selected variant 和 loop shape 的内部 PresencePlan。
- **FR-8:** PresencePlan 不得自动序列化为 legacy wire 字段。
- **FR-9:** 系统必须分别建模 FlagProjection、PayloadPresence 和 ValueAvailability。
- **FR-10:** 字段条件必须通过声明的 ContextBinding 引用 `netMode`、`whoAmI`、catalog 或 session context，不得隐式捕获全局对象。
- **FR-11:** ContextBinding 必须声明 phase、scope、purity、authority 和 volatility。

### 线序、循环和转换

- **FR-12:** DependencyDAG 必须支持环检测，并提供可定位的 cycle diagnostic。
- **FR-13:** WireSequence 必须独立保存，不能通过全图拓扑排序推断。
- **FR-14:** `AreaLoop` 必须声明维度源、迭代顺序、记录 scope 和最大迭代数。
- **FR-15:** `UntilSentinelLoop` 必须声明终止值、最大项数和畸形输入策略。
- **FR-16:** `StatefulLoop` 必须声明 StateSlot、transition、编码变体和展开上限。
- **FR-17:** `TransformSlot` 必须声明 transform 类型、输入/输出边界、完整消费要求和异常策略。
- **FR-18:** `SideTableSlot` 必须声明正文相对位置、count、record type 和最大记录数。

### 计划和兼容接缝

- **FR-19:** 系统必须从静态 IR 生成或构造 `EncodePlan` 和 `DecodePlan`。
- **FR-20:** EncodePlan 必须支持固定字段、flag projection、scope、loop、select、transform 和 side table。
- **FR-21:** DecodePlan 必须产生 decoded result 和 WireLayoutRecord。
- **FR-22:** DecodePlan 不得直接执行业务 effect 或 recipient routing。
- **FR-23:** 系统必须提供 IdentityPolicy、ValidationPolicy、ApplyEffect、ReplicationEffect 和 RoutingPlan 的接缝。
- **FR-24:** 系统必须通过 adapter 接入 `MessageFrame`、`SessionGate`、`PacketDefinitionRegistry` 和旧 `PacketCodec`。
- **FR-25:** custom codec 必须声明正常和 fallback 消费形状、预算和 purity。

### 安全和诊断

- **FR-26:** 所有动态 loop 必须有最大迭代/记录/展开预算。
- **FR-27:** DecodePlan 必须拒绝超过 payload remaining 的读取。
- **FR-28:** catalog index、entity index、tile coordinate、string length 和 frame length 必须有显式约束。
- **FR-29:** 静态校验必须报告未声明的前序依赖、group 部分存在、scope 泄漏、预算不足和 transform 边界缺失。
- **FR-30:** legacy exception 必须可查询、可输出到诊断文档，不能在 lowering 时静默修正。

## 7. Design Considerations

### 7.1 建议的最小公共接口

```csharp
public interface ICompiledPacketPlan
{
    byte MessageId { get; }
    PacketWireBytes Encode(PacketSnapshot snapshot, EncodeContext context);
    PacketDecodeResult Decode(ReadOnlySpan<byte> payload, DecodeContext context);
}
```

Effect 不应塞进这个接口；它应消费 `PacketDecodeResult`，由上层根据 authority 和 policy 执行。

### 7.2 编译器内部阶段

```text
Legacy IDL / C# definition
    -> Parser / Definition Builder
    -> Ordered AST
    -> SlotGraph IR
    -> Semantic Verifier
    -> EncodePlan / DecodePlan lowering
    -> Adapter registration
```

Kaitai Struct 可以作为 read-side parser/verifier，Construct 可以作为 Python reference/differential tool，但正式双向 schema 和 C# 接入由受限 custom legacy IDL/IR 负责。

### 7.3 错误模型

错误至少分为：

```text
DefinitionError       schema/slot/scope 定义错误
DependencyError       DAG 环、前序依赖错误
PresenceError         group/mask/flag projection 不一致
BudgetError           min/max/actual 或 frame/payload 超限
DecodeError           字节不足、selector 非法、transform 失败
CompatibilityWarning  legacy exception 或不可达旧分支
EffectError           decode 成功但 apply/replication policy 失败
```

DecodeError 不应自动触发世界状态修改；EffectError 的处理由上层 session/transport policy 决定。

## 8. Technical Considerations

- 现有 `PacketFramework.cs` 已有字段定义、条件、字段组和 `IPacketCustomCodec<TPacket>`，新 IR 应复用概念边界，但不能把现有 delegate 条件直接当作可查询的完整 IR。
- `MessageFrame` 仍然是 framing authority；SlotGraph 只处理 message body 或 transform 子流。
- `SessionGate` 仍然在 packet decode 前执行，不能让 field schema 重新实现 connection state gate。
- `PacketDefinitionRegistry` 应注册 plan adapter，而不是被迫了解所有 SlotGraph node kind。
- Packet 10 需要独立处理 Deflate；不能要求普通 `BinaryReader` 字段引擎解释压缩内部状态。
- Packet 20 的每 Tile scope 需要可复用模板实例，而不是为每个 Tile 复制完整静态 graph。
- 所有运行时状态应存在 `ProtocolInstance`/plan execution context 中，不回写静态 IR。
- legacy packet 总 frame 长度受 `ushort` 限制，静态预算和 runtime layout 都必须保留这个约束。
- 旧协议的 mode-dependent profile 可能导致 encode/decode 形状不完全对称；profile 和 direction 必须是显式维度。
- 现有 Concept 目录和 `NetWork.csproj` 的 Compile 路径需另行冻结、修复和验证；本 PRD 不将其隐藏为 SlotGraph 功能的一部分。

## 9. Success Metrics

- Packet 13、20、10 均能被 IR 静态表示，而不需要在 FieldSlot 中直接访问 `Main`、`Netplay` 或世界对象。
- Packet 20 的每个 TileScope 能产生独立的 presence/layout path，且不会发生跨 Tile 状态污染。
- Packet 10 的 transform、RLE state、nested flags 和 side tables 在 IR 查询结果中全部可见。
- 依赖环、group 半存在、无界 sentinel/RLE、缺失 transform boundary 和预算不足均能在编译/静态校验阶段被拒绝或明确警告。
- DecodePlan 的输出可以在没有执行 Terraria world mutation 的情况下独立检查。
- 现有 `MessageFrame`、`SessionGate` 和 `PacketDefinitionRegistry` 的外部职责保持不变。
- 至少一个 Packet 13/20 plan 可以通过现有 registry adapter 暴露，而不要求同步迁移 Packet 10 或全部 message ID。
- 报告和 plan diagnostics 能回答：字段为什么存在、位于哪个 scope、由哪个 selector/flag 决定、占用多少字节、读取后由哪个 effect 使用。

## 10. Delivery Constraints

- 第一阶段只处理设计和 IR/plan 验证，不修改 Terraria 参考源码。
- 不以“所有 packet 都已迁移”作为首批验收条件。
- 不把旧 custom codec 的存在视为失败；但每个 custom codec 必须逐步补齐 contract。
- 不把 scanner、graph export 或 JSON 自洽结果单独当作迁移完成证明；必须绑定源码快照、schema manifest 和 verifier evidence。
- 实现前必须保留本 PRD 与 [SlotGraph IR 设计报告](../docs/reports/2026-09-08-slotgraph-ir-design-report.md) 的需求追踪关系。

## 11. Open Questions

- `EncodePlan` 和 `DecodePlan` 第一版采用解释器、C# source generation，还是两者并行？
- custom legacy IDL 是先实现 Packet 13/20/10 子集，还是先为现有 C# definition 提供 IR builder？
- Packet 10 的 Deflate 子流是否由原生 `TransformSlot` 承担，还是在第一阶段继续保留 legacy custom codec adapter？
- `WireLayoutRecord` 是否需要稳定的 source span，以支持 AI/审计工具从 slot 回链到 IDL 和源码行？
- effect boundary 的输出是命令队列、事件，还是由现有 PacketReceived pipeline 直接消费？
- 现有 Concept/test1/test2/test3 哪些实验资产保留为参考，哪些在 IR 接入前归档？

## 12. Traceability

| PRD 范围 | 设计报告证据 | 源码证据 |
| --- | --- | --- |
| PresencePlan / GroupSlot | 报告第 6、7.1 节 | `NetMessage.cs` Packet 13；`MessageBuffer.cs` Packet 13 |
| scoped AreaLoop | 报告第 7.2 节 | `NetMessage.cs` Packet 20；`MessageBuffer.cs` Packet 20 |
| Transform / StatefulLoop / SideTable | 报告第 7.3 节 | `NetMessage.cs:1912-2410` 附近 |
| FrameEnvelope | 报告第 2、3、11 节 | `Core/Adaptation/MessageFrame.cs` |
| SessionGate | 报告第 2、11 节 | `Core/Server/Session/SessionGate.cs` |
| registry adapter | 报告第 11 节 | `Core/Protocol/PacketDefinitionRegistry.cs` |
| custom codec contract | 报告第 8、9、10 节 | `Core/Protocol/PacketFramework.cs` 与 MessageBuffer delegated readers |
| 全量 packet 特征 | 现有逐分支底稿 Appendix A-C | `test4/netmessage-messagebuffer-feature-report.md` |
