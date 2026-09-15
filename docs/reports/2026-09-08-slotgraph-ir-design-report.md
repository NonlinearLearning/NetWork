# SlotGraph IR 协议上层框架设计报告

日期：2026-09-08  
状态：设计报告；不包含生产实现，不修改 Terraria 参考源码。

## 1. 执行摘要

本报告将以下两个完整 legacy 源文件中的协议特征，整理为 SlotGraph IR 的设计依据：

```text
D:\TRbackup\无任何删减通过编译\Terraria\NetMessage.cs
D:\TRbackup\无任何删减通过编译\Terraria\MessageBuffer.cs
```

核心结论是：SlotGraph IR 不能设计成“字段节点 + 普通依赖边”的单一 DAG。真实协议同时包含：

```text
FrameEnvelope
SessionGate
PacketDispatch
PacketSchema
WireSequence
ScopeTree
DependencyDAG
LoopStateMachine
Transform
SideTable
ContextBinding
EffectPlan
RoutingPolicy
```

这些关系必须分域保存：

| 关系 | 作用 | 是否参与普通依赖 DAG |
| --- | --- | ---: |
| `Presence` / `Value` / `Shape` | 表达字段存在、值选择、重复形状 | 是，受限参与 |
| `Sequence` | 表达真实线上字节顺序 | 否，不能用拓扑序替代 |
| `Scope` | 表达 packet、record、iteration、side table 的局部命名空间 | 否，形成 scope tree |
| `LoopState` | 表达 RLE、sentinel、跨迭代状态 | 否，使用状态机 |
| `Transform` | 表达 Deflate 等子流转换 | 否，使用有界 transform 节点 |
| `Effect` | 表达世界状态修改、身份覆盖和回播 | 否，不进入 wire codec |
| `Routing` | 表达接收者选择和同步 bookkeeping | 否，不改变 payload layout |

首批设计验证应固定使用：

```text
Packet 13 -> fixed fields + wire flags + optional group
Packet 20 -> area loop + per-record scope + catalog predicate
Packet 10 -> Deflate + stateful RLE + nested flags + side tables
```

这三个包分别验证条件字段、局部作用域和复杂转换循环。Packet 8、23、27、34、50/54、65、56、69 等消息作为反例，防止 IR 只对三个最小示例成立。

## 2. 证据范围与当前状态

### 2.1 参考源码规模

| 文件 | 当前行数 | 主要职责 |
| --- | ---: | --- |
| `NetMessage.cs` | 3,038 | 发送入口、payload 写入、压缩/解压、接收 framing、广播和路由 |
| `MessageBuffer.cs` | 4,500 | 缓冲区、frame 解码后的 packet 读取、session 门禁、状态应用和服务器回播 |

完整特征、160 个接收侧顶层 ID 和发送侧特征索引已经记录在：

[test4/netmessage-messagebuffer-feature-report.md](../../test4/netmessage-messagebuffer-feature-report.md)

本报告是面向架构决策和后续实施的收敛版，不替代上述逐分支底稿。

### 2.2 现有 NetWork 接缝

当前工作树可观察到以下模块：

| 文件 | 当前职责 | SlotGraph 接入策略 |
| --- | --- | --- |
| `Core/Adaptation/MessageFrame.cs` | `ushort length + messageId + payload` framing；处理半包和粘包 | 保留为 `FrameEnvelope`，不纳入 packet body schema |
| `Core/Server/Session/SessionGate.cs` | 根据 session state 和 message id 决定允许、拒绝或 boot | 保留为 `SessionGate`，作为 DecodePlan 前置阶段 |
| `Core/Protocol/PacketFramework.cs` | 字段、条件、字段组、默认 `PacketCodec` 和 custom codec 接口 | 作为现有 codec seam；SlotGraph 通过 adapter 接入 |
| `Core/Protocol/PacketDefinitionRegistry.cs` | message id 到 packet definition 的注册和查找 | 保留 registry 入口，注册 compiled plan adapter |
| `Concept/test1`、`Concept/test2`、`Concept/test3` | 现有实验性 graph/layout/emit 方向 | 不视为统一的生产实现；新 IR 需明确替代或归档边界 |
| `NetWork.csproj` | 显式列出 `Concept\PacketNode.cs` 等根路径 Compile 项 | 作为独立 build 修复/验证事项，不由本报告隐式修改 |

