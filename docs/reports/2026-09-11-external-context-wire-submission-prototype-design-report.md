# 外部上下文投影与 WireSubmission 原型设计报告

状态：优化版；基于原型证据形成的设计报告，尚未代表生产实现
日期：2026-09-11
范围：NetWork 出站协议的外部上下文投影、不可变提交物和 wire 编码接缝
基线设计：docs/plans/2026-09-09-external-context-wire-submission-design.md

评审结论：三套原型足以冻结“外部事实在哪里结束、wire 编码从哪里开始”的边界，但不足以宣称 Packet 13/20 已经接入生产出站链路。凡是没有被原型 transcript、生产源码或完整 frame 对照共同支持的内容，在本文中都标为待证门禁，而不是设计事实。

证据等级：

- `E1`：源码/README 能直接证明的结构或不变量。
- `E2`：可重复运行的 prototype transcript 或 standalone 输出。
- `E3`：现有生产源码、旧 codec、registry 或 framing 的事实。
- `GATE`：必须在实现阶段补齐的完整 wire、兼容性或 pipeline 证据。

## 1. 执行摘要

仓库中有三套与外部上下文/WireSubmission 设计直接相关的原型。它们不是三种互相竞争的架构，而是依次回答三个问题：

1. 外部状态能否先被复制成 packet-specific 的不可变提交物，并在外部状态变化后继续稳定编码？
2. Packet 20 这种重复记录是否需要每个 Tile 自己拥有 presence 和可选值，且在绑定和编码失败时不产生半成品？
3. 固定段和有界段能否共用长度契约，同时把读边界、staging 内存、最终输出提交和 Packet 13 的条件字段放在明确的 wire 边界内？

原型给出的可采用结论是：出站链路应固定为：

~~~text
外部领域状态
    -> packet-specific ContextAdapter / PreparationInput
    -> packet-specific Projector / Binder
    -> immutable prepared submission
    -> SubmissionValidator
    -> EncodePlan
    -> bounded staging
    -> MessageFrame / send pipeline
~~~

其中，提交物只保存已经投影完成的 wire 事实；编码器不持有 Main、World、Netplay、Session、路由对象或延迟读取委托，也不通过重新读取领域状态来修复不合法输入。MessageFrame 继续负责 framing，发送 pipeline 继续负责 recipient resolution、过滤、去重和发送顺序。

本报告不把早期 Slot-DAG、graph/layout 实验列为本设计的直接原型，也不把 Verification/ProtocolDemoStandalone 误称为 context binding 原型。后者是生产迁移时要复用的验证基础。

## 2. 证据范围与原型清单

### 2.1 直接相关原型

| 原型 | 主要文件 | 证明的问题 | 状态 |
| --- | --- | --- | --- |
| 最小通用绑定 | Concept/ContextBindingPrototype/ContextBindingPrototype.cs、Program.cs、README.md | 外部快照、不可变 WireSubmission、固定/有界段共用长度契约、失败不覆盖此前结果 | `E1`/build 通过；入口仍是交互式，尚无 `--demo` |
| Packet 20 局部状态 | test4/context-binding-prototype/ContextBindingLogic.cs、Program.cs、README.md | checked(width * height)、Tile 数量、逐 Tile presence、值与 presence 的依赖、预算失败语义 | `E1`/`E2`；`--demo` 已执行成功，但 wire 是概念格式 |
| Packet 13 与 Segment Codec | Verification/PacketContextBindingPrototype/WireSegmentPrototype.cs、Program.cs、README.md | Fixed/Bounded、Exact/ParentDelimited、staging 原子提交、registry freeze、Packet 13 投影与确定性 | `E1`/`E2`；`--demo` 已执行成功 |

### 2.2 辅助生产验证资产

以下目录不是 context binding 原型，但它们是执行计划应复用的验证基础：

