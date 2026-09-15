## [ERR-20260909-GITCLONE] temporary-research-checkout

**Logged**: 2026-09-09T00:00:00+08:00
**Priority**: low
**Status**: resolved
**Area**: docs

### Summary
浅克隆目标 GitHub 仓库时，Windows checkout 遇到测试资源长文件名，另外大型仓库 checkout 仍在并发命令返回前运行。

### Error
```text
error: unable to create file ...: Filename too long
fatal: unable to checkout working tree
```

### Context
- 仅发生在 `C:\\Users\\shan\\AppData\\Local\\Temp\\net-context-github-research-20260909` 临时研究副本。
- 目标源码目录已经可读取；没有改动项目生产源码。

### Suggested Fix
研究源码时只读取目标 `src` 路径，或使用固定 commit 的 raw GitHub URL；不要为此 checkout 测试资源全集。

### Metadata
- Reproducible: yes
- Related Files: docs/research/2026-09-09-packet-context-api-github-research.md

---

## [ERR-20260915-MOVEDEST] safe-prototype-move-script

**Logged**: 2026-09-15T00:00:00+08:00
**Priority**: medium
**Status**: in_progress
**Area**: infra

### Summary
第一批原型源码移动脚本在目标目录已预创建时停止，避免将目录嵌套或覆盖到错误位置。

### Error
```text
Destination already exists: Prototypes\\packet13-segment-codec
```

### Context
- 操作：将多个现有概念目录移动到 `Prototypes/<concept-name>/`。
- 前置动作创建了目标目录，随后复用只接受“目标不存在”的目录移动函数处理 `Verification/PacketContextBindingPrototype`。
- 脚本在该项之前已经完成了 `Core`、Graph、Tools、SourceGen、ProtocolDemo 和 Legacy 文件的移动；没有执行覆盖操作。

### Suggested Fix
对已创建的目标目录使用“移动目录内容”操作，并在每个源/目标对上先做存在性检查；不要盲目重复执行已部分完成的整批脚本。

### Metadata
- Reproducible: yes
- Related Files: `docs/plans/2026-09-15-prototype-isolation-implementation.md`
- See Also: ERR-20260909-PSPIPE

---

## [ERR-20260911-LEGACYBUILD] missing-legacy-runtime-build

**Logged**: 2026-09-11T00:00:00+08:00
**Priority**: high
**Status**: pending
**Area**: infra

### Summary
The only discoverable legacy Terraria binary emits protocol version 315, while the checked-in legacy source and golden require 318; the available source cannot produce a replacement net40 assembly in this environment.

### Error
```text
NU1104: 找不到项目 ... src\Terraria.WorldGen.Ecs\Terraria.WorldGen.Ecs.csproj

随后绕过项目引用进入 CoreCompile 时，编译器报告大量 CS0246/CS0518，包含 System、Newtonsoft 及游戏类型缺失。
```

### Context
- Default command: `dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore`
- Actual direct legacy Hello frame ends in `...333135`; golden requires `...333138`.
- Packet 20 direct legacy bytes continue to match, so the wire migration code is not the source of the gate failure.
- The expected `D:\lodes\TR\Backup\New1.27\2` runtime path does not exist; exhaustive targeted search found no alternate `TerrariaServer.exe`.

### Suggested Fix
Provide or build the matching x86 legacy runtime from the external Terraria source tree, then set `NET_WORK_LEGACY_ASSEMBLY_PATH` or place the artifact at the bridge's discovered path. Do not change the golden snapshot or silently skip the Hello differential.

### Metadata
- Reproducible: yes
- Related Files: `Verification/ProtocolDemoStandalone/LegacyTrDirectBridge.cs`, `Verification/ProtocolDemoStandalone/ProtocolWireGoldenSnapshots.cs`
- See Also: ERR-20260911-X86REFLECT

---

## [ERR-20260911-PSMSBUILD] msbuild-switch-semicolon

**Logged**: 2026-09-11T00:00:00+08:00
**Priority**: low
**Status**: pending
**Area**: tests