### 2.3 关键源码入口

| 源码位置 | 观察到的事实 |
| --- | --- |
| `NetMessage.cs:95` 附近 | `SendData` 同时选择 buffer、写 payload、回填长度、发送和路由 |
| `MessageBuffer.cs:133` 附近 | `GetData` 同时处理 timeout、message id、session gate、reader 定位和 packet switch |
| `NetMessage.cs:1912` 附近 | Packet 10 建立 Deflate 压缩流 |
| `NetMessage.cs:1925` 附近 | Packet 10 扫描 tile、聚合 RLE、收集 side table |
| `NetMessage.cs:2255` 附近 | Packet 10 建立 Deflate 解压流 |
| `NetMessage.cs:2264` 附近 | Packet 10 执行 RLE、nested flags 和 side table 解码 |
| `NetMessage.cs:2491` 附近 | 接收 bytes 追加到 buffer |
| `NetMessage.cs:2519` 附近 | 按 frame length 拆包、保留半包和移动未消费尾部 |

## 3. 现状问题定义

### 3.1 `SendData` 和 `GetData` 的职责过深且相互交织

旧代码中同一个顶层分支可能同时包含：

```text
读取/写入 primitive；
读取/写入 BitsByte；
根据当前字段决定后续字段是否存在；
根据 Main.netMode 改写值；
根据 whoAmI 覆盖身份；
直接访问 Main.tile / Main.npc / Main.player；
执行 WorldGen 或 entity allocation；
决定是否回播；
决定发送给哪些客户端；
更新连接 state 或同步 bookkeeping。
```

这使以下问题同时发生：

1. 无法只通过字段定义得到完整 wire layout。
2. 无法区分“字段未出现”和“字段已读取但被业务忽略”。
3. 无法从普通依赖图恢复实际字节顺序。
4. 无法表达每个 Tile 自己的 mask 和作用域。
5. 无法表达 Packet 10 的 `previousTile` / `pendingRun`。
6. 无法在不启动完整世界状态的情况下验证 DecodePlan。
7. `PacketFramework` 的 custom codec 容易退化为不可查询的手写黑盒。

### 3.2 “一个 bool/mask 表示整个 packet”不足以覆盖真实协议

上层内部的 PresencePlan 是必要的，但必须按 scope 实例化：

```text
Packet 13 -> packet-level presence mask
Packet 20 -> one mask per TileScope
Packet 10 -> one local mask per RLE record plus loop state
```

同时，线上 flags 不能被内部 mask 替代：

```text
PresencePlan      -> 上层内部，不写入 legacy wire
FlagsByte / Bits  -> legacy wire 字段，必须保留
FlagProjection    -> 内部 presence 到线上 flag 的投影规则
```

### 3.3 依赖图无法独立表示线序和循环

Packet 20 发送侧采用 `x outer / y inner`，Packet 10 采用 `y outer / x inner`。二者都可以有同样的 TileRecord 字段，但不能使用同一个默认二维枚举器。

Packet 10 还存在：

```text
previousTile
pendingRun
short/long run encoding
Flags1 -> Flags2 -> Flags3 -> Flags4
Chest table -> Sign table -> TileEntity table
```

这些不是静态字段依赖，应由 `WireSequence`、`ScopeTree`、`StatefulLoop`、`NestedFlagScope` 和 `SideTable` 分别表达。

## 4. 设计原则

### 4.1 静态蓝图和运行时实例分离

静态 IR 保存：

```text
字段、类型、scope、线序、关系、loop boundary、transform、预算和约束。
```