- Verification/ProtocolDemoStandalone/Packet20FrozenBaseline.cs：结构化 Packet 20 baseline serializer/deserializer。
- Core/Protocol/Packets/WorldActionPacketDefinitions.cs 中的 `AreaTileChangePacket20Definition`：当前生产 Packet 20 custom codec；`Core/Protocol/Packets/AreaTileChangePacket20.cs` 保存 opaque `TileDataPayload` 和尚未被该 codec 读取的 `TileRecords`。
- Verification/ProtocolDemoStandalone/PlayerControlsPacket13TestSupport.cs：Packet 13 sample snapshot 和 legacy bytes builder。
- Verification/ProtocolDemoStandalone/GoldenWireSnapshotTests.cs：golden frame 验证。
- Verification/ProtocolDemoStandalone/WireConformanceTests.cs：wire 一致性验证。
- Verification/ProtocolDemoStandalone/Program.cs：当前 standalone 验证入口。

这些资产的证据等级不是一回事：`Packet20FrozenBaseline` 是结构化 case 20 的独立基线，当前生产 `AreaTileChangePacket20Definition` 仍然读写 `TileDataPayload` 不透明尾部；`Packet20GraphExportTests.cs`、`GraphExportTests.cs` 等 Graph 测试当前被 `Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj` 排除，不能把它们的历史生成结果当作默认 standalone 已执行的验证。生成 codec 位于 `Build/obj/.../GraphCodeGen` 等构建产物中，也不是提交到生产 registry 的源码契约。Packet 20 只有在完成“typed submission -> 结构化 case-20 bytes -> 当前 frame/registry 接缝”的对照后，才可以升级为 `GATE` 通过。

### 2.3 不属于本设计直接证据的资产

以下资产不应被写入本设计的“原型证明”清单：

- test4/slot-dag-prototype.html：Slot-DAG、presence propagation 和布局计划实验。
- Concept/test1、Concept/test2、Concept/test3：早期 graph/layout/codegen 实验。
- Verification/ProtocolDemoStandalone：它验证生产框架、legacy 差分和 framing，不是 context binding 原型本身。

## 3. 原型一：最小通用绑定

### 3.1 路径与核心类型

原型位于 Concept/ContextBindingPrototype/，核心类型为：

- ExternalContextState
- PreparationInput
- WireSubmission
- PacketContextBinder
- SegmentBounds
- SegmentComposer
- EncodeAttempt

它把外部可变状态先转换成 PreparationInput，再由 PacketContextBinder 生成 WireSubmission。编码阶段只消费 submission 和 segment contract。

### 3.2 已验证行为

- 外部状态先捕获为值快照。
- 绑定后修改外部状态不影响已生成的 WireSubmission。
- Fixed(4) 与 Bounded(20,70) 共用“容量、实际写入量、最终提交量分离”的长度契约。
- 19 和 71 被拒绝，20、42 和 70 可以提交。
- staging 通过长度检查后才创建 final frame。
- 新一次失败不会覆盖此前成功的最终结果。

### 3.3 证据边界

该原型当前仍依赖 `Console.ReadKey` 的交互入口。无交互 stdin 运行不能形成可重复 transcript，并可能抛出 `InvalidOperationException`，因此它不能单独作为 CI 自动化证据。执行计划中的 Task 0 要增加保留交互模式的 `--demo` 入口，并把上述边界情况变成脚本可重复的 transcript。

该原型只证明最小边界，不证明生产 API 应直接复用其中所有类型，也不证明所有 packet 都能压成相同的 submission。

## 4. 原型二：Packet 20 的逐 Tile 局部状态

### 4.1 路径与核心类型

原型位于 test4/context-binding-prototype/，核心类型为：

- ExternalPacket20Context
- ExternalTileContext
- TilePresence
- PreparedTile20
- PreparedPacket20
- Packet20Binder
- DemoPacket20Wire

### 4.2 已验证行为

