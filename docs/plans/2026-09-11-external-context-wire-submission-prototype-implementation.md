# 外部上下文投影与 WireSubmission 实现执行计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将三个 context binding/WireSubmission 原型验证过的边界落到 Packet 13、Packet 20 的生产出站路径，并保留 legacy framing、registry 和发送 pipeline 的兼容行为。

**Architecture:** 外部领域状态先由 packet-specific ContextAdapter 形成 PreparationInput，再由 projector/binder 生成不可变、强类型 submission。SubmissionValidator 和静态 EncodePlan 在 bounded staging 中完成确定性编码，成功后才构造 NetMessage/MessageFrame；routing、recipient selection、Effect 和世界修改留在现有接缝。

**Tech Stack:** C#、.NET 10、现有 PacketFramework/PacketDefinitionRegistry、MessageFrame、现有 standalone verification harness、golden/differential wire tests。

---

## 执行前置条件

- 当前工作树的 `NetWork.csproj` 和 `Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj` 仍引用仓库根 `Concept/PacketNode.cs`、`Concept/PacketLayout.cs`、`Concept/PacketGraphExport.cs`、`Concept/PacketGraphCodecEmitter.cs` 等路径；现有实验源实际位于 `Concept/test1` 或 `Concept/test2`。这会在 submission 代码尚未改变时触发 `CS2001`，属于基线项目文件/源文件归属问题。
- 在执行 Task 1 的 standalone focused test 或 Task 6 的生产 build 前，先单独解决上述引用问题：要么恢复项目文件期望的根级源文件，要么在独立的基线修复中改为明确的 `test1`/`test2` 源路径，并确认不会重复编译同名类型。不要把 graph 实验文件复制进 submission 实现，也不要把该构建失败归因于 WireSubmission 设计。
- 基线修复应有独立验证和独立提交；本计划后续门禁只在 `CS2001` 消失后评估 submission、opaque payload、frame 和 pipeline 行为。

---

## Task 0A: 先修复 Graph 源文件归属的基线阻塞

**Files:**

- Inspect/modify: `NetWork.csproj`
- Inspect/modify: `Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj`
- Inspect/modify if Graph verification is restored: `Tools/NetWork.Concept.GraphBootstrap/NetWork.Concept.GraphBootstrap.csproj`
- Modify only if the selected active Graph contract requires it: `Verification/ProtocolDemoStandalone/MinimalDependencyGraphTests.cs`

这不是 WireSubmission 实现的一部分，但必须先解决。当前项目文件引用仓库根 `Concept/*.cs`，而源码分散在 `Concept/test1` 和 `Concept/test2`；两个目录包含同名但不兼容的 Graph API，不能把它们全部加入同一个项目，也不能只复制一个文件来掩盖版本错配。

**Step 1: 固定当前失败和引用清单**

~~~powershell
dotnet build NetWork.csproj --no-restore
dotnet build Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
rg -n "Concept[\\/]" --glob '*.csproj' --glob '*.props' --glob '*.targets' .
rg --files Concept/test1 Concept/test2
~~~

Expected: 当前 build 明确以 `CS2001` 报告根 `Concept` 源文件缺失；输出列出所有过期 include，不把这个失败混入后续 submission 红灯。

**Step 2: 确认每个消费者需要的 Graph API**

逐个对照 `MinimalDependencyGraphTests.cs`、被项目排除的 Graph tests、`Tools/NetWork.Concept.GraphBootstrap` 和生成器调用方的类型签名，确认它们分别依赖 `test1`、`test2` 或已经废弃的版本。特别检查 `PacketNode`、`PacketEdge`、`PacketLayout`、`PacketGraphManifest` 和 `PacketGraphCatalog` 的构造方式；不能仅按文件名替换路径。

**Step 3: 选择一个明确的源文件映射**

将每个项目的 `Compile Include` 改成同一版本内的完整、可追踪源文件集合，或恢复该项目所需的根级源文件；移除失效路径和重复类型。若 Graph 资产保持排除状态，standalone 只能编译它实际调用的最小 Graph contract，并应继续保留 `Packet20GraphExportTests.cs` 等排除规则。不要把 Graph codec、Graph concept 或历史生成产物引入 submission/encoder 依赖。