运行时实例保存：

```text
字段值、PresencePlan、选择的 profile、cursor、offset、loop state、诊断。
```

禁止静态图保存：

```text
Main、Netplay、Tile、Player、NPC 等可变对象引用；
BinaryReader/BinaryWriter 实例；
previousTile、pendingRun 等一次解析的状态值；
直接执行 WorldGen 或 NetMessage.TrySendData 的 delegate。
```

### 4.2 wire 和 effect 分离

推荐执行链：

```text
FrameEnvelope
    -> SessionGate
    -> PacketDispatch
    -> DecodePlan
    -> DecodedPacket / WireLayoutRecord
    -> IdentityPolicy
    -> ValidationPolicy
    -> ApplyEffect
    -> ReplicationEffect
    -> RoutingPlan
```

发送链：

```text
Domain State / Request
    -> SnapshotResolver
    -> ContextBinding
    -> PresencePlan
    -> EncodePlan
    -> WireAssembler
    -> RecipientSelectionPlan
    -> FrameEnvelope
```

### 4.3 旧协议事实优先于“更干净”的新语义

Packet 20 的液体字段是关键例子：

```csharp
FlagProjection:  tile.liquid > 0 && Main.netMode == 2
PayloadPresence: tile.liquid > 0
```

迁移不能擅自把两个条件合并。IR 应保留 legacy exception，并能报告：

```text
FlagProjection != PayloadPresence
```

### 4.4 用深模块隐藏复杂实现

对外接口应尽量小：

```text
CompiledPacketPlan.Encode(snapshot, context)
CompiledPacketPlan.Decode(payload, context)
```

复杂性集中在：

```text
definition parser
semantic verifier
IR lowering
generated/interpreted plan
diagnostic layout recorder
```

调用方不应了解每个 slot 的图边实现细节。

## 5. SlotGraph IR 定义

### 5.1 顶层容器

```text
SlotGraphIR {
    protocolVersion
    packetSchemas[]
    messageProfiles[]
    slots[]
    scopes[]
    wireSequences[]
    dependencyEdges[]
    loopDefinitions[]
    stateDefinitions[]
    contextBindings[]
    transformDefinitions[]
    sideTableDefinitions[]
    customCodecContracts[]
    effectBoundaries[]
    routingPolicies[]
    diagnosticsMetadata
}
```

### 5.2 Slot 类型

| Slot | 责任 | 典型来源 |
| --- | --- | --- |
| `FieldSlot` | primitive 或结构字段 | `u8`、`i16`、`f32`、`vec2` |
| `PackedFlagSlot` | 线上 BitsByte/flag byte 和 bit projection | Packet 13、20、27、88、132 |
| `GateSlot` | 根据已解析条件决定存在 | Packet 13 optional fields |
| `GroupSlot` | all-or-none 或其他 cardinality | Packet 13 PotionOfReturn |
| `SelectSlot` | profile、selector、width variant | Packet 21/145、23、65 |
| `ScopeSlot` | 作用域树节点 | packet、tile record、side table |
| `LoopSlot` | Exactly、Count、Area、Until、RunLength | Packet 7、20、50/54、10 |
| `StateSlot` | 迭代状态 | Packet 10 previous/run |
| `TransformSlot` | 有界子流转换 | Packet 10 Deflate |
| `SideTableSlot` | 正文后的 count + records | chest/sign/tile entity |
| `CustomCodecSlot` | 声明过预算和消费规则的 delegated codec | NetworkText、TileEntity |
| `ConstraintSlot` | 范围、可逆性、legacy exception、完整消费 | coordinate/entity/type 校验 |

### 5.3 关系域

```text
Wire:
    Contains
    Sequence
    Presence
    Shape
    Value
    Context
    Transform

Runtime:
    Applies
    Allocates
    Normalizes
    Rebroadcasts
    Emits
    Policy
    SessionTransition
```