- 对区域尺寸执行 checked(width * height)。
- Tile 数量必须与区域形状匹配。
- 每个 Tile 独立保存自己的 presence。
- Color 不能脱离 Active，WallColor 不能脱离 Wall。
- 绑定后修改外部 Tile 不影响已有 PreparedPacket20 的编码结果。
- [20,70] segment bounds 与 payload budget 进行双重校验。
- staging 失败时不提交最终输出。
- demo 观察到正常提交 21 bytes；预算为 20 时 21 bytes 被拒绝。
- 所有 Tile 失活后得到 12 bytes，低于最小 20，因此被拒绝。
- 制造非法 WallColor 字段组时，绑定阶段失败。

需要明确证据边界：`DemoPacket20Wire` 写入的是原型自己的概念 body，包括概念 message id、宽高、记录数量、每个记录的 `Presence` 和局部值；它不是 `AreaTileChangePacket20Definition` 当前写入的 legacy `TileDataPayload` 字节。因此 21 bytes、12 bytes 和 `[20,70]` 的结果只能证明绑定/预算/staging 语义，不能证明 Packet 20 wire 兼容性。

### 4.3 生产含义与重要校正

Packet 20 是可重复记录集合。demo 为了验证 bounded staging，把自己的概念 body 限制在 `[20,70]`；这不能被提升为生产 Packet 20 的完整 packet 长度上限。生产实现应把 `[20,70]` 用作明确的独立 segment/framing 或局部 payload budget，而不是限制完整的可重复记录集合。

该原型最重要的生产结论是：presence 的所有权必须属于重复项自己的 scope。相邻 Tile 不能共享一个可变 packet-level mask，也不能让某个 Tile 的 Active、Wall、Color 或 WallColor 条件污染另一个 Tile。

## 5. 原型三：Packet 13 与 Segment Codec

### 5.1 路径与核心类型

原型位于 Verification/PacketContextBindingPrototype/，核心类型为：

- SegmentBounds
- SegmentBoundary
- SegmentSpec
- IWireSegmentCodec<TValue>
- SegmentHandle<TValue>
- SegmentRegistry
- SegmentComposer
- Packet13PreparationInput
- Packet13Presence
- PreparedPacket13
- Packet13Projector
- Packet13Encoder
- PrototypeEngine

### 5.2 已验证行为

- Fixed(n) 要求实际写入量精确等于 n。
- Bounded(min,max) 要求实际写入量位于闭区间。
- Exact 读边界和 ParentDelimited 读边界被显式区分。
- codec 只获得同步 staging Span<byte>，不能拿到最终 writer。
- composer 检查 written 后才把 [0,written) 复制到最终输出。
- callback 报告非法长度时，final output 保持原值。
- ParentDelimited 读取必须完整消费父范围；仅有写长度范围不足以定位下一个读 segment。
- SegmentRegistry.Freeze() 后新增注册会被拒绝。
- Packet 13 的 flag/presence 投影可以在不读取外部对象的情况下完成。
- PotionOfReturn 两个位置必须 all-or-none；部分存在会被拒绝。
- 外部输入变化不影响已经生成的 submission。
- 同一 submission 重复编码得到相同字节。
- 编码器不直接接收最终 writer，也不依赖 session、routing 或 transport。

### 5.3 生产含义

原型支持一个窄的 Segment Contract：固定段和有界段可以共享 bounds 验证，但读侧必须额外声明如何找到边界。`Exact` 要求调用方提供固定长度范围；`ParentDelimited` 要求调用方先提供父范围，codec 再完整消费该范围。`20..70` 是写入/消费长度契约，不是一个自动解决下一个 segment 边界的万能协议。

同时，registry freeze 说明可扩展 wire codec 不应在运行时无边界地变更。生产实现可以在启动/构建阶段完成注册，在发送阶段只消费冻结后的 schema/plan。

## 6. 三套原型共同证明的不变量

### 6.1 外部上下文只在投影阶段存在

外部状态可以由 adapter 读取，但应该先形成 packet-specific 的 PreparationInput。Projector/Binder 负责计算：

