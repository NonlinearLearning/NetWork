# NetMessage / MessageBuffer 协议特征报告

日期：2026-09-08  
状态：源码阅读与 SlotGraph IR 设计依据；不包含 Terraria 源码、生产 codec 或运行时修改。

## 1. 结论先行

`NetMessage.cs` 与 `MessageBuffer.cs` 中的协议不是一张“字段依赖 DAG”。实际代码同时包含：

1. TCP/缓冲区 framing；
2. message id 分派；
3. session/state 门禁；
4. 固定字段和 wire flag；
5. 条件字段、成组字段和 profile 变体；
6. 计数、二维区域、sentinel、RLE 等不同循环边界；
7. Deflate 等字节流转换；
8. 跨迭代解析状态；
9. 正文后的 chest/sign/tile entity side table；
10. 身份覆盖、资源分配、校验、spam 检查和世界状态副作用；
11. 服务器广播、区域过滤和重发下一种消息。

因此，足够强大的 `SlotGraph IR` 应被定义为一个带多个关系域的协议中间表示，而不是单一 DAG：

```text
SlotGraphIR
├── PacketSchema / VariantSchema
├── SlotTable
├── WireSequenceForest
├── ScopeTree
├── DependencyDAG
├── LoopStateMachines
├── ContextBindings
├── TransformNodes
├── SideTableNodes
├── LayoutContracts
└── Effect / Routing Boundaries
```

其中只有受限的 `DependencyDAG` 参与无环校验和计划依赖排序。以下关系不能被伪装成普通 DAG 边：

```text
WireSequence       字节线序
ScopeTree          重复项和局部字段作用域
LoopStateMachine   跨迭代状态、RLE、sentinel 消费
Transform          Deflate 或其他有界子流转换
SideTable          正文之后的独立记录区
EffectBoundary     业务状态变更
RoutingPolicy      接收者选择与重发
```

这个拆分正是解决“上下文混在一起”和“复杂嵌套循环不可见”的核心。字段 encoder/decoder 只消费编译后的计划与快照，不再直接访问 `Main`、`Netplay`、`Tile`、`NPC` 或 `Player`。

## 2. 报告范围和证据边界

本报告逐段阅读了以下两个完整源码文件：

```text
D:\TRbackup\无任何删减通过编译\Terraria\NetMessage.cs
D:\TRbackup\无任何删减通过编译\Terraria\MessageBuffer.cs
```

当前文件规模为：

| 文件 | 行数 | 字节数 | 主要职责 |
| --- | ---: | ---: | --- |
| `NetMessage.cs` | 3,038 | 83,874 | 发送入口、payload 写入、压缩/解压、framing 接收、广播路由 |
| `MessageBuffer.cs` | 4,500 | 121,442 | 缓冲区状态、packet body 读取、session 门禁、状态应用、服务器回播 |

关键入口：

| 位置 | 观察 |
| --- | --- |
| `NetMessage.cs:95` | `SendData`：选择 buffer、预留长度、写入 message id 和 payload、回填长度、发送/广播 |
| `MessageBuffer.cs:133` | `GetData`：连接计时、message id 校验、session 门禁、reader 定位、顶层 switch |
| `NetMessage.cs:1912` | `CompressTileBlock`：建立 Deflate 子流并写入区域头 |
| `NetMessage.cs:1925` | `CompressTileBlock_Inner`：二维 tile 扫描、嵌套 flags、RLE、side table 收集 |
| `NetMessage.cs:2255` | `DecompressTileBlock`：建立 Deflate 解压子流 |
| `NetMessage.cs:2264` | `DecompressTileBlock_Inner`：RLE、嵌套 flags、tail table 解码 |
| `NetMessage.cs:2491` | `ReceiveBytes`：接收分片追加到 read buffer |
| `NetMessage.cs:2519` | `CheckBytes`：按 ushort 长度拆帧、处理半包和剩余字节 |

本文描述的是当前这两个源码文件观察到的行为，不把建议的 SlotGraph、EncodePlan、DecodePlan 或 EffectPlan 误称为现有实现，也不把其他 Concept 文件的设计提议当成 Terraria 源码已经采用的事实。

## 3. 现有调用链：一个方法承载了多个模块的职责

### 3.1 发送路径

源码中的发送路径大致是：

```text
业务调用
  -> NetMessage.TrySendData
  -> NetMessage.SendData
       -> 选择目标 MessageBuffer
       -> writer.Position = 0
       -> 跳过前两个长度字节
       -> 写入 message id
       -> switch(message id) 写 payload
       -> 回填 ushort packet length
       -> 客户端发给服务器，或服务器按 policy 发送给客户端
```

`SendData` 在 payload 写入前还会重写 message profile：

```text
21 + shimmer 状态 -> 145
21 + item.type == 0 -> 151
```

因此 message id 不是单纯的字段值，它还参与 profile 选择。计划生成必须区分：

```text
MessageProfileSelector
PacketPayloadPlan
RecipientSelectionPlan
```

### 3.2 接收路径和 framing

`ReceiveBytes` 负责把网络分片追加到 `MessageBuffer.readBuffer`。`CheckBytes` 反复查看前两个字节表示的 `ushort` 长度：

```text
while (total bytes >= 2)
    length = UInt16(readBuffer[offset])
    if length < 3 -> 错误
    if 当前缓冲区不足一个完整包 -> 保留半包
    else
        GetData(offset + 2, length - 2, out messageType)
        消费整个 frame
        继续处理同一批数据中的后续 frame
```

`GetData` 的 `start` 指向 message id，而不是 frame 的长度开头。frame 的最小结构因此是：

```text
ushort TotalLength
byte   MessageId
bytes  Payload
```

这里的 `TotalLength` 不是任何 Packet body 的字段，不能被放进 Packet 13、20 或 10 的 SlotGraph 里。SlotGraph 外面需要独立的：

```text
FrameEnvelope
FrameDecoder
FrameAssembler
```

`SendData` 在 `NetMessage.cs:1678` 附近检查总长度不超过 `65535`，再把总长度回写到预留位置。`CheckBytes` 还负责异常时清理缓冲状态、移动未消费尾部数据和处理 coalesced frames。这些属于 framing module 的接口契约，不是字段 presence 关系。

## 4. SessionGate 与 PacketDispatch 不属于字段图

`MessageBuffer.GetData` 在进入 `switch (b)` 前已经做了以下工作：

```text
1. 重置客户端或服务器连接的 TimeOutTimer。
2. 从 readBuffer[start] 读取 message id。
3. 检查 message id 是否小于 MessageID.Count。
4. 更新网络诊断读包计数和连接状态计数。
5. 根据服务器连接 State 判断是否 BootPlayer。
6. 确保 reader 已创建并把 BaseStream.Position 定位到 start + 1。
7. 进入 packet switch。
```

这形成一个独立的会话门禁层：

```text
FrameEnvelope
    -> SessionGate
    -> PacketDispatch
    -> Packet DecodePlan
```

它不能被表达为：

```text
PlayerId --Presence--> Velocity
```

因为 `whoAmI`、`Netplay.Clients[whoAmI].State`、`Main.netMode` 和 `ServerSideCharacter` 不是 packet body 中的字段。IR 可以声明这些只读依赖，但应该把它们建成带 phase/purity/authority 元数据的 `ContextBinding`，而不是隐式捕获全局对象。

## 5. 消息 ID 全量范围

### 5.1 接收侧顶层 message id

基于 `MessageBuffer.GetData` 的顶层 `case`（排除内部嵌套 switch 的 case）得到：

```text
1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45,
46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60,
61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75,
76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90,
91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104,
105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117,
118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130,
131, 132, 133, 134, 135, 136, 137, 139, 140, 141, 142, 143, 144,
145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157,
158, 159, 160, 161
```

也就是说，当前接收 switch 覆盖 `1..161`，但没有 `138`，总数为 160 个顶层 message id。`15、25、26、44、67、83、93` 等分支在当前代码中显式忽略或 no-op；它们仍然是合法分派项，不能因为没有 payload 逻辑就从 registry 中静默删除。

### 5.2 发送侧 message id

`NetMessage.SendData` 的 payload switch 是接收集合的子集，主要为：

```text
1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 18, 19, 20,
21, 22, 23, 24, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 38, 39, 40,
41, 42, 43, 45, 46, 47, 48, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
60, 61, 62, 63, 64, 65, 66, 68, 69, 70, 71, 72, 73, 74, 76, 77, 78,
79, 80, 81, 84, 85, 86, 87, 88, 89, 90, 91, 92, 95, 96, 97, 98, 99,
100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 112, 113, 115,
116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 130,
131, 132, 133, 134, 135, 136, 137, 139, 140, 141, 142, 145, 146, 147,
148, 149, 150, 151, 152, 153, 155, 156, 157, 158, 159, 160, 161
```

当前扫描结果没有发现“只发送、不接收”的顶层 case。接收侧相对于发送侧额外存在：

```text
6, 15, 25, 26, 37, 44, 49, 67, 75, 82, 83, 93, 94,
111, 114, 129, 143, 144, 154
```

这些额外项说明 message registry 不能只从发送器反推：有些消息是服务器/客户端控制通知、事件通知或外部 `NetManager` 模块承载。

## 6. Wire 字段的基本类别

### 6.1 固定字段

固定字段在某个 profile 中每次出现，不需要根据本次实例的 presence 做判断。例如 Packet 13 的：

```text
PlayerId
ControlFlags1
ControlFlags2
ControlFlags3
ControlFlags4
SelectedItem
Position
```

“固定”只表示该 profile 的线上出现性固定，不表示值来自纯函数。发送端仍可能从 `Main.player[number]` 取值，接收端仍可能用 `whoAmI` 覆盖它。因此 IR 至少要分开：

