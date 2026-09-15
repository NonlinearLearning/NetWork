# Terraria NetMessage / MessageBuffer 复杂包实例与图模型调研

日期：2026-08-06

范围：只读取 `D:\ProjectItem\SourceCode\chain14\Terraria\NetMessage.cs` 和 `MessageBuffer.cs`，抽取可用于依赖图选型的复杂实例；不修改 Terraria 原代码、生成器、codec 或运行时。

## 结论

这些消息不能由“字段节点 + 普通有向边”完整表达。推荐继续采用一张带作用域、带线序、带类型边的有向异构多重图：

- `Block` / `Scope`：字段和子结构的拥有关系；
- `Sequence`：实际 wire order，不能由拓扑排序推导；
- `Gate` / `Presence`：flag 或上下文控制字段存在；
- `Select` / `Value`：按 selector、消息类型或值域选择分支；
- `Loop` / `Shape`：count、面积、sentinel 或 RLE 展开；
- `Transform`：Deflate 等有界转换流；
- `SideTable` / `TailLoop`：压缩正文之后的 chest、sign、tile entity 尾表。

其中 `Presence`、`Shape`、`Value`、`Context`、`Derives` 等依赖关系的子图必须保持 DAG；`Contains` 和 `Sequence` 是同一张图中的其他关系层。

## 源码实例

### Packet 13：多组 BitsByte 控制多个可选字段

发送端 [`NetMessage.cs`](../../../../../../chain14/Terraria/NetMessage.cs) case 13（约 444-509 行）依次写入：

```text
PlayerId
ControlFlags1
ControlFlags2
ControlFlags3
ControlFlags4
SelectedItem
Position
When(ControlFlags2.bit2) -> Velocity
When(ControlFlags2.bit7) -> MountType
When(ControlFlags3.bit6) -> OriginalUsePosition, HomePosition
When(ControlFlags4.bit5) -> NetCameraTarget
```

接收端 [`MessageBuffer.cs`](../../../../../../chain14/Terraria/MessageBuffer.cs) case 13（约 941-1042 行）以同样的顺序读取，并在 `Main.netMode == 2` 时把收到的 player id 投影为 `whoAmI`。这说明图中需要：

- 四个 flag atom；
- 多条 `Presence` 边；
- `PotionOfReturn` 两个字段组成一个共同存在的 `Block`；
- `Context` 边描述服务器端身份覆盖；
- 单独保存 wire order。

### Packet 20：二维 TileRecord 重复体

发送端 case 20（约 539-641 行）写入位置、宽高和 change type，然后以 `x` 外层、`y` 内层遍历矩形。每个 tile 先写三个 flag 字节，再按条件写 color、wall、type、frame、wall、liquid 等字段。

接收端 case 20（约 1315-1423 行）按相同的二维循环读取。`TileType` 读取后再查询 `Main.tileFrameImportant[tile4.type]` 决定是否读取 `FrameX/FrameY`；液体读取还受 flag 和 net mode 共同影响。

图中至少需要：

```text
Width  --Shape--> TileGrid
Height --Shape--> TileGrid
TileGrid --Contains--> TileRecordTemplate
TileType --Value--> RequiresFrame --Presence--> FrameX/FrameY
Flags2.bit2 --Presence--> TileColor
Flags2.bit3 --Presence--> WallColor
Flags1.bit2 --Presence--> WallType
Flags1.bit3 + ServerContext --Presence--> LiquidPayload
```

### Packet 7：世界初始化的固定序列、数组组和大量位图

发送端 case 7（约 225-407 行）先写时间、昼夜和世界尺寸，再写 WorldId、字符串、Guid、世界生成版本、背景样式、风速、云量和多个固定数组，随后写十六组左右的 `BitsByte` 世界状态，最后写矿层、侵略类型、lobby id、沙尘暴强度和额外出生点数据。

这不是“一个大字段”，而是一个有明确 wire order 的 `WorldInit` block：