只有 wire dependency 子图执行 DAG 无环校验。`Sequence` 是局部有序关系；`Contains` 是作用域树；runtime relations 只能被查询或降低为 EffectPlan，不能改变 wire sequence。

### 5.4 ContextBinding

```text
ContextBinding {
    contextId
    logicalType
    source
    phase = Encode | Decode | Apply | Route
    scope = Packet | Record | Iteration | Session
    purity = ReadOnly | Derived | Effectful
    authority = Client | Server | Shared
    volatility = Static | PerPacket | PerIteration
}
```

典型 binding：

```text
Main.netMode
whoAmI
Netplay.Clients[whoAmI].State
ServerSideCharacter
Main.tileFrameImportant[type]
Main.projHostile[type]
SectionRange
previousTile
pendingRun
```

字段节点只能引用声明过的 binding，不能在 schema 中直接捕获全局对象。

## 6. PresencePlan 与 Wire Projection

### 6.1 内部 PresencePlan

```text
PresencePlan {
    rootMask
    scopeMasks[ScopePath]
    groupStates[GroupId]
    selectedVariants[SelectId]
    loopShapes[LoopId]
    legacyExceptions[]
}
```

它只存在于上层的 encode/decode 实例中，不进入 legacy wire。

### 6.2 三种状态必须分离

```text
FlagProjection
    线上某个 flag bit 是否被设置。

PayloadPresence
    当前 payload 是否追加字段字节。

ValueAvailability
    当前上下文是否有合法值可写入或读取。
```

一个字段只能在三者都满足合法契约时被编译器自动优化合并；legacy exception 存在时必须保留三者的独立表达。

### 6.3 Encode 方向

```text
SnapshotResolver
    -> Resolve ContextBinding
    -> Resolve MessageProfile
    -> Compute PresencePlan
    -> Validate groups/scopes/variants
    -> Project to legacy flags
    -> Execute EncodePlan
```

### 6.4 Decode 方向

```text
Read fixed prefix
    -> Read flags/selectors
    -> Reconstruct scoped PresencePlan
    -> Read conditional fields
    -> Build DecodedPacket
    -> Record WireLayoutRecord
```

## 7. 三个首批 Packet 的设计落点

### 7.1 Packet 13

Packet 13 适合验证最小条件字段闭环：

```text
PlayerId
Flags1
Flags2
Flags3
Flags4
SelectedItem
Position
[Flags2.bit2] -> Velocity
[Flags2.bit7] -> MountType
[Flags3.bit6] -> PotionOriginalUsePosition + PotionHomePosition
[Flags4.bit5] -> NetCameraTarget
```

需要生成的结构：

```text
PackedFlagSlot(Flags1..Flags4)
GateSlot(Velocity)
GateSlot(MountType)
GroupSlot(PotionOfReturn, all-or-none)
GateSlot(NetCameraTarget)
```

不纳入 Packet 13 wire plan 的内容：

```text
服务器将 player id 改为 whoAmI；
客户端忽略自己的 packet 的 policy；
teleport smoothing；
mount apply；
server rebroadcast；
发送本地 camera target 后更新 lastSyncedNetCameraTarget。
```

### 7.2 Packet 20

Packet 20 的头部为：

```text
startX : i16
startY : i16
width  : u8
height : u8
changeType : u8
```

后面是：

```text
AreaLoop(width, height, order = x-outer-y-inner)
    -> TileScope
         -> Flags1
         -> Flags2
         -> Flags3
         -> Color?
         -> WallColor?
         -> Type?
         -> FrameX/FrameY?
         -> Wall?
         -> Liquid/LiquidType?
```

关键规则：

1. 每一个 `TileScope` 都拥有独立的 local mask。
2. `Type` 读取后，`FrameX/FrameY` 是否存在取决于 catalog predicate。
3. `FlagProjection` 和 `PayloadPresence` 保留源码差异。
4. `OnTileChangeReceived`、`Main.tile` 修改、`RangeFrame` 和服务器回播属于 EffectPlan。
5. 发送前坐标和宽高归一化属于 `SnapshotResolver`，不属于 primitive field writer。

