# Slot Graph IR

## 要回答的问题

wire order、slot presence、依赖 DAG、重复作用域和生成计划是否应当分层建模，而不是把所有关系堆进 packet 字段对象？

## 入口

这是浏览器交互原型，不参与 C# 构建。直接打开：

```text
Prototypes/slot-graph-ir/slot-dag-prototype.html
```

也可以从仓库根目录用浏览器打开该文件。`slot-dag-feasibility.md` 是配套可行性报告，`slot-dag-prototype-preview.png` 是预览资产。

## 范围与非目标

原型展示字段、slot、依赖、presence plan、wire sequence 和作用域之间的关系。它不提供生产 parser、运行时框架、C# 项目或可复用协议库。

## 当前验证状态

HTML、报告和预览资产已迁移到同一个语义原型目录；静态隔离审计确认它不参与 C# 项目引用。

## 已知限制

交互页面是设计探索工具，页面中的模型和示例不能替代实际协议一致性测试。
