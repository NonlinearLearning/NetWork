# 原型项目级隔离实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将当前仓库中的每条概念探索重组为语义命名、可独立构建和运行的原型项目，并移除顶层共享 `Core/` 运行时层。

**Architecture:** 以 `Prototypes/<concept-name>/` 为隔离边界。每个原型目录拥有自己的源码、测试、工具、项目文件和 README；同一原型内部允许多个项目，但不同顶层原型之间不共享源码、程序集或项目引用。当前最接近生产形态的网络框架与 submission 实现整体归入 `external-context-wire-submission`，不再作为仓库级生产层存在。

**Tech Stack:** C#/.NET 10、C# source generator（`netstandard2.0`）、legacy `net48` host、PowerShell、静态 HTML 原型、现有 NuGet 依赖和 Git。

**执行记录（2026-09-15）：** 该计划已按批次执行。源码、验证工具和生成器已迁移到十个语义原型目录；根 `NetWork.csproj`、顶层 `Core/`、`Concept/`、`Verification/`、`Tools/`、`Generators/` 和 `test4/` 已不再作为活动源码入口。全量 14 个原型项目构建通过；Minimal Graph、Graph Bootstrap/Exporter/Verification、AST、三条 Context Binding/Segment Codec demo、Typed Span SourceGen smoke、Packet 88 `--verify` 和 External Context 的六个 focused 入口（含 Packet 20 legacy differential）均在迁移后实际运行通过。External Context 默认完整入口仍需要与冻结 golden 匹配的 legacy Terraria assembly；当前环境解析到的 assembly 输出 `Terraria318`，而冻结快照要求 `Terraria315`。

---

## 执行约束

- 先完成本计划的结构迁移，再判断原型内部行为是否需要修复；不要借迁移机会重写协议算法。
- 保留当前工作树中的用户改动，尤其是未跟踪的 `test4/`、`Verification/PacketContextBindingPrototype/` 和文档文件；迁移前先记录它们的归属，不使用 `git reset`、`git checkout` 或清理命令。
- 不把任何源码复制回新的共享 `Core/`、`Common/` 或基础类库。
- 每个步骤完成后运行该步骤的 focused 检查，再进行下一步；失败时保留失败证据并修正当前边界。
- 只有在目标原型内部的所有验证通过后，才删除旧的顶层源码入口。
- 目录移动使用可追踪的移动操作；对未跟踪文件先确认目标不存在，再移动，不覆盖用户文件。
- 本计划不修改 `D:\TRbackup\无任何删减通过编译\Terraria` 等仓库外参考源码。

## Task 1: 固定迁移清单和当前基线

**Files:**

- Read only: `git status`, all existing `.csproj`, `Directory.Build.props`
- Reference: `docs/plans/2026-09-15-prototype-isolation-design.md`
- Create later: `Prototypes/README.md`

**Step 1: 记录工作树状态**

Run:

```powershell
git status --short --branch
git diff --stat
rg --files -g '*.csproj' -g '*.cs' -g '*.html' -g '*.md' | Sort-Object
```

Expected: 输出当前用户改动和完整源文件清单；不修改任何文件。

**Step 2: 固定旧项目引用**

Run:

```powershell
rg -n "<ProjectReference|<Compile Include|Core[\\/]|Concept[\\/]|Verification[\\/]|Tools[\\/]|Generators[\\/]" --glob '*.csproj' --glob '*.props' --glob '*.targets' .
```

Expected: 输出所有需要迁移的跨目录编译项和项目引用，作为后续静态验收的基线。

**Step 3: 建立归属检查表**

根据设计文档逐项确认以下来源至少有一个目标原型：`Core/**`、`Concept/**`、`test4/**`、`Tools/**`、`Generators/**`、`Verification/PacketContextBindingPrototype/**`、`Verification/PacketLayoutAstDemo/**`、`Verification/Packet88PerformanceStandalone/**`、`Verification/ProtocolDemoStandalone/**` 和 `Verification/LegacyTrHostWorker/**`。迁移过程中确认 `Concept/PacketNode.cs` 是独立的最小依赖图实验，因此目标原型总数为 10 个，不再把它归入 Graph 原型。

