# Kaitai Struct、Construct 与自定义 legacy IDL 调研

日期：2026-09-07  
范围：评估三种二进制协议描述方法是否适合当前 Terraria 旧式网络协议，以及它们与仓库现有 `PacketFramework` / `Concept` 的接缝。  
研究方式：阅读 Kaitai Struct 与 Construct 的官方文档、官方源码仓库说明，并对照本仓库的协议定义和既有复杂包研究。本文是设计调研，不授权替换现有实现。

## 结论摘要

三者不是同一层面的替代品：

| 方案 | 主要形态 | 双向读写 | 复杂旧 wire | C# 原生接入 | 最适合的角色 |
|---|---|---:|---:|---:|---|
| Kaitai Struct | 外部 `.ksy` DSL + 代码生成 | 官方资料明确偏向解析器生成；不能假定有对称 builder | 中高，特殊结构可扩展 | 高，官方 compiler 支持 C# target | 反向解析、协议文档、入站 verifier |
| Construct | Python 内的声明式组合器 | 强，官方定位为 symmetrical parser and builder | 高，但复杂处会使用 context lambda / custom construct | 低，核心运行时和编译输出都是 Python | 快速 codec 原型、字节样本验证 |
| 自定义 legacy IDL | 项目自有声明语言 + 语义分析 + IR/生成器 | 可以按项目需要设计为对称 | 最高，可以原生表达 Terraria 特例 | 最高，可直接落在 .NET/C# | 正式 wire schema 和长期生产实现 |

我的建议不是三选一地替换现有代码，而是分工：

```text
自定义 legacy IDL       作为正式的双向 wire 事实源
Kaitai Struct            作为读侧独立校验器和反向分析工具
Construct                作为 Python 快速实验台和 golden-byte 对照工具
PacketDefinitionRegistry 继续作为运行时 message-id 分派入口
Packet Handler / Effect  继续独立于 wire schema
```

如果当前只允许落地一个方案，优先选择**受限的自定义 legacy IDL**，但先只覆盖 Packet 13、Packet 50/54 和 Packet 10 三种不同复杂度，不要一次性实现完整语言。

## 1. 当前协议对方案的要求

当前正式模型仍是 `PacketDefinitionBuilder<TPacket>` + `PacketCodec`，条件和字段读写主要通过委托表达；复杂包可以通过 `IPacketCustomCodec<TPacket>` 绕过默认字段引擎。

本地证据：

- [`PacketFramework.cs`](../../Core/Protocol/PacketFramework.cs) 中的 `PacketConditionDefinition`、`PacketDefinitionBuilder`、`IPacketCustomCodec` 和 `PacketCodec`。
- [`PlayerControlsPacket13Definition.cs`](../../Core/Protocol/Packets/PlayerControlsPacket13Definition.cs) 使用四组 flag 和条件字段。
- [`CompressedWorldPacketDefinitions.cs`](../../Core/Protocol/Packets/CompressedWorldPacketDefinitions.cs) 的 Packet 10 使用 Deflate custom codec。
- [`netmessage-complex-examples.md`](netmessage-complex-examples.md) 和 [`2026-08-06-netmessage-dependency-graph-design-proposal.md`](../plans/2026-08-06-netmessage-dependency-graph-design-proposal.md) 对 Packet 8、10、13、20、23、27、50/54 的 wire / effect 区分。

任何候选方案至少需要回答这些问题：

1. 是否能把声明顺序当成真实 wire 顺序，而不是依赖拓扑排序。
2. 是否能表达 `BitsByte` 控制的可选字段和共同存在的 block。
3. 是否能表达按值选择的分支，例如 Packet 23 的变宽生命值和 Packet 27 的多级 flags。
4. 是否能区分 fixed count、count-based、area、sentinel 和 RLE/stateful loop。
5. 是否能把 Deflate 的有界子流、Tile RLE 和尾随 table 分开表示。
6. 是否能产生入站和出站一致的 codec，或者明确承认自己只负责读取。
7. 是否能把 Packet 8 的区域计算、发送和 Packet 34 的业务 action dispatch 留在 wire schema 外。
8. 是否能提供版本、预算、错误路径、wire offset 和字节 fixture 的验证接缝。

