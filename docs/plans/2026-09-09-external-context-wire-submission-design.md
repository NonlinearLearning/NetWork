# 外部上下文投影与 WireSubmission 设计

状态：设计提案，尚未实现  
日期：2026-09-09  
范围：NetWork 协议上层框架；首批验证 Packet 13、Packet 20  
相关设计：`tasks/prd-slotgraph-ir-framework.md`、`docs/reports/2026-09-08-slotgraph-ir-design-report.md`

> 本文只定义“领域事实如何进入 wire 编码器”的边界。它不是通用上下文框架、不是完整 SlotGraph 实现计划，也不冻结 Packet 10 的全部 RLE/Transform 结构。

## 1. 结论

出站路径采用一条窄而明确的接缝：

```text
外部快照
    -> Packet-specific Projector
    -> 不可变 WireSubmission
    -> SubmissionValidator
    -> EncodePlan
    -> bytes
```

核心决定如下：

1. `WireSubmission` 是编码器的强制输入契约；它只包含已经投影的 wire 语义，不包含 `Main`、`World`、`Netplay`、`Session`、路由对象或延迟求值委托。
2. `Projector` 读取外部上下文并计算字段存在性、局部 mask、重复形状和所需值，但不写缓冲、不发送消息、不修改领域状态。
3. `EncodePlan` 负责字段顺序、mask 分支、线上 flag 投影和字节编码，但不重新判断领域条件。
4. PresenceMask 属于它所控制的 scope。Packet 20 的每个 Tile 都有自己的局部状态，不能由一个 packet 级 mask 控制所有记录。
5. 语义存在性是单一事实源。线上 flag 是编码阶段的派生结果；legacy 中 flag 和 payload 条件不一致时，必须显式保留差异，不能静默合并。
6. 非法提交物被拒绝，不通过截断、补零、删除字段或回读领域上下文来“修复”。
7. `Effect`、身份覆盖、权限、世界应用、广播和路由不属于 `EncodePlan`。

首批只用 Packet 13 和 Packet 20 证明这条边界。Packet 10 继续通过明确标记的 legacy custom adapter 运行，等 RLE、Deflate 和尾表的实际证据稳定后再单独设计提交类型。

## 2. 问题与设计目标

当前 legacy 写入逻辑经常在一个过程内同时完成：

- 读取 `Main.tile`、`Main.netMode`、世界尺寸和目录；
- 裁剪坐标、计算区域和决定字段是否存在；
- 组织二维循环、RLE 或尾表；
- 生成 flag 并写入字节；
- 修改世界、广播或依赖会话状态。

这种混合使得“字段应该写什么”和“字段怎样编码”无法单独测试，也让一个重复项的条件很容易污染另一个重复项。

本设计要达到的结果是：

- 上下文读取和领域判断集中在 packet-specific Projector；
- 编码器可以脱离 Terraria 全局对象做确定性测试；
- 提交物在编码前可独立验证；
- 真实世界、回放、测试夹具和差分工具可以产生同一种提交物；
- 迁移可以按 packet 逐步进行，不要求一次改造整个 `PacketFramework`；
- 设计保持足够小，避免在没有实现证据前冻结一套新的通用 IR、上下文接口和诊断系统。

## 3. 边界与非目标

### 3.1 允许进入 Projector 的内容

外部 Adapter 可以从 `Main`、`World`、`Netplay`、目录、身份和路由状态读取信息，但应先形成 packet-specific 的只读 `PreparationInput`。输入应是快照或明确的只读视图，而不是把整个 Terraria 全局对象继续传给编码器。

例如：

```text
Packet20PreparationInput
    RegionRequest
    TileSnapshot
    TileCatalogSnapshot
    WireMode
    PayloadBudget
```

Projector 可以：

- 计算区域和坐标裁剪；
- 查询目录并决定 frame、颜色、液体等字段是否存在；
- 快照化字段值；
- 生成局部 PresenceMask；
- 准备已经确定的重复形状；
- 做依赖当前上下文的权限、模式和范围检查。

Projector 不可以：