### 7.3 Packet 10

Packet 10 形状为：

```text
DeflateTransform
    -> RegionHeader(xStart, yStart, width, height)
    -> StatefulAreaLoop(order = y-outer-x-inner)
         state previousTile
         state pendingRun
         nested Flags1 -> Flags2 -> Flags3 -> Flags4
         tile record payload
    -> ChestSideTable
    -> SignSideTable
    -> TileEntitySideTable
```

`StatefulAreaLoop` 必须声明：

```text
transition = isTheSameAs(previousTile)
             && AllowsSaveCompressionBatching[type]
run encoding = none | short | long
expanded limit = width * height
```

压缩正文之后的 side table 不能被优化器移动到正文之前。`TileEntity.Read`、`Chest.CreateWorldChest`、`Sign` materialize 等都是 apply effect，不属于 DecodePlan 的纯字段读取。

## 8. 其他源码特征对 IR 的约束

| 源码特征 | 约束 |
| --- | --- |
| Packet 8 读取三个字段后计算大量 section 并发送 Packet 10 | `JoinWorldEffect` 和 `EffectLoop`，不是 Packet 8 wire loop |
| Packet 21/90/145/148 共享部分字段但 profile 尾部不同 | `MessageProfileSelector`，不能全部转成 optional field |
| Packet 23 AI 稀疏字段和 life width | `SparseSequence` + `WidthSelect` |
| Packet 27 nested flags、hostile gate、projectile slot 分配 | `NestedFlagScope` + `EntityIdentityPolicy` + `ResourceAllocationEffect` |
| Packet 34 action selector | wire body 固定，action 分支属于 EffectSelect |
| Packet 50/54 以 `ushort 0` 结束 | `UntilSentinelLoop`，必须有 maxItems |
| Packet 56 按 server/client 写不同尾部 | mode-dependent `MessageProfile` |
| Packet 69 服务器分支不消费 name | profile 需要声明消费边界 |
| Packet 82 外部 `NetManager.Read(reader, whoAmI, length)` | length-aware `CustomCodecContract` |
| Packet 121/124 fallback dummy reader | `FallbackDecodeShape` 和消费预算 |
| SendPlayerHurt/Death 等使用静态 side channel | 显式 `ContextBinding`，禁止隐式纯函数假设 |
| SendSection/ResyncTiles/SyncConnectedPlayer 产生多包序列 | `CommandPlan`/`EffectPlan`，不是单包 schema |

## 9. 静态校验要求

### 9.1 图和 scope

1. `slotId`、`scopeId`、`profileId` 唯一。
2. 所有引用目标存在。
3. `Contains` 形成无环 scope tree。
4. `Sequence` 只允许在同一 block 内定义局部顺序。
5. 子 scope 读取父 scope 的可变状态时必须声明 capture。
6. 同一字段不能被两个不兼容的 wire sequence 隐式复用。

### 9.2 依赖和 presence

1. DependencyDAG 无环，并输出完整环路径。
2. Gate 必须引用已声明的 flag、selector 或 ContextBinding。
3. wire field 必须晚于其所依赖的 wire gate/selector。
4. Group 成员必须满足 all-or-none 或显式 cardinality。
5. scoped field 必须拥有明确的 scope path。
6. `FlagProjection` 与 `PayloadPresence` 不得在 legacy exception 未声明时合并。

### 9.3 loop、transform 和 side table

1. `Exactly(n)` 的 n 有界。
2. `Count(n)` 具备 count slot、record type 和总预算。
3. `Area(width,height)` 声明维度来源和 iteration order。
4. `Until` 声明终止值、最大项数和畸形输入策略。
5. `RunLength` 声明 state slots、short/long encoding 和展开上限。
6. Transform 声明输入边界、输出边界、完整消费规则和异常策略。
7. SideTable 声明相对正文位置和记录边界。

