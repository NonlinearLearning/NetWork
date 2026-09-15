# Typed Span SourceGen

## 要回答的问题

Source Generator 能否读取类型化 packet spec，生成 span/IBufferWriter codec，并在同一原型内被真实 smoke 输入编译和消费？

## 范围与非目标

- `src/NetWork.Concept.SourceGen/`：Analyzer/Source Generator 实验。
- `tests/Support/`：该实验需要的最小属性和协议运行时副本。
- `tests/SourceGenSmoke.cs`：带条件字段的本地生成与 round-trip 输入。
- 非目标：引用其他原型、恢复共享 `Core`、覆盖完整旧协议类型集合。

## 构建与运行

```powershell
dotnet build .\Prototypes\typed-span-sourcegen\src\NetWork.Concept.SourceGen\NetWork.Concept.SourceGen.csproj --no-restore
dotnet build .\Prototypes\typed-span-sourcegen\tests\SourceGenSmoke.csproj --no-restore
dotnet run --project .\Prototypes\typed-span-sourcegen\tests\SourceGenSmoke.csproj --no-restore
```

预期输出：`Typed span source generator smoke passed.`

## 当前验证状态

Analyzer 项目构建、smoke 项目编译、生成代码消费和 wire round-trip 均已通过。构建输出位于本目录的 `Build/`。

## 已知限制

生成器只支持当前实验覆盖的 primitive、`BitsByte`、nullable 和 `Vector2` 子集；诊断和生成 API 仍可能变化。
