# Slot-DAG 协议描述方案可行性调研

日期：2026-09-08  
状态：设计调研与可运行原型说明，不是生产实现，也不包含测试。

## 1. 调研问题

本调研验证以下设计是否成立：

> 下层只保存每个数据包的简单字段定义；上层把可扩展属性抽象成插槽（slot），由插槽之间的 DAG 表达存在性、选择、长度和作用域依赖；编译器将这些关系降低为高效的线性写入计划和读取计划。字段只引用插槽，不拥有 DAG。

当前验证范围固定为三个真实协议形状：

- Packet 13：固定字段、wire flags、普通条件字段和成组条件字段。
- Packet 20：二维重复 TileRecord，每个重复项拥有独立的局部条件状态。
- Packet 10：变长转换流、RLE 状态和尾随 side table 的复杂包形状。

## 2. 结论

方案可行，但必须把“依赖图”和“线序”视为两种不同的信息：

```text
下层 PacketSchema
    -> 只描述 packet、字段、wire type 和声明顺序

上层 SlotGraph
    -> 描述 slot 之间的 dependsOn / Presence / Shape / Value / Transform 关系

编译器
    -> 校验 DAG
    -> 保留显式 wireOrder
    -> 生成 EncodePlan 和 DecodePlan

运行时
    -> 上层 resolver 生成 PresencePlan / ScopeState
    -> 字段 encoder/reader 只处理已解析值
    -> assembler 按实际长度拼接 byte stream
```

因此推荐采用“受限 Slot-DAG IR + 显式线性 wire plan”的混合模型，而不是让 DAG 直接承担字节顺序。

### 2.1 最重要的边界

| 内容 | 所属位置 | 是否进入线上字节流 |
|---|---|---:|
| `packet P13 { field ... }` | 下层 PacketSchema | 否，作为定义来源 |
| `slot controlFlags2` | 上层 SlotGraph | 它对应的字段会写入 |
| `slot velocity dependsOn controlFlags2.bit2` | 上层 SlotGraph | 依赖关系本身不写入 |
| `PresencePlan.RootMask/ScopeMasks` | 一次运行的上层状态 | 否 |
| Packet 13 的 `Flags1..Flags4` | wire field | 是 |
| Packet 20 每个 Tile 的 `Flags1..Flags3` | 重复作用域中的 wire field | 是 |
| `WireLayoutRecord.start/end/actual/max` | 上层布局诊断和组装状态 | 否 |
| `WireOrder` | 编译出的线性计划 | 间接决定写入顺序 |

内部 `PresencePlan` 不能被误认为新的 wire mask。旧协议中的 flags 仍然是线上字段；上层只把已经求出的 slot presence 投影到这些 flags，或者从 flags 恢复 slot presence。

## 3. 建议的两层模型

### 3.1 下层：简单 packet 定义

下层定义保持接近旧协议，字段不携带 DAG：

```text
packet Packet13 {
    field PlayerId: u8;
    field ControlFlags1: bits8;
    field ControlFlags2: bits8;
    field ControlFlags3: bits8;
    field ControlFlags4: bits8;
    field SelectedItem: u8;
    field Position: vec2<f32>;
    field Velocity: vec2<f32>;
    field MountType: u16;
    field PotionOriginalUsePosition: vec2<f32>;
    field PotionHomePosition: vec2<f32>;
    field NetCameraTarget: vec2<f32>;
}
```

这层回答“有哪些字段以及字段的基础 wire 类型”。它不回答“某字段在本次实例中是否出现”，也不访问 `Main`、`Tile`、`World`、`Session` 或 `netMode`。

### 3.2 上层：插槽和关系

上层把字段绑定到 slot，并在 slot 上持有关系：

```text
slot ControlFlags2 {
    field = Packet13.ControlFlags2;
    kind = Fixed;
    maxBytes = 1;
}

slot Velocity {
    field = Packet13.Velocity;
    kind = Conditional;
    dependsOn = [ControlFlags2];
    presence = ControlFlags2.bit(2);
    maxBytes = 8;
}

slot PotionOfReturn {
    kind = PresenceGroup;
    dependsOn = [ControlFlags3];
    presence = ControlFlags3.bit(6);
    members = [PotionOriginalUsePosition, PotionHomePosition];
}
```

字段只有 `slotId`、`wireType`、`wireOrder` 等引用信息。关系属于 slot graph；同一个字段类型可以在多个作用域中被实例化，而不需要给字段对象复制一份 DAG。

### 3.3 运行时值模型

不建议把所有可能的属性都继续堆成一个无限膨胀的 packet 类。可以保留用户所说的“大而完整的承载类”，但把可扩展部分收敛到一个代数数据类型式的 slot value：