当前源码结构给出的待验证映射候选是：`NetWork.csproj` 的根级 Graph include 对应 `Concept/test2` 中同名的 `PacketNode.cs`、`PacketLayout.cs`、`PacketGraphExport.cs`、`PacketGraphCodecEmitter.cs` 和两个 example 文件；standalone 的 `MinimalDependencyGraphTests` 至少对应 `Concept/test2/PacketNode.cs`。`Tools/NetWork.Concept.GraphBootstrap` 和依赖 `PacketGraphCatalog`/`AreaTileChangePacket20GraphConcept` 的 Graph tests 则对应 `Concept/test1` 的完整 Graph 资产。该映射必须经过 Step 2 的 API 对照和 Step 4 的 build 验证后才能采用，不能仅按目录名称自动替换。

**Step 4: 验证基线项目恢复**

~~~powershell
dotnet build NetWork.csproj --no-restore
dotnet build Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
~~~

Expected: 两个项目不再出现 `CS2001`；若出现 API mismatch，修正源文件映射或对应测试契约后重新运行，不得通过复制同名文件或禁用失败测试绕过。

**Step 5: 独立提交基线修复**

~~~powershell
git add NetWork.csproj Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj Tools/NetWork.Concept.GraphBootstrap/NetWork.Concept.GraphBootstrap.csproj Verification/ProtocolDemoStandalone/MinimalDependencyGraphTests.cs
git commit -m "build: reconcile graph source paths before submission migration"
~~~

只有该 Task 的 build 证据成立后，才进入 Task 0 的 Concept 非交互入口和后续生产 submission tasks。

---

## 执行规则

- 每个 Task 先写失败测试，再写最小实现，再运行该 Task 的 focused test，最后运行相关回归。
- 不把 demo 的 TUI、[20,70] 完整 body 限制或临时类型名直接复制为生产 API。
- 明确区分三种 Packet 20 证据：test4 的概念 body、`Verification/ProtocolDemoStandalone/Packet20FrozenBaseline.cs` 的结构化 case-20 body、当前 `Core/Protocol/Packets/WorldActionPacketDefinitions.cs` 中 `AreaTileChangePacket20Definition` 的 opaque `TileDataPayload`。只有这三种证据完成明确的 translation boundary，并继续对照 `NetMessage` 与完整 `MessageFrame`，才能宣称 Packet 20 生产迁移通过。
- 每次 submission 构造都复制外部可变集合；测试必须在投影后 mutate 外部输入，证明 submission 仍稳定。
- 新 submission codec 不得访问 Main、World、Netplay、SessionContext、IServerSession、SendTarget、SendPipelineContext 或 MessageTcpSession。
- 失败时不得生成半成品 NetMessage，不得回读领域上下文修复，也不得覆盖调用方此前成功的输出。
- 执行中如发现现有用户改动，保留并在同一文件上协作；不使用 git reset --hard、git checkout -- 或清理命令。
- 生产公共抽象只有在两个以上真实调用点需要它时才提取；首版优先使用 packet-specific 类型。
- 每个 Task 完成后先运行 focused 验证，再提交小而可回滚的 commit。

## 迁移门禁

每个 packet 的 vertical slice 必须同时满足：

1. 新旧完整 frame bytes 一致。
2. 现有旧 reader/registry 能读取新 frame。
3. 同一 immutable submission 重复编码得到相同字节。
4. 非法 presence、shape、budget、segment length 在 final output 提交前失败。
5. framing、routing、recipient selection、ignore、dedup/order 回归通过。
6. encoder 和 segment codec 不泄漏外部上下文依赖。
7. Packet 20 的二维索引顺序、字段集合和 opaque payload translation 有真实 legacy bytes 证据；不能只凭 `Width`、`Height` 或生成 Graph 的字段名推断。

## Task 0: 固定最小原型证据和 Concept 非交互入口

**Files:**

- Modify: Concept/ContextBindingPrototype/Program.cs
- Test/verification: Concept/ContextBindingPrototype/Program.cs 内新增 --demo transcript 分支
- Modify only if required: Concept/ContextBindingPrototype/ContextBindingPrototype.cs

**Step 1: 写失败前置验证**

