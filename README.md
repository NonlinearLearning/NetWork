# NetWork

这个仓库现在是多个协议与运行时概念原型的实验工作区，不提供一个仓库级共享生产层。每个概念探索按语义放在 `Prototypes/<name>/` 中，源码、项目、测试和生成产物在其原型边界内闭合。

## 原型入口

完整索引见 [Prototypes/README.md](./Prototypes/README.md)。当前原型包括：

- `external-context-wire-submission`：最接近完整运行时形态的外部上下文与 wire submission 实验。
- `graph-layout-codegen`：类型化依赖图、manifest 和 codec 生成实验。
- `minimal-dependency-graph`：最小依赖图模型。
- `packet-layout-ast`：声明 AST 到布局 IR 的语义分析实验。
- `slot-graph-ir`：浏览器交互式 slot graph 设计实验。
- `immutable-context-binding`：不可变上下文绑定逻辑实验。
- `packet20-local-state`：Packet 20 局部状态投影实验。
- `packet13-segment-codec`：Packet 13 segment codec 与提交边界实验。
- `typed-span-sourcegen`：typed span source generator 与 smoke 输入。
- `packet88-performance`：Packet 88 多 codec 对照与性能测量实验。

即使 `external-context-wire-submission` 看起来最像生产代码，也不能把它当成仓库级生产实现或其他原型的公共依赖。原型之间不共享顶层 `Core/`，不使用跨原型 `ProjectReference`，也不通过跨原型源码包含建立隐藏共享层。

## 工作流程

涉及多个概念或目录边界的工作遵循：

1. 先写设计文档，记录问题、假设、边界和候选目录。
2. 再写执行文档，把迁移、实现、验证和清理拆成可检查的批次。
3. 最后按执行文档实际修改源码，并用各原型自己的命令验证。

本次隔离的文档是：

- [设计文档](./docs/plans/2026-09-15-prototype-isolation-design.md)
- [执行文档](./docs/plans/2026-09-15-prototype-isolation-implementation.md)

## 常用命令

```powershell
dotnet run --project .\Prototypes\minimal-dependency-graph\MinimalDependencyGraph.csproj --no-restore
dotnet run --project .\Prototypes\graph-layout-codegen\GraphVerification.csproj --no-restore
dotnet run --project .\Prototypes\packet-layout-ast\PacketLayoutAst.csproj --no-restore
dotnet run --project .\Prototypes\typed-span-sourcegen\tests\SourceGenSmoke.csproj --no-restore
dotnet run --project .\Prototypes\packet88-performance\Packet88PerformanceStandalone.csproj --no-restore -- --verify
```

External Context 原型的完整构建和 focused 验证命令见其 [README](./Prototypes/external-context-wire-submission/README.md)。它的默认 legacy differential 验证需要与冻结 golden 匹配的本地 Terraria legacy assembly；外部程序集缺失或版本不匹配时，focused 验证仍可独立运行。
