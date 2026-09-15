# Packet 88 Performance

## 要回答的问题

冻结手写 codec、Graph layout codec 和生成 codec 在 Packet 88 上的 wire 等价性、span/IBufferWriter 行为和性能测量边界是什么？

## 范围与非目标

本目录自带 Packet 88 所需的最小协议模型、Graph layout、生成 codec fixture 和 benchmark harness。它不引用 External Context 或 Graph Layout Codegen 原型，也不把 benchmark 结果当作生产性能承诺。

## 构建与验证

```powershell
dotnet build .\Prototypes\packet88-performance\Packet88PerformanceStandalone.csproj --no-restore
dotnet run --project .\Prototypes\packet88-performance\Packet88PerformanceStandalone.csproj --no-restore -- --verify
```

可选 benchmark：

```powershell
dotnet run --project .\Prototypes\packet88-performance\Packet88PerformanceStandalone.csproj --no-restore -- --benchmark --warmup 20000 --iterations 100000 --samples 7
```

## 当前验证状态

构建和 `--verify` 已通过；覆盖 sparse/full wire、round-trip、generated span/IBufferWriter、冻结/Graph/生成 codec 对照和 benchmark 配置解析。

## 已知限制

benchmark 只表示当前机器和当前实验实现的相对测量，不能替代生产环境 profiling。