### Summary
An MSBuild logger switch containing a semicolon was not quoted in PowerShell, so the shell executed `Summary` as a separate command after MSBuild failed.

### Error
```text
The term 'Summary' is not recognized as a name of a cmdlet, function, script file, or executable program.
```

### Context
- Command used `/clp:ErrorsOnly;Summary` without quoting.
- The MSBuild failure itself remained independently observable; no repository source was changed.

### Suggested Fix
Quote arguments containing PowerShell statement separators, for example `'/clp:ErrorsOnly;Summary'`.

### Metadata
- Reproducible: yes
- Related Files: none
- See Also: ERR-20260911-PSFOREACH

---

## [ERR-20260911-X86REFLECT] legacy-assembly-powershell-reflection

**Logged**: 2026-09-11T00:00:00+08:00
**Priority**: low
**Status**: pending
**Area**: tests

### Summary
64-bit PowerShell could not load the x86 legacy `TerrariaServer.exe` for IL inspection.

### Error
```text
Exception calling "LoadFrom": "Could not load file or assembly ... The assembly architecture is not compatible with the current process architecture."
```

### Context
- The inspection targeted the parent repository's stale legacy runtime to locate the protocol-version constant.
- The worker process can still execute the x86 assembly; only in-process reflection from the current PowerShell host is unsupported.

### Suggested Fix
Use an x86 host for reflection or inspect the PE/metadata without loading the assembly; guard later commands after a failed load to avoid cascading null-object errors.

### Metadata
- Reproducible: yes
- Related Files: none
- See Also: ERR-20260911-PSFOREACH

---

## [ERR-20260911-PSFOREACH] binary-version-audit-powershell-pipeline

**Logged**: 2026-09-11T00:00:00+08:00
**Priority**: low
**Status**: pending
**Area**: tests

### Summary
PowerShell version-audit command piped a `foreach` statement directly into `Format-Table`, producing an empty-pipeline parser error.

### Error
```text
ParserError: An empty pipe element is not allowed.
```

### Context
- The command attempted to count `Terraria315` and `Terraria318` occurrences for a legacy binary across multiple encodings.
- The failure happened during command parsing; no file was changed and no runtime verification was affected.

### Suggested Fix
Assign the `foreach` expression to a variable or wrap it in a subexpression before piping to a formatter.

### Metadata
- Reproducible: yes
- Related Files: none
- See Also: ERR-20260909-PSPIPE, ERR-20260907-PS1

---

## [ERR-20260909-APPHOST] standalone-project-shared-apphost

**Logged**: 2026-09-09T00:00:00+08:00
**Priority**: medium
**Status**: resolved
**Area**: config

### Summary
多个 standalone 项目共享默认中间目录时，并行构建会覆盖 apphost，导致 `dotnet run --no-build` 启动另一套原型。

### Error
```text
Unhandled exception. System.InvalidOperationException: Cannot read keys when either application does not have a console or when console input has been redirected.
at System.ConsolePal.ReadKey(Boolean intercept)
at Program.<Main>$(String[] args) in ...\Concept\ContextBindingPrototype\Program.cs
```

### Context
- 目标命令：`dotnet run --project Verification/PacketContextBindingPrototype/PacketContextBindingPrototype.csproj --no-build -- --script`。
- 工作树内同时存在 `Concept/ContextBindingPrototype`、`test4/context-binding-prototype` 和 `Verification/PacketContextBindingPrototype`，而 `Directory.Build.props` 的默认 `Build/obj/net10.0` 被多个项目共用。
- 目标 DLL 内容正确，但共享的 apphost 被另一个项目的构建覆盖。

### Suggested Fix
为需要独立运行的 standalone 项目显式设置项目专属 `OutputPath` 和 `IntermediateOutputPath`；并行验证时避免共享 apphost。

### Metadata
- Reproducible: yes
- Related Files: `Verification/PacketContextBindingPrototype/PacketContextBindingPrototype.csproj`, `Directory.Build.props`