- 字段是否存在；
- 依赖字段组是否完整；
- 局部 presence；
- 可重复记录形状和顺序；
- 需要复制到 submission 的值。

编码器不应保存或回读 Main、World、Netplay、Session、目录对象或路由上下文。

### 6.2 submission 是强类型、不可变、拥有数据的快照

首版使用 PreparedPacket13、PreparedPacket20 这类 packet-specific 类型，不先创建公共 PreparedPacket 基类。提交物只能拥有编码所需的标量、不可变集合、字符串和 blob；不能保存外部对象引用、可变数组别名、Func<T>、Lazy<T>、writer、cursor 或 scope stack。

因此一次投影可以支持多次编码、重试、golden 验证和差分比较，而不依赖领域对象在期间保持不变。零拷贝、借用 buffer、owner/refcount 和跨线程池化不在首版设计中。

### 6.3 presence 属于明确 scope

Packet 13 的 presence 属于 packet scope；PotionOfReturn 的两个位置属于一个 all-or-none field group。Packet 20 的 presence 属于单个 Tile。任何 mask 只能控制同一 scope 内声明的 optional slot。

### 6.4 flag 是派生结果，不是第二事实源

推荐的数据流是：

~~~text
外部领域事实
    -> Projector
    -> Presence / WireFacts
    -> EncodePlan
    -> 线上 flag + optional payload
~~~

不能同时暴露一份由调用方任意修改的 flag 字节和一份可独立修改的 presence/value 集合。legacy 中 flag、payload presence 和 value availability 不一致时，必须在 packet-specific schema 中显式声明，不得为了接口整齐而静默合并。

### 6.5 staging 到 final output 必须原子提交

所有 segment/packet 编码都应先写入受限的 staging 区域，验证实际写入量、bounds、budget 和父范围消费量，再一次性追加到 final output。失败时不截断、不补零、不删除字段，也不覆盖此前成功结果。

### 6.6 wire 顺序由静态 schema/plan 固定

调用者不能通过运行时上下文改变字段顺序、二维索引顺序或 segment 消费边界。Packet 13 的条件字段顺序由现有定义和 legacy bytes 验证固定。Packet 20 当前能直接从源码/夹具确认的是：固定头之后遍历 `TileRecords`，记录字段顺序为 `Flags1`、`Flags2`、`Flags3`、`TileColor`、`WallColor`、`TileType`、`Wall`、`Liquid`、`LiquidType`，各 optional 字段由对应 flag 控制；Graph 元数据的维度来源顺序是 `Width`、`Height`。

这还不足以单独证明运行时的 `x outer / y inner` 展开顺序，也不足以证明 frame 坐标字段属于当前 case-20 wire。原型和当前 baseline 没有提供这两个结论的直接证据，因此它们必须留在 Packet 20 的 `GATE`：使用带有非对称 tile 值的二维 fixture，从真实 legacy 来源或已冻结字节明确证明索引展开和字段集合后才能冻结。

## 7. 不应从原型推出的结论

原型证据不足以冻结以下内容：

- 一个万能 IContext、服务定位器或 Get(string) 字段 API。
- 一个包含所有 packet 字段的统一 WireSubmission DTO。
- submission 中保存 Main、World、Netplay、Session 或任何发送/路由对象。
- callback 直接获得最终 IBufferWriter<byte> 或持有长期 writer 能力。
- 默认零拷贝、owner/refcount、借用内存或池化生命周期。
- Packet 10 的通用 RLE/Deflate/Transform IR。
- 把 demo 的 [20,70] 提升为完整 Packet 20 的最大长度。
- 把早期 Slot-DAG/graph 原型当成 context binding/WireSubmission 的实现证据。
- 现在就公开 IPacketProjector<TInput,TSubmission> 或 IEncodePlan<TSubmission> 等公共泛型接口。

原型证明的是边界和不变量，而不是要求生产代码复制 demo 的 TUI、命名或全部类层次。