- 写入 `IBufferWriter<byte>` 或直接生成整包字节；
- 调用发送、广播或其他 packet；
- 修改 `Main.tile`、World、实体或 Session；
- 把领域对象、全局对象或 `Func<...>` 放进提交物。

### 3.2 明确不做的事情

- 不创建万能 `IContext`、服务定位器或 `Get(string)` 字段接口。
- 不把所有 packet 强行压成同一种循环、同一种提交类型或同一个通用 DTO。
- 不让 Projector 绕过 `PacketSchema` / `EncodePlan` 直接生成 legacy bytes。
- 不把 PresenceMask 序列化为额外的 legacy 字段。
- 不在本设计中实现全部 Packet 迁移、Source Generator、运行时布局追踪或统一错误平台。
- 不修改 `D:\TRbackup\无任何删减通过编译\Terraria` 下的参考源码。

## 4. 最小职责模型

| 组件 | 输入 | 负责 | 不负责 |
| --- | --- | --- | --- |
| `ContextAdapter` | Terraria 领域状态 | 形成 packet-specific `PreparationInput` | 编码、发送、世界修改 |
| `PacketProjector` | 只读准备输入 | 计算 wire 语义、局部 mask 和形状 | 写 buffer、路由、Effect |
| `WireSubmission` | Projector 的结果 | 保存一次可重复消费的快照 | 保存领域引用、延迟读取、输出游标 |
| `SubmissionValidator` | submission + 静态 schema | 检查类型、依赖、mask、数量和预算 | 访问 Main、自动修复 |
| `EncodePlan` | 已验证 submission + wire buffer | 按顺序写字段、消费 mask、投影 flag | 重新查询领域事实、执行 Effect |
| `MessageFrame` | 编码后的 body | framing 和传输交接 | 计算 tile 语义 |

这不是要求新增六层公共类。首版可以使用每个 packet 的具体实现；表格只是职责边界。只有在同一提交类型确实被实时世界、回放和测试夹具共享时，才考虑提取接口。

## 5. WireSubmission 契约

`WireSubmission` 是一个概念名称。首版优先使用 `PreparedPacket13`、`PreparedPacket20` 这样的 packet-specific 类型，而不是先建立公共基类。

### 5.1 必须满足的性质

提交物必须：

- 只保存编码所需的值、局部存在性和 wire 形状；
- 在进入编码器前已经完成上下文判断；
- 只包含不可变值或由提交物拥有的不可变集合；
- 可以被验证、缓存、重试或用于 golden/differential 测试；
- 在相同 schema revision、模式和预算下产生相同字节。

提交物不得包含：

- `Main`、`World`、`Netplay`、`Session`、`RouteContext` 或目录对象引用；
- `Func<T>`、`Lazy<T>`、服务定位器或任何隐式上下文访问；
- 输出 buffer、输出游标或编码器内部 scope stack；
- `ApplyEffect`、`RoutingPlan`、广播命令或接收者过滤结果。

### 5.2 所有权和生命周期

Projector 可以读取外部快照，但提交完成后，编码不能依赖这些对象继续保持不变。标量值直接复制；集合、字符串和 blob 要么由提交物拥有，要么使用明确不可变的值类型。首版不设计借用 buffer、零拷贝生命周期或跨线程池化协议。

这条规则使以下操作安全且可测试：

```text
一次投影 -> 多次编码/重试/差分比较
```

编码失败不能通过重新读取领域状态来继续；调用方应决定是丢弃提交物、修正输入后重新投影，还是重试相同提交物。

### 5.3 概念结构

以下类型只表达形状，不是当前实现必须采用的公共 API：

```csharp
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
```

不要把所有可选值塞进 `Dictionary<string, object>`。C# 类型应表达字段组、值类型和记录数量；验证器再检查运行时范围和组合约束。

## 6. Presence、Flag 和字段值

### 6.1 统一规则

编码方向的关系是：

```text
外部领域事实
    -> Projector
    -> 局部 Presence / WireFacts
    -> EncodePlan
    -> 线上 flag + optional payload
```

EncodePlan 可以执行：

```csharp
if (presence.Has(Tile20Field.BlockColor))
{
    WriteBlockColor(value);
}
```