```text
WirePresence = Always
ValueSource  = Snapshot / ContextOverride / Derived
ApplyPolicy  = 独立的 effect plan
```

### 6.2 Packed bits

`BitsByte` 是线上真实字节，不是内部 PresencePlan。常见形态有：

```text
一个 BitsByte 控制若干布尔属性；
一个 bit 控制后续字段是否出现；
一个 flag byte 控制下一个 flag byte 是否出现；
多个 bit 共同组成 enum、slope 或宽度 tag。
```

IR 需要保存 bit projection：

```text
PackedFlagSlot {
    wireType = u8
    bitWidth = 1
    bitOffset = 0..7
    logicalValue = bool / enum / selector
    projection = encode | decode | both
}
```

### 6.3 条件字段

条件字段的条件可能来自四种不同来源，不能都叫 `if`：

| 条件来源 | 例子 | IR 关系 |
| --- | --- | --- |
| 当前包中的 flag bit | Packet 13 `ControlFlags2.bit2` | `FlagProjection -> Presence` |
| 当前重复项中的局部 flag | Packet 20 每个 Tile 的 `Flags2` | `ScopedPresence` |
| selector/value | Packet 23 life width、Packet 65 target kind | `Select` / `WidthSelect` |
| 运行时 context/catalog | `Main.netMode`、`FrameImportant[type]` | `ContextBinding` + `Presence` 或 effect policy |

Packet 20 还证明必须将以下三件事分开：

```text
FlagProjection   线上 flags 的哪一位被置位；
PayloadPresence  这一实例是否真的写 payload；
ValueAvailability 当前 context 是否有合法值可供编码。
```

发送端 Packet 20 中液体 flag 的条件是：

```csharp
tile.liquid > 0 && Main.netMode == 2
```

液体 payload 的写入条件却是单独的：

```csharp
tile.liquid > 0
```

因此源码允许出现“flag 没有声明 liquid，但发送端仍追加 liquid payload”的 legacy 不一致形状，尤其不能在迁移时擅自把两个条件合并。接收端对 flag 导出的本地液体状态又有 `Main.netMode != 2` 的不同处理；Packet 10 还使用另一套 liquid subtype bits。这个事实要求 IR 分开记录 `FlagProjection`、`PayloadPresence` 和 `ValueAvailability`，并让静态校验显式报告二者不等价，而不是替旧协议修正行为。

### 6.4 成组字段

Packet 13 的药水返回位置是一个成组字段：

```text
ControlFlags3.bit6
    -> PotionOriginalUsePosition
    -> PotionHomePosition
```

两个 `Vector2` 必须 all-or-none。允许只存在其中一个字段会使读端错位，也不能表达源码语义。这里应使用：

```text
PresenceGroup(PotionOfReturn)
    members = [OriginalUsePosition, HomePosition]
    predicate = ControlFlags3.bit6
    cardinality = all-or-none
```

### 6.5 自定义 codec

源码中存在 `NetworkText.Serialize`、`PlayerDeathReason.WriteSelfTo`、`NetSoundInfo.WriteSelfTo`、`TileEntity.Write/Read`、`TEDisplayDoll.ReadData`、`TEHatRack.ReadItem`、`NPC.RevengeManager.AddMarkerFromReader` 等 delegated codec。

它们不能被当成透明的 primitive field。`CustomCodecSlot` 至少要声明：

```text
CodecId
InputLogicalType
OutputLogicalType
MinBytes / MaxBytes
ConsumesExactly / Delimited / SentinelTerminated
CanEncode / CanDecode
PureWire / ContextDependent
MayApplyEffect
```

若一个自定义 reader 会直接修改世界对象或连接状态，它应被拆为：

```text
CustomWireCodec -> DecodedRecord -> EffectAdapter
```

而不是让 `DecodePlan` 直接调用业务对象的 mutating reader。

## 7. 条件、选择和 profile 变体

### 7.1 Packet 13：flag-driven optional

Packet 13 的 wire sequence 为：

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

发送端位于 `NetMessage.cs:444` 附近，接收端位于 `MessageBuffer.cs:949` 附近。

这是一个适合验证基础 `PresencePlan` 的包，但它已经要求：

```text
固定 field；
wire flag field；
bit-level presence；
group presence；
发送端状态副作用；
接收端身份覆盖；
server replication。
```

### 7.2 Packet 21/90/145/148：message profile 变体

接收端把四个消息 id 的一部分 body 共享：

```text
ItemId
Position
Velocity
Stack
Prefix
Flags
Type
```

随后 profile 决定尾部：

```text
145 -> shimmered + shimmerTime
148 -> timeLeftInWhichTheItemCannotBeTakenByEnemies
90  -> instanced 语义主要在 ApplyEffect
151 -> 另一个 item removal body
```

发送入口还会在写 payload 前将 `21` 重写成 `145` 或 `151`。因此应使用：

```text
Envelope.MessageId
    -> MessageProfileSelector
        -> ItemBaseProfile
        -> ItemShimmerProfile
        -> ItemPickupCooldownProfile
```

这与同一 profile 内由 flag bit 控制可选字段不同；不能将所有变化塞进一个 `OptionalField` 列表。

### 7.3 Packet 23：稀疏序列和宽度选择

Packet 23 的接收端（`MessageBuffer.cs:1577`）具有：

```text
NpcSlot
Position
Velocity
Target
Flags1
Flags2
for ai[i] in 0..NPC.maxAI:
    Flags1.bit(i + 2) -> ai[i]
DifficultyPlayerCount?  -> byte
DifficultyOverride?     -> float
LifeWidthTag             -> sbyte / short / int
Catchable NPC            -> releaseOwner byte
```

`LifeWidthTag` 不是普通 optional field，而是：

```text
WidthSelect(tag)
    1 -> sbyte
    2 -> int16
    4 -> int32
    other -> legacy fallback
```

接收后还有 NPC slot 重用、`ResetForNewNPC`、`SetDefaults`、位置平滑、boss index 和 `releaseOwner` 应用。这些必须进入 EffectPlan 或 ApplyPolicy。

### 7.4 Packet 27：nested flags 和资源分配

Packet 27 的 wire 部分包括：

```text
ProjectileIdentity
Position
Velocity
Owner
ProjectileType
Flags1
[Flags1.bit2] -> Flags2
[Flags1.bit0] -> ai[0]
[Flags1.bit1] -> ai[1]
[Flags2.bit0] -> ai[2]
[Flags1.bit3] -> bannerId
[Flags1.bit4] -> damage
[Flags1.bit5] -> knockBack
[Flags1.bit6] -> originalDamage
[Flags1.bit7] -> projUUID
```

接收端还做：

```text
服务器 hostile projectile gate；
server owner 覆盖；
按 owner + identity 查找既有 projectile；
否则寻找 inactive slot；
仍没有则找 oldest projectile；
必要时 SetDefaults；
服务器回发 Packet 27。
```

前一组是 wire graph，后一组是 `EntityIdentityPolicy` 和 `ResourceAllocationEffect`。如果把二者合并，IR 会无法判断“读完字节”和“决定写入哪个实体槽位”之间的副作用边界。

## 8. Loop、Scope 和嵌套循环特征

### 8.1 Packet 20：Area loop + 每项局部 scope

发送端 `NetMessage.cs:539` 附近先写：

```text
startX : int16
startY : int16
width  : byte
height : byte
changeType : byte
```

然后使用：

```csharp
for (x = startX; x < startX + width; x++)
    for (y = startY; y < startY + height; y++)
        write TileRecord(x, y)
```

接收端 `MessageBuffer.cs:1323` 使用同样的 `x outer / y inner` 顺序。每个 TileRecord 都重新读取自己的 `Flags1/Flags2/Flags3`，因此必须是：

```text
TileGridScope
  -> TileScope[x,y]
       -> Flags1
       -> Flags2
       -> Flags3
       -> optional Color
       -> optional WallColor
       -> optional Type
       -> optional FrameX/FrameY
       -> optional Wall
       -> optional Liquid/LiquidType
```

不能用一个 packet 级 flat mask：

```text
错误：Tile[0].Color 和 Tile[1].Color 共享一个 ColorPresent
正确：每个 TileScope 都实例化自己的 local presence state
```

建议的循环节点：

```text
AreaLoop {
    widthSource = Width
    heightSource = Height
    iterationOrder = x-outer, y-inner
    itemScope = TileScope
    maxIterations = Width * Height
}
```

### 8.2 Packet 10：扫描方向与 Packet 20 相反

Packet 10 的 `CompressTileBlock_Inner` 和 `DecompressTileBlock_Inner` 使用：

```csharp
for (y = yStart; y < yStart + height; y++)
    for (x = xStart; x < xStart + width; x++)
```

也就是 `y outer / x inner`，与 Packet 20 相反。这个差异必须是 IR 中可查询、可生成的 metadata，不能由一个“二维循环”枚举器默认决定。

### 8.3 Packet 50 / 54：UntilSentinel loop

Packet 50 和 54 的 buff 列表不是 count-prefixed，而是：

```csharp
while ((value = reader.ReadUInt16()) > 0)
    consume value
```

发送端写完有效项后写 `ushort 0`。因此应记录：

```text
SentinelLoop {
    terminator = UInt16(0)
    itemType = UInt16
    maxItems = Player.maxBuffs 或 NPC.maxBuffs
    onTerminator = stop-and-clear-tail
}
```

这里不能误建成 `CountLoop`。静态校验必须要求 sentinel loop 有最大项数，以防畸形 payload 导致无限读取或数组越界。

### 8.4 Packet 7 和固定数组：Exactly loop

