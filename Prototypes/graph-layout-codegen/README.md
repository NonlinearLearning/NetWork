# Graph Layout Codegen

## 要回答的问题

类型化 packet member 节点和直接依赖边能否形成稳定的 Graph manifest，并进一步生成可验证的 span codec？原型同时保留两条概念 API 线，但默认验证项目只编译兼容的 `src/test1` 线。

## 范围与非目标

- 范围：Graph 节点、Packet layout、Packet 13/20/88 示例、Bootstrap、Exporter、manifest 和生成 codec。
- `src/test2` 是另一条不兼容的 API 探索，保留在本目录但不进入默认验证集合。
- 非目标：共享协议 runtime、跨原型复用源码、把生成 codec 宣布为生产实现。

## 构建与运行

```powershell
dotnet build .\Prototypes\graph-layout-codegen\tools\GraphBootstrap\NetWork.Concept.GraphBootstrap.csproj
dotnet run --project .\Prototypes\graph-layout-codegen\tools\GraphExporter\NetWork.Concept.GraphExporter.csproj -- --output .\Prototypes\graph-layout-codegen\Build\generated
dotnet build .\Prototypes\graph-layout-codegen\GraphVerification.csproj
dotnet run --project .\Prototypes\graph-layout-codegen\GraphVerification.csproj --no-restore
```

生成的 manifest 和 `*GeneratedCodec.g.cs` 只写入本目录的 `Build/generated/`。

## 当前验证状态

Bootstrap、Exporter、生成源码编译和 Graph verification 已通过。验证覆盖 manifest 字段顺序、依赖边、Packet 13/20/88 wire 对照、QuikGraph 依赖和生成 codec 消费。

## 已知限制

生成 codec 仍是概念验证，重复字段和旧协议全部消息号尚未覆盖；Graph 原型内部的代码重复是为了保持项目级隔离。