但不能执行：

```csharp
if (tile.Active && tile.Color > 0)
{
    // 这是外部上下文判断，不属于 EncodePlan。
}
```

提交物不应同时暴露两个可以独立编辑的事实源，例如一个可变的 `PresenceMask` 和一组由调用方手工填写的 flag 字节。flag 应由 mask 和明确的 `WireFacts` 投影得到。

### 6.2 Legacy 差异

某些 legacy packet 中，flag、payload 是否存在和值是否可用并不完全等价。此时保留三个可查询概念：

- `FlagProjection`：哪些线上 bit 由已提交事实产生；
- `PayloadPresence`：哪些 optional slot 实际写入；
- `ValueAvailability`：提交物是否拥有该值。

三者一致时可以由一个局部规则派生；不一致时必须在 packet-specific schema 中声明，并由验证器报告诊断。不能为了接口整齐而把液体、颜色或 frame 条件强行合并。

### 6.3 Scope 规则

- Packet 13 的条件字段属于该 packet 的 scope；all-or-none group 也在该 scope 中验证。
- Packet 20 的 mask 属于单个 `PreparedTile20`；相邻 Tile 不共享可变 mask。
- 任何 mask 只能控制同一 scope 内声明的 slot。
- packet 级 mask 不得控制所有重复记录的 optional 字段。

## 7. 首批 Packet 的边界

### 7.1 Packet 13：最小闭环

Packet 13 用来证明最小条件布局，不用来证明一个通用字段容器。提交物包含固定字段、四个 flag 所需的语义事实和 optional values。

需要覆盖的关系：

```text
ControlFlags2.bit2 -> Velocity
ControlFlags2.bit7 -> MountType
ControlFlags3.bit6 -> PotionOfReturn.OriginalUsePosition + HomePosition
ControlFlags4.bit5 -> NetCameraTarget
```

`PotionOfReturn` 是 all-or-none group。提交一部分必须拒绝。编码器从语义状态派生 flag，不能同时接受一份独立可编辑的 `Flags2`、`Flags3`、`Flags4` 作为第二事实源。

以下内容仍在 EncodePlan 之外：身份覆盖、mount apply、权限、server rebroadcast 和其他接收侧 Effect。

### 7.2 Packet 20：重复项的局部状态

Packet 20 的 header 和记录顺序由 schema 固定；Projector 生成按 wire order 排列的 `PreparedTile20` 集合。验证器检查：

```text
record count == checked(width * height)
```

每个 Tile 记录自己拥有：

- `WireFacts`，例如 active、wall、liquid 或 frame 相关事实；
- 局部 `Presence`；
- 对应的强类型 optional values。

Projector 可以读取 `Main.tile[x, y]`、`Main.tileFrameImportant[type]`、模式和坐标范围；EncodePlan 不读取它们。x outer / y inner 等具体顺序由 schema/plan 固定，不由调用方临时决定。

Packet 20 的第一验收重点是相邻记录不会互相污染，而不是先建立可表达所有 Tile 变体的通用 IR。

### 7.3 Packet 10：保留兼容边界

Packet 10 同时包含 Deflate、RLE、跨迭代状态和正文后的 Chest/Sign/TileEntity 表。它的复杂度足以单独成为设计，不应为了让三种 packet 看起来统一而在本文中预冻结：

- `PreparedTileRun` 的完整形状；
- `StatefulLoop` 的公共 API；
- Transform 的通用输入/输出子流协议；
- 尾表的公共 record 容器。

首版保留 `LegacyPacket10CodecAdapter`，并要求它的上下文依赖、消费边界和副作用被单独记录。未来若迁移 Packet 10，最小方向是：先定义 packet-specific submission，再决定哪些 RLE 状态在 Projector 中准备、哪些 wire 状态留在 EncodePlan 中；不能预先假定所有状态都外移或都保留。

这不是降低 Packet 10 的验收标准，而是把未知的 wire 事实从当前契约中移除，避免过早设计一套错误的通用模型。

## 8. 验证和失败语义

验证分三层，但每层只检查自己掌握的信息。

### 8.1 Projector 验证