Expected: 没有“暂时留在根目录”的运行时代码；只有文档、构建配置和生成输出可以暂时保留在根目录。

**Step 4: 固定迁移前 focused 验证状态**

Run only when the current project can resolve its existing references:

```powershell
dotnet build .\Concept\ContextBindingPrototype\ContextBindingPrototype.csproj --no-restore
dotnet build .\test4\context-binding-prototype\ContextBindingPrototype.csproj --no-restore
dotnet build .\Verification\PacketContextBindingPrototype\PacketContextBindingPrototype.csproj --no-restore
dotnet build .\Verification\PacketLayoutAstDemo\PacketLayoutAstDemo.csproj --no-restore
```

Expected: record each result. Existing root `Concept/*.cs` path failures are baseline evidence and must be fixed by localizing the owning prototype, not by adding a new shared path.

## Task 2: Create the prototype workspace skeleton

**Files:**

- Create: `Prototypes/README.md`
- Create: `Prototypes/external-context-wire-submission/README.md`
- Create: `Prototypes/graph-layout-codegen/README.md`
- Create: `Prototypes/packet-layout-ast/README.md`
- Create: `Prototypes/slot-graph-ir/README.md`
- Create: `Prototypes/immutable-context-binding/README.md`
- Create: `Prototypes/packet20-local-state/README.md`
- Create: `Prototypes/packet13-segment-codec/README.md`
- Create: `Prototypes/typed-span-sourcegen/README.md`
- Create: `Prototypes/packet88-performance/README.md`

**Step 1: Create only the directories and README placeholders**

Each README must state the hypothesis, scope, non-goals, current status, and placeholder build/run commands. Do not add project references yet.

**Step 2: Add prototype index**

`Prototypes/README.md` must contain a table mapping each semantic prototype to its design question, project entry point, and status. It must explicitly state that all prototypes are experimental and that there is no shared production `Core`.

**Step 3: Run the skeleton check**

Run:

```powershell
Get-ChildItem .\Prototypes -Directory | Sort-Object Name
Get-ChildItem .\Prototypes -Recurse -File -Filter README.md | Sort-Object FullName
```

Expected: exactly the ten prototype directories from the design, each with one README. If this task resumes after partial migration, verify the same invariant against the current moved files instead of moving already-owned files again.

## Task 3: Isolate External Context Wire Submission

**Files:**

- Move: `Core/**` -> `Prototypes/external-context-wire-submission/src/**`
- Move: selected `Verification/ProtocolDemoStandalone/**` -> `Prototypes/external-context-wire-submission/tests/ProtocolDemoStandalone/**`
- Move: `Verification/LegacyTrHostWorker/**` -> `Prototypes/external-context-wire-submission/tests/LegacyTrHostWorker/**`
- Create: `Prototypes/external-context-wire-submission/ExternalContextWireSubmission.csproj`
- Create: `Prototypes/external-context-wire-submission/tests/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj`
- Create: `Prototypes/external-context-wire-submission/tests/LegacyTrHostWorker/LegacyTrHostWorker.csproj`
- Modify: `Prototypes/external-context-wire-submission/README.md`

The External Context prototype owns the complete local copy of the current network runtime. It must include `Messages`, `Protocol`, `Server`, `Adaptation`, `Protocol/Wire`, Packet 13/20 submission code, and the non-Graph protocol/framing/pipeline tests.

**Step 1: Move the runtime source into the prototype**

Move the existing `Core/` children into `Prototypes/external-context-wire-submission/src/`, preserving relative subdirectories. Do not create a `Prototypes/Core` or root `Core` compatibility directory.

Expected: all runtime source has a single owner; no file remains under root `Core/`.

**Step 2: Split the verification files by concept**