## 8. 冻结的生产设计

### 8.1 ContextAdapter 与 PreparationInput

ContextAdapter 是外部领域到 packet-specific 输入的唯一入口。它可以从 Main、World、Netplay、目录、身份和路由状态读取数据，但输出必须是只读、可审计的 PreparationInput，例如：

~~~text
Packet20PreparationInput
    RegionRequest
    TileSnapshot
    TileCatalogSnapshot
    WireMode
    PayloadBudget
~~~

adapter 不编码、不发送、不广播、不修改世界，也不把完整 Terraria 全局对象继续传给编码器。

### 8.2 Projector/Binder

Projector 读取准备输入，完成字段存在性、局部 mask、形状、数量、坐标范围和 packet-specific 组合约束。它可以拒绝外部事实，但不能写 final buffer、调用发送或执行接收侧 Effect。

第一阶段不要求公共 IProjector 接口；使用 PlayerControlsPacket13Projector、AreaTileChangePacket20Projector 这类具体类型即可。

### 8.3 immutable prepared submission

submission 是编码器唯一的业务输入。建议形式：

~~~text
PreparedPacket13
    FixedValues
    Presence
    OptionalValues

PreparedPacket20
    RegionHeader
    TileRecordsInWireOrder

PreparedTile20
    WireFacts
    Presence
    OptionalValues
~~~

submission 只含值和 wire 语义，不含外部引用、领域服务、delegate、输出游标、路由计划或广播命令。

### 8.4 SubmissionValidator

`SubmissionValidator` 是生产职责边界，不是当前仓库已经存在的公共组件。首版可以由 packet-specific binder/projector 的验证阶段和 encoder 前的局部校验共同承担，但必须满足“只读取 submission/静态 schema”的依赖约束。它应检查：

- 数值、字符串、blob、固定数组的范围；
- mask 与 optional value 的一致性；
- all-or-none group 和字段依赖；
- scope 归属；
- width * height、run、side-table 等计数的 checked 结果；
- 已声明的 payload budget 和 transform 边界。

验证失败直接拒绝，不访问 Main，不自动截断、补零、删值或重新投影。错误至少应能定位 packet、schema revision（如果已有）、scope/record、期望值和实际值。

### 8.5 EncodePlan

EncodePlan 固定字段顺序、mask 分支、flag 投影和 codec 调用。当前原型只证明了这个职责形状，尚未证明需要一个公共 `EncodePlan<T>` API。首版应以 packet-specific encoder 落地，并做低成本 wire 防线：检查未知 bit、codec 能否处理值、输出是否超预算和写入是否越界；但不能重新判断领域条件。

Exact 和 ParentDelimited 必须是不同的边界声明。Segment codec 获得的是同步 staging Span<byte>，不是最终 writer。固定段和有界段共用 SegmentBounds，但读取方必须有明确边界来源。

### 8.6 bounded staging

编码流程如下：

~~~text
validated submission
    -> rent/allocate bounded staging area
    -> encode and report written/consumed
    -> validate bounds, budget and parent boundary
    -> append [0, written) to final output
    -> construct NetMessage / MessageFrame
~~~

任一阶段失败都不产生半成品 NetMessage，也不覆盖调用方之前保存的成功结果。首版可使用普通数组或栈上/短生命周期 buffer；只有真实性能证据出现后再引入池化。

### 8.7 MessageFrame 与 send pipeline

Core/Adaptation/MessageFrame.cs 继续负责 message id、payload length、TCP frame 和 frame reader/writer。它不计算 Tile 语义，也不读取外部领域状态。

Core/Server/Pipeline/SendNetMessagePipeline/SendNetMessagePipeline.cs 当前的 `Execute(object, SendTarget, SendPipelineContext, ignoreConnectionId)` 会调用 `PacketDefinitionRegistry.Write(message)`，然后做 recipient 去重和连接号排序。迁移时增加一个只接受已编码 `NetMessage` 的窄入口，复用同一 recipient resolution 逻辑，但不得再次调用 `PacketDefinitionRegistry.Write`。encoder 不接收 session、routing 或 send pipeline context；pipeline 也不重新计算 packet 语义。