### 9.4 字节预算和安全性

1. `minBytes <= maxBytes`。
2. `actualBytes <= maxBytes`。
3. frame 总长不超过 legacy `ushort` 限制。
4. decode cursor 不超过 payload remaining。
5. string、array、entity、tile coordinate 和 catalog index 均有边界。
6. custom codec 的正常和 fallback 路径都有消费预算。
7. 不可达 signedness 条件保留为诊断，不静默删除。

## 10. EncodePlan、DecodePlan 和 EffectPlan

### 10.1 EncodePlan

```text
ResolveProfile
ResolveContext
ResolvePresence
EmitFixed
EmitFlagProjection
EnterScope
IterateLoop
EmitConditional
EmitSelect
EmitTransform
EmitSideTable
CloseScope
FinalizeLayout
```

编译后的 Packet 13 计划应接近直接的 writer 和 branch，而不是运行时为每个字段分配通用 graph object。

### 10.2 DecodePlan

```text
ReadCursor
ReadFixed
ReadFlagsAndSelectors
CreateScopedPresence
ReadConditional
ExecuteLoopBoundary
DecodeTransform
DecodeSideTable
ProduceDecodedPacket
ProduceWireLayoutRecords
```

DecodePlan 禁止直接：

```text
修改 Main.tile/player/npc；
调用 WorldGen；
分配 projectile slot；
修改 Netplay.Clients state；
调用 NetMessage.TrySendData；
选择 recipient。
```

### 10.3 EffectPlan

```text
DecodedPacket
    -> IdentityPolicy
    -> ValidationPolicy
    -> ApplyEffect
    -> ReplicationEffect
    -> RecipientSelectionPlan
```

每个 effect 必须声明输入 slots、context bindings、authority、writes、emits 和 failure mode。

## 11. 与现有 PacketFramework 的兼容接缝

推荐保留现有外层链：

```text
MessageFrame
    -> SessionGate
    -> PacketDefinitionRegistry
    -> PacketCodec / CompiledSlotGraphAdapter
    -> PacketDecodeResult
    -> PacketReceived / ApplyEffect
```

新增适配器的职责：

```text
旧 DTO 或领域输入
    -> PacketSnapshot
    -> CompiledSlotGraphPlan
    -> EncodePlan / DecodePlan
    -> DecodedPacket / WireLayoutRecord
```

概念接口：

```csharp
public interface ICompiledPacketPlan
{
    byte MessageId { get; }
    PacketWireBytes Encode(PacketSnapshot snapshot, EncodeContext context);
    PacketDecodeResult Decode(ReadOnlySpan<byte> payload, DecodeContext context);
}

public interface IPacketPlanAdapter<TPacket>
{
    PacketSnapshot ToSnapshot(TPacket packet, EncodeContext context);
    TPacket FromDecoded(PacketDecodeResult result, DecodeContext context);
}
```

兼容接缝要求：

1. `MessageFrame` 继续负责 framing。
2. `SessionGate` 继续负责 session state 和 message id 门禁。
3. `PacketDefinitionRegistry` 继续负责 message id 查找。
4. SlotGraph adapter 不把 world effect 重新塞入 `PacketCodec`。
5. legacy custom codec 可以作为过渡实现，但必须拥有结构化 contract。
6. Packet 13/20 可以先接入 compiled plan；Packet 10 可以先由 custom adapter 包装，再迁移为 native transform/loop nodes。

## 12. 实施边界和迁移顺序

### Phase 0：冻结证据和边界

交付：

```text
完整源码特征报告；
Packet 13/20/10 wire inventory；
Wire/Effect/Route 分类；
现有 PacketFramework seam 说明；
legacy exception 清单。
```

### Phase 1：最小 IR 和静态校验

只实现：

```text
PacketSchema
FieldSlot
PackedFlagSlot
GateSlot
GroupSlot
ScopeSlot
WireSequence
DependencyDAG
PresencePlan
```