Move into this prototype only the tests for protocol framework, framing, stream conformance, legacy differential, Packet 13/20 submission, prepared pipeline, and dependency guard. Keep Graph tests, Packet Layout AST tests, Packet 88 benchmark files, and the standalone Context Binding prototype for their own tasks.

The first local test project must include the local `src/**/*.cs` and its local verification files. It must not use `Compile Include="..\\..\\Core..."`.

**Step 3: Localize the legacy host**

Keep the `net48` host as a separate project under the same prototype directory. Update all bridge paths so the `net10.0` verification project references only this local host project with the existing `ReferenceOutputAssembly="false"` behavior.

**Step 4: Rebuild the local project files**

Use explicit local compile items where generated files or test exclusions require them. The local project files may reference each other inside `external-context-wire-submission/`; they may not reference another `Prototypes/<name>` directory.

Run:

```powershell
dotnet build .\Prototypes\external-context-wire-submission\ExternalContextWireSubmission.csproj --no-restore
dotnet build .\Prototypes\external-context-wire-submission\tests\LegacyTrHostWorker\LegacyTrHostWorker.csproj --no-restore
dotnet build .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore
```

Expected: compilation uses only local source paths. If a missing type appears, copy the required source into this prototype and document the duplication; do not restore a cross-prototype reference.

**Step 5: Run the integrated verification**

Run:

```powershell
dotnet run --project .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore -- --focused segments
dotnet run --project .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore -- --focused packet13
dotnet run --project .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore -- --focused packet20
dotnet run --project .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore -- --focused prepared-pipeline
dotnet run --project .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore -- --focused guard
```

Expected: Segment Contract, Packet 13 submission, Packet 20 submission, legacy runtime, prepared pipeline, golden wire, framing and dependency guard checks report the same pass conditions as before migration.

## Task 4: Isolate Graph Layout and Codegen

**Files:**

- Move: `Concept/test1/**` -> `Prototypes/graph-layout-codegen/src/`
- Move: `Concept/test2/**` -> `Prototypes/graph-layout-codegen/src/`
- Move: `Tools/NetWork.Concept.GraphBootstrap/**` -> `Prototypes/graph-layout-codegen/tools/GraphBootstrap/`
- Move: `Tools/NetWork.Concept.GraphExporter/**` -> `Prototypes/graph-layout-codegen/tools/GraphExporter/`
- Move: Graph-only verification files from `Verification/ProtocolDemoStandalone/**` -> `Prototypes/graph-layout-codegen/tests/`
- Create or modify internal Graph project files and `Prototypes/graph-layout-codegen/README.md`

**Step 1: Keep one API generation line**

The selected Graph model version, packet examples, bootstrap and exporter must compile together. Do not combine incompatible `Concept/test1` and `Concept/test2` files merely because names match; preserve the API mapping documented by the existing Graph tests.

**Step 2: Localize tool references**

Rewrite GraphBootstrap and GraphExporter project files so every `Compile Include` and `ProjectReference` resolves inside `Prototypes/graph-layout-codegen/`. Keep the `QuikGraph` package reference only in this prototype.

**Step 3: Localize generated output**

Move Graph manifest and generated codec output under this prototype's build directory. Remove conditions in the root `Directory.Build.props` that assume the old project names; replace them with local settings if generation still needs them.

**Step 4: Verify Graph behavior**

Run the local bootstrap/exporter command from the README, including the existing exporter contract:

```powershell
dotnet run --project .\Prototypes\graph-layout-codegen\tools\GraphExporter\NetWork.Concept.GraphExporter.csproj -- --output .\Prototypes\graph-layout-codegen\Build\generated
```

Expected: manifest and generated codecs are written only below the Graph prototype; Graph verification passes without loading External Context source.

## Task 5: Isolate Minimal Dependency Graph, Packet Layout AST and Slot Graph IR

**Files:**