先运行当前入口的重定向 smoke command，记录当前缺口，而不是把失败误报为环境问题：

~~~powershell
cmd /c "dotnet run --project Concept/ContextBindingPrototype/ContextBindingPrototype.csproj --no-restore < NUL"
~~~

Expected before implementation: 交互入口尝试 `Console.ReadKey`，在无交互输入环境退出非零或无法形成可重复 transcript；这不是可接受的 CI 证据。

**Step 2: 设计 demo 断言**

--demo 应自动执行并断言：

- 绑定后 mutation 不改变 WireSubmission 的编码；
- Fixed(4) 接受 4，拒绝 3 和 5；
- Bounded(20,70) 接受 20、42、70，拒绝 19 和 71；
- staging/segment 失败不覆盖此前成功的 final output；
- 最终输出包含明确的 PASS 行并以退出码 0 结束。

**Step 3: 实现最小参数化入口**

在 Program.cs 入口先识别 --demo，调用不依赖 Console.ReadKey 的静态 transcript；没有该参数时保留原交互模式。不要通过 sleep、终端探测或吞掉异常来伪造通过。

**Step 4: 运行原型验证**

~~~powershell
dotnet run --project Concept/ContextBindingPrototype/ContextBindingPrototype.csproj --no-restore -- --demo
~~~

Expected: 退出码 0，包含绑定后 mutation、bounds 边界和 final output 保持不变的 PASS 证据。

**Step 5: 运行原型 build**

~~~powershell
dotnet build Concept/ContextBindingPrototype/ContextBindingPrototype.csproj --no-restore
~~~

Expected: exit code 0。

**Step 6: 提交**

~~~powershell
git add Concept/ContextBindingPrototype/Program.cs Concept/ContextBindingPrototype/ContextBindingPrototype.cs
git commit -m "test: add scripted context binding prototype transcript"
~~~

## Task 1: 建立生产 Segment Contract

**Files:**

- Create: Core/Protocol/Wire/SegmentBounds.cs
- Create: Core/Protocol/Wire/SegmentBoundary.cs
- Create: Core/Protocol/Wire/SegmentSpec.cs
- Create: Core/Protocol/Wire/IWireSegmentCodec.cs
- Create: Core/Protocol/Wire/SegmentHandle.cs
- Create: Core/Protocol/Wire/SegmentRegistry.cs
- Create: Core/Protocol/Wire/SegmentComposer.cs
- Create/modify: Verification/ProtocolDemoStandalone/ExternalContextWireSubmissionTests.cs
- Modify: Verification/ProtocolDemoStandalone/Program.cs
- Modify only if required: Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj

`ProtocolDemoStandalone.csproj` 当前没有包含 `Core/Protocol/Wire`，因此 Task 1 必须新增 `Compile Include="..\\..\\Core\\Protocol\\Wire\\**\\*.cs"`，否则 standalone focused test 不会实际编译这组 contract。

**Step 1: 写失败测试**

在 ExternalContextWireSubmissionTests.cs 增加 focused tests：

- Fixed(4) 接受 4，拒绝 3 和 5。
- Bounded(20,70) 接受 20、42、70，拒绝 19 和 71。
- codec 回报非法 written 时 final output 保持原值。
- codec 实际写入超出 staging 容量时不提交。
- Exact 读取不允许额外消费。
- ParentDelimited 必须完整消费父范围。
- SegmentRegistry.Freeze() 后不能新增注册。
- 成功 segment 之后的失败 segment 不改变已经存在的 final output。

示例断言形状：

~~~csharp
AssertThrows<InvalidOperationException>(
    () => SegmentBounds.Fixed(4).Validate(3),
    "Fixed segment must reject short writes.");
AssertTrue(
    SegmentBounds.Bounded(20, 70).Accepts(42),
    "bounded middle length");
~~~

**Step 2: 运行 focused test 确认失败**

~~~powershell
dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
~~~

Expected: 在基线项目引用问题已解决后，编译失败或新增断言失败，原因是生产 Segment Contract 尚不存在。若仍出现 `CS2001`，先回到“执行前置条件”，不得把项目路径错误当成该 Task 的红灯证据。

**Step 3: 写最小实现**

实现以下职责：