Packet 7 包含多个固定数组，如 `treeX[3]`、`treeStyle[4]`、`caveBackX[3]` 和 `caveBackStyle[4]`，还调用 `TreeTops.SyncSend(writer)` 和 `ExtraSpawnPointManager.Write(writer, networking: true)`。

这些属于：

```text
ExactlyLoop(3)
ExactlyLoop(4)
DelegatedBoundedBlock(TreeTops)
DelegatedBoundedBlock(ExtraSpawnPointManager)
```

不能因为它们使用 `for` 就和 Packet 8 的区域 effect loop、Packet 10 的 RLE loop 归为同一类型。

### 8.5 Packet 10：Stateful RLE loop

Packet 10 的 tile body 使用：

```text
previousTile
pendingRun
```

当相邻 tile 满足：

```text
tile2.isTheSameAs(previousTile)
&& TileID.Sets.AllowsSaveCompressionBatching[tile2.type]
```

就增加 run。run 长度又分为：

```text
0       -> 没有重复
短 run  -> 一个 byte
长 run  -> 两个 byte，并设置长 run 标志
```

这不是静态 `Presence`，而是：

```text
StatefulLoop {
    iterationOrder = y-outer, x-inner
    state = [previousTile, pendingRun]
    transition = SameAs(previousTile) && AllowsBatching(type)
    runEncoding = short-or-long
    expandedLimit = width * height
}
```

`previousTile` 和 `pendingRun` 是某次 decode 的实例状态，不应放入不可变的静态 SlotGraph 节点值中。静态图只声明它们的类型、更新时机、消费关系和上限。

## 9. Packet 10：Transform、嵌套 flags 和 SideTable

### 9.1 Deflate 是有界 transform

`CompressTileBlock`：

```text
创建 DeflateStream(CompressionMode.Compress)
写 xStart, yStart, width, height
进入 CompressTileBlock_Inner
```

`DecompressTileBlock`：

```text
创建 DeflateStream(CompressionMode.Decompress)
从压缩子流读 xStart, yStart, width, height
进入 DecompressTileBlock_Inner
```

因此 Packet 10 的 body 不是普通的 `BinaryWriter` 顺序字段，它应当是：

```text
Packet10
  -> DeflateTransform
       -> RegionHeader
       -> StatefulTileLoop
       -> ChestSideTable
       -> SignSideTable
       -> TileEntitySideTable
```

Transform 节点至少要声明：

```text
transformId = Deflate
inputBoundary = current packet payload
outputBoundary = decompressed substream
headerBeforeBody = xStart/yStart/width/height
mustConsume = true
```

### 9.2 多级 flag cascade

Packet 10 的第一个 tile flag byte 可以指示第二个 flag byte 是否存在，第二个可以指示第三个，形成：

```text
Flags1.bit0 -> Flags2
Flags2.bit0 -> Flags3
Flags3.bit0 -> Flags4
```

后续 payload 条件继续引用这些 flag：

```text
Flags1.bit1        -> active + type
Flags1.bit5        -> extended tile type high byte
Flags1.frame bits  -> run length mode
Flags1.wall bits   -> wall / liquid subtype
Flags2              -> wire / slope / actuator / inactive / wire4 / wall high byte
Flags3              -> color / wallColor / invisibility / fullbright / shimmer
Flags4              -> invisible/fullbright extension
```

这里的“flag 是否存在”和“flag 中的 bit 是否激活某字段”是两级依赖，IR 需要有嵌套 scope：

```text
NestedFlagScope(Flags1)
  -> NestedFlagScope(Flags2, presence = Flags1.bit0)
       -> NestedFlagScope(Flags3, presence = Flags2.bit0)
            -> NestedFlagScope(Flags4, presence = Flags3.bit0)
```

### 9.3 正文后的三个 side table

压缩正文结束后，源码按固定顺序写入：

```text
ChestCount
ChestRecord[ChestCount]

SignCount
SignRecord[SignCount]

TileEntityCount
TileEntityRecord[TileEntityCount]
```

发送过程中，扫描 tile 还会通过 `Chest.FindChest`、`Sign.ReadSign` 和 `TileEntityType<T>.Find` 收集对应索引。解码端再通过 `Chest.CreateWorldChest`、`Main.sign`、`TileEntity.Read/Add` 写回世界。

这些不是 TileRecord 的普通可选字段，而是：

```text
SideTableScope {
    ordering = after main body
    countSource = preceding count
    recordType = Chest / Sign / TileEntity
    indexDomain = bounded world collection
}
```

这也是“记录拼接位置和记录开始/结束，再把字节拼接成流”设计必须增加的地方：

```text
WireLayoutRecord.offsetBegin
WireLayoutRecord.offsetEnd
WireLayoutRecord.actualBytes
WireLayoutRecord.maxBytes
WireLayoutRecord.scopeId
WireLayoutRecord.recordKind
```

这些布局记录属于上层内部诊断/组装状态，不写入 Terraria legacy wire；除非未来明确增加新的 framing 扩展，否则不能把它们当成旧协议字段。

## 10. Context 特征：同名“条件”必须分层

源码中的条件至少来自以下 context 类别：

| Context 类别 | 典型来源 | 能否直接进入 Field Encoder |
| --- | --- | --- |
| 传输 context | frame length、reader position、payload remainder | 只能由 frame/reader module 管理 |
| session context | `Netplay.Clients[whoAmI].State`、连接状态 | 作为 `SessionGate` 输入 |
| authority context | `Main.netMode`、`whoAmI` | 作为 declared `ContextBinding` |
| local policy | `Main.myPlayer`、`ServerSideCharacter` | Decode/Apply policy，不是 wire field |
| catalog context | `Main.tileFrameImportant[type]`、`Main.projHostile[type]` | value/presence predicate 或 effect policy |
| world/entity context | `Main.tile`、`Main.npc`、`Main.projectile` | snapshot source 或 ApplyEffect 输入 |
| iteration context | `previousTile`、`pendingRun`、临时 AI array | LoopState，只在运行时实例存在 |
| route context | `SectionRange`、`IsSectionActive`、ignore client | RoutingPlan |

`ContextBinding` 建议结构：

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

静态校验要禁止 `FieldSlot` 直接捕获 `Main` 或 `Netplay`；它只能引用一个已声明的 binding。真正读取全局状态的代码位于 `SnapshotResolver` 或 `ApplyAdapter`，这样才可在不启动完整 Terraria world 的情况下验证 EncodePlan/DecodePlan。

## 11. Apply Effect、Identity Policy 与 Routing Policy

### 11.1 Packet 13 的接收副作用

Packet 13 解码后会：

```text
客户端可能忽略自己的 packet，除非 ServerSideCharacter；
服务器用 whoAmI 覆盖收到的 player id；
更新控制状态、位置和速度；
处理 teleport protection 和 netOffset；
根据 flag 设置/卸载 mount；
设置 PotionOfReturn 位置；
设置 camera target；
服务器在合适状态下重新发送 Packet 13。
```

因此应拆为：

```text
Packet13DecodePlan
Packet13IdentityPolicy
Packet13ApplyEffect
Packet13ReplicationEffect
```

### 11.2 Packet 8 的 wire body 极小，effect 极大

Packet 8 的 wire body 只有：

```text
int x
int y
byte team
```

但接收端 `MessageBuffer.cs:659` 附近在读取后会：

```text
发送 Packet 7；
验证 x/y；
根据 spawnTile 计算默认 section 区域；
根据请求坐标计算额外区域；
根据 team 计算 team spawn 区域；
同步 portal sections；
构造并去重 Point 列表；
发送大量 Packet 10；
同步 item、NPC、projectile 和其他状态；
改变客户端 State。
```

这些循环是 `JoinWorldEffect`，不是 Packet 8 的 wire loop：

```text
Packet8Wire(X, Y, Team)
    -> JoinWorldEffect
         -> RegionProjector
         -> Deduplicate
         -> SendSectionEffect
         -> SyncEntitiesEffect
         -> SessionStateTransition
```

### 11.3 Routing 是 recipient selection，不是 presence

`NetMessage.SendData` 完成 payload 后仍有独立路由分支：

```text
Packet 34 / 69 -> broadcast clients
Packet 20      -> SectionRange 过滤
Packet 23      -> NPC section / skipped sync state
Packet 27      -> projectile section / netImportant / skipped state
Packet 28      -> NPC section 或死亡状态
Packet 13      -> connected broadcast clients
default        -> broadcast 或 State >= 3 的客户端
```

应使用：

```text
EncodePlan
    -> WireAssembler
    -> RecipientSelectionPlan
    -> Transport
```

`RecipientSelectionPlan` 可以引用 packet snapshot 和 server state，但不能修改 payload sequence，也不能参与 FieldSlot 的 dependency DAG。

## 12. 代表性消息特征矩阵

| Packet | Wire 形状 | 关键非普通 DAG 特征 | 主要 Effect/Policy |
| ---: | --- | --- | --- |
| 8 | `X,Y,Team` | wire 极小，读取后生成区域集合 | join、section、实体批量同步、state transition |
| 10 | Deflate + area header + tile body + 3 tail tables | transform、RLE、previous/run state、nested flags、side table | world tile apply、loaded 标记、map update |
| 13 | 固定字段 + 4 个 flags + optional blocks | flag projection、presence group、身份覆盖 | player apply、teleport smoothing、replication |
| 20 | header + `width*height` 个 TileRecord | local scope、x/y nested loop、catalog-gated frame | tile apply、event、frame、rebroadcast |
| 21/90/145/148 | shared item body + profile tail | message profile select、入口重写 | item materialize、reservation、rebroadcast |
| 23 | NPC header + sparse AI + life width select | sparse sequence、width select、catalog gate | slot reuse、defaults、boss/catchable state |
| 27 | projectile header + nested flags | nested flag scope、UUID optional、identity lookup | hostile gate、slot allocation、rebroadcast |
| 34 | action + coordinates + style/id | effect-level action select | chest/dresser place/kill |
| 50/54 | player/NPC id + variable buffs | until-sentinel loop、capacity bound | apply/clear buffs、server rebroadcast |
| 65 | selector flags + target + position + optional int | target-kind select、context override、ack branch | teleport、section check、acknowledgement |

