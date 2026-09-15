# 原型项目级隔离设计

**日期：** 2026-09-15
**状态：** 已确认设计，执行计划已编写并完成实际迁移
**范围：** 当前 Networking 仓库中的所有概念探索、验证程序、工具和最接近生产形态的网络实现

## 1. 背景和目标

当前仓库同时包含多条概念探索线：网络协议框架、Graph/Layout、Packet Layout AST、Slot-DAG、上下文绑定、Packet 13/20 submission、Segment Codec、Source Generator 和 Packet 88 性能实验。它们现在分散在 `Concept/`、`test4/`、`Core/`、`Verification/`、`Tools/` 和 `Generators/` 中，并且部分验证项目通过 `Compile Include` 直接引用其他目录的源码。

这些代码即使其中一部分已经接近生产形态，当前仓库仍然是实验级探索仓库。目标不是声明一个正式生产核心层，而是把每条概念探索线变成一个可独立构建、运行和删除的原型项目。

本设计的目标是：

- 一个概念探索对应一个语义命名的顶层原型目录。
- 原型目录拥有该探索所需的全部运行时代码、适配器、测试、工具和文档入口。
- 原型之间不共享源码，也不通过项目引用共享运行时程序集。
- 当前顶层 `Core/` 不再作为共享生产层保留。
- 代码虽然可以采用生产级实现方式，但仍明确标记为所属原型的实验代码。
- 每个原型都可以脱离其他原型单独恢复、构建、验证和归档。

## 2. 设计决策

### 2.1 按概念归属分组

不按当前技术目录搬迁，也不简单把现有项目编号为“原型 1、原型 2”。目录名使用概念名称，目录边界由“它要验证的设计问题”决定。

同一个概念可以在一个原型目录内包含多个内部项目。例如 Graph 原型可以同时包含模型库、bootstrap、exporter 和验证入口；这些内部项目都必须位于该原型目录内，不能从另一个顶层原型借用源码。

### 2.2 原型目录自包含

每个原型目录至少包含：

- 一个 README，说明设计问题、边界、非目标、运行方式和实验结论。
- 一个或多个只属于该原型的 `.csproj`。
- 该原型需要的源码、测试、测试夹具和工具。
- 原型自己的输出路径和中间文件路径配置。

如果两个原型都需要 `MessageFrame`、`PacketType` 或基础 packet 类型，各自保留自己的副本。重复代码是本次隔离的有意代价，不能通过重新建立共享 `Core`、共享 runtime 项目或跨目录 `Compile Include` 来消除。

### 2.3 允许的共享内容

以下内容可以保留在仓库级别，因为它们不是运行时概念代码：

- `docs/` 中的设计、执行计划、研究和报告。
- 根级 `.gitignore`。
- 只包含通用构建输出约定的根级 `Directory.Build.props`；它不得引入源码、项目引用或概念特定的编译项。
- `Build/` 中的生成输出。每个原型必须使用不会相互覆盖的输出子目录。
- `Prototypes/README.md` 这样的原型索引文档。

原型 README 可以链接到 `docs/` 中的历史设计文档，但运行时构建不能依赖这些 Markdown 文件。

## 3. 目标目录布局

目标顶层布局如下：

```text
Prototypes/
├─ external-context-wire-submission/
│  ├─ src/
│  │  ├─ Messages/
│  │  ├─ Protocol/
│  │  ├─ Server/
│  │  └─ Adaptation/
│  ├─ tests/
│  │  ├─ ProtocolDemoStandalone/
│  │  └─ LegacyTrHostWorker/
│  ├─ ExternalContextWireSubmission.csproj
│  └─ README.md
├─ graph-layout-codegen/
│  ├─ src/
│  ├─ tools/
│  ├─ tests/
│  ├─ GraphLayoutCodegen.csproj
│  └─ README.md
├─ minimal-dependency-graph/
│  ├─ src/
│  ├─ tests/
│  ├─ MinimalDependencyGraph.csproj
│  └─ README.md
├─ packet-layout-ast/
│  ├─ src/
│  ├─ tests/
│  ├─ PacketLayoutAst.csproj
│  └─ README.md
├─ slot-graph-ir/
│  ├─ slot-dag-prototype.html
│  ├─ slot-dag-feasibility.md
│  └─ README.md
├─ immutable-context-binding/
│  ├─ ContextBindingPrototype.cs
│  ├─ Program.cs
│  ├─ ContextBindingPrototype.csproj
│  └─ README.md
├─ packet20-local-state/
│  ├─ ContextBindingLogic.cs
│  ├─ Program.cs
│  ├─ ContextBindingPrototype.csproj
│  └─ README.md
├─ packet13-segment-codec/
│  ├─ WireSegmentPrototype.cs
│  ├─ Program.cs
│  ├─ Packet13SegmentCodec.csproj
│  └─ README.md
├─ typed-span-sourcegen/
│  ├─ src/
│  ├─ tests/
│  ├─ TypedSpanSourceGen.csproj
│  └─ README.md
└─ packet88-performance/
   ├─ src/
   ├─ tests/
   ├─ Packet88Performance.csproj
   └─ README.md
```

