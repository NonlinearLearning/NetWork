# Packet Layout AST

## 要回答的问题

声明式 packet layout 能否先保留源码顺序，再由 `PacketSema` 绑定字段、检查前序条件和重复源，最后 lower 为带 dependency graph 的布局 IR？

## 范围与非目标

- 范围：字段声明、presence/value/shape/length 依赖、诊断和 manifest 投影。
- 非目标：实现真实 wire codec、连接运行时、跨原型共享 AST 基础库。

## 构建与运行

```powershell
dotnet build .\Prototypes\packet-layout-ast\PacketLayoutAst.csproj --no-restore
dotnet run --project .\Prototypes\packet-layout-ast\PacketLayoutAst.csproj --no-restore
```

预期输出：`PacketLayout AST demo passed.`

## 当前验证状态

fluent declaration、四类依赖边、wire order、manifest 顺序以及非法前序条件和 flag bit 诊断已通过。

## 已知限制

当前 demo 只验证编译器模型，不输出可运行的 Packet 13/20/88 codec。