```text
SlotValue
  = U8(byte)
  | U16(ushort)
  | I32(int)
  | F32(float)
  | Vec2(float, float)
  | Bits8(byte)
  | Bytes(byte[])
  | Struct(SlotValue[])
  | Sequence(SlotValue[])
  | Variant(tag, SlotValue)
  | Absent
```

建议 packet DTO 仍由领域层拥有明确的强类型属性；`SlotValue` 主要存在于编译器 IR、布局计划和 codec 接缝中。这样既不会把业务对象变成反射字典，也不会让每个字段承担图结构。

## 4. 编译器职责

推荐编译阶段如下：

```text
Packet DSL / C# definition
    -> Parser / Definition builder
    -> SlotGraph IR
    -> Binder
    -> Static verifier
    -> Wire plan lowering
    -> generated encoder + decoder
```

### 4.1 静态校验

编译器至少需要拒绝：

1. slot id 重复。
2. 依赖目标不存在。
3. `dependsOn` 形成环。
4. 条件字段没有稳定的 presence slot。
5. 条件字段在它的依赖字段之前出现在 wire order 中，导致读端尚未读到 gate。
6. `PresenceGroup` 成员只有部分出现。
7. 重复作用域引用了父作用域的易变状态，却没有显式声明 capture。
8. `MaxBytes` 小于 wire type 的最小编码长度。
9. RLE、sentinel 或 length-delimited loop 没有上限。
10. `Transform` 的输入、输出和尾表边界不明确。

### 4.2 DAG 不能替代 wire order

DAG 的拓扑序只能说明依赖先后，不能自动决定旧协议的所有线序。特别是：

- Packet 20 的重复项是局部 scope，每个 Tile 内部都有相同的字段顺序。
- Packet 10 的 Tile 扫描顺序、RLE 状态和尾随表具有协议语义。
- 两个互不依赖的字段仍然可能有明确的 wire order。

因此 IR 必须同时保存：

```text
SlotGraph edges: slot -> dependency
WireSequence: [slotA, slotB, slotC, ...]
ScopeTemplate: TileRecord / RleRecord / SideTable
```

编译器可以使用 DAG 拓扑序校验合法性，但实际 encoder/decoder 使用 `WireSequence` 和作用域模板生成直接分支。

### 4.3 高效生成目标

第一阶段的目标不是通用解释器，而是生成如下形状的代码：

```csharp
// EncodePlan 的概念形状
WriteFixed(PlayerId);
WriteFixed(ControlFlags1);
WriteFixed(ControlFlags2);
if ((ControlFlags2 & 0b0000_0100) != 0)
    WriteVector2(Velocity);
if ((ControlFlags2 & 0b1000_0000) != 0)
    WriteUInt16(MountType);
```

也就是把静态 DAG 关系在编译期变成直接的 bit check、循环和 primitive writer，避免运行时为每一个字段创建通用图节点或反射调用。

## 5. 三个首批 packet 的验证形状

### 5.1 Packet 13：最小可行 packet

Packet 13 是适合最小原型的条件结构：

```text
Fixed:
    PlayerId
    ControlFlags1..4
    SelectedItem
    Position

Conditional:
    ControlFlags2.bit2 -> Velocity
    ControlFlags2.bit7 -> MountType
    ControlFlags3.bit6 -> PotionOriginalUsePosition + PotionHomePosition
    ControlFlags4.bit5 -> NetCameraTarget
```

其中 `PotionOriginalUsePosition` 和 `PotionHomePosition` 不是两个独立条件，而是一个 `PresenceGroup`。原型需要展示：

- gate 为 false 时两个字段都没有字节；
- gate 为 true 时两个字段一起出现；
- 只提供一个字段时上层计划拒绝生成非法布局；
- 内部 presence mask 不出现在 payload 中。

### 5.2 Packet 20：重复作用域

Packet 20 的关键不是字段数量，而是每个 Tile 都有自己的 presence 状态：

```text
TileGrid
  -> TileScope[0]
       -> Flags1 / Flags2 / Flags3
       -> TileColor? / WallColor? / TileType? / Frame? / Wall? / Liquid?
  -> TileScope[1]
       -> 独立 flags 和独立条件字段
```

不能用一个全包 flat mask 表示所有 Tile。否则 `Tile[0].Color` 会和 `Tile[1].Color` 冲突，读取端也无法知道条件字段属于哪一个重复项。正确做法是 `ScopeMasks[path]` 或模板化的 local mask。

### 5.3 Packet 10：Transform + stateful Loop + SideTable

