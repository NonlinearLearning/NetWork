# External Context Wire Submission

这是当前最接近完整运行时形态的实验原型，但仍不是生产代码，也不构成仓库级共享运行时。

## 要回答的问题

外部上下文是否可以先投影为不可变的 packet-specific submission，再由 wire 层独立完成 segment staging、长度校验和最终提交？原型覆盖 Packet 13、Packet 20、帧适配、会话管线和 legacy differential 验证。

## 范围与非目标

- 范围：本目录内的协议、帧、服务端管线、上下文投影、Packet 13/20 submission 和验证工具。
- 非目标：把它提升为共享 `Core`、接入真实生产服务、完成全部 legacy packet 迁移，或替其他原型提供公共 API。
- 本目录内保留完整源码副本；其他原型不能通过项目引用或源码路径依赖它。

## 构建与运行

```powershell
dotnet build .\Prototypes\external-context-wire-submission\ExternalContextWireSubmission.csproj --no-restore
dotnet build .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore
dotnet run --project .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore -- --focused segments
dotnet run --project .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore -- --focused packet13
dotnet run --project .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore -- --focused packet20
dotnet run --project .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore -- --focused prepared-pipeline
dotnet run --project .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore -- --focused guard
```

默认运行会额外尝试 legacy Terraria runtime differential。可以通过 `NET_WORK_LEGACY_ASSEMBLY_PATH` 指向指定版本的本地 legacy assembly 后运行：

```powershell
dotnet run --project .\Prototypes\external-context-wire-submission\tests\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj --no-restore
```

## 当前验证状态

主 runtime、legacy host、standalone 验证项目和六个 focused 入口（包含 Packet 20 legacy differential）已构建或运行通过。当前环境能解析到工作树外的 Terraria legacy assembly，但其 Hello 版本为 `Terraria318`，仓库冻结 golden 要求 `Terraria315`，因此默认完整入口会在 golden snapshot 处停止；需要匹配冻结基线的 legacy assembly 才能通过完整入口。这是外部环境/版本限制，不是本目录的编译依赖。

## 已知限制

`Packet 10` 仍保留兼容性示例；匹配版本的 legacy assembly、真实服务端和完整协议覆盖不随本原型提供。目录中的源码只是该实验的自洽副本。
