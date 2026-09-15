# Minimal Dependency Graph

## 要回答的问题

验证最小 packet dependency graph 是否可以只依赖类型化 member-backed node、直接 dependency edge、声明顺序和构造期校验。

## 范围与非目标

本原型只保留最小图模型。它不包含 packet layout、codec、manifest、QuikGraph、source generation，也不引用其他原型。

## 构建与运行

```powershell
dotnet build .\Prototypes\minimal-dependency-graph\MinimalDependencyGraph.csproj --no-restore
dotnet run --project .\Prototypes\minimal-dependency-graph\MinimalDependencyGraph.csproj --no-restore
```

预期输出：`Minimal dependency graph prototype passed.`

## 当前验证状态

构建和运行通过；节点身份、边端点、声明顺序、环和外部节点拒绝均由 demo 验证。

## 已知限制

这是最小逻辑样例，不代表完整 packet schema 或生产图数据库设计。