目录名是语义名称，不表示成熟度。`external-context-wire-submission` 虽然包含目前最接近生产的实现，仍然必须在 README 中标注为实验原型。

## 4. 现有资产归属

### 4.1 External Context Wire Submission

这是当前最完整、最接近生产网络出站路径的一组实验。它拥有自己的完整协议运行时副本和验证路径。

迁入内容包括：

- 当前 `Core/Messages/**`、`Core/Protocol/**`、`Core/Server/**`、`Core/Adaptation/**`，迁入该原型的 `src/`。
- 当前 `Core/Protocol/Wire/**`，包括 Segment Contract。
- Packet 13 和 Packet 20 的 projector、submission、encoder、adapter。
- `Verification/ProtocolDemoStandalone/**` 中属于该出站迁移的测试、legacy differential、framing、pipeline、golden wire 和 dependency guard。
- `Verification/LegacyTrHostWorker/**` 及其仅供该验证路径使用的桥接代码。
- 当前根级 `NetWork.csproj` 的构建职责，重建为该原型自己的项目文件。

Graph-only 测试、Packet Layout AST 测试、Packet 88 benchmark 和独立 Context Binding 原型不归入这里；它们迁入各自的原型目录，并在该原型内拥有需要的测试夹具副本。

### 4.2 Graph Layout Codegen

该原型验证静态依赖图、布局、Graph manifest、codec emitter 和 Graph exporter 的组合。

迁入内容包括：

- `Concept/test1/**` 和 `Concept/test2/**` 中属于 Graph/Layout/codec generation 的源码。
- `Tools/NetWork.Concept.GraphBootstrap/**`。
- `Tools/NetWork.Concept.GraphExporter/**`。
- `Verification/ProtocolDemoStandalone/**` 中的 Graph tests、Graph export tests 和该原型专用 fixtures。
- Graph 原型需要的 `BitsByte`、`PacketType`、packet model 等代码副本。

该目录内部可以保留 bootstrap 到 exporter 的项目引用，但引用目标必须仍位于 `graph-layout-codegen/` 内。

### 4.3 Minimal Dependency Graph

该原型只验证最小依赖图节点、拓扑顺序和循环检测，不包含 Graph/Layout 原型的 packet model、codec emitter 或 exporter。它是独立的最小实验，不能因为都使用了 `PacketNode` 这个名称就与 Graph 原型合并。

迁入内容包括：

- `Concept/PacketNode.cs`。
- `Verification/ProtocolDemoStandalone/MinimalDependencyGraphTests.cs`。
- 该测试所需的独立运行入口和项目文件。

### 4.4 Packet Layout AST

该原型只验证协议布局 AST、静态分析和诊断模型，不把 AST demo 误认为生产 codec。

迁入内容包括：

- `Concept/test3/PacketLayoutAstDemo.cs`。
- `Verification/PacketLayoutAstDemo/**`。
- 仅该 demo 需要的 packet model 和运行入口副本。

### 4.5 Slot Graph IR

该原型是静态浏览器原型，不强行转换成 C# 生产项目。

迁入内容包括：

- `test4/slot-dag-prototype.html`。
- `test4/slot-dag-feasibility.md`。
- 与该页面直接绑定的说明和运行入口。

它可以没有 `.csproj`，但必须拥有 README，说明直接打开 HTML 的方式和它不参与任何 C# 构建的事实。

### 4.6 Immutable Context Binding

该原型验证外部状态快照、不可变提交物、固定/有界 segment 和失败不覆盖既有输出。

迁入内容包括：

- `Concept/ContextBindingPrototype/**`。

### 4.7 Packet 20 Local State

该原型验证 checked shape、逐 Tile presence、局部 optional value 和 budget failure 语义。

迁入内容包括：

- `test4/context-binding-prototype/**`。

### 4.8 Packet 13 Segment Codec

该原型验证 Fixed/Bounded、Exact/ParentDelimited、staging 原子提交、registry freeze 和 Packet 13 projection。

迁入内容包括：

- `Verification/PacketContextBindingPrototype/**`。

### 4.9 Typed Span Source Generation

该原型验证 C# source generator、typed span codec 和生成代码边界。

迁入内容包括：

- `Generators/NetWork.Concept.SourceGen/**`。
- 当前属于 source generation 的验证代码和最小输入模型副本。

它不直接引用 `external-context-wire-submission` 或 Graph 原型；需要测试生成结果时，在本目录内保留最小测试输入和生成结果验证项目。

### 4.10 Packet 88 Performance

该原型验证固定 Packet 88 codec 与 Graph codec 的性能和 wire 等价性。

迁入内容包括：

- `Verification/Packet88PerformanceStandalone/**`。
- 它当前从 `ProtocolDemoStandalone` 引用的必要 packet model、Graph concept、codec 和 wire fixture 的副本。

迁移后不能继续通过 `<ProjectReference Include="..\ProtocolDemoStandalone\...">` 跨原型复用验证程序集。

## 5. 项目和依赖边界

### 5.1 顶层原型隔离

每个顶层原型必须满足：