Packet 10 不应被当作 Packet 20 加上一个 `compressed=true`：

```text
DeflateTransform
  -> area loop (y outer, x inner)
       -> previous tile / pending run state
       -> local slot presence and value selection
  -> ChestSideTable
  -> SignSideTable
  -> TileEntitySideTable
```

因此上层 graph 需要把 `Transform`、`Loop`、`StateSlot` 和 `SideTable` 作为结构化 slot kind。DAG 只记录依赖，例如 `TileType -> FrameGate`；RLE 的“当前 run 是否延续”属于 loop state，不能伪装成静态字段关系。

首批原型只验证计划生成、存在性传播、局部 scope 和布局记录；不声称实现 Terraria Packet 10 的完整 Deflate/RLE wire codec。

## 6. Kaitai Struct、Construct 与自定义 IDL

### 6.1 Kaitai Struct

Kaitai 的官方用户指南提供 `if` 条件属性、`repeat` 重复属性、`switch-on` 分支和跨属性引用。这证明“声明字段，再由编译器生成解析器”的路线适合顺序二进制协议。

但 Kaitai 的核心产物是解析器生成。不能把它天然当作与旧协议完全对称的 builder，也不能假设它会替上层处理 Terraria 的业务上下文、Effect 或 session 状态。

适合借鉴：

- 条件、重复、选择和子结构的 DSL 语法；
- 编译时表达式绑定；
- 生成只读 parser 作为独立 verifier。

不适合作为唯一正式层：

- 项目需要明确的双向 EncodePlan/DecodePlan；
- 项目需要内部 `PresencePlan` 和 scoped slot graph；
- 旧协议的业务条件不能落回 generated parser。

来源：