```text
WorldInit
  Header atoms
  Fixed arrays: treeX[3], treeStyle[4], caveBackX[3], caveBackStyle[4]
  Packed state blocks: Flags1 ... Flags16
  Tail atoms and ExtraSpawnPoint block
```

图模型需要 `Loop(Exactly(n))`，但不需要把每个数组元素误当成独立的顶层协议字段。

### Packet 8：三字段请求触发区域批量发送（wire 与 effect 必须分层）

Packet 8 的 wire body 实际只有三个字段：`int x`、`int y`、`byte team`。接收端 case 8（约 651-864 行）读取这三个字段后，才在服务器内部校验坐标、计算主区域、请求区域、team spawn 区域和 portal 区域，去重后形成 `List<Point>`，再对多个矩形循环调用 `NetMessage.SendSection`。

因此它不能把下面的处理循环直接放进 Packet 8 的 wire layout graph。正确的分层是：

```text
Wire graph:
X:int32 -> Y:int32 -> Team:byte

Effect graph:
SpawnX/SpawnY -> SectionProjector
RequestedX/RequestedY -> OptionalRequestedRegion
TeamSpawnContext -> OptionalTeamRegion
PortalSections -> TailRegionList
All regions -> Deduplicate(PointSet)
PointSet -> Loop(SendSection)
```

effect graph 可以记录 `Emits(Packet10/21/23/54)` 和 `Policy` 边，但不能让这些边参与 wire codec 的顺序或依赖校验。

### Packet 21 / 90 / 145 / 148：同一基础结构的 profile 分支

发送端 case 21、90、145、148（约 642-670 行）共享物品基础字段：索引、位置、速度、stack、prefix、保留标志和 type，但：

- `145` 追加 `shimmered` 和 `shimmerTime`；
- `148` 追加敌人拾取冷却时间；
- `90` 与客户端保留/实例化语义有关；
- `151` 则是只发送 item index 的删除/释放消息。

接收端 case 21/90/145/148（约 1425-1531 行）按消息 id `b` 选择 profile，并在 client/server 两种 `netMode` 下采取不同的物品 materialization 和 rebroadcast 行为。

图模型需要：

```text
MessageId -> Select(ItemProfile)
ItemProfile -> BaseItemBlock
Profile145 -> ShimmerBlock
Profile148 -> PickupCooldown
NetMode -> ApplyPolicy / RebroadcastPolicy
```

### Packet 23：NPC 状态的位图、稀疏 AI 数组和变宽生命值

发送端 case 23（约 684-760 行）写入基础位置、速度、target，然后：

- Flags1 的 bit2-bit5 表示哪些 `ai[i]` 非零；
- 只有对应 bit 为真时才写入 AI 浮点值；
- Flags2 控制多人难度缩放、spawn state 和 shimmer transparency；
- `life == lifeMax` 时不写完整生命值；
- 否则写一个宽度 tag，再以 `sbyte`、`short` 或 `int` 写生命值；
- 对 catchable NPC 追加 `releaseOwner`。

接收端 case 23（约 1569-1697 行）按 flags 读取稀疏 AI，按宽度 tag 使用 `switch` 读取生命值，并根据 type 和 spawn flags 决定是否重新初始化 NPC。

该实例需要：

```text
NPCFlags.ai[i] --Presence--> AI[i]
NPCFlags.fullLife --Gate--> LifePayload
LifeWidthTag --Select--> SByteLife | Int16Life | Int32Life
NPCType --Context/Value--> Catchable -> ReleaseOwner
SpawnFlags --Context--> ResetOrReuseNPC
```

这已经超出单一 Presence edge，需要 `Select` 和数组索引 scope。

### Packet 27：Projectile 的双层 flags 和 UUID 条件

发送端 case 27（约 773-847 行）写入 identity、位置、速度、owner、type，随后用两个 flag 字节控制：