- Move: `Concept/PacketNode.cs` -> `Prototypes/minimal-dependency-graph/src/`
- Move: `Verification/ProtocolDemoStandalone/MinimalDependencyGraphTests.cs` -> `Prototypes/minimal-dependency-graph/tests/`
- Create: `Prototypes/minimal-dependency-graph/MinimalDependencyGraph.csproj`, `Program.cs`, and `README.md`
- Move: `Concept/test3/PacketLayoutAstDemo.cs` -> `Prototypes/packet-layout-ast/src/`
- Move: `Verification/PacketLayoutAstDemo/**` -> `Prototypes/packet-layout-ast/tests/`
- Move: `test4/slot-dag-prototype.html` -> `Prototypes/slot-graph-ir/`
- Move: `test4/slot-dag-feasibility.md` -> `Prototypes/slot-graph-ir/`

**Step 1: Verify the minimal dependency graph prototype**

Run:

```powershell
dotnet build .\Prototypes\minimal-dependency-graph\MinimalDependencyGraph.csproj --no-restore
dotnet run --project .\Prototypes\minimal-dependency-graph\MinimalDependencyGraph.csproj --no-restore
```

Expected: `Minimal dependency graph prototype passed.` and no source reference to Graph or External Context.

**Step 2: Rebuild AST project with local sources**

Remove the missing root `Concept/PacketLayoutAstDemo.cs` link and compile the actual AST source from the local `src/` directory. Include only the local test runner and the minimum packet declaration model it needs.

Run:

```powershell
dotnet build .\Prototypes\packet-layout-ast\PacketLayoutAst.csproj --no-restore
dotnet run --project .\Prototypes\packet-layout-ast\PacketLayoutAst.csproj --no-restore
```

Expected: `PacketLayout AST demo passed.` and no reference to another prototype.

**Step 3: Package the browser prototype**

Update `slot-graph-ir/README.md` with the direct-open command/path and explicitly state that this prototype does not participate in the C# build. Keep its HTML, feasibility report and any directly used assets in one directory.

## Task 6: Isolate the three Context Binding experiments

**Files:**

- Move: `Concept/ContextBindingPrototype/**` -> `Prototypes/immutable-context-binding/`
- Move: `test4/context-binding-prototype/**` -> `Prototypes/packet20-local-state/`
- Move: `Verification/PacketContextBindingPrototype/**` -> `Prototypes/packet13-segment-codec/`
- Modify: each local README and project file only as needed for new paths

**Step 1: Preserve the minimal immutable binding prototype**

Run:

```powershell
dotnet build .\Prototypes\immutable-context-binding\ContextBindingPrototype.csproj --no-restore
dotnet run --project .\Prototypes\immutable-context-binding\ContextBindingPrototype.csproj --no-restore -- --demo
```

Expected: the scripted transcript proves external mutation does not change the bound submission and failed staging does not overwrite committed output.

**Step 2: Preserve the Packet 20 local-state prototype**

Move its local `Directory.Build.props` with it or replace it with project-local output paths. Run:

```powershell
dotnet build .\Prototypes\packet20-local-state\ContextBindingPrototype.csproj --no-restore
dotnet run --project .\Prototypes\packet20-local-state\ContextBindingPrototype.csproj --no-restore -- --demo
```

Expected: shape multiplication, tile count, local presence, optional values and budget rejection transcript passes.

**Step 3: Preserve the Packet 13 Segment Codec prototype**

Run:

```powershell
dotnet build .\Prototypes\packet13-segment-codec\Packet13SegmentCodec.csproj --no-restore
dotnet run --project .\Prototypes\packet13-segment-codec\Packet13SegmentCodec.csproj --no-restore -- --demo
```

Expected: Fixed/Bounded, Exact/ParentDelimited, staging atomicity, registry freeze and Packet 13 deterministic transcript passes.

## Task 7: Isolate Typed Span SourceGen and Packet 88 Performance

**Files:**

- Move: `Generators/NetWork.Concept.SourceGen/**` -> `Prototypes/typed-span-sourcegen/src/`
- Move: source-generator-specific verification inputs -> `Prototypes/typed-span-sourcegen/tests/`
- Move: `Verification/Packet88PerformanceStandalone/**` -> `Prototypes/packet88-performance/`
- Create: local project files and READMEs