## 9. Packet 13 迁移决策

Packet 13 是第一条 vertical slice，因为它有固定字段、有限条件字段和明确的 all-or-none group。

当前生产状态必须写清：`PlayerControlsPacket13Definition` 已经有条件字段和 `PotionOfReturn` group 校验；`Verification/PacketContextBindingPrototype` 里的 `Packet13Projector`/`Packet13Encoder` 是独立 prototype，`Verification/ProtocolDemoStandalone/PlayerControlsPacket13TestSupport.cs` 是测试夹具，不是生产 submission adapter。生产迁移仍需证明新路径与当前 `PacketDefinitionRegistry.Write`/`MessageFrame` 的完整 frame 一致。

现有条件关系必须保留：

~~~text
ControlFlags2.bit2 -> Velocity
ControlFlags2.bit7 -> MountType
ControlFlags3.bit6 -> PotionOfReturn
ControlFlags4.bit5 -> NetCameraTarget
~~~

PotionOfReturn.OriginalUsePosition 和 HomePosition 必须同时存在或同时不存在。Projector 形成 Packet13Presence 和 typed optional values；encoder 从 presence 派生 flags，不接收一份可独立编辑的 flags 作为第二事实源。

验收证据：

- 四组条件字段分别覆盖存在和缺失；
- partial PotionOfReturn 在投影或 submission validation 阶段被拒绝；
- 新 encoder 与 legacy 完整 frame bytes 一致；
- 旧 registry/reader 能读取新 frame；
- 同一 submission 重复编码字节一致；
- encoder 测试不需要启动 Main、World 或 Session。

身份覆盖、mount apply、权限、server rebroadcast 和其他接收侧 Effect 仍在 EncodePlan 之外。

## 10. Packet 20 迁移决策

Packet 20 用来验证可重复记录的 scope 和顺序。Projector 生成按静态 wire order 排列的 PreparedTile20 集合。验证器必须执行：

~~~text
record count == checked(width * height)
~~~

当前结构化证据可以支持每个 Tile 自己拥有：

- WireFacts，例如 flags、tile color、wall color、tile type、wall、liquid amount/type；
- 局部 Presence；
- 与 presence 对应的强类型 optional values。

当前生产 `AreaTileChangePacket20Definition` 只把 header 后的 bytes 当作 `TileDataPayload` 读写；`TileRecords` 和 `Packet20FrozenBaseline` 来自结构化 Graph/verification 路径。迁移必须先建立二者的 translation boundary，再验证完整 body 和 frame bytes。相邻 Tile 的 wall、liquid、color 条件不能互相污染；二维 index order 和是否存在 frame 字段必须以非对称 fixture 的 legacy bytes 证据冻结，不能从 `Width`/`Height` 元数据名称推断。

不要把 test4 demo 的 `[20,70]` 作为完整 Packet 20 上限；它应在生产实现中变成独立 segment 或明确 payload budget。未知 flags 或未覆盖的条件必须拒绝，或者在 schema 中显式处理，不能静默丢字段。

## 11. Packet 10 延后原因与边界清单

Packet 10 同时涉及 Deflate、RLE、跨迭代状态和正文后的 Chest/Sign/TileEntity 尾表。当前原型没有提供足够证据来冻结这些公共 API，因此本轮只保留 legacy custom adapter，不实现 Packet 10 通用 submission。

下一次 Packet 10 设计前，需要单独盘点：

- Deflate 的输入和完整消费边界；
- y outer/x inner 的循环顺序；
- previous tile 与 pending run state；
- flag cascade；
- RLE short/long 形式和展开上限；
- Chest/Sign/TileEntity tail table 的正文相对顺序、数量和最大大小；
- legacy context/effect 依赖。