### Resolution
- **Resolved**: 2026-09-09T00:00:00+08:00
- **Notes**: 目标项目使用 `Build/bin/PacketContextBindingPrototype/...` 和 `Build/obj/PacketContextBindingPrototype/...`，之后串行 `dotnet run -- --script` 与交互输入均通过。

---

## [ERR-20260909-GITIDENT] prototype-commit-missing-identity

**Logged**: 2026-09-09T21:59:42+08:00
**Priority**: low
**Status**: resolved
**Area**: config

### Summary
The prototype commit was rejected because this worktree has no Git author identity configured.

### Error
```text
Author identity unknown
fatal: unable to auto-detect email address
```

### Context
- Command: `git commit -m "prototype: demonstrate context binding submission"`
- The five intended prototype files were already staged and passed `git diff --cached --check`.
- `git config --show-origin --get-regexp '^user\\.(name|email)$'` returned no identity.

### Suggested Fix
Configure the user's preferred repository or global Git identity, or supply an explicit author/committer identity for this throwaway commit. Do not change global configuration automatically.

### Metadata
- Reproducible: yes
- Related Files: Concept/ContextBindingPrototype, docs/plans/2026-09-09-context-binding-prototype-design.md

### Resolution
- **Resolved**: 2026-09-09T21:59:42+08:00
- **Notes**: Created the commit with one-time command-local identity `Codex <codex@local>`; no Git configuration was changed.

---

## [ERR-20260909-PROTOTYPE-COLLISION] parallel-prototype-output-collision

**Logged**: 2026-09-09T19:05:00+08:00
**Priority**: medium
**Status**: resolved
**Area**: config

### Summary
The prototype project used the generic project name `ContextBindingPrototype`, colliding with another AI's concurrently running prototype under the repository-wide build output rules.

### Error
```text
MSB3027: ... ContextBindingPrototype.exe ... 文件被“ContextBindingPrototype (...)”锁定。
```

### Context
- My project was `test4/context-binding-prototype/ContextBindingPrototype.csproj`.
- Another active process was verified from its command line as `Concept/ContextBindingPrototype/...`.
- Both projects inherited the same `Build/bin/net10.0/ContextBindingPrototype.exe` output path.

### Suggested Fix
Every parallel throwaway prototype must use a unique assembly name and isolated output/intermediate directories; never stop another agent's process just to release a shared artifact.

### Metadata
- Reproducible: yes
- Related Files: test4/context-binding-prototype/ContextBindingPrototype.csproj

### Resolution
- **Resolved**: 2026-09-09T19:05:00+08:00
- **Notes**: Added unique `NetWork.ContextBindingApiPrototype` output. The first attempt to override `MSBuildProjectExtensionsPath` in the project file produced MSB3540; isolation was moved to a local `Directory.Build.props` so it runs before the repository-wide props.

---

## [ERR-20260909-PROTOTYPE-CLEAR] context-binding-prototype-console-clear

**Logged**: 2026-09-09T19:00:00+08:00
**Priority**: medium
**Status**: resolved
**Area**: backend

### Summary
The logic prototype exited when `Console.Clear()` was called from a non-interactive terminal without a valid console handle.

### Error
```text
System.IO.IOException: 句柄无效。
at System.ConsolePal.GetBufferInfo(...)
at System.ConsolePal.Clear()
```

### Context
- Command: `dotnet run --project test4/context-binding-prototype/ContextBindingPrototype.csproj --no-build -- --demo`
- The prototype rendered correctly in an interactive terminal but failed under redirected/tool output.

### Suggested Fix
Only clear when output is not redirected, and fall back to a frame separator when the host cannot clear the console.

### Metadata
- Reproducible: yes
- Related Files: test4/context-binding-prototype/Program.cs

### Resolution
- **Resolved**: 2026-09-09T19:00:00+08:00
- **Notes**: Added a guarded `ClearFrame()` fallback for redirected and hosted terminals.

---

## [ERR-20260806-PS1] powershell-web-research-command

**Logged**: 2026-08-06T00:00:00+08:00
**Priority**: low
**Status**: pending
**Area**: docs

### Summary
PowerShell command used an empty pipeline element while collecting official documentation page titles.