这个矩阵说明：Packet 13 是条件字段的最小闭环；Packet 20 是局部 scope；Packet 10 是 Transform + stateful loop + side tables；其他 packet 用来阻止 IR 退化成只支持三个示例的特化树。

## 13. SlotGraph IR 的建议结构

### 13.1 静态容器

```text
SlotGraphIR {
    ProtocolVersion
    PacketSchemas[]
    MessageProfiles[]
    Slots[]
    Scopes[]
    Sequences[]
    Dependencies[]
    Loops[]
    ContextBindings[]
    Transforms[]
    SideTables[]
    CustomCodecs[]
    EffectBoundaries[]
    RoutingPolicies[]
}
```

建议以不可变 IR 为主；一次 encode/decode 的临时值、presence、cursor 和 loop state 不回写静态图。

### 13.2 Slot 类型

```text
FieldSlot
    具有 wire type、logical type、线序、大小契约和值源。

PackedFlagSlot
    是真实线上 flag byte/bit 的投影。

GateSlot
    用已解析的 flag/context 决定字段或 block 是否存在。

GroupSlot
    约束成员 all-or-none 或指定 cardinality。

SelectSlot
    根据 profile、selector 或 width tag 选择结构/编码分支。

ScopeSlot
    定义 packet、record、iteration、side-table 的局部命名空间。

LoopSlot
    具有 Exactly、Count、Area、Until、RunLength 等明确 boundary。

StateSlot
    描述 previousTile、pendingRun、临时 AI 等迭代状态的类型、更新和上限。

TransformSlot
    描述 Deflate 等有界输入/输出子流。

SideTableSlot
    描述正文之后的 count + records 区域。

CustomCodecSlot
    调用声明过预算、读写能力和 purity 的 delegated codec。
```

### 13.3 关系域

```text
Wire domain:
    Contains
    Sequence
    Presence
    Shape
    Value
    Context
    Transform

Runtime/Effect domain:
    Applies
    Allocates
    Normalizes
    Rebroadcasts
    Emits
    Policy
    SessionTransition
```

只有 `Wire` 域中表示“静态依赖”的那一部分参与依赖 DAG 校验。`Contains` 应形成 scope tree；`Sequence` 应在局部 block 内有唯一顺序；`Effect` 域不能反向改变 wire order。

### 13.4 FieldSlot 大小和布局契约

每一个能进入线性计划的 slot 应声明：

```text
SlotContract {
    slotId
    scopeId
    wireOrder
    wireType
    logicalType
    minBytes
    maxBytes
    presenceExpr
    valueExpr
    dependencies[]
    alignment = none / explicit
    delimiter = none / sentinel / length
    customCodecId?
}
```

一次执行再产生：

```text
WireLayoutRecord {
    slotId
    scopePath
    offsetBegin
    offsetEnd
    actualBytes
    maxBytes
    presence = present / absent
    selectedVariant?
    sourceSpan?
}
```

`maxBytes` 是静态/编译期契约，`actualBytes` 是实例结果，`offsetBegin/End` 是当前拼接过程的诊断信息。它们属于上层内部，不进入 legacy packet 字节流。

## 14. PresencePlan：内部字段存在机制

用户提出的“用 bool 或掩码表示整个数据包哪些字段的数据存在”适合成为上层内部的 `PresencePlan`，但必须与 wire flags 分开：

```text
PresencePlan
    RootMask
    ScopeMasks[ScopePath]
    GroupStates[GroupId]
    SelectedVariants[SelectId]
    LoopShapes[LoopId]
```

### 14.1 Encode 方向

```text
SnapshotResolver
    -> 计算 ContextBinding
    -> 计算 PresencePlan
    -> 校验 Group / Variant / Scope
    -> 将 PresencePlan 投影到 wire flags
    -> 按 WireSequence 写入固定和存在字段
```

对于 Packet 13：

```text
RootMask:
    Velocity = Flags2.bit2
    MountType = Flags2.bit7
    PotionGroup = Flags3.bit6
    CameraTarget = Flags4.bit5
```

但最终线上仍然只出现旧协议规定的 `Flags1..Flags4` 和 payload；`RootMask` 本身不写入。

对于 Packet 20：

```text
ScopeMasks[Tile[0,0]]
ScopeMasks[Tile[0,1]]
ScopeMasks[Tile[1,0]]
...
```

不能把所有 Tile 合并到一个根 mask。

### 14.2 Decode 方向

```text
读取 wire flag
    -> 从 flag projection 恢复 scoped PresencePlan
    -> 按 wire sequence 读取对应字段
    -> 生成 DecodedPacket / DecodedRecord
    -> 交给 IdentityPolicy 和 ApplyEffect
```

decode 过程中 PresencePlan 是“已从 wire 恢复的内部索引”，不是新协议字段。它不应成为 `PacketFramework` 的公共 wire DTO 属性，除非调用方明确需要诊断视图。

## 15. EncodePlan 与 DecodePlan

### 15.1 EncodePlan 的目标

EncodePlan 是把静态 IR 降低成稳定、可审计的线性写入计划：

```text
EncodePlan {
    ResolveProfile
    ResolveContext
    ResolvePresence
    EmitFixed(slot)
    EmitFlagProjection(slot)
    EnterScope(scope)
    Iterate(loop)
    EmitConditional(slot, predicate)
    EmitSelect(select)
    EmitTransform(transform)
    EmitSideTable(table)
    CloseScope(scope)
    FinalizeLayout
}
```

Packet 13 的降低结果概念上应接近：

```csharp
Write(PlayerId);
Write(ControlFlags1);
Write(ControlFlags2);
Write(ControlFlags3);
Write(ControlFlags4);
Write(SelectedItem);
Write(Position);
if (ControlFlags2.Bit(2)) Write(Velocity);
if (ControlFlags2.Bit(7)) Write(MountType);
if (ControlFlags3.Bit(6)) {
    Write(PotionOriginalUsePosition);
    Write(PotionHomePosition);
}
if (ControlFlags4.Bit(5)) Write(NetCameraTarget);
```

这不是要求最终使用字符串生成或解释器，而是说明编译器应把图上的关系降低成直接 branch、loop 和 writer 调用，避免运行时为每个字段创建通用 graph object。

### 15.2 DecodePlan 的目标

DecodePlan 只负责：

```text
读取 cursor
-> 读取固定字段
-> 读取 flags / selectors
-> 建立局部 PresencePlan
-> 执行条件字段读取
-> 执行 loop boundary
-> 执行 transform / side-table 解码
-> 产生 DecodedRecord 和 WireLayoutRecord
```

它不应该负责：

```text
修改 Main.tile / Main.player / Main.npc；
分配 projectile slot；
调用 WorldGen；
决定发给哪些客户端；
调用 NetMessage.TrySendData；
改变 Netplay.Clients[whoAmI].State。
```

### 15.3 EffectPlan

为了让 decode 足够纯，解码结果之后增加：

```text
DecodedPacket
    -> IdentityPolicy
    -> ValidationPolicy
    -> ApplyEffect
    -> ReplicationEffect
    -> RoutingPlan
```

EffectPlan 可以有副作用，但副作用必须显式登记：

```text
EffectBoundary {
    effectId
    inputSlots
    contextBindings
    authority
    writes[]
    emits[]
    failureMode
}
```

这样可以在审计时回答：某个字段是如何从 wire 进入世界状态的；某个回播是由哪个 effect 触发的；某个接收者过滤是否依赖 section state。

## 16. 静态校验规则

### 16.1 图和作用域

1. `slotId`、`scopeId`、`profileId` 全局唯一。
2. 所有关系端点必须存在。
3. `Contains` 形成无环 scope tree。
4. `Sequence` 只能定义同一 block/scope 内的局部顺序。
5. 子 scope 不能隐式读取父 scope 的可变 iteration state；必须显式 `capture`。
6. 一个 record template 的 slot 不能同时被两个不兼容的 wire sequence 使用。

### 16.2 依赖和存在性

1. DependencyDAG 无环；诊断中要输出完整环路径。
2. conditional slot 的 gate 必须引用已声明的 flag、selector 或 ContextBinding。
3. field 在 wire sequence 上必须晚于它依赖的 wire gate/selector；ContextBinding 可以在 sequence 外部提供，但必须声明 phase。
4. `PresenceGroup` 成员必须 all-or-none，或者声明合法 cardinality。
5. `FlagProjection` 与 `PayloadPresence` 不能在未声明 legacy exception 时合并。
6. 每个 scoped field 的 presence 状态必须归属于明确的 scope path。

### 16.3 Loop 和变长数据

1. `Exactly(n)` 的 n 必须是常量或有上限的表达式。
2. `Count(n)` 必须有 count slot、记录类型和总预算。
3. `Area(width,height)` 必须声明维度来源和 iteration order。
4. `Until(terminator)` 必须有最大项数、终止值和畸形输入策略。
5. `RunLength` 必须有 state slots、短/长编码规则和 expanded item 上限。
6. `Transform` 必须声明输入边界、输出边界、完整消费规则和异常策略。
7. Side table 必须声明正文相对位置，不能让 planner 自由重排到正文之前。

### 16.4 字节预算和安全性