延后是边界控制，不是降低 Packet 10 的验证标准。真实 RLE/Deflate 证据出现后，再决定哪些状态进入 preparation input、哪些状态留在 EncodePlan。

## 12. 与当前生产代码的接缝

### 12.1 PacketFramework

Core/Protocol/PacketFramework.cs 当前通过 PacketCodec.Validate、PacketCodec.Write、PacketCodec.Read 和 IPacketCustomCodec<TPacket> 处理 packet。它主要面向 BinaryWriter/BinaryReader 和 byte[]。迁移可先在 packet-specific adapter 内引入 submission encoder，不要求一次重写整个框架。

### 12.2 PacketDefinitionRegistry

Core/Protocol/PacketDefinitionRegistry.cs 继续负责 definition 查找、Read(NetMessage) 和现有 Write(object) 兼容入口。新增 prepared-submission 入口应是窄的、明确的，并避免把 submission 重新包装成会触发旧上下文 codec 的对象。

### 12.3 Packet 13 与 Packet 20 definitions

Core/Protocol/Packets/PlayerControlsPacket13.cs 和 PlayerControlsPacket13Definition.cs 提供 Packet 13 的字段及条件定义。Core/Protocol/Packets/AreaTileChangePacket20.cs 与 WorldActionPacketDefinitions.cs 当前仍保留 Packet 20 的 opaque custom codec 边界。迁移时应先复用 definitions 和 baseline 证据，再替换出站写路径。

这里存在一个不能跳过的 Packet 20 接缝：`AreaTileChangePacket20Definition.Codec.Write` 写入的是 message id、五个 header 字段和 `TileDataPayload`；它不会读取 `TileRecords`。因此 `Packet20FrozenBaseline` 与生成 codec 的结构化字节对照，不能直接充当当前 registry 的完整兼容性证明。执行计划必须先决定 adapter 是把 typed submission 编成 `TileDataPayload` 后走兼容 registry，还是直接构造 `NetMessage` 并绕过 object registry；两条路径都要用 `MessageFrame.ToPacketBytes()` 做最终 frame 对照。

### 12.4 framing 与发送

Core/Adaptation/MessageFrame.cs 保持 framing 责任；Core/Server/Pipeline/SendNetMessagePipeline/SendNetMessagePipeline.cs 保持 recipient resolution、ignore、dedup/order 和发送责任。提交物编码与这些横切职责解耦。

## 13. 已知缺口、风险与控制措施

| 缺口/风险 | 影响 | 控制措施 |
| --- | --- | --- |
| 最小通用原型没有非交互 --demo | 无法直接作为 CI transcript | Task 0 增加参数化 demo，并保持交互模式 |
| test4 的 [20,70] 容易被误读为 Packet 20 上限 | 可能截断真实重复记录集合 | 生产只把它用于独立 segment/budget；Packet 20 用 baseline 和真实 framing 验证 |
| test4 的概念 body 容易被误称为 Packet 20 legacy bytes | 产生错误的兼容结论 | 报告和计划明确区分概念 transcript、结构化 case-20 baseline、当前 opaque registry payload |
| `TileRecords` baseline 与当前 `TileDataPayload` codec 尚未桥接 | 结构化测试通过但生产出站仍走旧 opaque 路径 | Task 3 增加 translation boundary，并对照四层输出：结构化 body、opaque `TileDataPayload`、`NetMessage`、`MessageFrame` |
| x/y 展开顺序和 frame 字段没有被当前 context 原型直接证明 | 迁移后可能得到合法但顺序错误的 wire | 使用非对称二维 fixture 与真实 legacy bytes 建立 `GATE`，未通过前不冻结顺序 |
| Graph 测试默认从 ProtocolDemoStandalone 排除 | “standalone 已验证 Graph/Packet 20”会被夸大 | 单独启用/运行 Graph harness，或新增明确被 Program 调用的 submission tests |
| legacy flag 与 payload 条件可能不完全等价 | 静默归一化会改变 wire | 保留 flag projection、payload presence、value availability 三个可诊断概念 |
| Packet 10 的 RLE/Deflate 状态未建模 | 过早公共抽象会锁死错误边界 | 保留 legacy adapter，先做 inventory |
| 当前生产 registry 仍以 object/byte[] 为中心 | submission 入口可能被旧路径绕回 | 增加窄入口和依赖护栏测试，禁止 submission codec 回读上下文 |
| Segment codec 只报告长度但没有读边界 | 后续字段可能被错误消费 | 要求 Exact 或 ParentDelimited 等显式 boundary |
| 只做单元测试没有完整 frame 对照 | 可能出现 framing 或旧 reader 回归 | Packet 13/20 都必须接入 golden/differential 和旧 reader 验证 |
| 依赖不可变集合的实现细节不明确 | 外部数组 alias 可能泄漏 | submission 构造时复制并在测试中 mutate 外部输入 |