**Step 1: Build SourceGen locally**

Keep the analyzer package versions and `netstandard2.0` target, but remove any path or generated-output dependency on the root project. Add a local smoke input that proves the generator assembly can build and the generated source is consumed within this prototype.

Run:

```powershell
dotnet build .\Prototypes\typed-span-sourcegen\TypedSpanSourceGen.csproj --no-restore
```

Expected: the generator builds without loading any source from another prototype.

**Step 2: Remove Packet 88 cross-prototype project reference**

The current Packet 88 project references `Verification/ProtocolDemoStandalone`. Replace that reference with local copies of the packet model, Graph concept, generated codec fixture and wire test support that the benchmark actually uses. Do not reference `external-context-wire-submission` or `graph-layout-codegen`.

Run:

```powershell
dotnet build .\Prototypes\packet88-performance\Packet88Performance.csproj --no-restore
dotnet run --project .\Prototypes\packet88-performance\Packet88Performance.csproj --no-restore -- --verify
```

Expected: frozen/generated/Graph wire equivalence and benchmark configuration checks pass from local source only. Use `--benchmark` only after `--verify` passes.

## Task 8: Remove old top-level project and source boundaries

**Files:**

- Modify: `README.md`
- Modify: `docs/README.md`
- Modify: relevant `docs/01-总体设计.md`, `docs/04-传输适配与验证.md`, research and report path references
- Remove after migration: root `NetWork.csproj`
- Remove after migration: empty or obsolete top-level source directories `Core/`, `Concept/`, `Verification/`, `Tools/`, `Generators/`, `test4/`
- Modify: root `Directory.Build.props` to remove project-specific legacy paths or move them into owning prototype directories

**Step 1: Update repository entry points**

Replace commands such as `dotnet build .\NetWork.csproj` and `dotnet run --project .\Verification\ProtocolDemoStandalone\...` with links to the owning prototype README and its local project path.

**Step 2: Update historical references carefully**

Keep historical plan/research conclusions, but mark old paths as migration-time paths where necessary. Current navigation docs must point to `Prototypes/`.

**Step 3: Remove only empty/obsolete source roots**

Before removal, verify each directory contains no file that is not already present in an owning prototype. Do not recursively delete broad paths. Remove only exact empty directories or tracked files whose ownership was verified in Task 1.

**Step 4: Verify no root runtime project remains**

Run:

```powershell
Get-ChildItem -Force | Select-Object Name,Mode
rg --files -g '*.csproj' -g '*.cs' -g '*.html' | Sort-Object
```

Expected: runtime source and project files are under `Prototypes/`; root contains docs, build policy, generated output and repository metadata only.

## Task 9: Enforce static isolation

**Files:**

- Verify: every `Prototypes/**.csproj`
- Modify only if required: prototype project files, `Prototypes/README.md`

**Step 1: Check compile includes**

Run:

```powershell
rg -n "<Compile Include=.*(\\.\\.\\|Core[\\/]|Concept[\\/]|Verification[\\/]|Tools[\\/]|Generators[\\/])" Prototypes --glob '*.csproj'
```

Expected: no result. Local `src/**/*.cs` and local test paths are allowed.

**Step 2: Check project references**

Run:

```powershell
rg -n "<ProjectReference" Prototypes --glob '*.csproj'
```

For every match, resolve the absolute path and confirm it remains inside the same top-level `Prototypes/<name>/` directory. Expected: no cross-top-level reference.

**Step 3: Check old source names**

Run:

```powershell
rg -n "(^|[\\/])(Core|Concept|Verification|Tools|Generators)([\\/]|$)" Prototypes README.md docs --glob '*.csproj' --glob '*.props' --glob '*.targets' --glob '*.md'
```

Expected: only historical-path notes or migration documentation remain; no active build command or project reference uses the old source roots.