```text
Flags1.bit0 -> ai[0]
Flags1.bit1 -> ai[1]
Flags1.bit2 -> Flags2
Flags1.bit3 -> bannerId
Flags1.bit4 -> damage
Flags1.bit5 -> knockBack
Flags1.bit6 -> originalDamage
Flags1.bit7 -> projUUID
Flags2.bit0 -> ai[2]
ProjectileType -> NeedsUUID -> Flags1.bit7
```

接收端 case 27（约 1716-1810 行）使用 owner + identity 查找或分配 projectile slot，并在服务器端根据 type 和 hostile 集合做门禁。这同时包含结构依赖和安全/路由上下文，不应混成一条普通字段边。

### Packet 50 / 54：sentinel-terminated repeated list

发送端 case 50 和 54（约 999-1042 行）遍历固定容量的 buff 数组，只写有效项，最后写 `ushort 0` 结束。接收端 case 50/54（约 2473-2611 行）以 `while (ReadUInt16() > 0)` 读取，随后清空剩余槽位。

这不是 `Count(field)` 数组，也不是 `Width * Height` shape。图模型应有明确的：

```text
Loop(kind = Until, terminator = 0, max = Player.maxBuffs/NPC.maxBuffs)
```

### Packet 65：bit 选择传送目标、类型和可选参数

发送端 case 65（约 1092-1112 行）用四个 bit 同时编码 NPC/player 目标类别、teleport cause、服务器标志和可选 `number7`。接收端 case 65（约 2964-3048 行）将 bit 0/1 解码为 target kind，bit 2 决定是否使用玩家当前位置，bit 3 决定是否读取额外 int，随后用 `switch (num136)` 分派到 player、NPC 或确认分支。

它需要：

```text
TeleportFlags -> TargetKind
TargetKind --Select--> PlayerTarget | NpcTarget | AckTarget
TeleportFlags.bit3 --Presence--> ExtraInt
TeleportFlags.bit2 --ContextOverride--> Position
```

### Packet 10：Deflate + Tile RLE + side tables

发送端 `CompressTileBlock` / `CompressTileBlock_Inner`（约 1906-2247 行）先用 Deflate 包裹 `(xStart, yStart, width, height)`，再按 `y` 外层、`x` 内层二维扫描；这与 case 20 的 `x` 外层、`y` 内层顺序不同，不能只保存 `Width` 和 `Height` 而省略 iteration order：

- 相邻等价 tile 且允许 batching 时累计 run length；
- tile type、frame、color、wall、liquid、wire、slope、visibility 等由多级 flag 控制；
- type 大于 255 时扩展为两字节；
- run length 为 0、短长度或长长度时使用不同编码；
- chest、sign、tile entity 不全部塞入正文，而是收集到三个尾随表。

接收端 `DecompressTileBlock_Inner`（约 2249-2460 行）维护 `previous tile` 和 `pending run` 状态，按 flag 层级读取 tile，然后依次读取 chest/sign/entity 数量和记录。

图模型不能把它当成 case 20 的简单升级，至少需要：

```text
Transform(Deflate)
  -> Loop(Area(width, height), state = previousTile/pendingRun)
  -> TailLoop(ChestTable)
  -> TailLoop(SignTable)
  -> TailLoop(TileEntityTable)
```

`Transform` 和 `Loop` 的状态属于一次解析实例，不应污染不可变的静态定义图。

### Packet 34：动作码驱动的 union-like 分派

发送端 case 34（约 917-932 行）固定写入 action、坐标、tile type、style 和服务器侧 chest index。接收端 case 34（约 2005-2177 行）按 action byte 分派到 chest、dresser 或特殊 chest 的放置/销毁逻辑，但这些分支的 wire 字段形状不变。

这类消息适合：

```text
Wire graph: ActionCode, X, Y, Style, ChestId
Effect graph: ActionCode -> Select(PlaceChest | KillChest | PlaceDresser | KillDresser | SpecialChest)
```

不要把每个 action 的业务处理误认为新的 wire variant；这里的 `Select` 属于 effect-level dispatch。只有分支字段形状不同的时候，才在 wire graph 中建立 `Select`。