- SegmentBounds 表达 Fixed 和 Bounded，拒绝负数、反向范围和范围外 written。
- SegmentBoundary 至少包含 Exact 和 ParentDelimited。
- SegmentSpec 固定 id、bounds、boundary。
- IWireSegmentCodec<TValue> 只接收 in TValue、同步 staging Span<byte>/ReadOnlySpan<byte>，输出 written/consumed 和错误信息。
- SegmentRegistry 在构建阶段注册并通过 Freeze 固定；冻结后注册失败。
- SegmentComposer 先写 bounded staging，验证长度和父范围，再复制 [0,written) 到 final output。

不要让 codec 拿到最终 IBufferWriter<byte>，不要增加万能 context 参数。

**Step 4: 运行 focused test 确认通过**

~~~powershell
dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
~~~

Expected: Segment Contract tests PASS，并且现有 standalone verification 继续通过。

**Step 5: 检查 API 依赖**

~~~powershell
rg -n "Main|World|Netplay|Session|IBufferWriter|SendTarget" Core/Protocol/Wire Verification/ProtocolDemoStandalone/ExternalContextWireSubmissionTests.cs
~~~

Expected: Segment Contract 代码不依赖领域状态、session、routing 或最终 writer；测试文本中的断言说明可以排除。

**Step 6: 提交**

~~~powershell
git add Core/Protocol/Wire Verification/ProtocolDemoStandalone/ExternalContextWireSubmissionTests.cs Verification/ProtocolDemoStandalone/Program.cs Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj
git commit -m "feat: add bounded wire segment contract"
~~~

## Task 2: Packet 13 submission vertical slice

**Files:**

- Create: Core/Protocol/Packets/PlayerControlsPacket13Submission.cs
- Create: Core/Protocol/Packets/PlayerControlsPacket13Projector.cs
- Create: Core/Protocol/Packets/PlayerControlsPacket13SubmissionEncoder.cs
- Create: Core/Protocol/Packets/PlayerControlsPacket13SubmissionAdapter.cs
- Create/modify: Verification/ProtocolDemoStandalone/PlayerControlsPacket13SubmissionTests.cs
- Modify: Verification/ProtocolDemoStandalone/Program.cs
- Reuse: Verification/ProtocolDemoStandalone/PlayerControlsPacket13TestSupport.cs
- Reuse: Verification/ProtocolDemoStandalone/GoldenWireSnapshotTests.cs

**Step 1: 写失败测试**

覆盖以下行为：

- Packet13PreparationInput 可投影成 immutable typed submission。
- ControlFlags2.bit2、bit7、ControlFlags3.bit6、ControlFlags4.bit5 分别从 presence 派生。
- PotionOfReturn 只提供一个位置时拒绝。
- 投影后修改外部 input/数组不改变 submission。
- 同一 submission 编码两次 bytes 相同。
- 新 encoder 的完整 frame bytes 与 legacy builder/golden 相同。
- PacketDefinitionRegistry.Read 或现有旧 reader 能读取新 frame。
- encoder 不持有 Main、World、session、writer、cursor 或 delegate。
- 空 optional group 的 flags 与 payload 均符合当前 legacy definition。

**Step 2: 运行 focused test 确认失败**

~~~powershell
dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
~~~

Expected: 新测试因 submission/projector/encoder 尚不存在而失败。

**Step 3: 实现 preparation、projection 和 validation**

- PlayerControlsPacket13Submission 只保存固定值、Packet13Presence 和拥有的 optional values。
- Projector 负责把外部 snapshot 转成值快照；PotionOfReturn 两个位置 all-or-none。
- Validator 检查 flags 所需 presence、optional value 和范围一致性。
- 不把独立可编辑的 flags 与 presence 同时作为输入事实源。
- 对数组、字符串和其他可变输入执行复制；submission 不保存外部对象引用。
- 将 invalid projection 与 submission validation 的错误区分开，至少能定位 packet 和字段组。

**Step 4: 实现确定性 encoder 和 adapter**