```text
prototype A source -> only prototype A source or external package
prototype A project -> only prototype A project or external package
```

禁止：

- `Compile Include` 指向另一个 `Prototypes/<name>`。
- `ProjectReference` 指向另一个顶层原型。
- 指向迁移前的 `Core/`、`Concept/`、`Verification/`、`Tools/` 或 `Generators/`。
- 通过根级共享 runtime 项目间接复用代码。
- 通过生成目录、隐式 glob 或符号链接绕过项目边界。

### 5.2 原型内部项目

同一个原型目录内可以有多个项目，例如 library、bootstrap、tool 和 test runner。内部项目引用必须使用该目录内的明确相对路径，并在 README 中列出构建顺序。

如果一个原型只有一个可执行入口，优先使用一个项目，避免为简单 demo 引入不必要的内部项目层次。

### 5.3 Namespace 和程序集

迁移时优先保留实验代码的 namespace 语义，只有在旧 namespace 会造成不同原型之间类型冲突或会误导为正式生产 API 时才修改。每个原型使用唯一的程序集名称前缀，例如：

- `NetWork.Prototype.ExternalContextWireSubmission.*`
- `NetWork.Prototype.GraphLayoutCodegen.*`
- `NetWork.Prototype.PacketLayoutAst.*`

程序集名称不能继续使用会暗示共享生产库的 `NetWork.Core`。

## 6. 文档组织

- 全局设计、执行计划、研究和报告继续保留在 `docs/`，以保留跨原型的历史决策链。
- `docs/README.md` 增加 `Prototypes/README.md` 的入口，并删除指向旧源码目录作为运行入口的描述。
- 每个原型的 README 记录：问题、假设、输入、输出、非目标、运行命令、验证结果和已知限制。
- 生产迁移计划中的“生产路径”措辞改成“该原型的集成路径”，除非有独立发布和部署证据，否则不宣称正式生产代码。
- 迁移计划、原型报告和已有历史文档中的旧路径需要同步更新；历史文档可以保留原始路径说明，但要标注“迁移前路径”。

## 7. 迁移和验证策略

迁移不改变原型内部的协议语义，先保持现有行为，再逐个切断跨目录依赖：

1. 创建 `Prototypes/` 和每个原型的 README、项目骨架。
2. 按归属表迁移源码、测试、工具和生成器。
3. 将 `Core/` 的内容改为 `external-context-wire-submission/src/` 的本地代码，不建立共享替代层。
4. 重写 `.csproj`，移除所有旧目录 `Compile Include` 和跨原型 `ProjectReference`。
5. 按原型修正 namespace、输出目录、生成目录、运行命令和文档链接。
6. 先构建每个原型的最小项目，再运行其完整 demo/test 入口。
7. 通过静态检查确认旧共享目录和跨原型引用已经消失。
8. 更新全局 README 和设计/报告中的当前路径。

## 8. 验收标准

结构验收：

- 所有运行时代码都位于某个 `Prototypes/<name>/` 下。
- 顶层不存在共享 `Core/` 运行时代码。
- 顶层旧的 `Concept/`、`Verification/`、`Tools/`、`Generators/` 代码目录不再作为源码入口存在。
- 每个原型至少有 README 和可定位的项目或静态运行入口。
- 任意原型目录都不需要先构建另一个顶层原型。

依赖验收：

- `Prototypes/` 下没有指向顶层原型之外源码的 `Compile Include`。
- `Prototypes/` 下没有跨顶层原型的 `ProjectReference`。
- 没有对迁移前 `Core/`、`Concept/`、`Verification/`、`Tools/`、`Generators/` 的构建引用。
- 每个原型的 output/intermediate/generated 文件不会覆盖其他原型。

行为验收：

- 原有各原型 README 中的运行命令在新路径下可执行。
- Context Binding、Packet 20 Local State、Packet 13 Segment Codec 三个原型的 `--demo` 结果保持通过。
- Graph、AST、Slot Graph、SourceGen、Packet 88 benchmark 和 External Context Wire Submission 各自使用自己的验证入口。
- External Context Wire Submission 的 submission、wire、framing、pipeline 和 dependency guard 验证仍在其自身目录内完成。
- 迁移不会把实验代码重新包装成仓库级生产 API。

## 9. 非目标

- 本次不合并不同原型的算法或类型。
- 本次不抽取新的共享 `Common`、`Core` 或基础类库。
- 本次不修复原型内部尚未解决的协议事实，除非迁移导致项目无法构建且修复是保持原行为所必需的。
- 本次不把所有 demo 强行改成同一种测试框架。
- 本次不删除历史设计文档、研究证据或实验结论；只更新当前路径和状态说明。
- 本次不因为某个原型拥有生产级命名、完整 pipeline 或 golden tests，就把它标记为正式生产实现。

## 10. 完成定义

只有同时满足以下条件，原型隔离才算完成：

```text
每个概念探索有语义命名的自包含目录
所有运行时代码归属于某个原型目录
不存在共享 Core 生产层
不存在跨顶层原型源码和项目引用
每个原型有独立运行/验证入口
文档和命令反映新目录
结构检查和逐原型构建/运行均通过
```