## 网络项目对照

### Kaitai Struct

- [User Guide](https://doc.kaitai.io/user_guide.html)
- [Conditionals](https://doc.kaitai.io/user_guide.html#_conditionals)
- [Repetitions](https://doc.kaitai.io/user_guide.html#_repetitions)
- [Attributes in other types](https://doc.kaitai.io/user_guide.html#_accessing_attributes_in_other_types)
- [Official compiler README](https://raw.githubusercontent.com/kaitai-io/kaitai_struct_compiler/master/README.md)

Kaitai 是最接近 Terraria 这类顺序二进制结构的参考：条件、重复、switch、子类型和表达式都被声明式描述。它仍然以结构化布局为中心，所以本项目应借鉴语义，再导出类型化依赖图。

### Construct

- [Construct documentation](https://construct.readthedocs.io/en/latest/)

Construct 将有序 `Struct`、上下文依赖数组、`If`、`Switch` 和 parse/build 对称性放在同一声明体系。它适合验证“图不能只保存边，还要保留局部顺序和上下文表达式”的判断。

### binrw

- [binrw crate documentation](https://docs.rs/binrw/latest/binrw/)
- [binrw source repository](https://github.com/jam1garner/binrw)

binrw 的官方文档展示了 `#[derive(BinRead, BinWrite)]`、`count`、`if`、`magic`、`assert` 和 `try_calc` 等属性。它是一个很好的实现侧参考：

- `count` 对应 `Loop(Count(source))`；
- `if` 对应 `Gate/Presence`；
- `magic` 和 `assert` 对应 `Constraint`；
- 结构体字段顺序对应 `Sequence`。

### Protocol Buffers

- [Encoding guide](https://protobuf.dev/programming-guides/encoding/)
- [proto3 language guide](https://protobuf.dev/programming-guides/proto3/)

Protocol Buffers 使用 field number + wire type 的 TLV 记录，支持 repeated、oneof 和未知字段跳过。它适合做兼容性和 variant 的反例，但不适合作为 Terraria case 20 的直接模型：字段在 wire 上不是固定声明顺序，`oneof` 的语义也不同于 flag 控制的连续可选字段。

### FlatBuffers

- [Schema language](https://flatbuffers.dev/schema/)
- [Internals](https://flatbuffers.dev/internals/)

FlatBuffers 提供 vectors、optional scalars、unions 和 verifier，适合借鉴 `Vector`、`Union` 和存在性验证；但其 table/offset 布局不是 Terraria 这种严格按字段顺序写入的流式 packet，因此只能作为形状和 variant 的参考。

### MLIR

- [Language Reference](https://mlir.llvm.org/docs/LangRef/)

MLIR 将 graph-like Operation/Value、Region/Block 层次和 block 内顺序分离。它不应直接作为协议格式，但适合借鉴“scope、sequence、dataflow 三层关系不混淆”的图组织方式。

### Graphviz DOT

- [DOT language](https://graphviz.org/doc/info/lang.html)

DOT 适合作为图的可视化输出，不提供协议依赖语义、wire order 校验或 repeat 边界校验。

## 当前模型的决定

推荐节点类型保持小而稳定：

```text
Atom / Field
Block / Scope
Gate / Predicate
Select
Loop
Transform
Context
SideTable
```

推荐边类型：

```text
Contains
Sequence
Presence
Shape
Value
Context
Derives
```

依赖子图必须无环；`Sequence` 不能用拓扑排序替代；二维、sentinel 和 RLE 必须是不同的 Loop boundary；Deflate 和尾随 side table 必须显式建模。

## 证据边界

- 本文依据本地源码行和上述官方/一手资料；没有使用 GitHub star 数量作为证据。
- 本文是模型研究，不是生成器设计实现；没有修改原 Terraria 文件或运行时。
- 发送端和接收端都已读取对应分支，涉及业务副作用的描述只作为上下文/门禁证据，不等于建议将业务副作用塞入图中。
