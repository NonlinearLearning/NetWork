# Immutable Context Binding

## 要回答的问题

外部调用方提供一次类型化快照后，框架能否生成不再持有外部对象、writer 或延迟委托的 immutable `WireSubmission`？

## 范围与非目标

这是一次性逻辑演示，覆盖快照绑定、外部状态变更、segment staging 和失败时保留上一次提交。它不接入服务端、会话、传输或生产 registry。

## 构建与运行

```powershell
dotnet build .\Prototypes\immutable-context-binding\ContextBindingPrototype.csproj --no-restore
dotnet run --project .\Prototypes\immutable-context-binding\ContextBindingPrototype.csproj --no-restore -- --demo
```

## 当前验证状态

demo 验证了绑定后外部变化不影响 submission，以及失败 staging 不覆盖已提交 frame。

## 已知限制

这是最小概念模型，不代表 Packet 13/20 的完整协议实现。
