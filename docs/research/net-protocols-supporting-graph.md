# 支持“图”的网络协议调研

日期：2026-09-07  
范围：为当前 `Concept` / `Wire + Effect` 网络协议设计寻找可借鉴的外部协议和编码标准。

## 结论摘要

“支持图”至少有四种不同含义，不能用一个协议名称概括：

1. **图数据交换**：传输节点、边、链接以及图的子集。最接近的是 IPLD GraphSync。
2. **语义图查询**：把数据定义成 RDF 图，并通过查询语言访问。代表是 RDF + SPARQL。
3. **图形状的应用响应**：客户端选择需要的关联字段。代表是 GraphQL，但 GraphQL 本身不是固定二进制 wire protocol。
4. **协议 schema 内部的依赖图**：描述字段顺序、条件、选择、循环和变换。ASN.1/PER、Protocol Buffers 等可以提供部分 schema 能力，但它们不是“协议依赖图传输协议”。

对当前仓库的直接判断：

- 如果问题是“哪种协议原生传输图”，优先研究 **IPLD Data Model + GraphSync**。
- 如果问题是“哪种协议能查询关系图”，优先研究 **RDF + SPARQL**。
- 如果问题是“哪种 schema/编码方式最像当前 Terraria 的固定二进制 packet”，优先研究 **ASN.1 + PER**，其次是受限使用的 Protobuf；但都需要自定义适配。
- 如果问题是“Concept 是否应该直接改成某个外部协议”，答案是 **不建议直接替换**。当前 Concept 描述的是 packet wire layout 和业务 Effect 的关系模型，不是通用图数据库或图同步协议。

## 候选协议对比

| 候选 | 一手规范定义的核心能力 | 对当前 Concept 的匹配度 | 不能直接解决的问题 |
|---|---|---:|---|
| IPLD + GraphSync | 用 IPLD 链接组织可寻址数据，GraphSync 根据 selector 在 peer 之间同步整个图或图的部分 | 图数据交换：高 | 固定字段线序、BitsByte gate、sentinel loop、Deflate、业务 Effect |
| RDF + SPARQL | RDF graph 是 subject-predicate-object triples 的集合；SPARQL Protocol 通过 HTTP 发送查询/更新并返回结果 | 语义关系查询：高 | 紧凑二进制 packet、固定 byte layout、实时 session packet 状态 |
| GraphQL + GraphQL over HTTP | GraphQL 定义查询语言和执行引擎；HTTP 绑定定义请求/响应语义 | 应用层关联数据：中 | GraphQL 不规定 Terraria 类固定字节顺序；HTTP 绑定页面当前仍标为 draft |
| ASN.1 + PER | ITU-T X.691 定义 Packed Encoding Rules；编码依赖 ASN.1 schema。RFC 8949 也指出 PER 用 schema 解析数据表面结构 | 二进制 schema：中高 | 不是图同步协议；Effect、业务 policy 和任意运行时 graph 仍需外层模型 |
| Protocol Buffers + gRPC | Protobuf 提供 typed message、nested message、repeated、map、oneof；gRPC 提供 RPC/HTTP2 传输 | typed message/RPC：中 | Protobuf 不把字段顺序作为语义；没有原生任意节点身份、边、循环图模型 |
| CBOR | 通用二进制数据格式；RFC 8949 明确基本结构限制为 arrays 和 trees，不支持 loops 和 lattice-style graphs | 运行时实例/manifest：中 | 普通 CBOR 不是图模型，也不描述 Concept 的依赖、scope、sequence 或 Effect |
| DAG-CBOR | 在 CBOR 上加入 CID link 和确定性编码，适合 IPLD DAG 节点 | 图 manifest：中高 | 约束为 DAG/link 数据模型，不是 packet layout compiler；当前规范页面标为 descriptive draft |
| QUIC | 为应用提供有流控的 stream；应用通过有序 byte sequence 通信 | 传输承载：高 | 完全不定义图语义、字段 schema 或业务关系 |

## 1. IPLD Data Model + GraphSync：最接近“图传输协议”