覆盖 Packet 13。

### Phase 2：重复作用域和 area loop

增加：

```text
AreaLoop
ScopedMask
CatalogPredicate
WireLayoutRecord
```

覆盖 Packet 20，确保每个 Tile 的状态隔离。

### Phase 3：sentinel、transform、stateful loop

增加：

```text
UntilSentinelLoop
TransformSlot
StateSlot
StatefulRunLengthLoop
SideTableSlot
```

先验证 Packet 10-like 结构，再进行 Terraria legacy bytes 对照。

### Phase 4：PacketFramework adapter

增加：

```text
CompiledPacketPlan
PacketSnapshot
PacketDecodeResult
LegacyPacketCodecAdapter
```

只替换 codec seam，不改变 `MessageFrame`、`SessionGate` 和外层 packet event 生命周期。

### Phase 5：effect/routing 迁移

将 Packet 13、20、10 的应用、回播和区域路由迁移到显式 EffectPlan；在此之前不得声称“上下文已从协议中移除”。

## 13. 风险与决策

| 风险 | 后果 | 决策 |
| --- | --- | --- |
| 把全图拓扑序当 wire order | legacy bytes 漂移 | 强制 `WireSequence` |
| 用一个 root mask 表示所有重复项 | Packet 20/10 读取错位 | scoped masks |
| 把 custom codec 当透明字段 | 无法预算和审计消费 | `CustomCodecContract` |
| 把 effect select 当 wire union | 业务分支污染 schema | Wire/Effect 分域 |
| 把 Packet 10 当普通 TileRecord | 丢失 RLE state 和 side table | Transform + StatefulLoop + SideTable |
| 把 `Main.netMode` 直接捕获到字段条件 | codec 不可独立验证 | ContextBinding |
| 一次迁移全部 160 个 packet | 变更面不可控 | 13 -> 20 -> 10 垂直切片 |
| 修正 legacy 条件而非保留事实 | 与旧客户端不兼容 | 显式 legacy exception |
| 现有 Concept 路径/状态未冻结 | 编译和设计证据混淆 | 单独处理 build/目录边界 |

## 14. 验收判断

设计可以进入实现阶段的最低条件：

1. Packet 13 能由 `PackedFlagSlot + GateSlot + GroupSlot` 表达并降低为直接 branch。
2. Packet 20 能为每个 TileScope 生成独立 PresencePlan，且线序明确为 x outer/y inner。
3. Packet 10 能表达 Deflate、y outer/x inner、previous/run state、nested flags 和三个 side table。
4. `FlagProjection`、`PayloadPresence`、`ValueAvailability` 的差异可查询。
5. `DecodePlan` 不直接触碰世界状态，EffectPlan 接管副作用。
6. `WireLayoutRecord` 能记录 scope、offset、actual/max bytes，但不写入 legacy wire。
7. 现有 `MessageFrame`、`SessionGate`、`PacketDefinitionRegistry` 的外部职责不被 SlotGraph 侵入。
8. 全量源码特征仍可追溯到逐分支底稿，而不是只剩抽象设计。

## 15. 最终结论

SlotGraph IR 的关键不是制造一张更复杂的图，而是建立一个有明确内部结构的协议编译模型：

```text
字段定义       -> PacketSchema / FieldSlot
存在性         -> PresencePlan / Gate / Group
真实线序       -> WireSequence
重复作用域     -> ScopeTree
循环边界       -> LoopSlot
跨迭代状态     -> StateSlot / LoopStateMachine
压缩/子流      -> TransformSlot
尾随记录       -> SideTableSlot
运行时上下文   -> ContextBinding
业务副作用     -> EffectPlan
接收者选择     -> RoutingPolicy
```

只有这样，`NetMessage.cs` 和 `MessageBuffer.cs` 中混在一起的上下文、复杂嵌套循环和 wire layout 才能被拆成可查询、可校验、可降低为 EncodePlan/DecodePlan 的上层框架。