- Encoder 只按 PlayerControlsPacket13Definition 的 wire order 写 staging/final output。
- Encoder 从 Packet13Presence 派生 ControlFlags2、ControlFlags3 和 ControlFlags4。
- Adapter 负责把 submission encoder 的结果包装成生产需要的 NetMessage，不重新调用旧 context codec。
- 保持入站 registry/read 和现有 framing 责任不变。
- 不在 encoder 中执行身份覆盖、mount apply、权限、rebroadcast 或其他接收侧 Effect。

**Step 5: 运行 Packet 13 验证**

~~~powershell
dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
~~~

Expected: Packet 13 submission tests、golden、legacy differential、round-trip 和现有 pipeline tests PASS。

**Step 6: 手工检查 wire 与依赖**

~~~powershell
rg -n "Main|World|Netplay|SessionContext|IServerSession|SendTarget|SendPipelineContext|MessageTcpSession|IBufferWriter|Func<|Lazy<" Core/Protocol/Packets/PlayerControlsPacket13Submission.cs Core/Protocol/Packets/PlayerControlsPacket13Projector.cs Core/Protocol/Packets/PlayerControlsPacket13SubmissionEncoder.cs Core/Protocol/Packets/PlayerControlsPacket13SubmissionAdapter.cs
~~~

Expected: submission/encoder 不出现外部上下文、session、routing、writer 或延迟委托依赖。

**Step 7: 提交**

~~~powershell
git add Core/Protocol/Packets/PlayerControlsPacket13Submission.cs Core/Protocol/Packets/PlayerControlsPacket13Projector.cs Core/Protocol/Packets/PlayerControlsPacket13SubmissionEncoder.cs Core/Protocol/Packets/PlayerControlsPacket13SubmissionAdapter.cs Verification/ProtocolDemoStandalone/PlayerControlsPacket13SubmissionTests.cs Verification/ProtocolDemoStandalone/Program.cs
git commit -m "feat: add packet 13 prepared submission encoder"
~~~

## Task 3: Packet 20 submission vertical slice

**Files:**

- Create: Core/Protocol/Packets/AreaTileChangePacket20Submission.cs
- Create: Core/Protocol/Packets/AreaTileChangePacket20Projector.cs
- Create: Core/Protocol/Packets/AreaTileChangePacket20SubmissionEncoder.cs
- Create: Core/Protocol/Packets/AreaTileChangePacket20SubmissionAdapter.cs
- Create/modify: Verification/ProtocolDemoStandalone/AreaTileChangePacket20SubmissionTests.cs
- Modify: Verification/ProtocolDemoStandalone/Program.cs
- Reuse: Verification/ProtocolDemoStandalone/Packet20FrozenBaseline.cs

不要直接复用当前被 `ProtocolDemoStandalone.csproj` 排除的 `Packet20GraphExportTests.cs` 作为 submission test。它可以作为独立 Graph 证据参考；本任务需要新增并在 `Program.cs` 明确调用 `AreaTileChangePacket20SubmissionTests.Run()`。

**Step 1: 写失败测试**

覆盖以下行为：

- checked(width * height) 溢出被拒绝。
- Tile 数量不足或过多被拒绝。
- 每个 Tile 有独立 presence；相邻 Tile 不互相污染。
- Color、WallColor 等 optional value 不能脱离对应 presence。
- record 顺序、字段集合和 optional flag 关系与 `Packet20FrozenBaseline` 一致；不要在没有非对称二维 fixture 和真实 legacy bytes 之前把 `x outer/y inner` 写成已证事实。
- submission 后修改外部 Tile 不改变编码。
- segment budget 超限、未知 flags 或未覆盖条件在 final submission 前拒绝。
- 失败不生成半成品 NetMessage。
- 不把 [20,70] 作为完整 Packet 20 record 集合的最大长度。
- 记录集合按静态 schema 顺序编码，而不是按外部集合的偶然枚举顺序编码。

**Step 2: 运行 focused test 确认失败**

~~~powershell
dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
~~~

Expected: 新测试因 Packet 20 submission 类型或 encoder 尚不存在而失败。

**Step 3: 实现 typed Tile submission**

