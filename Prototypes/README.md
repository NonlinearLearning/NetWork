# Prototype Workspace

这里存放仓库中的概念探索。每个概念探索都是一个独立的语义原型项目；目录名描述问题或方案，不使用“原型 1”“原型 2”这类编号。

所有目录都是实验级代码，不是生产代码。仓库不保留共享生产 `Core/`，原型之间不通过顶层 `ProjectReference` 或跨原型 `Compile Include` 共享源码。必要的重复代码是项目级隔离的有意代价。`external-context-wire-submission` 虽然最接近完整运行时形态，也仍然只是实验原型。

| 原型 | 要回答的问题 | 入口 | 当前状态 |
|---|---|---|---|
| [external-context-wire-submission](./external-context-wire-submission/) | 外部上下文能否投影为不可变提交物，再由 wire 层独立编码？ | `tests/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj` | 构建和 6 个 focused 入口通过；完整 golden 需要匹配版本的外部 legacy 程序集 |
| [graph-layout-codegen](./graph-layout-codegen/) | 类型化字段图能否导出 manifest 并生成 codec？ | `GraphVerification.csproj`、`tools/GraphExporter/` | Bootstrap、Exporter、生成 codec 和验证通过 |
| [minimal-dependency-graph](./minimal-dependency-graph/) | 最小类型化依赖图的节点、边和冻结校验是什么？ | `MinimalDependencyGraph.csproj` | 构建和运行通过 |
| [packet-layout-ast](./packet-layout-ast/) | 声明 AST 如何 lower 为带依赖边的布局 IR？ | `PacketLayoutAst.csproj` | 构建和 demo 通过 |
| [slot-graph-ir](./slot-graph-ir/) | wire 顺序、slot 依赖和重复作用域如何分层？ | `slot-dag-prototype.html` | 浏览器原型和设计报告保留 |
| [immutable-context-binding](./immutable-context-binding/) | 外部快照能否绑定成不再回读外部对象的提交物？ | `ContextBindingPrototype.csproj` | 构建和 `--demo` 通过 |
| [packet20-local-state](./packet20-local-state/) | Packet 20 的局部状态和重复 tile 是否能独立投影？ | `ContextBindingPrototype.csproj` | 构建和 demo 通过 |
| [packet13-segment-codec](./packet13-segment-codec/) | 固定/有界 segment 与 Packet 13 提交边界如何组合？ | `PacketContextBindingPrototype.csproj` | 构建和 demo 通过 |
| [typed-span-sourcegen](./typed-span-sourcegen/) | Source Generator 能否生成并被本地 smoke 输入消费？ | `tests/SourceGenSmoke.csproj` | Analyzer、smoke 编译和运行通过 |
| [packet88-performance](./packet88-performance/) | 冻结 codec、Graph codec 和生成 codec 的性能对照如何？ | `Packet88PerformanceStandalone.csproj` | 构建和 `--verify` 通过 |

## 常用验证

```powershell
dotnet run --project .\Prototypes\minimal-dependency-graph\MinimalDependencyGraph.csproj --no-restore
dotnet run --project .\Prototypes\graph-layout-codegen\GraphVerification.csproj --no-restore
dotnet run --project .\Prototypes\packet-layout-ast\PacketLayoutAst.csproj --no-restore
dotnet run --project .\Prototypes\typed-span-sourcegen\tests\SourceGenSmoke.csproj --no-restore
dotnet run --project .\Prototypes\packet88-performance\Packet88PerformanceStandalone.csproj --no-restore -- --verify
```

完整命令、限制和证据以各原型目录的 README 为准。