### Error
```text
ParserError: An empty pipe element is not allowed.
```

### Context
- Attempted to iterate official documentation URLs and pipe the loop output directly to `Format-Table`.
- The script block syntax placed the pipeline after the closing brace in a way PowerShell parsed as an empty element.

### Suggested Fix
Collect objects in an array or assign the loop result before formatting.

### Metadata
- Reproducible: yes
- Related Files: none

---

## [ERR-20260909-PSVAR] report-section-read-tool-orchestration

**Logged**: 2026-09-09T18:11:09+08:00
**Priority**: low
**Status**: resolved
**Area**: docs

### Summary
A report verification command failed because the tool orchestration referenced `$r` instead of the returned result variable.

### Error
```text
ReferenceError: $r is not defined
```

### Context
- The command was intended to read the API prototype section of `docs/research/2026-09-09-context-api-memory-exposure-design-research.md`.
- The failure occurred in the orchestration wrapper after the PowerShell command completed; no repository file was changed by the failed command.

### Suggested Fix
Use the actual result variable when forwarding `exec_command` output, and keep the PowerShell command independent from the wrapper variable name.

### Metadata
- Reproducible: no
- Related Files: docs/research/2026-09-09-context-api-memory-exposure-design-research.md

### Resolution
- **Resolved**: 2026-09-09T18:11:09+08:00
- **Notes**: Corrected the wrapper variable and resumed verification.

---

## [ERR-20260909-GITHUBRAW] github-raw-audit-timeout

**Logged**: 2026-09-09T17:55:00+08:00
**Priority**: low
**Status**: pending
**Area**: docs

### Summary
A fresh raw-source verification of the GitHub project report was interrupted by a transient connection timeout to raw.githubusercontent.com.

### Error
```text
curl: (28) Failed to connect to raw.githubusercontent.com port 443 after 21687 ms
```

### Context
- The same six pinned raw files had already been read successfully earlier in the task.
- The report structure and local diff checks passed in the failed verification round.

### Suggested Fix
Retry the same pinned raw URLs with bounded retries; treat a transient network failure as an observation failure, not as evidence against the report.

### Metadata
- Reproducible: unknown
- Related Files: docs/research/2026-09-09-github-memory-segment-patterns.md

---

## [ERR-20260909-GITHUBLINK] github-link-audit-timeout

**Logged**: 2026-09-09T17:53:00+08:00
**Priority**: low
**Status**: pending
**Area**: docs

### Summary
A batch audit of all GitHub web links in the research report timed out while connecting to github.com.

### Error
```text
curl: (28) Failed to connect to github.com port 443 after 21120 ms
```

### Context
- Direct raw-source reads for the six pinned repository files succeeded.
- The failed batch audit used GitHub HTML links and was not used as evidence that the source files were missing.

### Suggested Fix
Prefer the already pinned raw source URLs for evidence checks and audit web links separately when GitHub connectivity is available.

### Metadata
- Reproducible: unknown
- Related Files: docs/research/2026-09-09-github-memory-segment-patterns.md

---

## [ERR-20260909-PSSEGMENT2] powershell-verification-assumption

**Logged**: 2026-09-09T17:25:00+08:00
**Priority**: low
**Status**: pending
**Area**: docs

### Summary
A research-note verification command expected `SegmentRegistry`, while the note intentionally uses the more specific name `SegmentCodecRegistry`.

### Error
```text
Missing research topics: SegmentRegistry
```

### Context
- The verification checked required topics in `docs/research/2026-09-09-memory-segment-api-research.md`.
- The document content was correct; only the checker used a mismatched expected term.

### Suggested Fix
Verify concepts by accepted aliases or use the exact vocabulary established in the artifact before asserting missing content.

### Metadata
- Reproducible: yes
- Related Files: docs/research/2026-09-09-memory-segment-api-research.md

---

## [ERR-20260909-JSQUERY] github-search-orchestration

**Logged**: 2026-09-09T17:30:00+08:00
**Priority**: low
**Status**: pending
**Area**: docs