- 使用 packet-specific PreparedTile20 和 PreparedPacket20。
- 构造时执行 checked(width * height)，并验证 records 数量完全匹配。
- 复制 Tile 数组和其中的可选值，防止外部 alias。
- 把 presence 放在 Tile scope 内；不要使用 packet-level 可变 mask 控制所有记录。
- 对不支持的 flags、未知组合和无法表示的字段显式拒绝。
- 将 wire order 作为 schema/encoder 的固定规则，不从上下文动态推导。

**Step 4: 实现 baseline 对齐的 encoder**

- 静态固定 header 和记录顺序。
- Encoder 只读取 submission，不读取 Main.tile、catalog 或 session。
- 通过独立 segment/budget 检查输出，不把 demo [20,70] 写成完整 Packet 20 上限。
- 生产 adapter 失败时返回明确失败结果，不返回半成品 NetMessage。
- typed submission 先编码为结构化 case-20 body，与 `Packet20FrozenBaseline` 做逐字节对照；这一步只能证明结构化 baseline body 一致。
- 再明确把结构化 body 转换为当前生产需要的 `TileDataPayload`，或者明确采用直接构造 `NetMessage` 绕过 object registry 的适配路径；不得把两条路径混为一个“兼容”结论。
- 分别验证四层输出：case-20 body bytes、`TileDataPayload` bytes、`NetMessage.MessageId`/payload，以及 `MessageFrame.ToPacketBytes()` 的完整 frame bytes。测试名称和失败信息必须指出具体哪一层不一致。
- 增加非对称二维 fixture（`Width != Height`，且每个坐标/记录使用可区分值），用真实 legacy bytes 或现有生产 reader 确认二维展开顺序和字段集合；在证据不足时，测试保持红灯并记录为迁移阻塞项，不将 `x outer / y inner` 或 frame 字段猜测写入实现。
- differential 断言必须覆盖 opaque payload translation 和完整 frame，而不是只比较结构化 baseline 的中间 body。

**Step 5: 运行 Packet 20 验证**

~~~powershell
dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
~~~

Expected: Packet 20 submission tests、frozen baseline、legacy differential、shape/budget negative cases 和现有 verification PASS。

**Step 6: 手工检查 wire 与依赖**

~~~powershell
rg -n "Main|World|Netplay|SessionContext|IServerSession|SendTarget|SendPipelineContext|MessageTcpSession|IBufferWriter|Func<|Lazy<" Core/Protocol/Packets/AreaTileChangePacket20Submission.cs Core/Protocol/Packets/AreaTileChangePacket20Projector.cs Core/Protocol/Packets/AreaTileChangePacket20SubmissionEncoder.cs Core/Protocol/Packets/AreaTileChangePacket20SubmissionAdapter.cs
~~~

Expected: submission/encoder 不出现领域全局、session、routing、writer 或延迟委托依赖。

**Step 7: 提交**

~~~powershell
git add Core/Protocol/Packets/AreaTileChangePacket20Submission.cs Core/Protocol/Packets/AreaTileChangePacket20Projector.cs Core/Protocol/Packets/AreaTileChangePacket20SubmissionEncoder.cs Core/Protocol/Packets/AreaTileChangePacket20SubmissionAdapter.cs Verification/ProtocolDemoStandalone/AreaTileChangePacket20SubmissionTests.cs Verification/ProtocolDemoStandalone/Program.cs
git commit -m "feat: add packet 20 prepared tile submission encoder"
~~~

## Task 4: 将已编码 NetMessage 接入发送 pipeline

**Files:**

- Modify: Core/Server/Pipeline/SendNetMessagePipeline/SendNetMessagePipeline.cs
- Create/modify: Verification/ProtocolDemoStandalone/PreparedSubmissionPipelineTests.cs
- Modify: Verification/ProtocolDemoStandalone/Program.cs
- Reuse: Core/Adaptation/MessageFrame.cs
- Reuse: Core/Protocol/PacketDefinitionRegistry.cs

当前生产发送入口是 `SendNetMessagePipeline.Execute(object, SendTarget, SendPipelineContext, int ignoreConnectionId = -1)`；其现有 object 路径会解析 recipient，随后调用 `PacketDefinitionRegistry.Write(message)`。Task 4 的测试和实现必须以这个真实入口为基线，不把 standalone harness 中的简化发送函数当成生产契约。

**Step 1: 写失败测试**

验证：

