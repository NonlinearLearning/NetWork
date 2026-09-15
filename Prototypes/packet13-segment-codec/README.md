# Packet 13 Segment Codec

## 要回答的问题

固定 segment、`20..70` bounded segment、parent-delimited read framing 和 Packet 13 immutable submission 是否可以组合成清晰的 wire boundary？

## 范围与非目标

原型覆盖 segment registry、staging/commit、Packet 13 projection、可选字段组和外部 mutation 隔离。它不依赖生产 `Main`、`World`、`Session`、transport 或 packet registry。

## 构建与运行

```powershell
dotnet build .\Prototypes\packet13-segment-codec\PacketContextBindingPrototype.csproj --no-restore
dotnet run --project .\Prototypes\packet13-segment-codec\PacketContextBindingPrototype.csproj --no-restore -- --demo
```

`--script` 是 `--demo` 的别名。

## 当前验证状态

构建和 demo 已通过；覆盖 Fixed/Bounded、Exact/ParentDelimited、staging atomicity、registry freeze 和 Packet 13 deterministic transcript。

## 已知限制

Packet 10 legacy transform 尚未迁移；这些代码仍然只是实验级 API 形状验证。