可以使用外部上下文检查：

- 区域是否在有效世界范围；
- 当前模式、authority 和权限是否允许投影；
- catalog 是否支持某种 frame、颜色或 batching；
- 是否能形成一个完整字段组和合法 wire 形状。

Projector 失败时不产生可编码的 submission。

### 8.2 Submission 验证

只读取 submission 和静态 schema，检查：

- 数值、字符串、blob 和固定数组范围；
- mask 与 optional value 的一致性；
- all-or-none group 和字段依赖；
- scope 归属；
- `width * height`、run、side-table 等计数的 checked 结果；
- transform 或 payload budget 已声明的边界。

验证失败直接拒绝。验证器不访问 Main，也不自动截断、补零、删值或重新投影。

### 8.3 EncodePlan 防线

编码器只做低成本的 wire 防线：

- 当前 plan 能否处理该 submission；
- mask 是否包含未知 bit；
- 当前 slot 的值是否可按 schema codec 写入；
- 输出是否超过传入预算；
- writer 是否发生越界或写入失败。

发现错误时失败，不回退到领域上下文。EncodePlan 不是第二个 Projector。

### 8.4 错误最小集合

首版只需要能区分以下结果：

```text
ProjectionRejected   外部事实无法形成合法 submission
SubmissionRejected   submission 的类型、mask、shape 或预算非法
EncodeFailed         plan、writer 或输出预算失败
```

错误至少带有 packet、schema revision（如已有）、scope/record 定位、期望值和实际值。更细的错误分类等真实调用点出现后再增加，避免先建立一个没有消费者的错误层级。

## 9. 与现有 PacketFramework 的接缝

现有职责保持不变：

```text
MessageFrame       framing
SessionGate        入站门禁
PacketDefinitionRegistry  packet 查找和适配器选择
PacketReceived / Effect    解码后的业务应用
```

建议的出站适配流程是一个具体的 packet adapter：

```text
Project(input)
    -> validate(submission)
    -> EncodePlan.Encode(submission, output)
    -> MessageFrame
```

不要求现在公开：

```csharp
IPacketProjector<TInput, TSubmission>
IEncodePlan<TSubmission>
```

这些泛型接口只有在多个来源稳定地产生同一种 submission、并且调用方从抽象中获得实际收益时才值得提取。当前可以使用 `Packet13Projector`、`Packet20Projector` 和 packet-specific encoder。

兼容层必须明确区分两种 codec：

- `SubmissionCodec`：只消费已验证 submission；
- `LegacyContextCodec`：仍读取旧上下文，直到该 packet 完成迁移。

一个 adapter 不能先传入 submission，再让旧 codec 私下重新读取 `Main`。这条规则是迁移护栏，而不是命名偏好。

入站方向暂不重写：DecodePlan 可以产生 decoded values 和局部 presence，但世界应用、身份策略、广播和路由继续由现有接缝负责。它们不应因为出站 submission 的设计而回到 codec 内部。

## 10. 可控的延伸设计

这些方向有价值，但不属于首版强制契约。

### 10.1 一次投影，多次消费

不可变 submission 可以支持重试、回放、golden bytes、差分编码和多接收者复用，而无需再次读取世界状态。首版只要保证生命周期和确定性，不需要引入缓存服务或对象池。

### 10.2 来源信息只放诊断

如果排查问题需要知道 tile snapshot、catalog 或源码证据版本，可以在诊断元数据中保存来源标识。来源信息不应成为编码字段，也不应要求 EncodePlan 理解领域版本。是否落地取决于实际诊断需求。

### 10.3 上下文泄漏检查

迁移到 submission codec 后，可以增加测试或静态检查，确认 EncodePlan/slot codec 的依赖集合不包含 `Main`、`World`、`Netplay`、`Session` 和路由类型。这比先创建一套通用 ContextBinding 更直接，也更容易在 code review 中验证。

## 11. 迁移和最小验收证据

### 11.1 顺序

1. **Packet 13**：固定字段、局部 mask、flag projection、all-or-none group、golden bytes。
2. **Packet 20**：二维记录、独立 Tile scope、记录数量和顺序、相邻记录隔离。
3. **Packet 10**：先补齐上下文读取和消费边界清单，再基于真实 RLE/Deflate 证据形成独立设计。