- 新窄入口接受已编码 NetMessage。
- 入口不再次调用 `PacketDefinitionRegistry.Write`；测试使用可观察的 registry writer 调用计数或等价探针证明没有二次编码。
- recipient selection、ignore、dedup 和发送顺序与旧入口一致。
- MessageFrame 仍负责 frame length、message id 和 payload。
- prepared encoder 不接收 session/routing。
- 发送失败不会把编码失败重解释为 recipient selection 成功。

**Step 2: 运行 focused test 确认失败**

~~~powershell
dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
~~~

Expected: 新 pipeline test 因窄入口尚不存在而失败。

**Step 3: 实现最小窄入口**

增加只消费 NetMessage/已编码结果的 pipeline 分支；保留现有 object/registry 入口作为兼容路径。新分支只做 recipient resolution、过滤和发送，不做 packet 语义编码。

新入口应接受已编码的 `NetMessage`（或仓库已有的等价不可变编码结果），复用现有 `SendTarget`、`SendPipelineContext` 和 recipient resolution 逻辑，但不得再次进入 `PacketDefinitionRegistry.Write(message)`。如果现有发送方法无法在不改变旧行为的情况下复用，应提取只负责 recipient resolution、ignore、dedup/order 的窄内部 helper，并让两条入口共享该 helper。

入口应清晰区分：

- ContextAdapter/projector/encoder 负责产生已编码 NetMessage。
- send pipeline 负责目标选择、忽略规则、去重和顺序。
- MessageFrame 负责 frame 级 length 和 message id/payload 交接。

**Step 4: 验证 framing 和 routing**

~~~powershell
dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
~~~

Expected: prepared submission pipeline test、现有 NetPipelineTests、MessageTcpRoundTripTests 和 framing tests PASS。

**Step 5: 提交**

~~~powershell
git add Core/Server/Pipeline/SendNetMessagePipeline/SendNetMessagePipeline.cs Verification/ProtocolDemoStandalone/PreparedSubmissionPipelineTests.cs Verification/ProtocolDemoStandalone/Program.cs
git commit -m "feat: route pre-encoded net messages through send pipeline"
~~~

## Task 5: 增加依赖护栏并完成 Packet 10 inventory

**Files:**

- Create: Verification/ProtocolDemoStandalone/SubmissionDependencyGuardTests.cs
- Create: docs/reports/2026-09-11-packet10-submission-boundary-inventory.md
- Modify only if needed: docs/README.md

**Step 1: 写失败护栏测试**

对 Packet 13/20 submission 和 encoder 的源码或编译依赖做检查，禁止依赖以下类型/标识：

~~~text
Main
World
Netplay
SessionContext
IServerSession
SendTarget
SendPipelineContext
MessageTcpSession
~~~

同时验证 submission 不包含 delegate、writer、cursor、routing plan 或 send target。

**Step 2: 运行护栏测试确认当前状态**

~~~powershell
dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
~~~

Expected: 在任何错误依赖加入时给出明确失败文件和标识；若当前实现尚未存在，则先让测试以目标类型缺失/未接线的方式红灯。

**Step 3: 实现可审计的 guard**

优先使用编译期依赖边界和 focused source scan；不要依赖脆弱的字符串黑名单来替代架构设计。至少检查以下四类可审计事实：目标文件清单是否只包含 submission/projector/encoder/adapter 的预期边界；public/internal 字段及属性类型是否引入 session、routing、writer 或 cursor；显式构造参数是否把这些依赖带入对象；项目编译引用是否把不应依赖的生产层带入 standalone 验证。

source scan 只作为诊断补充，必须输出具体文件、命中的符号、声明位置和修复方向；不能因为源码中没有某个字符串就推断依赖边界成立。guard 只检查 submission/encoder 的纯 wire 层；packet-specific ContextAdapter/projector 可以访问外部上下文，不应被 guard 误伤，但 adapter 仍不得把上下文引用保存进 immutable submission。

**Step 4: 记录 Packet 10 inventory**

文档只记录真实 legacy 证据：

- Deflate 边界；
- y outer/x inner；
- previous tile/pending run state；
- flag cascade；
- RLE short/long；
- Chest/Sign/TileEntity tail tables；
- legacy context/effect 依赖。