1. `minBytes <= maxBytes`。
2. 静态最大值必须覆盖固定字段、flag、delimiter、side table 和 transform header。
3. 运行时 `actualBytes` 不能超过 `maxBytes`。
4. packet/frame 总大小必须经过 `ushort` 预算检查。
5. decode loop 的总消费不能超过 payload remaining。
6. 数组索引、entity slot、tile coordinate、string length 和 custom codec 消费都要有上限。
7. static catalog predicate 不能把未经验证的类型值直接用作数组索引。

### 16.5 Effect 和 routing 隔离

1. Effect edge 不参与 wire dependency cycle check。
2. Routing policy 不得改变 packet payload 的 wire sequence。
3. DecodePlan 不得直接持有可变 world object。
4. custom codec 若声明 `MayApplyEffect`，必须通过 EffectBoundary 运行，不能作为纯 FieldSlot 内联。
5. server identity override 必须被建模为 policy，不能悄悄覆盖 decoded field。

## 17. 与现有 PacketFramework 的兼容接缝

兼容目标是：保留现有 message id 注册、frame 承载和外部错误传播，把 SlotGraph 只放到 packet codec 内部。

建议的接缝：

```text
PacketDefinitionRegistry
    -> LegacyPacketCodecAdapter
         -> PacketSnapshotResolver
         -> CompiledSlotGraphPlan
              -> EncodePlan / DecodePlan
         -> PacketDecodeResult
         -> ApplyEffectAdapter
```

职责划分：

| 现有 PacketFramework | SlotGraph 新模块 |
| --- | --- |
| message id 注册和查找 | PacketSchema / Profile / SlotGraph |
| MessageFrame 或外层 payload 承载 | FrameEnvelope 继续保留 |
| codec 生命周期、异常传播 | DecodePlan 的结构化错误和布局诊断 |
| 现有 packet DTO 或 custom codec | SnapshotResolver / ApplyEffectAdapter |
| packet received 事件 | EffectBoundary 之后的业务事件 |

概念接口：

```csharp
public interface ICompiledPacketPlan
{
    int MessageId { get; }
    PacketWireBytes Encode(PacketSnapshot snapshot, EncodeContext context);
    PacketDecodeResult Decode(ReadOnlySpan<byte> payload, DecodeContext context);
}

public interface IPacketPlanAdapter<TPacket>
{
    PacketSnapshot ToSnapshot(TPacket packet, EncodeContext context);
    TPacket FromDecoded(PacketDecodeResult result, DecodeContext context);
}
```

兼容接缝不应要求 `PacketFramework` 理解每个 slot 的 presence、RLE 或 side table。它只接收一个已编译的计划和标准化的 decode result。Packet 10 的 Deflate/RLE 可以先保留 legacy custom adapter，再逐步替换为原生 `TransformSlot + StatefulLoop`，避免一次迁移所有复杂业务副作用。

## 18. 自定义 legacy IDL 的最小表达能力

如果 SlotGraph IR 是编译器内部目标，legacy IDL 只需要表达能稳定 lowering 到 IR 的受限语法，不需要一开始成为完整通用语言。

### 18.1 Packet 13 示例

```text
packet 13 PlayerControls {
    fixed playerId: u8;
    fixed flags1: bits8;
    fixed flags2: bits8;
    fixed flags3: bits8;
    fixed flags4: bits8;
    fixed selectedItem: u8;
    fixed position: vec2<f32>;

    when flags2.bit(2) {
        field velocity: vec2<f32>;
    }

    when flags2.bit(7) {
        field mountType: u16;
    }

    group potionOfReturn when flags3.bit(6) {
        field originalUsePosition: vec2<f32>;
        field homePosition: vec2<f32>;
    }

    when flags4.bit(5) {
        field netCameraTarget: vec2<f32>;
    }
}
```

其中 `when` 只描述 wire presence；`playerId` 的服务器覆盖、player apply 和 replication 不应写在这个 packet schema 里。

### 18.2 Packet 20 示例

```text
packet 20 TileSquare {
    fixed startX: i16;
    fixed startY: i16;
    fixed width: u8;
    fixed height: u8;
    fixed changeType: u8;

    area tiles(width, height) order x-outer-y-inner {
        scope tile {
            fixed flags1: bits8;
            fixed flags2: bits8;
            fixed flags3: bits8;
            when flags2.bit(2) { field color: u8; }
            when flags2.bit(3) { field wallColor: u8; }
            when flags1.bit(0) {
                field type: u16;
                when catalog.frameImportant(type) {
                    field frameX: i16;
                    field frameY: i16;
                }
            }
            when flags1.bit(2) { field wall: u16; }
            when flags1.bit(3) {
                field liquid: u8;
                field liquidType: u8;
            }
        }
    }
}
```

这里的 `catalog.frameImportant(type)` 必须被编译为显式 ContextBinding 或 resolver predicate；不能让生成的 primitive reader 直接访问 `Main.tileFrameImportant`。

### 18.3 Packet 10 示例形状

```text
packet 10 CompressedTileBlock {
    transform deflate {
        fixed xStart: i32;
        fixed yStart: i32;
        fixed width: i16;
        fixed height: i16;

        stateful-area tiles(width, height) order y-outer-x-inner {
            state previousTile: TileValue;
            state pendingRun: u16;
            record tile {
                nested-flags flags1 -> flags2 -> flags3 -> flags4;
                run-if same-as(previousTile) && catalog.allowsBatching(type);
                ...
            }
        }

        side-table chests: count<u16> ChestRecord;
        side-table signs: count<u16> SignRecord;
        side-table tileEntities: count<u16> TileEntityRecord;
    }
}
```

`...` 不是允许在生产 schema 中逃避定义的语法；它只表示 Packet 10 的完整 tile codec 需要继续逐字段落地。首批设计验收先确认 IR 形状能承载这些结构，再单独验证 golden bytes 和 legacy compatibility。

## 19. 首批验证范围和非目标

首批固定使用：

```text
Packet 13 -> 固定字段 + flag optional + presence group
Packet 20 -> area loop + 每项 scoped presence + catalog value dependency
Packet 10 -> transform + stateful RLE + nested flags + side tables
```

首批应该验证的不是“把所有业务搬进图”，而是：

1. 静态 SlotGraph 能生成无环依赖关系。
2. wire sequence 不依赖全图拓扑排序。
3. Packet 20 的每个 TileScope 有隔离的 mask。
4. Packet 10 的 y/x 扫描顺序与 RLE state 可查询、可降低。
5. `minBytes/maxBytes/actualBytes/offsetBegin/offsetEnd` 可以在内部布局记录中闭合。
6. 内部 `PresencePlan` 不被写入 legacy wire。
7. `FlagProjection`、`PayloadPresence`、`ValueAvailability` 不会被错误合并。
8. DecodePlan 产出记录，不直接修改世界。
9. PacketFramework 只通过 adapter 接触编译计划。

明确非目标：

```text
不把 MessageBuffer 的业务副作用复制到 SlotGraph；
不让一个通用 DAG 解释所有循环；
不把 Packet 8 的 join section effect 当成 wire loop；
不把 Packet 34 的 action effect select 当成 wire union；
不从本报告直接生成 Terraria 兼容 codec；
不修改 D:\TRbackup\无任何删减通过编译\Terraria 下的源代码。
```

## 20. 最终设计判断

### 能解决的问题

SlotGraph IR 可以把旧源码中的以下混杂拆开：

```text
字段是什么              -> PacketSchema / FieldSlot
字段是否存在            -> PresencePlan / GateSlot / GroupSlot
字段何时读写            -> WireSequence
字段属于哪个重复项      -> ScopeTree
循环何时结束            -> LoopBoundary
循环是否依赖上一次      -> LoopStateMachine
字节是否经过转换        -> TransformSlot
尾部有哪些独立记录      -> SideTableSlot
值依赖什么 catalog      -> ContextBinding / Value relation
读取后如何修改世界      -> EffectBoundary
最终发给谁              -> RoutingPolicy
```

### 不能假装解决的问题

SlotGraph 本身不能自动推导：

```text
WorldGen 的正确业务语义；
NPC / projectile slot 分配的安全策略；
服务器对客户端请求的权限和 spam 规则；
自定义 codec 内部未声明的动态格式；
旧协议中 flag 与 payload 条件的 legacy 差异；
发送端和接收端存在的非对称应用逻辑。
```

这些必须以 `EffectPlan`、`Policy`、`CustomCodecContract` 或人工审计方式显式登记。

最终推荐的执行分层是：

```text
FrameEnvelope
    -> SessionGate
    -> PacketDispatch
    -> DecodePlan
    -> DecodedPacket / WireLayoutRecords
    -> IdentityPolicy / ValidationPolicy
    -> ApplyEffect
    -> ReplicationEffect
    -> RoutingPlan
```

发送方向则是：

```text
Domain State / Request
    -> SnapshotResolver
    -> ContextBindings
    -> PresencePlan
    -> EncodePlan
    -> WireAssembler
    -> RecipientSelectionPlan
    -> FrameEnvelope
```

所以，当前设计的关键结论不是“用一张更复杂的图替代 `switch`”，而是：

> 用一个具有明确关系域、作用域和执行计划的 SlotGraph IR，把 `switch` 中真正属于 wire layout 的信息提取出来；把上下文、循环状态、业务副作用和路由保留为显式但隔离的上层结构。这样既能表达 Packet 13/20/10，也不会把 Packet 8、23、27、50/54、65 的非字段逻辑错误地压缩成同一种 DAG 边。

## Appendix A. 接收侧 160 个顶层分支的完整特征索引

下面的索引以 `MessageBuffer.GetData` 的顶层 `case` 为分母；内部嵌套 `switch` 的 selector case 不另算 message id。类型缩写为：`u8/u16/u32`、`i16/i32`、`f32`、`bool`、`str`、`vec2`、`bits`、`custom`。一行可以覆盖多个 ID，但这些 ID 在当前源码中 wire 形状和结构特征相同或足够接近，具体业务分支仍由 `Effect`/`Policy` 区分。

