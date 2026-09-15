# 上下文 API 与内存暴露设计调研

日期：2026-09-09  
范围：NetWork 上下文层/非上下文层划分、注册函数的能力边界、固定/可变内存段暴露、提交和拼接。  
交付物：设计研究报告，不包含实现代码。  
研究方式：直接读取 GitHub 一手源码，锁定到具体 commit；源码事实和 NetWork 设计推论分开记录。

## 1. 结论摘要

成熟项目没有把“上下文、业务扩展、可写内存、最终输出 writer”塞进一个万能对象。它们通常拆成几种不同的接口：

1. **显式操作上下文**：传递请求范围的取消、截止时间、诊断和少量跨边界值；上下文不拥有 wire buffer。
2. **类型化能力集合**：按类型注册可选能力，调用方只取得自己声明需要的 feature，而不是依赖字符串键或服务定位器。
3. **受限内存视图**：暴露当前可写区域，同时通过 `filled`、`advance` 或 `commit` 表示实际有效长度。
4. **不可变输出或明确所有权**：可变 buffer 完成后转成只读对象，或者以引用计数/所有权转移的 segment chain 交接。

对 NetWork 的直接结论是：

> 上下文 API 不应直接暴露最终 wire memory。上下文层负责把外部状态投影成强类型、不可变的 `WireSubmission`；非上下文层再按 schema 为 codec 提供受限的 segment lease，校验实际写入量后完成拼接。

建议保留两个独立的 seam：

```text
OperationContext
    -> ContextAdapter / Projector
    -> immutable WireSubmission
    -> SegmentPlanner
    -> bounded write lease
    -> validate committed length
    -> final assembler / frame
```

`OperationContext` 可以携带取消、截止时间、schema revision、诊断和类型化只读能力；它不应携带 `Span<byte>`、最终 `IBufferWriter<byte>` 或允许 codec 延迟回读的领域对象。

## 2. 当前问题的设计边界

当前 NetWork 已经把上层分为上下文层和非上下文层，并且现有 `WireSubmission` 设计要求：

- Projector 读取外部上下文并形成 wire 语义；
- `WireSubmission` 是编码器的强类型输入；
- EncodePlan 不重新访问 `Main`、`World`、`Session` 或路由状态；
- 非法提交物被拒绝，不通过补零、截断或重新读取上下文修复；
- 首版不冻结万能 `IContext`、服务定位器、完整 IR 或零拷贝生命周期协议。

因此本报告不建议把“注册函数可以读写内存”解释成“注册函数可以拿到全部上下文和最终输出”。注册函数如果决定 wire bytes，它本质上是 codec；codec 应位于非上下文层，接收已经投影的值和明确的内存能力。

## 3. GitHub 项目与源码证据

以下项目按设计问题选择，不按 star 数量选择。链接均锁定到调研时的 commit。