明确本任务不实现 Packet 10 公共 API，也不把 inventory 的未知项伪装成设计结论。

**Step 5: 运行护栏和文档检查**

~~~powershell
dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
git diff --check
~~~

Expected: guard PASS；文档无 whitespace error；Packet 10 inventory 明确列出未决问题。

**Step 6: 提交**

~~~powershell
git add Verification/ProtocolDemoStandalone/SubmissionDependencyGuardTests.cs docs/reports/2026-09-11-packet10-submission-boundary-inventory.md docs/README.md
git commit -m "test: guard submission dependencies and inventory packet 10"
~~~

## Task 6: 全量验证与迁移门禁

**Files:**

- Verify only: Tasks 0-5 的 changed files
- Do not modify unrelated user files solely to make status clean

**Step 1: 构建生产项目**

~~~powershell
dotnet build NetWork.csproj --no-restore
~~~

Expected: 在基线项目引用问题已解决且 Tasks 1-5 已完成后，exit code 0，生产项目 build succeeded；当前工作树已知会因根 `Concept` graph 源文件引用缺失而以 `CS2001` 失败，不能把该失败归因于本计划实现。

**Step 2: 运行 standalone verification**

~~~powershell
dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore
~~~

Expected: 在“执行前置条件”的基线引用问题已解决且 Tasks 1-5 已完成后，exit code 0，且 `Program.cs` 明确调用并输出 Segment Contract、Packet 13 submission、Packet 20 submission、opaque `TileDataPayload` translation、golden/differential、prepared-submission pipeline 和 dependency guard 的 PASS。默认被 `ProtocolDemoStandalone.csproj` 排除的 Graph tests 不得用 standalone 默认成功输出替代；当前工作树若仍有 `CS2001`，只能记录为基线阻塞。

**Step 3: 运行三个原型 transcript**

~~~powershell
dotnet run --project test4/context-binding-prototype/ContextBindingPrototype.csproj --no-restore -- --demo
dotnet run --project Verification/PacketContextBindingPrototype/PacketContextBindingPrototype.csproj --no-restore -- --demo
dotnet run --project Concept/ContextBindingPrototype/ContextBindingPrototype.csproj --no-restore -- --demo
~~~

Expected: 三个命令都以退出码 0 结束；如果 Task 0 尚未执行，最后一条应明确记录为已知失败，不能把整个迁移标记为完成。

**Step 4: 检查文档和工作树**

~~~powershell
git diff --check
git status --short
git diff --stat
~~~

Expected: 无 whitespace error；status 只显示本次改动和用户原有改动；不删除、不覆盖用户文件。

**Step 5: 逐项确认迁移门禁**

- 新旧完整 frame bytes 一致。
- 旧 reader/registry 能读新 frame。
- 同一 submission 重复编码一致。
- 非法 presence/shape/budget/segment length 在 final output 前拒绝。
- framing/routing/recipient selection/ignore/dedup/order 无回归。
- encoder 无领域上下文依赖。
- Packet 20 的结构化 body、opaque `TileDataPayload`、`NetMessage` 和 `MessageFrame` 之间存在可复核的转换和输出证据；如果任一层只凭概念原型或生成 Graph 元数据推断，Packet 20 不得标记为 migrated。

**Step 6: 提交最终验证结果**

~~~powershell
git diff --check
git status --short
git diff --stat
~~~

Expected: 输出可直接附在交付记录中；只有全部门禁通过，才把对应 packet 标记为 migrated。不要用“代码已写入”代替运行证据。

## 交付后的判定

完成本计划不等于 Packet 10 已迁移，也不要求所有 packet 共享一个公共 submission 基类。可交付结果是：

- Packet 13 和 Packet 20 各自有可验证的 typed submission/projector/encoder/adapter；
- Segment Contract 有明确读写边界和 staging 语义；
- pre-encoded NetMessage 可以进入现有发送 pipeline；
- 所有迁移门禁都有命令输出证据；
- Packet 10 的边界 inventory 已形成，但没有未经证据支持的公共 RLE/Deflate API；
- Concept 最小原型、test4 Packet 20 原型和 Verification Packet 13 原型的证据状态在文档中保持诚实可追溯。