| ID | Wire 形状与结构特征 | Effect、门禁和特殊依赖 |
| ---: | --- | --- |
| 1 | `str(version)` | 仅服务器；ban 检查、状态必须为初始状态，成功后进入 password/connected 流程 |
| 2 | `NetworkText` delegated codec | 仅客户端；断开连接并设置状态文本 |
| 3 | `u8 playerId + bool serverFlag` | 仅客户端；改变 `myPlayer`、载入玩家、批量触发同步发送，包含固定数量 inventory loops |
| 4 | `u8 + u8 + u8 + f32 + u8 + str + u8 + accessory codec + u8 + 7 RGB + bits*3` | 玩家外观/能力 profile；服务器身份覆盖、重名/名称长度/难度校验、必要时回播 |
| 5 | `u8 player + i16 slot + i16 stack + u8 prefix + i16 type + bits` | item slot apply；`whoAmI` 覆盖、锁定 inventory policy、可 relay 条件 |
| 6 | 无 payload | 服务器连接状态转换、发送世界同步和 invasion 状态 |
| 7 | 世界初始化固定 block、多个 `bits`、固定数组、GUID 16 bytes、`u64`、`TreeTops`/`ExtraSpawnPointManager` delegated block | 仅客户端；世界状态批量赋值、ServerSideCharacter、事件/背景/雨雪状态、副作用顺序不可打乱 |
| 8 | `i32 x + i32 y + u8 team` | 仅服务器；不是 wire region loop，而是 join effect：section 投影、portal 去重、发送 Packet 10/实体同步、session state transition |
| 9 | `i32 count + NetworkText + bits` | 仅客户端；状态进度、服务器特殊 flags |
| 10 | Deflate substream：区域 header、stateful tile loop、three tail tables | 仅客户端；解压、Tile/Chest/Sign/TileEntity apply、loaded/map 标记 |
| 11 | `i16 x + i16 y + i16 width + i16 height` | 仅客户端；区域 frame 操作 |
| 12 | `u8 player + i16 spawnX/Y + i32 respawn + i16 death counts + u8 team + u8 spawnContext` | Spawn apply；服务器连接进入 State 10、同步玩家和 host 状态 |
| 13 | 固定 player/control fields + 4 个 `bits` + flag-controlled `vec2/u16/vec2 group` | identity override、teleport smoothing、mount/potion/camera apply、服务器回播 |
| 14 | `u8 player + u8 active` | 仅客户端；Player connect/disconnect lifecycle |
| 15, 25, 26, 44, 67, 83, 93 | 无 payload，显式 no-op 分支 | 保留 dispatch compatibility；不能从 registry 静默删除 |
| 16 | `u8 player + i16 life + i16 lifeMax` | 生命状态 apply；本地玩家和 ServerSideCharacter gate；服务器回播 |
| 17 | `u8 action + i16 x/y + i16 tile/value + u8 style` | tile action selector；`InWorld`、section、spam policy、WorldGen/Wiring effect、可能 tile-square 回发 |
| 18 | `u8 dayTime + i32 time + i16 sunModY + i16 moonModY` | 仅客户端；时间状态 apply |
| 19 | `u8 action + i16 x/y + u8 direction` | door/trapdoor/tall-gate selector；`InWorld`、WorldGen apply、服务器回播 |
| 20 | `i16 startX/Y + u8 width/height + u8 changeType + Area(TileRecord)` | 每 Tile 独立 scope、x-outer/y-inner；frame catalog、liquid legacy 差异、tile callback/frame/rebroadcast |
| 21, 90, 145, 148 | shared item block：`i16 id + vec2 position + vec2 velocity + i16 stack + u8 prefix + bits + i16 type`；profile tail | `MessageProfileSelector`；145 shimmer、148 enemy cooldown、90 instanced apply、server reservation/rebroadcast |
| 151 | `i16 itemId` | item removal；reservation/time-slot policy、server回播 |
| 22 | `i16 itemId + u8 owner + vec2 position` | item reservation/position；客户端显示保持时间 |
| 23 | NPC header + `bits` + sparse AI + difficulty optional + life width select + catchable optional | NPC slot reuse、SetDefaults、smoothing、boss/catchable effect；仅客户端接收 |
| 24 | `i16 npcId + u8 playerId` | NPC hit；server identity override、Strike、回发 24/23 |
| 27 | projectile header + nested flags + sparse AI/UUID optional | hostile/owner policy、identity lookup、inactive/oldest slot allocation、server回发 |
| 28 | `i16 npc + i16 damage + f32 knockBack + u8 direction + u8 critical` | NPC Strike/kill；server interaction、回发和死亡后的 Packet 23 |
| 29 | `i16 identity + u8 owner` | 1000-slot identity lookup loop；Kill projectile；server owner override和回播 |
| 30 | `u8 player + bool hostile` | PvP 状态 apply；server owner override、广播 chat、回播 |
| 31 | `i16 x + i16 y` | 仅服务器；Chest.Find/Using、rigged chest Wiring effect、多个后续发送 |
| 32 | `i16 chest + u8 slot + i16 stack + u8 prefix + i16 type` | chest item mutation；范围/空 chest 检查、服务器回播 |
| 33 | `i16 chest + i16 x/y + u8 nameMode + optional str + bits` | chest UI/name apply；客户端音效/界面或服务器命名回发 |
| 34 | `u8 action + i16 x/y + i16 style/type + i16 result` | effect-level chest/dresser/special chest selector；wire body 不随 action 改变 |
| 35 | `u8 player + i16 heal` | HealEffect；自我忽略 policy、服务器回播 |
| 36 | `u8 player + 6 u8 zone/town values` | 玩家区域状态 apply；服务器检测 zone 变化并生成 NPC effect |
| 37 | 无 payload | 客户端 password UI/自动通过状态转换 |
| 38 | `str password` | 仅服务器；密码验证，State 改变或发送 boot/error |
| 39 | `i16 itemId` | reservation release；客户端/服务器路径不同，可能重新 FindOwner 并发送 Packet 22 |
| 40 | `u8 player + i16 talkNpc` | Talk NPC apply；server identity override和回播 |
| 41 | `u8 player + f32 rotation + i16 animation` | item animation/channel state；server identity override和回播 |
| 42 | `u8 player + i16 mana + i16 manaMax` | 本地玩家/ServerSideCharacter gate；server identity override和回播 |
| 43 | `u8 player + i16 manaEffect` | ManaEffect；自我忽略、server identity override和回播 |
| 45, 157 | `u8 player + u8 team` | Team apply；157 额外触发 team spawn section；chat、回播和 section policy |
| 46 | `i16 signX + i16 signY` | 仅服务器；Sign.ReadSign 后发送 Packet 47 |
| 47 | `i16 signId/x/y + str text + u8 editor + bits` | Sign materialize/open UI；服务器只在文本变化时回发 |
| 48 | `i16 x/y + u8 liquid + u8 liquidType` | SpamWater proximity policy；tile lock、frame、液体清空后的回发 |
| 49 | 无 payload | 客户端连接状态与 Spawn transition |
| 50 | `u8 player + RepeatUntil(u16 terminator=0)` | sentinel loop、`Player.maxBuffs` 上限、清理尾部、server回播 |
| 51 | `u8 player + u8 action` | Skeletron/sundial/moondial/mimic/bestiary selector；分支按 authority 回播或播放音效 |
| 52 | `u8 action + i16 x/y` | Chest/door lock selector；WorldGen/Chest apply、tile-square 回播 |
| 53 | `i16 npc + u16 buff + i16 time` | NPC buff add；服务器发送 Packet 54 |
| 54 | `i16 npc + RepeatUntil(u16 type=0, u16 time)` | sentinel pair loop、`NPC.maxBuffs` 上限、客户端清理尾部 |
| 55 | `u8 player + u16 buff + i32 time` | PvP buff permission；server relay 或客户端 apply |
| 56 | `i16 npc + optional str name + i32 variation` | 客户端命名/variation，服务器只转发请求 profile |
| 57 | `u8,u8,u8` | 客户端世界/树相关三个状态值 |
| 58 | `u8 player + f32 pitch` | 当前物品类型 selector；乐器/音效 effect，server回播 |
| 59 | `i16 x + i16 y` | Wiring current-user context、HitSwitch、server回播 |
| 60 | `i16 npc + i16 homeX/Y + u8 housingAction` | housing action selector；NPC 合法性检查、客户端/服务器 WorldGen 分支 |
| 61 | `i16 player + i16 eventCode` | 大型 event-code selector：敌人、入侵、月相、宠物许可证等；仅服务器，权限/世界状态 gate |
| 62 | `u8 player + u8 dodgeKind` | Ninja/shadow/brain dodge selector；server identity override和回播 |
| 63, 64 | `i16 x/y + u8 paint + u8 coatKind` | tile/wall paint selector；WorldGen apply、服务器回播 |
| 65 | `bits + i16 target + vec2 position + u8 style + optional i32` | target-kind select、position override、teleport ack、section check、server广播 |
| 66 | `u8 player + i16 heal` | life clamp、HealEffect；server回播 |
| 68 | `str` | 读取但主要作为控制/同步通知消费；无独立字段状态 apply |
| 69 | `i16 chest + i16 x/y + str name` | client chest materialize 或 server consistency check；按坐标和 id policy 回发 |
| 70 | `i16 npc + u8 owner` | 仅服务器；CatchNPC |
| 71 | `i32 x/y + i16 type + u8 style` | 仅服务器；ReleaseNPC，使用 `whoAmI` authority |
| 72 | `i16[TravelShopMaxSlots]` | fixed-count loop；仅客户端 shop state |
| 73 | `u8 teleportKind` | teleportation potion/conch/shellphone selector |
| 74 | `u8 anglerQuest + bool finished` | 仅客户端 quest state |
| 75 | 无 payload | 仅服务器；记录 angler completion set |
| 76 | `u8 player + i32 questsFinished + i32 golferScore` | 本地玩家/ServerSideCharacter gate；server回播 |
| 77 | `i16 animationType + u16 tileType + i16 x/y` | temporary animation effect |
| 78 | `i32 progress + i32 max + sbyte invasion + sbyte wave` | 仅客户端 invasion UI/progress |
| 79 | `i16 x/y + i16 type/style + u8 alternative + sbyte random + bool direction` | object placement；InWorld/section/spam policy，WorldGen，server relay |
| 80 | `u8 player + i16 chest` | 仅客户端 player chest association |
| 81, 119 | 81：`f32 x/y + RGB + i32 amount`；119：`f32 x/y + RGB + NetworkText` | CombatText；119 使用 custom NetworkText，不应与 81 合并成同一 wire profile |
| 82 | 外部 `NetManager.Read(reader, whoAmI, length)` | length-aware custom module；必须登记消费预算和 effect boundary |
| 84 | `u8 player + f32 stealth` | player identity override、服务器回播 |
| 85 | server：custom inventory slots + bool；client：blocked chest list custom | mode-dependent schema；QuickStack effect，不能把两种形状声明成一个普通 optional |
| 86 | `i32 tileEntityId + bool exists + optional TileEntity record` | optional custom codec；client remove/add TileEntity |
| 87 | `i16 x/y + u8 entityType` | 仅服务器；InWorld 和 occupied gate，PlaceEntityNet |
| 88 | `i16 itemId + bits1 + optional item properties + optional bits2/properties` | nested flag scope；仅客户端 inner item patch |
| 89 | `i16 x/y + i16 itemType + u8 prefix + i16 stack` | 仅服务器；TEItemFrame 放置，`InWorld`/occupied policy 属于 effect 层 |
| 91 | `i32 bubbleId + u8 anchor + optional u16/u16 + u16 lifetime + u8 emote + optional i16 metadata` | nested selector/optional metadata；EmoteBubble map insert/update/remove和lock |
| 92 | `i16 npc + i32 value + f32 x/y` | NPC money ping/extra value；client apply或server累加后回播 |
| 94 | `str command + i32 + f32 + f32` | debug command gate；DebugOptions effect，非普通 gameplay 字段 |
| 95 | `u16 projectileOwner + u8 aiKey` | 服务器 1000-slot search loop，命中特定 projectile type 后 Kill/rebroadcast |
| 96 | `u8 player + i16 portal + vec2 position + vec2 velocity` | player portal teleport；authority override和server回播 |
| 97, 98 | 各自 `i16` event value | 仅客户端；achievement/progression effect |
| 99, 115 | 99：`u8 player + vec2 target`；115：`u8 player + i16 npcTarget` | minion target state；server identity override和回播 |
| 100 | `u16 npc + i16 portal + vec2 position + vec2 velocity` | NPC portal teleport；清除 netOffset |
| 101 | `u16[4]` | fixed-count tower shield state，客户端 clamp |
| 102 | `u8 player + u16 event + vec2 origin` | server relay；客户端按玩家/队伍/距离执行 nested visual loop 和 Dust effects |
| 103 | `i32 maxCountdown + i32 countdown` | 仅客户端 Moon Lord countdown state |
| 104 | `u8 shopSlot + i16 type + i16 stack + u8 prefix + i32 value + bits` | 仅客户端 shop item apply；bounded slot gate |
| 105 | `i16 x/y + bool` | 仅服务器；toggle gem lock |
| 106 | `u32 HalfVector2.PackedValue` | 仅客户端；packed transform 后触发 visual effect |
| 107 | `RGB + NetworkText + i16 width` | 仅客户端 multiline text；custom text codec |
| 108 | `i16 damage + f32 knockBack + i16 x/y/angle/ammo + u8 player` | 仅客户端且 target player gate；cannon effect |
| 109 | `i16 x1/y1/x2/y2 + u8 toolMode` | 仅服务器；temporarily overrides WiresUI mode，MassWireOperation |
| 110 | `i16 itemType + i16 count + u8 player` | 仅客户端；bounded `for count` item consume |
| 111 | 无 payload | 仅服务器；manual party toggle |
| 112 | `u8 effectKind + i32 x/y + u8 + i16 + u8 flag` | effect selector；TreeGrowFX 或 FairyEffects，server relay/client apply 不同 |
| 113 | `i16 x/y` | 仅服务器；DD2 crystal spawn，world/event gate |
| 114 | 无 payload | 仅客户端；DD2 wipe entities |
| 116 | `i32 timeLeft` | 仅客户端 DD2 wave state |
| 117, 118 | `u8 player + PlayerDeathReason custom + i16 damage + u8 direction + bits`；117 另有 `sbyte hitContext` | PvP/authority policy、Hurt/KillMe、server death/hurt relay |
| 120 | `u8 player + u8 emote` | server authority、EmoteBubble spawn/reaction |
| 121 | `u8 player + i32 entityId + u8 slot + u8 action + custom data` | TileEntity display-doll codec；未知 entity 使用 dummy reader，server relay |
| 122 | `i32 entityId + u8 player` | `-1` clear 或 occupied/entity anchor select；client/server apply/rebroadcast |
| 123, 133, 149 | `i16 x/y + i16 itemType + u8 prefix + i16 stack` | 不同 TileEntity placement effect；字段形状相似但 effect owner 不同 |
| 124 | `u8 player + i32 entityId + encoded u8 slot + custom item record` | slot bit 编码、dummy consume path、TEHatRack custom codec、server relay |
| 125 | `u8 player + i16 x/y + u8 style` | pick-tile visual/request；server relay或本地 player effect |
| 126 | `Player/RevengeMarker custom codec` | 仅客户端 marker add；custom codec contract required |
| 127 | `i32 markerId` | 仅客户端 marker destroy |
| 128 | `u8 player + u16 x/y + u16 cup/style values` | server relay或客户端 Golf contact/effects |
| 129 | 无 payload | 仅客户端 UI/准备状态；可能主动发送 team packet |
| 130 | `u16 x/y + i16 npcType` | 仅服务器；fished NPC allocation、special type unlock、可能回发 Packet 23 |
| 131 | `u16 npcId + u8 operation + conditional i32/i16` | 仅客户端；operation-controlled immunity update |
| 132 | `vec2 + u16 soundKey + bits + optional i32/f32/f32` | flag-controlled sound style/volume/pitch，custom sound effect |
| 134 | `u8 player + i32 + f32 + u8 + bool*2 + f32*2 + u8` | luck state apply/recalculate；server identity override和定向回发 |
| 135 | `u8 player` | 仅客户端；immune alpha effect |
| 136 | `u16[2,3]` | nested fixed loops；cavern monster table |
| 137 | `i16 npc + u16 buffType` | 仅服务器；bounded NPC index、buff removal |
| 139 | `u8 player + bool` | 非服务器 authority；host-for-gameplay table apply |
| 140 | `u8 selector + i32 value` | selector 分支：credits client、slime transform server |
| 141 | `u8 source + u8 + vec2 velocity + i32/i32 position` | server relay或客户端 Lucy axe custom effect |
| 142 | `u8 player + piggyBankTracker custom + voidLensChest custom` | custom delegated codec、identity override、server relay |
| 143, 144 | 无 payload | 服务器侧 DD2 skip wait 或 Dryad animation effect |
| 146 | `u8 effectKind` 后分支：`vec2` 或 `vec2 + i32` | selector-controlled shimmer/coin effect，仅客户端 |
| 147 | `u8 player + u8 loadout + accessory visibility custom` | loadout apply、identity override、server relay |
| 150 | `u8 player + i16 spectatingTarget` | server request/ack 与 client local spectating policy |
| 152 | `u8 player` | server relay或客户端播放 selected item sound |
| 153 | `u8 npc + i16 damage` | NPC debuff hurt effect；server relay |
| 154 | 无 payload | ping request/response；不是 field schema 的复杂性，而是 transport/control effect |
| 155 | `i16 chest + i16 newSize` | bounded chest index、resize apply |
| 156 | `i16 x/y + i16 itemType` | 仅服务器；TE leash anchor item insertion |
| 158 | `u8 player` | 非服务器；team-swap spawn |
| 159 | `u16 sectionX + u16 sectionY` | 仅服务器；按 section 发送 Packet 10，典型 routing/effect loop |
| 160 | `i16 itemId + vec2 position` | 非服务器；world item position apply |
| 161 | `str hostToken` | host token authority gate；不匹配时无显式业务回播，属于 session/security context |