不要求 Packet 13/20/10 同步迁移，也不把“所有 packet 使用同一提交基类”作为完成条件。

### 11.2 Packet 13 证据

- 固定字段和四个线上 flag 的 golden bytes 与 legacy 一致；
- Velocity、MountType、PotionOfReturn、NetCameraTarget 各有存在和缺失用例；
- PotionOfReturn 部分存在被拒绝；
- 相同 submission 重复编码得到相同 bytes；
- encoder 测试不需要启动 Main、World 或 Session。

### 11.3 Packet 20 证据

- header 和 x outer / y inner 顺序与 legacy 一致；
- 每个 Tile 有独立局部 presence；
- 相邻 Tile 的 frame、wall、liquid、color 条件不互相影响；
- 记录数量溢出、尺寸乘法溢出和预算超限会被拒绝；
- Projector 使用 catalog，EncodePlan 不使用 catalog 或 `Main.tile`。

### 11.4 Packet 10 当前证据

在迁移前只要求记录：

- Deflate 的输入和完整消费边界；
- RLE 的状态、计数和展开上限；
- 三个尾表的正文相对顺序、数量和最大大小；
- custom codec 的上下文读取和副作用。

这份清单用于下一次 Packet 10 设计，不把未验证的假设写成当前提交契约。

## 12. 当前决定与暂不决定

| 当前决定 | 暂不决定 |
| --- | --- |
| submission 是 EncodePlan 的唯一业务输入 | 公共 `PreparedPacket` 基类 |
| 首版使用 packet-specific 类型 | 泛型 Projector/EncodePlan 接口 |
| 外部判断字段存在性，内部消费 mask | 通用 `ContextBinding` 或服务定位器 |
| flag 是派生结果，legacy 差异显式声明 | 运行时 `WireLayoutRecord` 和完整遥测系统 |
| 非法 submission 拒绝而不修复 | 来源 revision 的强制字段 |
| Packet 13/20 先证明边界 | Packet 10 的通用 RLE/Transform/SideTable IR |
| Effect 和 Routing 留在现有接缝 | 全量 packet 迁移计划 |

如果后续实现证据证明某个“暂不决定”反复出现并有多个消费者，再将它提升为公共设计；在此之前保持具体、局部和可删除。

## 13. 最终不变量

```text
外部上下文只能进入 ContextAdapter / PreparationInput / Projector
WireSubmission 是不可变、强类型、无业务引用的 wire 语义快照
Presence 属于具体 scope，重复项不共享可变 mask
EncodePlan 消费已投影事实，但不计算领域条件
线上 flag 由明确规则派生，legacy 差异不能静默合并
WireSequence 决定字节顺序，不能由运行时上下文改变
非法 submission 被拒绝，编码器不回读领域状态修复
Effect、Routing 和世界修改不属于 EncodePlan
```

当这些不变量成立时，外部上下文外移就形成了一个可验证的深模块边界：Projector 把领域事实变成合法提交物，EncodePlan 把合法提交物稳定地变成 legacy bytes。除此之外的抽象，都应等真实 packet 和测试证据出现后再增加。

## 14. 追踪关系

| 本文内容 | 现有设计/证据 |
| --- | --- |
| 外部上下文、wire 和 Effect 分离 | `docs/reports/2026-09-08-slotgraph-ir-design-report.md` 的 Context、Effect、EncodePlan 章节 |
| Packet 13 条件字段和 group | `NetMessage.cs` / `MessageBuffer.cs` 的 Packet 13；现有 PRD US-006 |
| Packet 20 局部 Tile scope | `NetMessage.cs` / `MessageBuffer.cs` 的 Packet 20；现有 PRD US-007 |
| Packet 10 保留兼容边界 | `NetMessage.cs` / `MessageBuffer.cs` 的 Packet 10；现有 PRD US-008 |
| framing、SessionGate、registry 接缝 | 现有 PRD US-010；`docs/01-总体设计.md` 至 `docs/04-传输适配与验证.md` |