[IPLD GraphSync 规范](https://ipld.io/specs/transport/graphsync/) 的开头直接把 GraphSync 定义为：在 peer 之间同步图的协议，并使用 IPLD Selectors 传输整个图或图的一部分。

这和当前 Concept 的以下需求有明显相似处：

- 节点之间存在显式链接；
- 可以只请求图的一个子集；
- 传输关系不是单个扁平 DTO；
- graph traversal 可以成为协议的一等概念。

但它解决的是“已经存在的链接数据如何被请求和同步”，不是“如何从字段定义生成 Terraria packet 的字节布局”。它没有直接表达：

- `ControlFlags2.bit2 -> Velocity`；
- 一个 flag 控制共同存在的字段 block；
- `ushort 0` 结束的 sentinel loop；
- Deflate 子流中的二维 RLE；
- Packet 8 收到坐标后触发多区域发送的 Effect；
- session state 和 message id 的合法门禁。

因此 GraphSync 最适合成为未来的**图 manifest / 协议描述图交换层**，不适合作为 Packet 13/10/23/27 的 wire codec。

IPLD 的 [DAG-CBOR 规范](https://ipld.io/specs/codecs/dag-cbor/spec/) 说明，DAG-CBOR 在 CBOR 上使用 CID link，并增加确定性编码和更严格的校验。它可以作为 Concept graph manifest 的一种外部表示，但当前页面的状态仍是 descriptive draft；而且它表达的是 CID 链接的 DAG 数据，不是 `Sequence`、`Presence`、`Shape`、`Transform` 和 `Effect` 关系的协议编译器。

## 2. RDF + SPARQL：最接近“关系图查询协议”

[RDF 1.2 Concepts](https://www.w3.org/TR/rdf12-concepts/) 定义 RDF graph 为 subject-predicate-object triples 的集合，并用 RDF dataset 组织默认图和命名图。

[SPARQL 1.1 Protocol](https://www.w3.org/TR/sparql11-protocol/) 定义了通过 HTTP 把 SPARQL 查询和更新发送到处理服务，并把结果返回给请求方的协议。

它适合：

- 查询“某个 packet 节点被哪些 gate 依赖”；
- 查询“某个字段影响哪些后继字段”；
- 查询“所有包含 `Transform` 或 `TailLoop` 的 packet”；
- 维护跨 packet、跨版本的知识图谱；
- 做审计、检索、影响分析。

它不适合直接承担当前实时 packet wire：RDF/SPARQL 的重点是资源和关系的语义查询，不是字节级的固定布局、拆包、压缩和 session 状态。因此它更适合做**离线分析索引或审计投影**，不适合放在 `MessageFrame -> PacketCodec` 主链中。

## 3. GraphQL：图形状响应，不是协议布局图

[GraphQL specification](https://spec.graphql.org/October2021/) 将 GraphQL 定义为用于描述 client-server data model 能力和需求的 query language and execution engine。

[GraphQL over HTTP](https://graphql.github.io/graphql-over-http/draft/) 则描述 HTTP 请求、响应、序列化格式、GET/POST 和状态码等绑定；该页面明确仍是 draft。

GraphQL 的“图”主要是：

```text
客户端选择关联字段
    -> 服务端执行 resolver
    -> 返回嵌套结果
```

这适合上层查询，例如：

```text
查询 player -> inventory -> item -> modifier
```

但它不适合作为当前 Concept 的直接替代，因为：

- 客户端 query 决定响应形状，而 Terraria packet 的 wire shape 通常由协议版本固定；
- GraphQL 不提供 `BitsByte`、byte offset、sentinel、Deflate 或二维扫描顺序的原生语义；
- resolver 可以有业务副作用，难以作为纯静态协议图；
- GraphQL 响应通常是 JSON 或其他应用层序列化，而不是当前 packet 的紧凑二进制布局。

GraphQL 可以借鉴的是**按关联关系选择子图**的接口思想，而不是它的 wire encoding。

## 4. ASN.1 + PER：最值得比较的二进制 schema 方案

[ITU-T X.691](https://www.itu.int/rec/T-REC-X.691/en) 的正式标题是 “ASN.1 encoding rules: Specification of Packed Encoding Rules (PER)”。

[RFC 8949](https://www.rfc-editor.org/rfc/rfc8949.txt) 在比较 PER 时指出，PER 使用 schema 解析数据的表面结构，需要相应的工具支持。

这和当前 Concept 的目标最接近的部分是：

- schema 参与解析，而不是完全依赖 self-describing payload；
- 可以针对紧凑二进制编码做设计；
- 协议的结构、条件和编码规则可以被工具读取。

但 ASN.1/PER 仍然不是当前 Concept 要求的完整关系图：

- 它的重点是抽象数据类型和编码规则，不是 Wire/Effect 两域关系图；
- 业务 Effect、session policy、身份覆盖和 rebroadcast 不应由 PER schema 负责；
- 当前 Terraria 的特殊 RLE、压缩尾表、业务门禁仍可能需要自定义类型和 adapter；
- 即使采用 PER，也仍需要保留独立的 `ProtocolInstance` 和业务应用层。

因此它更适合作为**验证 Concept wire 子集设计的参考标准**，而不是直接导入整个 ASN.1 runtime。

## 5. Protocol Buffers + gRPC：强 schema，但不是 graph protocol

[Protocol Buffers proto3 language guide](https://protobuf.dev/programming-guides/proto3/) 明确支持 `repeated`、`map`、`oneof` 和嵌套 message 等结构。

[Protocol Buffers encoding guide](https://protobuf.dev/programming-guides/encoding/) 明确指出：`.proto` 中字段声明顺序不影响序列化；已知字段和未知字段的序列化顺序没有保证，序列化顺序是实现细节，解析器必须能接受任意顺序。

[gRPC core concepts](https://grpc.io/docs/what-is-grpc/core-concepts/) 描述 gRPC 的 RPC、服务定义、流和 HTTP/2 传输模型。

这使 Protobuf/gRPC 适合：

- 稳定的 typed message；
- 服务调用和双向流；
- schema evolution；
- nested message、repeated list、oneof 分支。

但它和当前 Terraria wire 有一个关键冲突：当前协议需要显式线序，而 Protobuf 的字段号而非声明顺序决定 wire identity，字段序列化顺序不是协议语义。

此外，Protobuf 的 `repeated`、`map` 和 `oneof` 不是任意图：如果需要共享节点身份、反向边或循环引用，必须自己引入 node id / reference id 字段并定义生命周期。

结论是：Protobuf/gRPC 可以借鉴其 schema evolution 和 typed boundary，但不能直接证明 Concept 的 graph model 已被替代。

## 6. CBOR、DAG-CBOR 与 QUIC：分别是数据格式、图节点编码和传输承载

### CBOR

RFC 8949 的目标是通用、紧凑、可扩展的二进制数据格式。它明确说明基本结构限制为 arrays 和 trees，loops 和 lattice-style graphs 不受支持。

因此普通 CBOR 不能直接表达当前 Concept 中的任意关系图。它适合：

- 保存 `ProtocolInstance` 的值、presence、selector 和 diagnostics；
- 保存协议图的 manifest 快照；
- 在调试或管理接口中交换结构化数据。

但需要额外 schema 才能表达当前 packet 的 wire 语义。

### DAG-CBOR

DAG-CBOR 为 IPLD Data Model 提供 CBOR 表示，使用 CID tag 42 表示链接，并要求确定性编码。

它可以作为：

```text
Concept blueprint graph
    -> manifest
    -> DAG-CBOR
    -> CID
```

但它更适合不可变、可寻址的 DAG 数据，不适合直接描述带 Effect、副作用和 session 状态的运行时协议。

### QUIC

[QUIC RFC 9000](https://www.rfc-editor.org/rfc/rfc9000.txt) 把 QUIC 的基本服务抽象定义为有流控的 streams，应用在 stream 上交换有序 byte sequences。

因此 QUIC 可以替换或补充当前 TCP frame adapter，但它不会替 Concept 决定：

- packet 字段如何组织；
- graph 关系如何查询；
- 哪些字段存在；
- 哪个业务 Effect 可以执行。

## 对当前仓库 Concept 的落地建议

当前本地设计是：

```text
Concept blueprint graph
    -> static validation / manifest / generated codec

ProtocolInstance
    -> values
    -> presence decisions
    -> selector values
    -> loop state
    -> wire spans
    -> diagnostics

Packet handler / Effect layer
    -> authoritative state
    -> response / rebroadcast
```

外部协议的最佳组合不是“选一个替代 Concept”，而是分层使用：

```text
实时 packet wire:
    自有 Wire graph + 自有 codec

图 manifest / 审计交换:
    DAG-CBOR 或普通 CBOR

图子集请求 / peer graph sync:
    参考 IPLD Selectors / GraphSync

关系查询 / 影响分析:
    RDF projection + SPARQL

底层传输:
    TCP 继续使用，或评估 QUIC streams
```

其中不能被外部协议替代的仍然是当前提案中特有的关系：

```text
Contains
Sequence
Presence
Shape
Value
Context
Transform
Effect
```

特别是：

- `Sequence` 不能被一般 graph traversal 或 Protobuf 字段声明顺序替代；
- `Presence` / `Shape` / `Value` 要进入可分析的 wire graph；
- `Effect` 要和 wire graph 分开；
- `ProtocolInstance` 不能由静态 graph 或外部 graph transport 代替。

## 推荐优先级

1. **先把当前 Concept 的活动边界和项目引用修复到可构建。** 目前项目仍引用不存在的根级 `Concept/*.cs`。
2. **将 Wire graph 作为自有规范模型保留。** 不要因为 GraphSync、GraphQL 或 RDF 都出现“graph”一词，就把它们当成 packet layout engine。
3. **为 graph manifest 评估 DAG-CBOR/CBOR。** 这能改善快照、hash、审计和 AI 检索，但不承担实时 codec。
4. **为复杂协议结构参考 ASN.1/PER。** 重点比较 `Block`、`Gate`、`Select`、`Loop`、约束和 schema-driven codec，而不是照搬整个运行时。
5. **为离线影响分析评估 RDF/SPARQL。** 将 `Contains`、`Presence`、`Sequence`、`Transform` 和 `Effect` 投影成查询图。
6. **只有在传输层需求明确时才评估 QUIC。** QUIC 解决的是 streams 和可靠传输，不解决 Concept 的建模问题。

## 主要来源

- [GraphQL Specification](https://spec.graphql.org/October2021/)
- [GraphQL over HTTP](https://graphql.github.io/graphql-over-http/draft/)
- [Protocol Buffers proto3 Language Guide](https://protobuf.dev/programming-guides/proto3/)
- [Protocol Buffers Encoding Guide](https://protobuf.dev/programming-guides/encoding/)
- [gRPC Core Concepts](https://grpc.io/docs/what-is-grpc/core-concepts/)
- [RFC 8949: Concise Binary Object Representation (CBOR)](https://www.rfc-editor.org/rfc/rfc8949.txt)
- [RDF 1.2 Concepts](https://www.w3.org/TR/rdf12-concepts/)
- [SPARQL 1.1 Protocol](https://www.w3.org/TR/sparql11-protocol/)
- [IPLD GraphSync Specification](https://ipld.io/specs/transport/graphsync/)
- [IPLD DAG-CBOR Specification](https://ipld.io/specs/codecs/dag-cbor/spec/)
- [ITU-T X.691: ASN.1 Packed Encoding Rules](https://www.itu.int/rec/T-REC-X.691/en)
- [RFC 9000: QUIC](https://www.rfc-editor.org/rfc/rfc9000.txt)

## 与本仓库的本地材料

- `docs/plans/2026-08-06-netmessage-dependency-graph-design-proposal.md`
- `docs/research/netmessage-complex-examples.md`
- `Core/Protocol/PacketFramework.cs`
- `Core/Protocol/PacketFrameworkConcept.cs`
- `Concept/test1/`
- `Concept/test2/`
- `Concept/test3/`