## 14. 原型验证记录

在本次调查中已确认：

- test4/context-binding-prototype/ContextBindingPrototype.csproj -- --demo 可执行成功。
- Verification/PacketContextBindingPrototype/PacketContextBindingPrototype.csproj -- --demo 可执行成功。
- 最小通用原型可编译，但当前直接运行重定向 stdin 会触发 Console.ReadKey 的 InvalidOperationException；这不是被隐藏的通过条件，而是执行计划 Task 0 要修复的可重复性缺口。
- `Verification/ProtocolDemoStandalone/Program.cs` 当前默认执行的是生产骨架、legacy/golden/framing/pipeline 等验证；Packet 20 Graph export 测试在项目文件中被排除，不能把默认 standalone 退出码解释为 submission migration 已通过。
- 当前生产 Packet 20 的 `PacketDefinitionRegistry.Read/Write` 只保留 opaque `TileDataPayload`，而结构化 `Packet20FrozenBaseline` 只证明独立 case-20 记录格式；二者的完整 frame equivalence 尚未建立。
- 本轮重新执行时，`Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj` 和根 `NetWork.csproj` 均因既有项目文件引用不存在的 `Concept/PacketNode.cs`、`Concept/PacketLayout.cs` 等根目录 graph 源文件而出现 `CS2001`；实际同名实验源位于 `Concept/test1` 或 `Concept/test2`。这是当前工作树的基线构建阻塞，不是 context binding 设计的通过证据，也不应通过把 graph 文件复制进 submission 实现来规避。执行计划必须先解决该路径/源文件归属问题，再执行 standalone 和生产项目门禁。
- 三个原型的 README、源码和入口均支持本报告中的职责区分。
- 当前工作树存在用户已有未提交/未跟踪文件；本报告只新增本文件，不覆盖这些改动。

## 15. 最终设计判断

三个原型共同支持一个小而可验证的深模块边界：

~~~text
外部状态
    -> packet-specific preparation input
    -> projector/binder
    -> immutable prepared submission
    -> validated encode plan
    -> bounded staging
    -> final output
~~~

这个边界足以承接 Packet 13 和 Packet 20 的第一阶段迁移，同时不要求立即设计万能上下文、统一 IR、零拷贝生命周期或 Packet 10 的公共 RLE/Deflate 模型。真正应冻结的是不变量：外部判断只在投影阶段完成，presence 属于明确 scope，flag 由已投影事实派生，wire 顺序由已验证的静态 plan 决定，失败不提交半成品，编码器不回读领域上下文。Packet 20 还必须额外完成 opaque payload 到结构化 tile records 的桥接，以及二维顺序/字段集合的 legacy 证据闭环。

后续实现应以执行计划中的迁移门禁为准，而不是以 demo 的类名或 TUI 形式为准。只有当新旧完整 frame bytes、旧 reader、重复编码、非法输入拒绝、pipeline/framing 回归和依赖护栏全部有证据时，才可以把对应 packet 标记为完成迁移。