### Summary
The first parallel GitHub search orchestration referenced an undefined JavaScript variable before issuing any network request.

### Error
```text
ReferenceError: uri is not defined
```

### Context
- The query list was prepared correctly, but the command construction used `uri` instead of encoding each query explicitly.
- No GitHub request or repository file was changed by the failed call.

### Suggested Fix
Use `encodeURIComponent(query)` directly in the orchestration expression and inspect each query result independently.

### Metadata
- Reproducible: yes
- Related Files: none

---

## [ERR-20260909-PSAUDIT] powershell-research-assertion

**Logged**: 2026-09-09T17:21:00+08:00
**Priority**: low
**Status**: pending
**Area**: docs

### Summary
A completion-audit command used an exact Chinese sentence that did not match the equivalent wording in the research note.

### Error
```text
Missing research evidence: IBufferWriter 没有通用 rollback 契约
```

### Context
- The research note contains the rollback conclusion, but the audit asserted a wording that is not literal text in the file.
- No repository content was changed by the failed assertion.

### Suggested Fix
Assert stable technical terms and source URLs, not exact prose wording.

### Metadata
- Reproducible: yes
- Related Files: docs/research/2026-09-09-memory-segment-api-research.md

---

## [ERR-20260909-PSSEGMENT] powershell-verification-quoting

**Logged**: 2026-09-09T17:17:00+08:00
**Priority**: low
**Status**: pending
**Area**: docs

### Summary
A PowerShell verification command for the new research note had an unbalanced closing parenthesis.

### Error
```text
ParserError: Missing closing ')' in expression.
```

### Context
- The command attempted to count official URLs and local evidence markers in `docs/research/2026-09-09-memory-segment-api-research.md`.
- No file content was changed by the failed command.

### Suggested Fix
Prefer separate simple verification expressions instead of composing several nested regex/count calls in one command.

### Metadata
- Reproducible: yes
- Related Files: docs/research/2026-09-09-memory-segment-api-research.md

---

## [ERR-20260909-AGENT502] researcher-background-channel

**Logged**: 2026-09-09T17:07:00+08:00
**Priority**: medium
**Status**: pending
**Area**: docs

### Summary
The background research agent failed before producing the requested official API research because its model endpoint returned HTTP 502.

### Error
```text
unexpected status 502 Bad Gateway: error code: 502, url: https://aihub.top/responses
```

### Context
- The agent was asked to research official .NET buffer and pipeline APIs for bounded fixed/variable wire segments.
- No research file was produced by the agent.
- The main agent continued with local repository evidence and browser-based lookup attempts.

### Suggested Fix
Retry the research with an available channel, or collect the official sources directly in the main agent.

### Metadata
- Reproducible: unknown
- Related Files: docs/research/2026-09-09-memory-segment-api-research.md

---

## [ERR-20260907-PSQUOTE] local-source-audit-powershell-quoting

**Logged**: 2026-09-07T00:00:00+08:00
**Priority**: low
**Status**: pending
**Area**: docs

### Summary
A PowerShell `rg` audit command failed because embedded double quotes were parsed by PowerShell before reaching ripgrep.

### Error
```text
ParserError: Missing property name after reference operator.
```

### Context
- Attempted to search project XML for `Compile Include="...Concept` and related properties in one double-quoted PowerShell argument.
- The failure affected only the audit command; no repository source or project file was modified.

### Suggested Fix
Use single-quoted PowerShell strings for the ripgrep pattern, or pass each project file and pattern as separate arguments.

### Metadata
- Reproducible: yes
- Related Files: NetWork.csproj, Directory.Build.props

---

## [ERR-20260907-PS1] powershell-explicit-compile-audit

**Logged**: 2026-09-07T00:00:00+08:00
**Priority**: low
**Status**: pending
**Area**: docs

### Summary
PowerShell explicit-compile audit used a pipeline directly after a `foreach` statement and failed to parse.

### Error
```text
ParserError: An empty pipe element is not allowed.
```

### Context
- Attempted to enumerate non-glob `Compile Include` entries from every `.csproj` and pipe the `foreach` result directly to `Format-Table`.
- The repository inspection itself was unaffected; the command failed before evaluating the audit body.

