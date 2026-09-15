# Packet 88 大样本基准设计

目标是在同一进程、交替执行顺序下，以可复现的样本规模比较固化基线、Graph exporter 生成的 codec 和 Graph 运行时实现。

基准配置由命令行显式传入：预热次数、每样本迭代数和样本数。默认保留原有轻量基准；本次大样本运行使用 `--warmup 200000 --iterations 1000000 --samples 15`。每个场景保持 Frozen、Generated、Graph 的轮换顺序，报告 min/median/max、ops/s 和 B/op，避免将不同进程或单次计时当作结论。

配置解析需要拒绝零、负值和未知参数。验证运行可用极小参数，随后执行完整大样本运行；结果单独标注 Graph 的分配开销，以免其影响对 Generated 与 Frozen 热路径差异的解释。