关键判断是：**方案可以不用图作为作者模型，但不能丢失这些关系的语义。** 依赖关系可以由有序树、符号表、状态机或指令表表达；编译器内部仍可以派生依赖索引来做循环和前置引用检查。

## 2. Kaitai Struct

### 2.1 官方定位

Kaitai Struct 官方 compiler README 将它定位为一种声明式语言，用于描述文件或内存中的二进制数据结构，包括 network stream packet formats；`.ksy` 描述随后由 compiler 翻译成目标语言的解析库。官方 User Guide 还提供 `seq`、自定义 `types`、条件、重复、`switch-on`、实例和处理流等章节。

来源：

- [Kaitai Struct compiler README](https://github.com/kaitai-io/kaitai_struct_compiler#readme)
- [Kaitai Struct User Guide](https://doc.kaitai.io/user_guide.html)
- [条件字段](https://doc.kaitai.io/user_guide.html#_conditionals)
- [重复结构](https://doc.kaitai.io/user_guide.html#_repetitions)
- [按值切换类型](https://doc.kaitai.io/user_guide.html#tlv)
- [流处理和压缩/解密入口](https://doc.kaitai.io/user_guide.html#process)
- [opaque types / custom processing](https://doc.kaitai.io/user_guide.html#opaque-types)

Kaitai 的基本工作流是：

```text
.ksy binary-format description
    -> kaitai-struct-compiler
    -> C++ / C# / Java / JavaScript / Perl / PHP / Python / Ruby 等目标代码
    -> 解析 bytes / stream
```

官方 compiler README 的产物描述是包含 parser 的源文件；User Guide 的工作流也围绕 parsing 展开。当前一手资料没有定义与 Construct `build()` 对等的官方序列化/写入工作流，因此不能把 Kaitai 当成已经具备双向 outbound codec 的方案。若要用它生成出站包，需要另做 builder、手写 writer，或从 `.ksy` 派生第二个写入器；这应作为 PoC 的明确验收项，而不是默认能力。

### 2.2 能表达当前协议的部分

Kaitai 对以下结构有直接表达能力：

| 当前结构 | Kaitai 机制 | 评价 |
|---|---|---|
| 固定字段和 wire 顺序 | `seq` | 强；列表顺序就是解析顺序 |
| `BitsByte` | `b1` 等 bit-sized integer，或 byte 后用表达式 | 可行；需要约定 bit order 和命名方式 |
| flag 控制的可选字段 | 字段上的 `if` | 强；条件表达式引用前面字段 |
| 类型/ profile 分支 | `switch-on` + `cases` | 强；适合 Packet 21/90/145/148、Packet 23 部分分支 |
| 固定次数数组 | `repeat: expr` | 强 |
| 宽高决定的重复体 | `repeat: expr`，例如 `width * height` | 可行，但结果通常是 1D array，x/y 映射和扫描顺序需要明确写出 |
| EOF 重复 | `repeat: eos` | 强 |
| sentinel 重复 | `repeat: until` | 可行；官方示例中 terminator 仍会进入结果数组，需要由模型或后处理决定是否保留 |
| 变长 byte 区域 | `size` / `size-eos` / terminator | 强 |
| 压缩或预处理 | `process`，也支持 custom processing | 可行；具体 Deflate/RLE 仍需确认目标语言 runtime 和边界行为 |
| 固定值、范围和表达式校验 | `valid` | 强；适合基础 structural validation |
| 外部特殊类型 | opaque type | 可行，但会留下目标语言外部实现接缝 |

例如 Packet 13 的局部结构可以自然写成如下形态：

```yaml
seq:
  - id: control_flags_2
    type: u1
  - id: position
    type: vector2
  - id: velocity
    type: vector2
    if: (control_flags_2 & 4) != 0
  - id: mount_type
    type: u2le
    if: (control_flags_2 & 128) != 0
```

这段是 Kaitai 风格的示意，不是当前仓库的已批准 schema。它能表达 `ControlFlags2 -> Velocity/MountType`，但对于 Packet 13 中两个药水位置必须共同出现的 block，最好定义一个可选的子 `type`，不要把两个独立 `if` 写成允许半组出现的形式。

Packet 50/54 可以用 `repeat: until` 表达：

```yaml
seq:
  - id: buffs
    type: buff_record
    repeat: until
    repeat-until: _.buff_id == 0
```

但这里必须额外声明：

- terminator 是否应该进入 DTO；
- 最大项数和最大字节预算是多少；
- writer 如何生成终止项；
- 终止项后是否还有尾随字段。

Kaitai 官方语义默认把满足条件的最后一个元素也加入数组；当前 Terraria DTO 通常更适合把 terminator 作为 wire control，而不是作为业务 buff。

### 2.3 Kaitai 对当前协议的硬缺口

#### 缺口一：出站 codec 不应假定存在

这是 Kaitai 与当前项目最重要的差异。当前 `PacketDefinitionRegistry` 需要入站读取，也需要出站写入；Kaitai 官方 compiler 的一手定位是生成 parser。可以使用 Kaitai 作为入站 verifier，但不能仅凭 `.ksy` 编译成功就声称已经完成 PacketDefinition 的双向替代。

#### 缺口二：`process` 不是完整的 Terraria Transform 模型

Kaitai 的 `process` 是对 bytes 或 user type 做预处理，然后交给正常结构解析；官方文档也提供 custom processing routine 接缝。它能够帮助描述：

```text
bounded bytes
    -> Deflate/custom process
    -> clear substream
    -> nested type
```

但 Packet 10 还要求：

```text
Deflate
    + y-outer / x-inner iteration order
    + previousTile / pendingRun 状态
    + RLE 短/长编码
    + chest/sign/tile-entity tail tables
```

这些状态和多个 tail loop 仍需要自定义 type、custom processor 或外部 C# 逻辑。此时 Kaitai 仍然是有价值的结构描述，但不能把自定义实现隐藏后统计为完整自动生成。

#### 缺口三：Effect 不属于 Kaitai wire schema

Packet 8 收到 `x`、`y`、`team` 后计算区域、去重并发送 section；Packet 34 根据 action 执行 chest 等业务操作。这些不是字段解析或序列化，不能塞进 `.ksy` 的 wire 描述中。需要独立的 C# handler / Effect seam：

```text
.ksy decode
    -> typed DTO
    -> explicit application handler
    -> world state / response / rebroadcast
```

#### 缺口四：表达式可读，但不等于项目级静态契约

Kaitai 的表达式比当前 `Func<TPacket, bool>` 更可见，也能被 compiler 检查类型。但项目仍需要额外的规则来检查：

- 条件只能引用已读取字段或显式 context；
- loop 有最大项数、最大面积和最大解压预算；
- 所有子流完全消费；
- identity / session / permission 不被 schema 读取为业务事实；
- schema revision 与 Terraria wire fixture 绑定。

Kaitai compiler 不会替当前项目决定这些 Effect、session 和安全政策。

### 2.4 Kaitai 的适用定位

Kaitai 很适合作为：

1. 由旧 `NetMessage.cs` / `MessageBuffer.cs` 反向整理出的**读取规范**。
2. 独立于 C# 主 codec 的入站字节 verifier。
3. 协议文档和 packet field inspector 的来源。
4. 对 Packet 13、20、23、27、50/54 做读侧 golden fixture 验证。
5. 对 opaque packet 逐步替换时的中间工具。

它不适合直接作为：

1. 当前项目唯一的双向 wire codec 来源。
2. Packet 8 / 34 的业务 Effect 描述。
3. session gate、身份覆盖、权限、rebroadcast 的实现。
4. Packet 10 全部 RLE 和 tail table 的无代码自动生成保证。

## 3. Construct

### 3.1 官方定位

Construct 官方文档将它描述为声明式且对称的 binary data parser and builder。它使用 Python 对结构进行组合，既可以把 bytes 解析为 Python 对象，也可以把对象构建为 bytes；官方列出的能力包括 fields、Struct/Sequence、bit/byte 处理、adapters、arrays、context-based computation、If/Switch、lazy parsing、pointers 和 compression。

来源：

- [Construct Introduction](https://construct.readthedocs.io/en/latest/intro.html)
- [Construct Basics](https://construct.readthedocs.io/en/latest/basics.html)
- [Conditional API：If / Switch / Union](https://construct.readthedocs.io/en/latest/api/conditional.html)
- [Repeater API：Array / GreedyRange / RepeatUntil](https://construct.readthedocs.io/en/latest/api/repeaters.html)
- [Bitwise API](https://construct.readthedocs.io/en/latest/bitwise.html)
- [Tunneling API：Compressed / Restreamed 等](https://construct.readthedocs.io/en/latest/api/tunneling.html)
- [Stream manipulation](https://construct.readthedocs.io/en/latest/streaming.html)
- [Compilation](https://construct.readthedocs.io/en/latest/compilation.html)
- [Extending Construct](https://construct.readthedocs.io/en/latest/extending.html)

Construct 的主要形态是：

```text
Python Construct object
    -> parse(bytes)
    -> Python container / typed value

Python Construct object
    -> build(value)
    -> bytes
```

这与当前 `PacketCodec.Read` / `PacketCodec.Write` 的需求更接近。

### 3.2 能表达当前协议的部分

| 当前结构 | Construct 机制 | 评价 |
|---|---|---|
| wire 顺序 | `Struct` / `Sequence` | 强；组合顺序就是执行顺序 |
| flag / bit | `BitStruct`、`Bitwise`、`Flag` 等 | 强，但需要确认 bit order 与 Terraria 定义一致 |
| optional 字段 | `If` / `Optional` | 强 |
| if/else | `IfThenElse` | 强 |
| value selector | `Switch` | 强；由 context lambda 选择 case |
| union / 多视图 | `Union` | 可行，但不能误认为普通 Packet profile |
| fixed count | `Array` | 强 |
| sentinel | `RepeatUntil` | 强；官方语义是直到 predicate 为真，最后元素默认包含在结果中 |
| 宽高矩形 | `Array(this.width * this.height, subcon)` | 可行，但仍是线性 array；坐标映射需由应用或 adapter 定义 |
| Deflate / bounded transform | `Compressed`、`Prefixed`、`FixedSized`、`Restreamed` 等 | 强，但要正确限定子流，不能让压缩 codec 消费整个 packet |
| 类型表示转换 | `Adapter` / `SymmetricAdapter` | 强，适合 wire value 与领域 value 之间的转换 |
| 自定义特殊行为 | custom `Construct` 或 adapter | 可行，但会重新引入手写实现 |

Packet 13 的 Construct 风格示意：

```python
packet13 = Struct(
    "flags1" / Byte,
    "flags2" / Byte,
    "player_id" / Byte,
    "selected_item" / Int16ul,
    "position" / Vector2,
    "velocity" / If(this.flags2 & 0x04, Vector2),
    "mount_type" / If(this.flags2 & 0x80, Int16ul),
)
```

Packet 23 的宽度选择可以使用：

```python
health = Switch(
    this.health_width,
    {
        1: Int8sl,
        2: Int16sl,
        4: Int32sl,
    },
)
```

Packet 50/54 可以使用 `RepeatUntil`。但和 Kaitai 一样，需要显式处理：

- terminator 是否进入领域列表；
- build 时谁负责补 terminator；
- sentinel loop 的最大长度；
- 解析失败时是 `RepeatError`、`StreamError` 还是项目统一的 `ProtocolFault`。

Packet 10 可以组合 `Compressed` 和 bounded subconstruct，但官方文档特别说明 `Compressed` 处理整个底层 stream；通常需要配合 `Prefixed` 或其他有界 wrapper。当前 `MessageFrame` 已经先把 frame body 放入 byte array，这为在内存中建立有界子流提供了条件，但仍需验证 Deflate header、长度、尾表和 RLE 状态是否与 Terraria 完全一致。

### 3.3 Construct 的硬缺口

#### 缺口一：Python 运行时与 C# 主项目有明显接缝

当前仓库目标是 .NET/C#，正式注册表和服务器管线均在 C# 中。Construct 的官方运行时、context、Construct class 和 compiled output 都是 Python 体系；官方 compilation 文档说明它把 schema 编译成纯 Python module，而且编译的主要目标是性能。

可选接法只有三类：

```text
1. Python 独立 verifier / test tool
2. Python service / subprocess 参与正式 runtime
3. 把 Construct 试验结果手工移植到 C# 自定义 IDL / codec
```

前两种都会扩大部署、错误传播和性能接缝，不适合作为当前服务器的核心 `PacketDefinitionRegistry` 实现。第三种最实际，但 Construct 此时主要承担参考实现作用。

#### 缺口二：context lambda 仍不天然可静态分析

Construct 的 context lambda 很方便，例如 `this.flags2 & 0x04`，但它与当前 `Func<TPacket, bool>` 有相同的设计风险：运行时可以执行，静态工具不一定能可靠提取完整字段依赖。

Construct 自带的 compiled mode 还有明确限制：官方文档指出 compiled classes 只支持 parse/build，部分 lambda、`_index`、某些 context entries、parsed hooks 和调试能力不支持；生成代码也省略了一些检查，不应直接用来解析损坏或不受信任的数据。

因此 Construct 适合作为执行组合器，却不能自动成为当前项目所需的：

```text
field dependency manifest
scope verifier
wire/effect separation checker
version fingerprint binder
```

这些仍需项目外层补齐。

#### 缺口三：复杂 custom Construct 会退化成另一套 CustomCodec

Construct 官方扩展文档提供 Adapter 和 custom Construct。Adapter 适合值层转换；当现有构造器无法表达时，可以实现 `_parse` / `_build` / `_sizeof` 自定义 Construct。

这对 Packet 10 的 RLE、复杂 TileRecord 和尾表很有用，但如果每个 Terraria 特例都写一个 custom Construct，结果会变成：

```text
Python library primitives
    + many project-specific custom classes
    + context lambdas
    + external C# Effect
```

它解决了快速实现问题，却没有自动解决长期的跨语言规范、静态 manifest 和 C# 正式 codec 问题。

### 3.4 Construct 的适用定位

Construct 最适合作为：

1. 快速验证某个 packet 的字节顺序和条件分支。
2. 在迁移前建立第二实现，与 C# codec 做 differential test。
3. 研究 Packet 10 的 bounded Deflate / RLE 分段行为。
4. 对 Packet 23/27 的 selector 和可选字段做交互式实验。
5. 作为自定义 legacy IDL 的执行语义参考。

它不适合直接作为：

1. C# server 运行时的核心 codec 库。
2. 当前项目唯一的协议规范来源。
3. Packet 8 / 34 的业务 Effect 层。
4. 无额外静态约束的安全边界；预算和异常需由项目 wrapper 补齐。

## 4. 自定义 legacy IDL

### 4.1 它不是“再造一个图”，而是定义一个受限语言

自定义 legacy IDL 的目标不是允许任意节点和任意边，而是把当前协议实际需要的结构收敛成少数语法：

```text
Packet
  -> Sequence
       -> Atom
       -> Block
       -> Gate
       -> Select
       -> Loop
       -> Transform
       -> Constraint
```

字段依赖使用显式的符号引用，不使用不可见的 closure：

```text
Flag(flags2, HasVelocity)
Equals(profile, 145)
Count(itemCount)
Area(width, height)
Until(buffId == 0)
```

源码可以是 YAML/TOML 风格的文本 IDL，也可以是受限的 C# builder；建议把文本格式作为审查和版本控制的 source of truth，把 C# records 作为内部 AST/IR，而不是把运行时 delegate 直接放进静态定义。

### 4.2 建议的最小语法

示意：

```text
packet PlayerControls id 13 {
  wire endian little;

  sequence {
    atom u8 PlayerId;
    atom bits8 ControlFlags1;
    atom bits8 ControlFlags2;
    atom bits8 ControlFlags3;
    atom bits8 ControlFlags4;
    atom u16 SelectedItem;
    atom vector2 Position;

    gate flag(ControlFlags2, HasVelocity) {
      atom vector2 Velocity;
    }

    gate flag(ControlFlags2, HasMount) {
      atom u16 MountType;
    }

    gate flag(ControlFlags3, HasPotionOfReturn) {
      block PotionOfReturn {
        atom vector2 OriginalUsePosition;
        atom vector2 HomePosition;
      }
    }
  }
}
```

Packet 50/54：

```text
packet PlayerBuffs id 50 {
  sequence {
    loop sentinel {
      stop_when BuffId == 0;
      max_items PlayerBuffCapacity;
      block BuffRecord {
        atom u16 BuffId;
        atom u8 BuffSlot;
        atom u16 BuffTime;
      }
    }
  }
}
```

Packet 10：

```text
packet TileSection id 10 {
  transform deflate {
    sequence {
      atom i16 StartX;
      atom i16 StartY;
      atom u16 Width;
      atom u16 Height;

      loop area(Width, Height) order(y_outer, x_inner) state(previous_tile, pending_run) {
        block TileRecord {
          atom flags TileFlags;
          select TileRecordShape by TileFlags {
            case RleShort: atom u8 RunLength;
            case RleLong: atom u16 RunLength;
          }
        }
      }

      loop tail ChestRecords max_items ChestCapacity { atom ChestRecord; }
      loop tail SignRecords max_items SignCapacity { atom SignRecord; }
      loop tail TileEntityRecords max_items TileEntityCapacity { atom TileEntityRecord; }
    }
  }
}
```

以上仍是设计示意。Packet 10 的 `TileRecord` 需要根据实际 Terraria wire 逐项补足 flag cascade、type width、frame、color、wall、liquid、wire、slope 和 RLE 规则；不能把一个示意 `TileRecord` 当作完成的协议实现。

### 4.3 自定义 IDL 必须配套语义分析

IDL 的价值不在于换一种语法，而在于把当前 `PacketFramework` 无法检查的规则变成编译期错误：

| 规则 | 目的 |
|---|---|
| `sequence` 顺序固定 | 防止把 wire order 误当成拓扑顺序 |
| 条件只能引用已声明前置字段、静态 metadata 或显式 context | 防止解码时引用未来值 |
| `Contains` / block 形成 scope tree | 防止重复项之间的字段污染 |
| `Select` 分支互斥且有 default/error 策略 | 防止 selector 无分支或多分支 |
| 每个 loop 必须声明 boundary 和 max budget | 防止无限 sentinel loop、面积乘法溢出和恶意包 |
| area loop 必须声明维度和 iteration order | 区分 Packet 10 的 y/x 与 Packet 20 的 x/y |
| transform 必须有输入边界和完整消费规则 | 防止 Deflate 子流吞掉 tail 或父包剩余数据 |
| structural constraint 与 Effect policy 分开 | 防止权限、world write 和 rebroadcast 混进 wire codec |
| source revision、message id、wire fixture 绑定 | 防止 schema 与真实旧协议漂移 |

内部可以保留一个派生 dependency index 或 DAG 用于检查循环，但它不是用户直接编辑的主模型：

```text
legacy IDL source
    -> ordered AST
    -> symbol binding / semantic checks
    -> immutable Protocol IR
    -> derived dependency index
    -> interpreter or generated C# codec
```

### 4.4 运行时接口

自定义 IDL 不应直接生成“包对象自己读写所有状态”的旧式 schema。建议固定以下接缝：

```csharp
public interface IProtocolPlan
{
    ProtocolReadResult Read(ReadOnlySpan<byte> bytes, ProtocolContext context);
    ProtocolWriteResult Write(PacketValue value, ProtocolContext context);
}
```

内部一次读写需要独立的 instance：

```text
ProtocolInstance
  values
  presence decisions
  selector values
  loop state
  wire spans
  diagnostics
  budget counters
```

静态 IDL / blueprint 不允许保存：

- 当前 packet 的字段值；
- `BinaryReader` / `BinaryWriter`；
- session 对象；
- world object；
- `Main.*` 或业务副作用 delegate；
- 未声明的全局上下文。

这样可以让同一个 plan 同时用于：

```text
入站 decode
出站 encode
结构校验
wire trace
文档导出
golden fixture
```

### 4.5 自定义 IDL 的成本和风险

主要成本不是解析文本，而是维护一套真正的协议编译器：

1. grammar/parser；
2. symbol binding 和 scope；
3. expression type checking；
4. loop / transform / budget 语义；
5. C# interpreter 或 source generator；
6. 统一错误模型和 wire offset；
7. 版本和 fingerprint；
8. golden-byte、截断包、额外字节、错误 selector、恶意长度测试；
9. 文档、manifest 和调试 trace；
10. 与 `PacketDefinitionRegistry` 的 adapter。

最大的设计风险是 IDL 逐渐变成“任意 C# 代码的容器”。需要明确拒绝：

```text
任意 lambda
任意整包 read/write delegate
任意 world/session 访问
没有边界的 custom loop
把 Effect 写进 wire sequence
```

特殊 Terraria 行为可以通过具名、受限的 `Atom` / `Transform` adapter 接入，但每个 adapter 必须声明输入输出边界、版本和测试证据。

## 5. 三者在当前协议上的逐包适配

| Packet | Kaitai Struct | Construct | 自定义 legacy IDL |
|---|---|---|---|
| 13 PlayerControls | `seq` + `if` 很合适；共同 block 需用 subtype；适合读侧 | `Struct` + `If` 很合适；可直接 parse/build | `Sequence` + `Gate(Block)` 最清晰，适合第一批正式迁移 |
| 20 AreaTileChange | `repeat: expr` 可表示面积；二维顺序需显式约定；Tile 条件多时表达式较长 | `Array(width * height, subcon)` 可行；适合快速验证 | `Loop(Area)` + state/scope，能把 order 和 tile rule 变成静态契约 |
| 23 NPC | `switch-on` + `if` 可覆盖大量结构；动态 AI 和业务 context 需要外部代码 | `Switch` + context 可快速完成；复杂 lambda 会削弱静态性 | `Select` + nested scope + `ContextRef`，长期最稳 |
| 27 Projectile | 多级条件可写，但 UUID/type 集合和 server policy 需分离 | `If` / `Switch` 可写；业务 policy 仍在 Python 外 | `Select`、嵌套 `Gate`，显式区分 wire 与 policy |
| 50/54 Buffs | `repeat: until` 直接可用；需处理末尾 terminator 语义 | `RepeatUntil` 直接可用；需补 max budget 和统一错误 | `Loop(Sentinel)`，可强制 max items、terminator 和 tail 规则 |
| 10 TileSection | `process` + bounded type 可做读侧；RLE/state/tail 需要 custom | `Compressed` + custom construct 适合原型；C# 接入弱 | `Transform(Deflate)` + `Loop(Area)` + state + TailLoop，最完整 |
| 8 RequestSection | 只描述三字段 wire；Effect 必须在 schema 外 | 同样只做三字段 parse/build | wire `Sequence` + 独立 Effect handler，边界最容易固化 |

## 6. 推荐架构

### 6.1 推荐的主次关系

```text
正式规范：自定义 legacy IDL
        ↓
        Ordered AST
        ↓ semantic analysis
        Protocol IR
        ↓
        C# interpreter / generated codec
        ↓
        PacketDefinitionRegistry adapter

独立验证：Kaitai Struct read-side parser
交互实验：Construct parse/build reference

业务应用：DTO -> explicit PacketHandler / Effect -> world state / response
```

图结构的角色调整为：

```text
IDL / AST 的派生分析视图
```

它可以继续用于：

- 循环依赖检测；
- 字段影响查询；
- manifest 导出；
- AI 检索；
- schema diff；
- 生成文档。

但协议作者只维护有序声明和显式引用，不直接维护可变的任意 edge collection。

### 6.2 推荐的第一批 PoC

不要从全部 162 个 message id 开始。选择三种能覆盖主要语义的 packet：

1. **Packet 13**：验证 `Sequence`、`BitsByte`、`Gate`、共同存在 block、读写对称性。
2. **Packet 50 或 54**：验证 `SentinelLoop`、terminator、max items、截断和额外字节。
3. **Packet 10**：验证 bounded `Transform`、二维 loop 顺序、RLE instance state 和 tail tables。

每个 PoC 必须同时具备：

```text
真实发送字节 fixture
真实接收字节 fixture
encode(decode(bytes)) 的 canonical 规则
截断包测试
尾随字节测试
错误 flag / selector 测试
长度、面积、解压比和循环预算测试
结构描述和 wire span 输出
```

通过这三组 PoC 后，再决定是否把 `PacketDefinitionBuilder` 改成 IDL facade，或保留它作为简单包的薄 adapter。

### 6.3 推荐的选择结论

如果目标是：

| 目标 | 选择 |
|---|---|
| 先把旧协议读懂、生成可视化字段文档 | Kaitai Struct |
| 一两天内验证某个 packet 的 byte layout | Construct |
| 做 Python 侧 differential parser | Construct + Kaitai |
| 正式 C# 双向 codec | 自定义 legacy IDL |
| 复杂 Packet 10/23/27 长期可维护 | 自定义 legacy IDL，必要时允许具名 adapter |
| 业务路由、权限、world apply | 三者都不负责，使用独立 handler / Effect module |

最终推荐：**自定义 legacy IDL 为主，Kaitai 为读侧验证，Construct 为实验和差分参考。** 这不是因为外部工具不够强，而是当前协议同时要求 C# 双向 codec、旧 wire 精确兼容、结构静态检查和业务 Effect 分层，单一外部工具无法同时承担这些职责。

## 7. 不应做的事情

1. 不要把 Kaitai `.ksy` 编译成功当成出站 codec 已完成。
2. 不要把 Construct 的 `parse/build` 成功当成 C# 运行时可以直接使用。
3. 不要把外部工具的 `Switch`、`If` 或 `process` 当成 Packet 8/34 的业务 Effect。
4. 不要让自定义 IDL 退化成允许任意 C# delegate 的脚本语言。
5. 不要把二维顺序、sentinel boundary、RLE state 和 Deflate scope 隐藏在“通用 repeated field”里。
6. 不要把 opaque packet 的 payload 透传统计为结构化协议完成度。
7. 不要在没有真实 wire fixture 的情况下对 Packet 10 的完整兼容性作出承诺。

## 8. 研究边界与验证状态

- 本文使用的是 Kaitai Struct 和 Construct 的官方一手文档、官方 compiler / project 文档，以及本仓库现有源码和设计文档。
- 本文没有把外部工具安装进仓库，也没有修改生产代码、项目引用、协议定义或生成器。
- 当前仓库既有项目引用和构建问题仍需单独修复；本调研不声称当前根项目 build 已通过。
- 外部工具的具体版本、目标语言生成细节和许可证策略在正式引入前仍需锁定版本并做依赖审查。

## 主要来源

### Kaitai Struct

- [Kaitai Struct compiler README](https://github.com/kaitai-io/kaitai_struct_compiler#readme)
- [Kaitai Struct User Guide](https://doc.kaitai.io/user_guide.html)
- [Kaitai conditions](https://doc.kaitai.io/user_guide.html#_conditionals)
- [Kaitai repetitions](https://doc.kaitai.io/user_guide.html#_repetitions)
- [Kaitai switch types](https://doc.kaitai.io/user_guide.html#tlv)
- [Kaitai stream processing](https://doc.kaitai.io/user_guide.html#process)
- [Kaitai opaque types](https://doc.kaitai.io/user_guide.html#opaque-types)

### Construct

- [Construct Introduction](https://construct.readthedocs.io/en/latest/intro.html)
- [Construct Basics](https://construct.readthedocs.io/en/latest/basics.html)
- [Construct Conditional API](https://construct.readthedocs.io/en/latest/api/conditional.html)
- [Construct Repeater API](https://construct.readthedocs.io/en/latest/api/repeaters.html)
- [Construct Bitwise](https://construct.readthedocs.io/en/latest/bitwise.html)
- [Construct Tunneling API](https://construct.readthedocs.io/en/latest/api/tunneling.html)
- [Construct Stream manipulation](https://construct.readthedocs.io/en/latest/streaming.html)
- [Construct Compilation](https://construct.readthedocs.io/en/latest/compilation.html)
- [Construct Extending](https://construct.readthedocs.io/en/latest/extending.html)

### 本仓库

- [`PacketFramework.cs`](../../Core/Protocol/PacketFramework.cs)
- [`PlayerControlsPacket13Definition.cs`](../../Core/Protocol/Packets/PlayerControlsPacket13Definition.cs)
- [`CompressedWorldPacketDefinitions.cs`](../../Core/Protocol/Packets/CompressedWorldPacketDefinitions.cs)
- [`PacketLayoutAstDemo.cs`](../../Concept/test3/PacketLayoutAstDemo.cs)
- [`netmessage-complex-examples.md`](netmessage-complex-examples.md)
- [`2026-08-06-netmessage-dependency-graph-design-proposal.md`](../plans/2026-08-06-netmessage-dependency-graph-design-proposal.md)