- [Kaitai Struct User Guide](https://doc.kaitai.io/user_guide.html)
- [Conditionals](https://doc.kaitai.io/user_guide.html#_conditionals)
- [Repetitions](https://doc.kaitai.io/user_guide.html#_repetitions)
- [Switches](https://doc.kaitai.io/user_guide.html#_switch)

### 6.2 Construct

Construct 官方文档把 `Struct`、`If`、`Switch`、`Array`/重复和 parse/build 放在同一个声明体系中，适合快速验证“一个声明能否支持读写对称性”。它尤其适合用少量样本快速确认字节布局和局部 context 是否足够。

限制是核心运行时是 Python；它不能直接成为当前 .NET `PacketFramework` 的生产生成器。更合理的角色是：

- 原型实验台；
- golden bytes 对照工具；
- 新 IDL 语义的早期可行性验证。

来源：

- [Construct Introduction](https://construct.readthedocs.io/en/latest/intro.html)
- [Construct Basics](https://construct.readthedocs.io/en/latest/basics.html)

### 6.3 自定义 legacy IDL

自定义 IDL 最适合当前正式方案，因为它可以明确表达旧协议特有的：

- 固定线序；
- wire flags 与内部 presence 的投影；
- `PresenceGroup`；
- 每个重复项的 scoped mask；
- sentinel、count、area、RLE 等不同 loop boundary；
- Deflate 或其他有界 transform；
- side table；
- C# 双向代码生成和 `PacketFramework` adapter。

代价是项目必须自己实现 parser、绑定、静态验证、IR、错误诊断和 codegen。不要一开始做完整通用语言；第一版只实现 Packet 13、Packet 20 所需子集，再为 Packet 10 添加 transform/loop 状态。

### 6.4 方案决策

推荐组合：

```text
自定义受限 legacy IDL -> 正式双向 schema / codegen
Kaitai Struct          -> 独立 read-side verifier / 协议文档
Construct              -> Python 快速实验与样本对照
PacketFramework        -> 继续负责 message id 注册、入口和错误传播
```

不建议把 Kaitai、Construct 或 Protobuf 直接当作当前旧 wire 的替代协议。Protobuf 的字段编号和 TLV evolution 很有价值，但它的字段序列化顺序不是当前 Terraria 顺序流的等价表达。

## 7. 与现有 PacketFramework 的兼容接缝

现有运行链保持不变：

```text
MessageFrame
    -> NetMessage
    -> SessionGate
    -> PacketDefinitionRegistry
    -> PacketCodec / CustomCodec
    -> PacketReceived
```

新增框架只在 codec 内部提供一个可选 adapter：

```text
PacketDefinitionRegistry
    -> PresenceAwareCodecAdapter
         -> PacketSnapshot / upper context
         -> SlotGraph compiler output
         -> EncodePlan or DecodePlan
         -> existing PacketCodec result
```

接缝职责：

| 现有 PacketFramework | Slot-DAG 框架 |
|---|---|
| message id 注册和查找 | slot graph、scope 和 relation 编译 |
| codec 调用入口 | EncodePlan / DecodePlan |
| `MessageFrame`/`NetMessage` 承载 | PresencePlan 和布局记录 |
| 外层错误传播 | 字段存在性、依赖和长度诊断 |
| legacy custom codec 生命周期 | 纯 snapshot 到 bytes、bytes 到 records |

概念接口：

```csharp
public interface ICompiledPacketPlan
{
    PacketId PacketId { get; }
    WirePacketBytes Encode(PacketSnapshot snapshot);
    PacketDecodeResult Decode(ReadOnlySpan<byte> payload);
}

public interface IPacketPlanAdapter<TPacket>
{
    PacketSnapshot ToSnapshot(TPacket packet, PacketEncodeContext context);
    TPacket FromDecoded(PacketDecodeResult result, PacketDecodeContext context);
}
```

旧 `PacketCodec`、`PacketDefinitionRegistry` 和外层消息承载不需要知道每个 slot 的 DAG。反向接入时，adapter 把旧 DTO 和上层 context 转成 snapshot；codec 只接触 snapshot、计划和字节。上下文查找和业务副作用不能重新塞回 field encoder。

## 8. 主要风险和对策

| 风险 | 表现 | 对策 |
|---|---|---|
| 把 DAG 当作顺序 | 两个无依赖字段顺序漂移 | IR 显式保存 `WireSequence` |
| 全局 flat mask | 重复 Tile 的 presence 互相污染 | 每个重复 scope 维护 local mask |
| 字段组半出现 | 读取错位或产生非法包 | 编译期和运行期都检查 `PresenceGroup` |
| 依赖图有环 | 无法生成确定计划 | Kahn/topological cycle check，报出环路径 |
| 变长字段预算错误 | offset 或 buffer overflow | `MinBytes/MaxBytes/ActualBytes` 分开记录 |
| 把业务条件放到底层 | codec 访问全局状态和副作用 | resolver 先生成 snapshot，encoder 只写值 |
| RLE 被伪装成普通字段 | 丢失 previous/run state | 使用显式 `StatefulLoop` 和上限 |
| 过早追求通用 IDL | 语言复杂度超过协议收益 | 先锁定 Packet 13/20/10 子集 |

## 9. 原型说明

`Prototypes/slot-graph-ir/slot-dag-prototype.html` 是一个无需构建的单文件逻辑原型，双击即可打开。它包含：

- Packet 13 最小条件 packet；
- Packet 20 重复 Tile 的局部 slot mask；
- Packet 10-like 的 RLE/side-table 计划展示；
- slot DAG、wire sequence、PresencePlan 和 WireLayoutRecord 的可视化；
- encode/decode 的内存内演示；
- 成组字段不一致、局部 mask 隔离和布局 offset 的引导场景。

原型中的 Packet 10 只验证“复杂计划形状”，没有实现 Terraria 正式 Deflate/RLE 全部细节，不能作为生产兼容性证明。

## 10. 验收结论

当前方案通过设计级可行性判断的条件是：

1. Packet 13 的简单条件和成组条件可以由 slot DAG 编译为直接分支。
2. Packet 20 的每个 Tile 可以实例化独立 scoped slot state，而不复制全局图。
3. Packet 10 的 transform、stateful loop 和 side table 能作为显式结构存在，且不被误归类为普通 field edge。
4. 内部 `PresencePlan` 不进入 wire；旧 flags 仍是 wire projection。
5. encoder/decoder 共享静态描述，但不共享同一个有副作用的读写执行模型。
6. `PacketFramework` 的 message-id/codec 接缝不需要承担 slot graph 细节。

实现阶段仍需要独立的 golden-byte、截断输入、异常 mask、循环上限和旧 codec 对照验证；本次用户要求“不编写任何测试”，因此这些验证不在本次原型目录内创建。

## 11. 本地源码依据

- `Prototypes/external-context-wire-submission/src/Protocol/PacketFramework.cs`
- `Prototypes/external-context-wire-submission/src/Protocol/PacketFrameworkConcept.cs`
- `Prototypes/external-context-wire-submission/src/Protocol/Packets/PlayerControlsPacket13Definition.cs`
- `Prototypes/external-context-wire-submission/src/Protocol/Packets/CompressedWorldPacketDefinitions.cs`
- `docs/research/netmessage-complex-examples.md`
- `docs/research/2026-09-07-kaitai-construct-legacy-idl.md`
- `docs/plans/2026-08-06-netmessage-dependency-graph-design-proposal.md`