### Suggested Fix
Collect the objects in an array or assign the `foreach` result to a variable before formatting.

### Metadata
- Reproducible: yes
- Related Files: NetWork.csproj, Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj

---

## [ERR-20260806-PS2] powershell-link-audit-command

**Logged**: 2026-08-06T00:45:00+08:00
**Priority**: low
**Status**: pending
**Area**: docs

### Summary
A helper command for auditing two relative documentation links repeated the empty-pipeline PowerShell syntax mistake.

### Error
```text
ParserError: An empty pipe element is not allowed.
```

### Context
- The command attempted to pipe a `foreach` statement directly into `Format-Table`.
- The individual `NetMessage.cs` relative target had already been checked successfully with a non-pipeline command.

### Suggested Fix
Assign loop output to a variable before formatting, or check each target explicitly.

### Metadata
- Reproducible: yes
- Related Files: docs/research/netmessage-complex-examples.md

---

## [ERR-20260806-AGENT1] researcher-background-channel

**Logged**: 2026-08-06T00:30:00+08:00
**Priority**: medium
**Status**: pending
**Area**: docs

### Summary
The research skill's background researcher failed before producing a report because its model channel returned HTTP 503.

### Error
```text
503 Service Unavailable: No available channel for model gpt-5.4-mini
```

### Context
- Background task was asked to compare complex Terraria message structures with first-party protocol projects.
- No research file was produced by that agent.

### Suggested Fix
Retry with an available model channel or perform the same primary-source research in the main agent.

### Metadata
- Reproducible: unknown
- Related Files: docs/research/netmessage-complex-examples.md

---

## [ERR-20260909-PSPIPE] final-path-check-foreach-pipeline

**Logged**: 2026-09-09T18:12:53+08:00
**Priority**: low
**Status**: resolved
**Area**: docs

### Summary
A final path-check command repeated the PowerShell empty-pipeline mistake by piping a `foreach` statement directly to `Format-Table`.

### Error
```text
ParserError: An empty pipe element is not allowed.
```

### Context
- The command intended to verify that the research report, plan, index, and error log paths existed.
- Other final checks completed; no source or report content was modified by the failed command.

### Suggested Fix
Assign the `foreach` output to a variable before formatting, or use an explicit loop body without a pipeline.

### Metadata
- Reproducible: yes
- Related Files: docs/research/2026-09-09-context-api-memory-exposure-design-research.md
- See Also: ERR-20260907-PS1, ERR-20260806-PS2

### Resolution
- **Resolved**: 2026-09-09T18:12:53+08:00
- **Notes**: Replaced the direct `foreach` pipeline with an array assignment for the final check.

---

## [ERR-20260909-PROTOASSERT] prototype-output-assertion

**Logged**: 2026-09-09T00:00:00+08:00
**Priority**: low
**Status**: resolved
**Area**: tests

### Summary
验证脚本未匹配到已知的原型 transcript，误报为缺少行为证据。

### Error
```text
Missing demo evidence: read 42 bytes within ParentDelimited boundary, final=136->136, external input changed; existing PreparedPacket13 was not rebound, identical=True, ProjectionRejected:, registration rejected: The segment registry is frozen.
```

### Context
- Command: `dotnet run --project Verification/PacketContextBindingPrototype/PacketContextBindingPrototype.csproj -- --script`，随后用 PowerShell `Out-String` 和统一正则断言输出。
- 原型本身已经能运行；失败来自断言对多行/包装输出的匹配方式，不是编译或运行时错误。

### Suggested Fix
对 transcript 使用 `Select-String` 逐行检查稳定的短证据，或先按输出行拆分后再断言；不要把多段带换行的输出交给一个正则匹配。

### Metadata
- Reproducible: yes
- Related Files: `Verification/PacketContextBindingPrototype/Program.cs`

### Resolution
- **Resolved**: 2026-09-09T00:00:00+08:00
- **Notes**: 后续验证改用逐行稳定片段，并重新运行 `--script`。

---