**Step 4: Check generated/output isolation**

Run:

```powershell
rg -n "BaseOutputPath|OutputPath|BaseIntermediateOutputPath|IntermediateOutputPath|GraphCodeGenDirectory" Prototypes --glob '*.csproj' --glob '*.props'
```

Expected: each prototype's generated files point to a unique path.

## Task 10: Build and run every prototype

**Files:**

- Verify only: all local prototype projects and README commands

**Step 1: Enumerate projects**

Run:

```powershell
Get-ChildItem .\Prototypes -Recurse -File -Filter *.csproj | Sort-Object FullName
```

Expected: every C# prototype project is listed under exactly one prototype directory.

**Step 2: Build each project separately**

Run each command from the README, and additionally run:

```powershell
Get-ChildItem .\Prototypes -Recurse -File -Filter *.csproj | ForEach-Object {
    dotnet build $_.FullName --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $($_.FullName)" }
}
```

Expected: every project builds without first building a project in another top-level prototype. If restore is needed, run the same loop without `--no-restore` and record network/package requirements.

**Step 3: Run executable and scripted demos**

At minimum run:

```powershell
dotnet run --project .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore
dotnet run --project .\Prototypes\graph-layout-codegen\tools\GraphExporter\NetWork.Concept.GraphExporter.csproj --no-restore -- --output .\Prototypes\graph-layout-codegen\Build\generated
dotnet run --project .\Prototypes\packet-layout-ast\PacketLayoutAst.csproj --no-restore
dotnet run --project .\Prototypes\immutable-context-binding\ContextBindingPrototype.csproj --no-restore -- --demo
dotnet run --project .\Prototypes\packet20-local-state\ContextBindingPrototype.csproj --no-restore -- --demo
dotnet run --project .\Prototypes\packet13-segment-codec\PacketContextBindingPrototype.csproj --no-restore -- --demo
dotnet run --project .\Prototypes\packet88-performance\Packet88PerformanceStandalone.csproj --no-restore -- --verify
```

The default External Context command additionally runs legacy Terraria differential checks and requires a matching assembly, optionally selected with `NET_WORK_LEGACY_ASSEMBLY_PATH`; the focused commands above are the locally reproducible checks when the external assembly is absent or its version does not match the frozen golden. Open `Prototypes/slot-graph-ir/slot-dag-prototype.html` directly and record that it remains independent of C# builds.

Expected: each command passes using only its own prototype's source and local generated output.

## Task 11: Final repository audit

**Files:**

- Verify: entire repository
- Modify only if required: current README/index/docs links

**Step 1: Check repository status**

Run:

```powershell
git status --short
git diff --check
```

Expected: only intended prototype migration, docs and build metadata changes remain; unrelated user changes remain present.

**Step 2: Check top-level source absence**

Run:

```powershell
Test-Path .\Core
Test-Path .\NetWork.csproj
rg --files Core Concept Verification Tools Generators test4 2>$null
```

Expected: the old runtime source roots and root project are absent or contain no active source; if an artifact remains, it must be explicitly documented as non-runtime historical material.

**Step 3: Check prototype ownership**

Run:

```powershell
Get-ChildItem .\Prototypes -Directory | ForEach-Object {
    Write-Output "--- $($_.Name)"
    Get-ChildItem $_.FullName -Recurse -File | Select-Object -ExpandProperty FullName
}
```

Expected: every runtime source, test, tool and project has one owning prototype directory.

**Step 4: Record verification results**

Append final build/run results and known limitations to `Prototypes/README.md` and the affected prototype READMEs. Do not claim production readiness; report the result as successful prototype isolation.

## Completion Definition

The implementation is complete only when:

```text
all concept explorations have semantic prototype directories
all runtime code belongs to one prototype directory
no shared top-level Core remains
no cross-prototype source or project reference remains
each prototype has a local project or explicit static entry point
README/docs commands use the new paths
all prototype builds and required demos pass
static isolation checks pass
```