这个附录补足了“代表性案例”和“完整分支覆盖”的差别：代表性章节解释 SlotGraph 为什么需要这些结构；本表保证 `MessageBuffer.GetData` 的每一个顶层 case 都有归类。`21/90/145/148`、`45/157`、`81/119`、`99/115`、`117/118`、`123/133/149` 等共享部分 wire 形状的消息仍然保留独立 profile/effect 身份，不能仅按字段列表合并。

## Appendix B. NetMessage 发送侧的额外特征

接收分支索引不能单独代表 `NetMessage.cs`。发送侧还有一组不会自然出现在 packet 字段表中的行为，这些行为正是上下文混入旧实现的主要来源。

### B.1 发送入口的隐式 context

`NetMessage.SendData` 在 `NetMessage.cs:95` 附近同时完成：

```text
检查 Main.netMode；
根据 item 状态重写 message id；
选择共享 MessageBuffer；
取得并锁定可复用 BinaryWriter；
预留前两个 length bytes；
写 message id 和 payload；
检查 ushort 总长度上限；
回填 frame length；
按客户端/服务器选择发送路径；
清除 writeLocked 状态；
Packet 2 时设置 PendingTermination。
```

这说明 `EncodePlan` 只应接管 payload 的 wire layout；buffer 锁、frame length、transport error 和终止连接仍属于 `FrameAssembler`/transport module。