| 项目 | 锁定提交 | 观察重点 |
| --- | --- | --- |
| [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore/tree/67a02a66a21ac4f2e8ab19201b59cf45e97c8c98) | `67a02a66a21ac4f2e8ab19201b59cf45e97c8c98` | 上下文 facade、typed feature、连接与 PipeWriter 能力 |
| [Azure/DotNetty](https://github.com/Azure/DotNetty/tree/379d8cc1d32d2b557347aad1f7b6c48656212a4b) | `379d8cc1d32d2b557347aad1f7b6c48656212a4b` | handler context 与 ByteBuffer 的职责分离 |
| [golang/go](https://github.com/golang/go/tree/49c3ea64647d7440350eab1e67e9a5ca261d8c29) | `49c3ea64647d7440350eab1e67e9a5ca261d8c29` | 显式 context、取消/期限和值的边界 |
| [tokio-rs/tokio](https://github.com/tokio-rs/tokio/tree/346dd031d007ff2f83ba7b2ab8dc0e61c1dffe7c) | `346dd031d007ff2f83ba7b2ab8dc0e61c1dffe7c` | `ReadBuf` 的 filled/initialized/borrowed contract |
| [tokio-rs/bytes](https://github.com/tokio-rs/bytes/tree/7930d93d583757e922014312d4aa3e58a415131f) | `7930d93d583757e922014312d4aa3e58a415131f` | `BufMut` 提交、`BytesMut` split/freeze/所有权 |
| [hyperium/http](https://github.com/hyperium/http/tree/ae157da76e4a9cdf791b2c7aba4c21e3d618cb59) | `ae157da76e4a9cdf791b2c7aba4c21e3d618cb59` | 类型键扩展集合 |
| [facebook/folly](https://github.com/facebook/folly/tree/49fdfebcecc129d96eebb94fda80880ff40b5e8a) | `49fdfebcecc129d96eebb94fda80880ff40b5e8a` | writable tail、chain、preallocate/postallocate、coalesce |
| [grpc/grpc](https://github.com/grpc/grpc/tree/2687d24ee07b1582ba430485936e2d17ea0eee86) | `2687d24ee07b1582ba430485936e2d17ea0eee86` | slice 引用计数、分片读取和 view 生命周期 |
| [rust-lang/rust](https://github.com/rust-lang/rust/tree/eca445e5ae4a6679cc27d3a09106ce245e13a5a6) | `eca445e5ae4a6679cc27d3a09106ce245e13a5a6` | 借用 buffer 和 cursor 的安全提交语义 |

### 3.1 ASP.NET Core：上下文 facade、typed feature 和传输能力

`HttpContext` 是一个面向请求的 facade，提供 `Features`、`Request`、`Response`、`User`、`Items`、`RequestServices`、`RequestAborted` 和 `Abort` 等能力。它说明“上下文”可以是操作范围的聚合入口，但它本身并不等于某个具体的内存段。

来源：[`HttpContext.cs#L12-L78`](https://github.com/dotnet/aspnetcore/blob/67a02a66a21ac4f2e8ab19201b59cf45e97c8c98/src/Http/Http.Abstractions/src/HttpContext.cs#L12-L78)

ASP.NET Core 的 `IFeatureCollection` 用 `Type` 作为 feature key，提供 `Get<TFeature>`/`Set<TFeature>`，并暴露 `IsReadOnly` 和 `Revision`。它把可选能力从主上下文类型中拆出来，同时允许实现根据当前服务器能力增删 feature。

来源：[`IFeatureCollection.cs#L9-L43`](https://github.com/dotnet/aspnetcore/blob/67a02a66a21ac4f2e8ab19201b59cf45e97c8c98/src/Extensions/Features/src/IFeatureCollection.cs#L9-L43)

连接上下文进一步把连接标识、feature、Items、关闭通知、端点和 Abort 组织在 `BaseConnectionContext` 中；`ConnectionContext` 再暴露 `IDuplexPipe Transport`。这说明“连接生命周期/元数据”和“输入输出管道”可以同属一个操作对象，但仍是不同的 capability。

来源：

- [`BaseConnectionContext.cs#L13-L66`](https://github.com/dotnet/aspnetcore/blob/67a02a66a21ac4f2e8ab19201b59cf45e97c8c98/src/Servers/Connections.Abstractions/src/BaseConnectionContext.cs#L13-L66)
- [`ConnectionContext.cs#L10-L35`](https://github.com/dotnet/aspnetcore/blob/67a02a66a21ac4f2e8ab19201b59cf45e97c8c98/src/Servers/Connections.Abstractions/src/ConnectionContext.cs#L10-L35)

`IHttpResponseBodyFeature` 同时描述 `Stream` 和 `PipeWriter`，还包含 `StartAsync`、`CompleteAsync` 和 buffering 控制；`HttpResponse.BodyWriter` 是上层 facade 对 writer capability 的投影。这是“把 writer 暴露给确实需要输出的层”的例子，但它并没有把 writer 塞进所有 feature 或所有回调。

来源：

- [`IHttpResponseBodyFeature.cs#L8-L48`](https://github.com/dotnet/aspnetcore/blob/67a02a66a21ac4f2e8ab19201b59cf45e97c8c98/src/Http/Http.Features/src/IHttpResponseBodyFeature.cs#L8-L48)
- [`HttpResponse.cs#L49-L63`](https://github.com/dotnet/aspnetcore/blob/67a02a66a21ac4f2e8ab19201b59cf45e97c8c98/src/Http/Http.Abstractions/src/HttpResponse.cs#L49-L63)

**对 NetWork 的启发：**可以借鉴 typed feature 和 capability 分离，但不应复制一个包含几十种可变服务的 `HttpContext`。NetWork 的上下文应该是只读投影输入；segment writer 应是一个只在具体 codec 调用期间存在的窄 capability。

### 3.2 DotNetty：handler context 不是 byte buffer

`IChannelHandlerContext` 提供 `Channel`、`Allocator`、`Executor`、`Name`、`Handler`、事件传播以及 `WriteAsync`/`Flush` 等执行能力。它是 handler 的运行时上下文，负责“我是谁、在哪个 pipeline、怎样继续事件”，而不是承担 payload 的内存表示。

来源：[`IChannelHandlerContext.cs#L13-L74`](https://github.com/Azure/DotNetty/blob/379d8cc1d32d2b557347aad1f7b6c48656212a4b/src/DotNetty.Transport/Channels/IChannelHandlerContext.cs#L13-L74)

同一项目的 `IByteBuffer` 位于另一个模块，单独负责 reader/writer index、capacity、slice 和引用计数；上一份内存段调研已经记录了它的 `MarkWriterIndex`/`ResetWriterIndex`。这种拆分比“上下文回调直接持有任意 byte buffer”更清晰：执行环境和数据容器可以分别替换、测试和限制。

**对 NetWork 的启发：**上层注册函数应收到“当前 slot 的受限能力”，而不是完整 pipeline context。若需要调度、诊断或取消，应通过另一个只读 operation context 传递；若需要写 wire bytes，应单独取得 segment lease。

### 3.3 Go：显式、可派生、不可隐式持久化的 Context

Go `context` 包将 Context 定义为跨 API 边界传递 deadline、cancel signal 和 request-scoped values 的对象。源码明确要求：不要把 Context 存入 struct，而是显式传给需要它的函数，并通常放在第一个参数；Context value 只用于跨进程/API 的请求范围数据，不用于可选参数。

来源：

- [`context.go#L5-L54`](https://github.com/golang/go/blob/49c3ea64647d7440350eab1e67e9a5ca261d8c29/src/context/context.go#L5-L54)
- [`context.go#L68-L164`](https://github.com/golang/go/blob/49c3ea64647d7440350eab1e67e9a5ca261d8c29/src/context/context.go#L68-L164)
- [`context.go#L719-L741`](https://github.com/golang/go/blob/49c3ea64647d7440350eab1e67e9a5ca261d8c29/src/context/context.go#L719-L741)

Context 是可并发调用的；派生 context 通过 `WithCancel`、`WithDeadline`、`WithTimeout` 或 `WithValue` 形成父子关系。取消函数负责释放关联资源，调用者不能把它当作普通配置字典。

**对 NetWork 的启发：**

- `PacketProjector`/codec 可以显式接收 operation context，但不应从静态注册表或 `AsyncLocal` 隐式取得当前领域状态。
- cancellation、deadline 和诊断 metadata 可以在 Context 中；wire value、segment writer 和可变 buffer 不应放在 Context 中。
- 如果提供扩展值，优先定义类型化 accessor；不要让 codec 依赖通用字符串 key。

### 3.4 hyperium/http：类型键扩展，而不是字符串字典

Rust `http::Extensions` 是请求/响应的 type map，内部使用 `TypeId`，提供 `insert<T>`、`get<T>`、`get_mut<T>`、`remove<T>` 和 `extend`。扩展值要求 `Send + Sync + 'static`，因此它适合存放跨层可共享的请求扩展，但不适合表达只在一次同步 callback 中有效的借用 `Span`。

来源：

- [`extensions.rs#L30-L105`](https://github.com/hyperium/http/blob/ae157da76e4a9cdf791b2c7aba4c21e3d618cb59/src/extensions.rs#L30-L105)
- [`extensions.rs#L234-L265`](https://github.com/hyperium/http/blob/ae157da76e4a9cdf791b2c7aba4c21e3d618cb59/src/extensions.rs#L234-L265)

**对 NetWork 的启发：**如果确实需要可选上下文 capability，可以采用“类型化 slot/feature”思想；核心 codec 的输入仍应是明确的 packet-specific submission，而不是把所有值放进一个动态 map。尤其不能把 `Span<byte>` 或 staging lease 放入可跨线程、可保存的 extension 集合。

## 4. 内存暴露的成熟模式

### 4.1 Tokio `ReadBuf`：容量、初始化和有效数据是三个状态

Tokio `ReadBuf` 包装一个可借用 buffer，内部同时记录 `filled` 和 `initialized`。源码文档用如下关系描述它：

```text
[ filled |         unfilled         ]
[    initialized    | uninitialized ]
```

`filled` 是调用者可以安全读取的有效数据；`initialized` 记录哪些位置曾经被初始化；剩余容量可能仍未初始化。`unfilled_mut` 是 unsafe 的底层入口，安全的高层方法则通过 `initialize_unfilled`、`assume_init`、`set_filled` 或 `put_slice` 推进状态。

来源：

- [`read_buf.rs#L4-L27`](https://github.com/tokio-rs/tokio/blob/346dd031d007ff2f83ba7b2ab8dc0e61c1dffe7c/tokio/src/io/read_buf.rs#L4-L27)
- [`read_buf.rs#L62-L177`](https://github.com/tokio-rs/tokio/blob/346dd031d007ff2f83ba7b2ab8dc0e61c1dffe7c/tokio/src/io/read_buf.rs#L62-L177)
- [`read_buf.rs#L215-L285`](https://github.com/tokio-rs/tokio/blob/346dd031d007ff2f83ba7b2ab8dc0e61c1dffe7c/tokio/src/io/read_buf.rs#L215-L285)

**设计价值：**内存暴露不是单纯返回一个数组。接口必须区分“能写多少”“已经初始化多少”“协议上已经提交多少”。C# 的 `byte` 不需要 Rust 的 `MaybeUninit` 安全模型，但仍需要保留 `capacity` 与 `written/committed` 的区分。

### 4.2 Rust `BorrowedBuf`：借用期限和 cursor 提交

Rust 标准库的 `BorrowedBuf` 明确表示“借用的、初始可能未初始化、逐步填充的 buffer”；`BorrowedCursor` 只操作未填充区域，并通过 `advance` 或 `append` 推进 filled 状态。文档还提供 `with_unfilled_buf`，使调用者在一个受限回调内操作临时视图，回调结束后由外层吸收新的 filled 量。

来源：

- [`borrowed_buf.rs#L8-L29`](https://github.com/rust-lang/rust/blob/eca445e5ae4a6679cc27d3a09106ce245e13a5a6/library/core/src/io/borrowed_buf.rs#L8-L29)
- [`borrowed_buf.rs#L191-L206`](https://github.com/rust-lang/rust/blob/eca445e5ae4a6679cc27d3a09106ce245e13a5a6/library/core/src/io/borrowed_buf.rs#L191-L206)
- [`borrowed_buf.rs#L315-L413`](https://github.com/rust-lang/rust/blob/eca445e5ae4a6679cc27d3a09106ce245e13a5a6/library/core/src/io/borrowed_buf.rs#L315-L413)
- [`borrowed_buf.rs#L422-L442`](https://github.com/rust-lang/rust/blob/eca445e5ae4a6679cc27d3a09106ce245e13a5a6/library/core/src/io/borrowed_buf.rs#L422-L442)

**设计价值：**这比把可写 `Memory<byte>` 作为长期对象暴露更适合注册 codec。NetWork 不需要复制 Rust 的 unsafe 细节，但可以采用同一个语义：codec 只在同步调用期间借用本段，返回实际写入量后 lease 结束；不能保存视图到 callback 之外。

### 4.3 `bytes::BufMut`：可写 chunk 与 advance_mut

`BufMut` 将写入分成三步：查询 `remaining_mut`，取得当前 `chunk_mut`，写入后调用 `advance_mut(cnt)`。源码特别说明：`chunk_mut()` 返回的连续片段可能小于 `remaining_mut()`，返回内存也可能是未初始化的；调用者必须保证 advance 的字节已经初始化。

来源：

- [`buf_mut.rs#L30-L64`](https://github.com/tokio-rs/bytes/blob/7930d93d583757e922014312d4aa3e58a415131f/src/buf/buf_mut.rs#L30-L64)
- [`buf_mut.rs#L66-L107`](https://github.com/tokio-rs/bytes/blob/7930d93d583757e922014312d4aa3e58a415131f/src/buf/buf_mut.rs#L66-L107)
- [`buf_mut.rs#L132-L179`](https://github.com/tokio-rs/bytes/blob/7930d93d583757e922014312d4aa3e58a415131f/src/buf/buf_mut.rs#L132-L179)
- [`buf_mut.rs#L246-L263`](https://github.com/tokio-rs/bytes/blob/7930d93d583757e922014312d4aa3e58a415131f/src/buf/buf_mut.rs#L246-L263)

**设计价值：**“请求空间”和“提交多少”必须是两个操作；不能把一次 `GetSpan(max)` 返回的长度误当成最终输出长度。对于 NetWork 的 20-70 段，`MaxBytes` 是可写上限，`written` 才是 wire 上实际消费的长度。

### 4.4 `BytesMut`：可变状态完成后 freeze 为只读所有权

`BytesMut` 持有可变 view 和共享 storage；`split_to`/`split_off` 可以通过调整 view 和引用计数产生 O(1) 的分段，`freeze` 将可变 buffer 转换成可共享的只读 `Bytes`。源码还提供 `spare_capacity_mut` 和显式 `set_len`，再次说明“已写长度”不能由 capacity 推断。

来源：

- [`bytes_mut.rs#L45-L65`](https://github.com/tokio-rs/bytes/blob/7930d93d583757e922014312d4aa3e58a415131f/src/bytes_mut.rs#L45-L65)
- [`bytes_mut.rs#L250-L270`](https://github.com/tokio-rs/bytes/blob/7930d93d583757e922014312d4aa3e58a415131f/src/bytes_mut.rs#L250-L270)
- [`bytes_mut.rs#L320-L430`](https://github.com/tokio-rs/bytes/blob/7930d93d583757e922014312d4aa3e58a415131f/src/bytes_mut.rs#L320-L430)
- [`bytes_mut.rs#L1188-L1218`](https://github.com/tokio-rs/bytes/blob/7930d93d583757e922014312d4aa3e58a415131f/src/bytes_mut.rs#L1188-L1218)

**设计价值：**NetWork 如果未来需要零拷贝发送，可以让“可变 staging 完成”转换为“不可变 output part”；但这应是第二条所有权明确的路径，不应污染首版同步 bounded lease。

### 4.5 Folly `IOBuf` / `IOBufQueue`：分段链与所有权转移

Folly `IOBuf` 把数据指针、可写 tail、当前 length、headroom/tailroom 和 chain 组织在一个 buffer node 中。调用者写入 tail 后调用 `append(amount)` 扩大有效长度；拼接通过 `appendToChain(unique_ptr<IOBuf>&&)` 转移链所有权。

来源：

- [`IOBuf.h#L793-L827`](https://github.com/facebook/folly/blob/49fdfebcecc129d96eebb94fda80880ff40b5e8a/folly/io/IOBuf.h#L793-L827)
- [`IOBuf.h#L991-L1009`](https://github.com/facebook/folly/blob/49fdfebcecc129d96eebb94fda80880ff40b5e8a/folly/io/IOBuf.h#L991-L1009)
- [`IOBuf.h#L1137-L1165`](https://github.com/facebook/folly/blob/49fdfebcecc129d96eebb94fda80880ff40b5e8a/folly/io/IOBuf.h#L1137-L1165)

`IOBufQueue` 进一步提供 `preallocate(min, newAllocationSize, max)` / `postallocate(n)`，并可以 `move()` 把整个 chain 的所有权交给调用者。需要连续内存时，`coalesce()` 显式复制/合并，并且源码承诺失败时 chain 不变。

来源：

- [`IOBufQueue.h#L36-L54`](https://github.com/facebook/folly/blob/49fdfebcecc129d96eebb94fda80880ff40b5e8a/folly/io/IOBufQueue.h#L36-L54)
- [`IOBufQueue.h#L387-L428`](https://github.com/facebook/folly/blob/49fdfebcecc129d96eebb94fda80880ff40b5e8a/folly/io/IOBufQueue.h#L387-L428)
- [`IOBufQueue.h#L468-L548`](https://github.com/facebook/folly/blob/49fdfebcecc129d96eebb94fda80880ff40b5e8a/folly/io/IOBufQueue.h#L468-L548)
- [`IOBuf.h#L1596-L1668`](https://github.com/facebook/folly/blob/49fdfebcecc129d96eebb94fda80880ff40b5e8a/folly/io/IOBuf.h#L1596-L1668)

**设计价值：**零拷贝拼接真正需要的是 ownership contract，而不是把一个 `Span` 传给更多函数。NetWork 首版应先采用 copy-to-final-writer；只有在性能证据出现后，才增加 `OwnedSegment`/chain，并单独定义谁负责释放、何时只读和何时允许 coalesce。

### 4.6 gRPC：slice 引用计数和分片读取 view

gRPC 的 `grpc_slice` 提供 ref/unref、malloc、copied/static buffer 和 split 操作；`grpc_byte_buffer` 可以由多个 slice 构成，并通过 reader 逐片读取。reader 的 `peek` 文档明确要求底层 byte buffer 在 slice 使用期间保持有效且不可变；读取完成后仍由调用者负责释放 slice。

来源：

- [`slice.h#L30-L121`](https://github.com/grpc/grpc/blob/2687d24ee07b1582ba430485936e2d17ea0eee86/include/grpc/slice.h#L30-L121)
- [`byte_buffer.h#L46-L95`](https://github.com/grpc/grpc/blob/2687d24ee07b1582ba430485936e2d17ea0eee86/include/grpc/byte_buffer.h#L46-L95)

**设计价值：**读取 API 必须说明 view 的生命周期；“把底层内存地址交给注册函数”不是完整设计。若 NetWork 暴露 `ReadOnlySequence<byte>` 或 segment view，应同时说明 owner 是否保持、是否允许并发、何时失效。

## 5. 设计模式对比

| 设计问题 | 成熟项目的共同答案 | NetWork 的建议 |
| --- | --- | --- |
| 上下文如何传递 | Go 显式参数；ASP.NET/DotNetty 提供操作 facade | Projector/codec 显式接收只读 operation context；不使用隐式全局状态 |
| 可选能力如何扩展 | ASP.NET typed feature；HTTP `TypeId` extensions | 类型化 capability；核心字段用 packet-specific 类型，不用字符串 map |
| 内存如何暴露 | Tokio/Rust 借用 view；Bytes `chunk_mut` | 同步 bounded lease；视图不能逃逸 callback |
| 实际长度如何提交 | `filled`、`advance`、`advance_mut`、`append` | codec 返回 `written` 或调用一次 `Commit(written)`，由 assembler 校验 |
| 固定/可变如何表达 | 容量与有效长度分开 | `Fixed(n)` 表示 exact；`Bounded(min,max)` 表示合法提交范围 |
| 读写是否共用对象 | 成熟项目通常区分 read view / mutable writer / frozen bytes | 读侧只读、写侧可写；不要共用一个 read-write context segment |
| 输出如何拼接 | Folly/gRPC 使用 chain；需要连续时显式 coalesce | 首版 copy 拼接；零拷贝作为独立 ownership 设计 |
| 失败如何处理 | 不安全操作由 contract 约束；Folly coalesce 保证失败不改 chain | segment 未通过 bounds 校验就不提交最终 writer |
| 异步生命周期 | Pipe/ownership 类型明确表达跨 await 存活 | 首版只允许同步 lease；异步 memory 另设 owner/lease API |

## 6. NetWork 推荐设计

### 6.1 上下文层：只提供 operation context 和 typed capability

上下文层的接口应回答：

- 当前操作的 cancellation/deadline 是什么；
- 当前 schema/profile/revision 是什么；
- 允许 Projector 使用哪些只读目录、模式和预算；
- 如何把必要的诊断信息绑定到一次投影。

它不应回答：

- 当前 segment 的最终 byte offset 是多少；
- codec 可以把多少字节直接提交到传输 writer；
- callback 结束后是否还能继续使用某个 memory view；
- 领域对象是否在编码期间保持可变且可回读。

上下文能力应有三层可见性：

| 能力 | 使用者 | 生命周期 |
| --- | --- | --- |
| `OperationMetadata` | Projector、验证器、诊断 | 一次投影/编码操作 |
| `ReadOnlyCapability<T>` | 明确声明需要该目录/模式的 Projector | 与 operation context 相同 |
| `SegmentWriteLease` | 非上下文 codec/assembler | 一次同步 segment callback |

第三种能力不应放进前两种对象中。这样可以在 code review 中直接检查“哪个模块拿到了可写内存”，而不是从一个庞大 Context 类型里猜测。

### 6.2 内存写侧：`MaxBytes` 暴露，`written` 提交

对 `20-70` 段，建议语义是：

```text
schema bounds: min = 20, max = 70
codec writable capacity: exactly 70 bytes
codec result: written = 20..70
assembler action: append only [0, written)
```

这不是把“长度范围”交给 `IBufferWriter.GetSpan(20)`。`sizeHint` 只能表达一次请求至少需要多少空间，并不表达协议上限或最终长度。

固定段只是同一个 contract 的特例：

```text
min = max = n
codec writable capacity = n
written must equal n
```

注册函数应只能写当前 lease 的范围。lease 结束后，注册函数不得保存 span、memory、writer 或 owner；如果未来需要跨异步边界，必须改成显式 `IMemoryOwner<byte>`/lease 所有权模型，不能偷偷放宽同步 API。

### 6.3 内存读侧：边界先于 codec

写侧可以由 schema 给出 `MaxBytes`，读侧不能仅凭 `20-70` 判断一段输入在哪里结束。读侧必须由以下至少一种机制提供边界：

- 固定长度字段；
- 前置长度或父级 length-delimited 边界；
- 外层 frame/record 已经切出的精确 range；
- 自描述格式能够可靠确定结束位置。

边界确定后才调用 segment decoder，并且 decoder 只收到 `ReadOnlySpan<byte>`/`ReadOnlySequence<byte>` view。decoder 不拥有输入，也不改变 reader 的全局位置；消费位置由外层 framing/assembler 控制。

### 6.4 拼接器：提交是唯一进入最终输出的门

拼接器应拥有以下职责：

1. 根据 schema 选择 bounds 和 staging 存储；
2. 将长度恰好为 `MaxBytes` 的写 lease 交给 codec；
3. 检查 codec 返回的 `written` 是否在范围内；
4. 检查本段的 framing、预算和 slot 顺序；
5. 验证成功后将 `[0, written)` 追加到最终 writer；
6. 失败时丢弃 staging，不要求最终 writer 支持 rollback。

因此注册函数不是“拥有输出流的函数”，而是“填充一个受限本段并报告提交长度的 codec”。这比把 `IBufferWriter<byte>` 暴露到所有注册函数更容易保证失败隔离和测试确定性。

### 6.5 注册模型：slot 声明能力，不暴露动态上下文

注册表中应至少存在以下静态事实：

- slot identity；
- `Fixed(n)` 或 `Bounded(min,max)`；
- 写 codec 和读 codec 的类型；
- 是否需要 framing/length prefix；
- 是否允许 copy assembly 或未来的 owned segment；
- schema revision/profile。

注册函数通过 slot 获得强类型值和受限 lease，而不是自行从 Context 中寻找 `Main`、`Session`、目录或某个字符串服务。这样 registration 的失败可以在启动/构建 schema 时发现，而不是在线上 callback 中才暴露。

## 7. 三种方案比较

### 方案 A：完整 Context + 最终 writer 直接暴露

**形状：**注册函数同时接收上下文、领域对象、最终 `IBufferWriter<byte>` 和任意服务。

**优点：**迁移初期改动少，复杂 codec 可以自由读取和写入。

**问题：**

- 重新合并上下文判断和 wire 编码；
- callback 可以绕过 `WireSubmission`；
- 最终 writer 失败后通常无法通用 rollback；
- 生命周期、异步逃逸和所有权无法从接口看出；
- 测试必须构造整个运行时上下文。

**结论：**不采用。它保留了当前要删除的耦合。

### 方案 B：强类型 submission + bounded staging lease

**形状：**Projector 生成不可变 submission；非上下文 codec 获得当前 slot 的固定/有界 staging lease；返回 `written` 后由 assembler 校验并追加。

**优点：**

- 与当前 WireSubmission 设计一致；
- 上下文和内存生命周期分离；
- 失败不会污染最终 writer；
- 固定段和可变段使用同一套语义；
- 可以在不引入 ownership graph 的情况下验证 Packet 13/20。

**代价：**每个 segment 可能发生一次 copy；需要明确 framing 和 bounds 验证。

**结论：**首选。copy 是可测量、可替换的实现细节，不应先用零拷贝复杂度换取未经证明的性能。

### 方案 C：能力注册 + owned segment chain / zero-copy

**形状：**codec 直接产生带 owner 的不可变 segment，assembler 只连接 chain；需要连续输出时显式 coalesce。

**优点：**可减少大 payload 的 copy，适合真正的 scatter/gather 传输。

**代价：**需要定义 owner、refcount、线程安全、释放、跨 async、合并失败和 transport 是否接受多 segment；会显著扩大公共接口。

**结论：**作为后续性能分支，不作为首版 Context API。只有 profiling 证明 copy 是瓶颈，且传输适配层能保留 segment chain 时才进入设计。

## 8. 明确删除的过度设计

本次调研支持删除或暂不引入以下设计：

1. **万能 `IContext`**：同时放取消、领域对象、目录、writer、registry 和任意服务。
2. **字符串键上下文**：`Get("whatever")`、`Dictionary<string, object>` 和匿名扩展字段。
3. **上下文持有 raw memory**：把 `Span<byte>`/`Memory<byte>` 放进长期 context 或注册表。
4. **一个对象同时读写**：同一 segment 类型既允许 codec 写入，又允许另一个层任意推进输入 reader。
5. **默认 backpatch/transaction writer**：没有协议需求时，不增加 reserve、checkpoint token 栈和通用 rollback。
6. **首版零拷贝链**：没有 ownership、释放和传输证据时，不把 component chain 公开成基础 contract。
7. **自动修复非法长度**：超出 `[min,max]` 时截断、补零或偷偷改 schema；应直接拒绝。
8. **用 20-70 推断读取边界**：范围只校验合法长度，不能代替 framing。
9. **用 `AsyncLocal` 隐式注入当前上下文**：这会隐藏依赖，且不利于并发、回放和确定性测试。
10. **把上下文 API 做成新的通用 IR**：当前只需要 operation metadata、typed capability、submission 和 segment lease 四个概念。

## 9. 建议的最终设计语言

为了避免后续文档混用概念，建议固定以下词义：

| 术语 | 含义 | 不应表示 |
| --- | --- | --- |
| `OperationContext` | 一次投影/编码操作的只读元数据和 typed capability | 领域全局对象或输出 buffer |
| `WireSubmission` | 已完成上下文判断的不可变 wire 语义快照 | 延迟求值、writer、业务 Effect |
| `SegmentBounds` | 固定或有界 segment 的 schema 约束 | `GetSpan` 的 size hint |
| `WriteLease` | 一次同步 callback 可写的最大范围 | 可跨 callback 保存的 owner |
| `written` / `committed` | 本段实际进入 wire 的字节数 | buffer capacity |
| `ReadView` | 已有 framing 边界的只读输入 range | 自动寻找 segment 终点的 parser |
| `Assembler` | 校验 segment 并按顺序提交/拼接 | 重新计算领域条件 |
| `OwnedSegment` | 明确转移所有权的不可变输出片段 | 普通 `Span` 的别名 |

其中 `OwnedSegment` 暂不属于首版必需接口；先把它作为未来零拷贝方案的术语，避免把“借用视图”和“拥有内存”混为一谈。

## 10. 结论与验收方向

当前最稳的设计不是“上下文层暴露 20-70 字节内存段”，而是：

```text
上下文层：显式 operation context + typed read-only capability
投影层：外部状态 -> immutable WireSubmission
非上下文 codec：submission slot -> bounded synchronous WriteLease
拼接器：validate written -> append committed bytes
传输层：接收已完成的 frame/output
```

首版验收应是设计不变量，而不是先验收零拷贝性能：

- EncodePlan 和 segment codec 无法取得 `Main`、`World`、`Session` 或路由对象；
- 固定段只能提交 exact length；
- `Bounded(20,70)` 只能提交 20 到 70 字节；
- codec 返回失败或越界长度时，最终 writer 没有本段残留；
- 读 codec 只处理外层已切出的精确范围；
- lease 只能在同步调用内有效；
- 相同 `WireSubmission` 可以重复编码而不重新读取外部上下文；
- 只有真实性能证据出现后，才增加 owned segment chain。

这套设计直接延伸现有 `WireSubmission` 边界，同时吸收 GitHub 项目中已被反复验证的三个原则：上下文显式传递、能力类型化、内存写入显式提交。它足以支持固定段和 20-70 可变段，又不会把 NetWork 提前变成一个通用异步内存/零拷贝框架。

