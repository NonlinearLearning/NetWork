# Packet 20 Local State

## 要回答的问题

Packet 20 的 tile 重复作用域能否由类型化外部数据投影为独立的 `PreparedPacket20`，并在 wire 编码时保持局部字段组、shape 和 20..70 段预算约束？

## 范围与非目标

本原型包含 Packet 20 binder、局部 tile state、字段组校验和 demo wire encoder。它不共享外部上下文、不引用 Graph 或 External Context 原型，也不承担生产 packet registry。

## 构建与运行

```powershell
dotnet build .\Prototypes\packet20-local-state\ContextBindingPrototype.csproj --no-restore
dotnet run --project .\Prototypes\packet20-local-state\ContextBindingPrototype.csproj --no-restore -- --demo
```

## 当前验证状态

构建和 demo 已通过；覆盖 shape multiplication、tile count、局部 presence、可选字段、非法字段组和预算拒绝。

## 已知限制

这是 Packet 20 的实验性局部模型，不能推断全部旧协议消息的通用设计。