### B.2 静态 side channel 传递 custom payload

`NetMessage` 维护以下静态字段：

```text
_currentPlayerDeathReason
_currentNetSoundInfo
_currentRevengeMarker
```

发送 helper 先写入这些静态字段，再调用没有显式参数的 `SendData`：

```text
SendPlayerHurt -> _currentPlayerDeathReason -> Packet 117
SendPlayerDeath -> _currentPlayerDeathReason -> Packet 118
PlayNetSound -> _currentNetSoundInfo -> Packet 132
SendCoinLossRevengeMarker -> _currentRevengeMarker -> Packet 126
```

这是一个重要的 legacy context 特征：payload 的真实值不完全来自 `SendData` 的参数列表。迁移时应改成显式 `PacketSnapshot` 或 `CustomCodecInput`，但在兼容期必须把该 side channel 标记为：

```text
ContextBinding {
    phase = Encode
    source = LegacySendHelperSideChannel
    purity = ContextDependent
    concurrencyRisk = SharedMutable
}
```

不能让生成器误判这些 custom codec 是无参数的纯字段。

### B.3 几何归一化和发送前值变换

Packet 20 的发送端在写 header 之前会：

```text
把 width/height 的负值截为 0；
根据 width/height 修正 startX/startY；
把越界区域夹到世界边界附近；
然后再写 startX/startY/width/height/changeType。
```

这不是字段 presence，而是 `SnapshotResolver` 或 `EncodeNormalizationPolicy`。如果迁移到上层，必须记录：

```text
RequestedRegion -> NormalizedRegion -> WireHeader
```

不能让 `FieldSlot<int16>` 自己偷偷改变坐标。

Packet 7 发送中还有 `!Main.raining` 时把 `Main.maxRaining` 归零后再写出的行为；这属于发送前状态规范化副作用，不应放进 world-init 字段的 primitive encoder。

### B.4 发送 helper 会生成 packet 序列

以下 helper 不是单个 packet codec，而是上层 command/sequence：

| Helper | 观察到的结构 | IR 对应 |
| --- | --- | --- |
| `SendTileSquare` | 多个 overload 归一化 centered size，再调用 Packet 20 | `Command -> SnapshotResolver -> Packet20` |
| `ResyncTiles` | 按 200 x 150 分块，嵌套循环发送 Packet 10 | `EffectLoop(AreaChunk) -> Emit(Packet10)` |
| `SendSection` | 检查 section bounds/loaded 标志，分块发送 Packet 10，再同步 NPC/chest | `SectionSyncEffect` |
| `sendWater` | 服务器按 client section active 过滤 Packet 48 | `RecipientSelectionPlan` |
| `SyncConnectedPlayer` | 按玩家状态发送 Packet 14/4/13/16/30/45/42/50/80/142/147、物品数组和 projectile | `JoinSyncSequence` |
| `SyncOnePlayer_ItemArray` | 对数组逐槽发送 Packet 5 | `ExactlyLoop -> Emit(Packet5)` |
| `SendObjectPlacement` | 根据 server/client 选择 remoteClient/ignoreClient 后发送 Packet 79 | `CommandRoutingPolicy` |
| `SendPlayerHurt` / `SendPlayerDeath` | 设置 custom side channel，再发送 Packet 117/118 | `CustomSnapshot + EncodePlan` |

这些 helper 中的嵌套循环、分包和多消息序列不应塞进某一个 packet 的 `WireSequence`。它们应该在 packet codec 上层由 `CommandPlan`、`EffectLoop` 或 `JoinSyncPlan` 表达。

### B.5 发送路由中的 mutable sync state

`SendData` 的 Packet 23/27 路由不只是判断 recipient：

```text
Packet 23 会读取并清理 NPC 的 spawnNeedsSyncing、netStream、netUpdate、skippedSyncs 等状态；
Packet 27 会读取并清理 projectile 的 netSyncSkippedForPlayer；
Packet 20 使用 recipient 的 SectionRange；
Packet 28 根据 NPC 是否死亡或 recipient 是否 active section 决定发送；
Packet 34/69/13 使用 broadcast/connected policy。
```

因此 `RoutingPolicy` 的接口不仅返回 `IEnumerable<ClientId>`，还必须声明是否会提交同步 bookkeeping：

```text
RecipientSelectionPlan {
    recipients
    excluded
    stateReads
    stateWrites
    ignoreClient
    authority
}
```

如果把这些写操作隐藏在 `SendPacket` 后面，新的上层框架仍然会重新产生不可审计的副作用。

### B.6 发送侧覆盖核对

对 `NetMessage.SendData` payload switch 的顶层 label 做了独立扫描：

```text
发送侧顶层 case label：141 个
接收侧顶层 case label：160 个
接收侧额外 ID：19 个
发送侧独有 ID：0 个
```

发送侧的 141 个 label 全部包含在接收侧 160 个 label 中；但“存在接收 case”不代表收发语义对称。Packet 8、31、61、70、71、79、105、109、113、123、133、149、156 等主要是客户端请求进入服务器 effect；Packet 7、9、10、11、18、57、72、74、78、81、103、106、107、114、116、131、132、135、136 等主要是服务器到客户端状态/视觉通知。

因此 registry 的兼容元数据至少需要：

```text
Direction = ClientToServer | ServerToClient | Bidirectional
WireProfile
DecodeAuthority
ApplyEffectAuthority
ReplicationPolicy
```

## Appendix C. 收发不对称、消费边界和 legacy 陷阱

### C.1 frame 隔离不等于 payload 必须被完整消费

Packet 69 是一个典型反例。发送端 `NetMessage.cs:1121` 附近固定写入：

```text
i16 chestId
i16 x
i16 y
string chestName
```

但 `MessageBuffer.cs:3081` 的接收端只在 `Main.netMode == 1` 分支读取 `string chestName`；服务器分支读取前三个值后执行 chest 一致性检查和回发，没有继续读取 name。随后 `CheckBytes` 在 `MessageBuffer.cs:2554-2555` 把 reader position 移到整个 frame 的结束位置。

因此新计划必须区分：

```text
ConsumePolicy = ExactPayload
ConsumePolicy = ModeSpecificPayload
ConsumePolicy = PermittedUnconsumedTail
```

不能默认把“发送端写了字段”和“每个接收端分支都会读取字段”当成同一个事实。`DecodePlan` 应在 profile 中声明每种 authority/direction 的消费边界，并记录 `consumedBytes` 与 `frameRemainingBytes`。

### C.2 Packet 56 是 mode-dependent profile，而不是普通 optional

发送端 Packet 56 先写 NPC id；只有 `Main.netMode == 2` 时才继续写：

```text
string givenName
i32 townNpcVariationIndex
```

接收端服务器分支只读取 NPC id，客户端分支才读取 name 和 variation。这里字段是否存在由发送/接收角色和运行模式决定，不是某个 packet flag 控制的 optional field。IR 应使用：

```text
Profile(Packet56.ServerToClient)
    -> id + name + variation
Profile(Packet56.ClientToServer)
    -> id
```

### C.3 signedness 与不可达验证条件必须保留为诊断

源码中存在读取无符号值后做负值判断的 legacy 形状，例如：

```text
Packet 91：num266 = ReadByte()，随后存在 num266 < 0 的条件；
Packet 101：四个 shield 值来自 ReadUInt16()，随后存在 < 0 的检查。
```

这些判断从 wire 类型上看不可由正常读取结果触发，但不应在迁移时未经记录就删除。静态 IR 可以给出：

```text
Constraint {
    sourcePredicate
    satisfiability = UnreachableUnderWireType
    action = PreserveForCompatibility | Warn | Reject
}
```

这样既不会把不可达分支误认为有效 presence，也不会让 legacy 行为差异从审计记录中消失。

### C.4 delegated codec 可能有“dummy consume”路径

Packet 121 和 124 说明 custom codec 的消费长度可能由 lookup 结果改变：

```text
Packet 121：找不到 TEDisplayDoll 时调用 ReadDummySync；
Packet 124：找不到 TEHatRack 或 slot 非法时仍读取固定 dummy 字段；
Packet 86：exists bool 决定 remove 或读取 TileEntity record；
Packet 82：外部 NetManager.Read 直接接收 reader 和 packet length。
```

所以 `CustomCodecContract` 必须同时声明：

```text
NormalDecodeShape
FallbackDecodeShape
ConsumedBytesByVariant
FailureConsumptionPolicy
```

否则 field-level `maxBytes` 不能覆盖真实的 fallback 消费路径。

### C.5 大小、坐标和数组的验证不能只依赖 schema 类型

源码还反复使用运行时范围校验：

```text
WorldGen.InWorld(x, y, padding)
player/npc/projectile/chest/sign/entity index bounds
Main.tileFrameImportant[type]
Main.projHostile[type]
TileSections[sectionX, sectionY]
Main.maxBuffs / NPC.maxBuffs
```

这些不是 `i16` 或 `u8` 类型系统自动能证明的约束，应在 IR 中作为独立 `Constraint`/`ValidationPolicy` 记录。尤其是 `type` 先从 wire 读取、再作为 catalog 数组索引的字段，必须先通过 bounded lookup 才能驱动后续 `Presence` 或 `Select`。
