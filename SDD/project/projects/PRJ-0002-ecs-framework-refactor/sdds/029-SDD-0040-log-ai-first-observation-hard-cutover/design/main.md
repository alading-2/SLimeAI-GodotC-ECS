# Log AI-first Observation Hard Cutover

> 2026-06-11 note：本文件是 SDD-0040 初始执行设计快照。Log 记录层仍参考本文件；整理层当前契约以项目级 `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/第二部分-语义提炼整理/03-最终设计与完成清单.md` 为准。旧 `by-owner` / `by-phase` raw 分桶和 pretty `flows.json` 不再是默认 analyzer 产物。

## Goal

把 SlimeAI 当前 Logger 从“文本打印工具”升级为 AI-first Observation 入口。目标不是多加几个日志等级，而是让运行调试、测试验证、scene runner 分析和 AI 排障都消费同一套结构化事实。

本 SDD 解决的问题：

- 当前 `Log.cs` 以 `GD.PrintRich`、墙钟时间、全局等级和 context filter 为主，缺少 `owner / operation / phase / entity / correlation / outcome / validationStatus`。
- 测试结果分裂在 `GD.Print("PASS")`、`GD.PushError("FAIL")`、`_log.Info("[PASS]")`、`_log.Error("[FAIL]")` 和 `throw` 中，runner 只能靠字符串 pattern 猜结果。
- `godot-scene-test` runner / analyzer 仍维护 stdout pattern 和日志拆分规则，长期会和 Log owner 形成第二事实源。
- AI 分析经常直接读取全量 stdout，无法稳定按 owner、phase、flow、failure、noise 和 missing-fields 缩小范围。

非目标：

- 不引入外部 logging/observability 平台或第三方依赖。
- 不把详细日志逐条改成 `Console.WriteLine`。
- 不把 Godot editor Output / `GD.PrintRich` 作为 AI-first 默认主链路。
- 不在本 SDD 范围内重构整个 TestSystem；只先建立 ValidationSession / artifact 主链路和第一批高价值测试迁移。

## Context

### 必读事实源

1. 项目入口：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
2. Log 设计包：`design/README.md`、`01-现状分析与AI-first裁决.md`、`02-目标架构与数据契约.md`、`03-控制面与CLI设计.md`、`04-测试统一与Observation接入.md`、`05-调用点迁移与验证计划.md`、`06-功能OwnerLog文档与分析流程.md`
3. 当前 Logger docs：`DocsAI/ECS/Tools/Logger/README.md`
4. Godot scene test skill：`.ai-config/skills/godot/godot-scene-test/SKILL.md`
5. 当前代码与脚本：`Src/ECS/Tools/Logger/Log.cs`、`Src/ECS/**/Tests/**/*.cs`、`.ai-config/skills/godot/godot-scene-test/scripts/godot-scene-runner.mjs`、`.ai-config/skills/godot/godot-scene-test/scripts/analyze-logs.sh`

### 现有裁决

- 默认详细事实：C# buffered JSONL file sink。
- 默认可见输出：C# stdout summary sink。
- Validation / runner 主事实源：artifact + structured log + exit code。
- Godot editor sink：默认关闭，只服务人工 editor debug。
- `Success` 不再是 severity；改为 `severity / outcome / validationStatus` 拆分。
- 长过程使用 `OperationTrace` / flow summary，console 只展示摘要，详细 step 进 JSONL / artifact。
- `logctl` 既做运行控制，也做离线 `analyze/query/ingest/suggest`。
- `godot-scene-test` 长期只负责运行场景、保存 run dir、调用 `logctl`、读取 gate report。

## Design

### 目标主链路

```text
Log source / Validation check / Flow step
  -> LogEntry structured envelope
  -> LogProfile / LogRule / budget
  -> sinks
      -> StdoutSummarySink
      -> JsonlBufferedFileSink
      -> MemorySink
  -> ArtifactSink
      -> optional GodotEditorSink
  -> logctl analyze/query
  -> analysis/summary.md + ai-context.md + flows/flows.jsonl + noise/templates.jsonl + failures + missing-fields + raw/entries.jsonl
```

### 核心契约

- `LogEntry` 必须包含运行内时间、路由、过程、目标、结果、负载和噪声字段，至少能表达 `runElapsedMs / frame / severity / channel / owner / context / operation / message / fields`。
- `OperationTrace` 用于 AbilityCast、DamageProcess、TargetQuery、ObjectPoolRelease、TimerDispatch、SystemPreflight、ValidationSceneRun 等跨步骤过程。
- `ValidationSession` 写出 `CheckResult`、expected/actual、reasonCode、failureReasons、artifact path 和 final verdict。
- `logctl analyze` 固定生成 `summary.md`、`ai-context.md`、`flows/flows.jsonl`、`noise/templates.jsonl`、`failures/`、`noise/`、`missing-fields/` 和 `raw/entries.jsonl`；默认不生成 `by-owner` / `by-phase` raw 复制分桶或 pretty `flows.json`。
- `logctl query` 支持对 run dir、analysis dir、raw JSONL 和 legacy stdout ingest 后产物按 owner/sourceFile/operation/entityId/severity/reasonCode 过滤。
- `Config/Log` profile/rules/overrides 是稳定事实源；CLI override 必须带 reason、expires 和 run metadata。

### 影响范围

- `Src/ECS/Tools/Logger/**`
- `Src/ECS/Runtime/*/Tests/**` 和第一批 `Src/ECS/Test/**` validation 场景
- `.ai-config/skills/godot/godot-scene-test/**` 及同步副本
- `DocsAI/ECS/Tools/Logger/**`
- 第一批 owner 文档：Data、System、Entity、Ability、Damage、Movement、ObjectPool、Timer、TargetSelector 的 `Log.md` 或 README `## Log`
- SDD/project PRJ-0002 状态、roadmap 和执行提示词

## DeepThink

### Goal

本任务要把 Log 作为 AI-first Observation 入口落地，解决日志自然语言化、PASS/FAIL 分裂、runner 字符串扫描和 AI 全量 stdout 分析的问题。非目标是不引入第三方依赖、不做云观测平台、不一次性重构所有业务测试系统。

### Context Read

- Git boundary：`/home/slime/Code/SlimeAI/SlimeAI`。
- 已读取：PRJ-0002 README / roadmap / progress / design index、`design/Tool/10.Log/`、`DocsAI/ECS/Tools/Logger/README.md`、`.ai-config/skills/godot/godot-scene-test/SKILL.md`、SDD CLI 格式文档和已有 Tools execution prompt。
- 未读取/未验证：当前环境没有跑 Godot 场景采样真实 run；没有 profiler 证据证明 Logger 字符串分配已是最大热路径。

### Evidence / Search Coverage

- `Log.cs` 当前仍以 legacy 文本输出为事实。
- `godot-scene-test` skill 已写入 Log hard cutover 后调用 `logctl analyze/query` 的方向。
- PRJ-0002 roadmap 已把 `design/Tool/10.Log/` 标记为 proposed/TBD，并建议创建 Log hard cutover SDD。
- `DocsAI/ECS/Tools/Logger/README.md` 已把当前实现和 AI-first 目标边界分开。

### Problem Reality Check

问题真实存在。证据来自设计包对 `Log.cs`、测试场景和 runner pattern 的本地扫描：当前日志无法稳定表达 owner/phase/operation/validationStatus，runner 仍依赖 `[PASS]` / `[FAIL]` / `Exception` 等文本 pattern。

未知项是第一版预算阈值和真实噪声分布，需要通过后续 run artifact 校准。

### Idea Check

用户要求“按照要求生成对应 SDD 和提示词，深度思考”成立。直接开始源码实现风险较高，因为 Log 改动横跨 Logger、Validation、runner、skill、DocsAI 和多 owner 文档；先落 SDD 和执行提示词更稳。

### Options

1. **小切片只改 Logger core**：更快，但无法解决 PASS/FAIL 分裂和 runner 第二事实源，容易形成半套结构化日志。
2. **推荐：单个 hard cutover SDD，内部 T1~T8 分阶段执行**：范围完整，能保证 Logger core、Validation artifact、runner analyzer 和 owner 文档同一方向收口。
3. **拆多个长期兼容 SDD**：短期风险低，但会长期保留文本日志、stdout pattern 和 Log CLI 第二阶段，和 hard cutover 原则冲突。

### Recommendation

采用方案 2。创建单个 `SDD-0040 Log AI-first Observation Hard Cutover`，状态保持 pending；后续执行会话按 TDD 和小任务推进，不在本轮直接改源码。

### Must Confirm

思路问题：暂无。项目级设计已经裁决 Log 是 AI-first Observation 入口，不是 Logger 热路径小修。

信息缺口：

- 第一版 `logctl` 实现语言是否必须为 C#，还是允许先用 Python/Node 脚本？默认使用项目现有 CLI 风格，优先 C# runtime + 仓内脚本薄入口，具体由执行基线确认。
- 第一批必须迁移的测试范围是否只限 Data/System/Entity/Logger/Timer/ObjectPool，还是扩到 Ability/Movement/Damage？默认按任务清单第一批迁核心测试，业务 flow 文档先覆盖高价值 owner。

决策未定：

- 是否要求本次执行必须跑 Godot 场景？默认需要；若当前游戏 runner 或 Godot CLI 不可用，记录 blocker，不用 stdout fallback 冒充结构化通过。

### Defaults I Will Use

- `JsonlBufferedFileSink + StdoutSummarySink + MemorySink + ArtifactSink` 默认开。
- `GodotEditorSink` 默认关。
- `stdout-pattern-fallback` 只保留过渡，并必须写入 gate report。
- owner Log 文档先覆盖 Logger、Data、System、Entity、Ability、Damage、Movement、ObjectPool、Timer、TargetSelector。

### Not Recommended

- 不建议只把 `GD.PrintRich` 替换成每条 `Console.WriteLine`。
- 不建议只提高 `GlobalLevel` 或 context level 降噪。
- 不建议让 `godot-scene-test` 长期维护自己的 analyzer 规则。
- 不建议让 AI 直接消费全量 stdout。

### Artifact Updates

本轮写入本 SDD 的 `design/`、`tasks.md`、`bdd.md`、`progress.md`、`notes.md` 和 `execution-prompt.md`，并同步 PRJ-0002 README / roadmap / progress。

## DesignCritic

### Assumptions

- Log hard cutover 有足够收益，因为它会影响后续所有场景验证和 AI 排障。
- 当前不引入第三方依赖是合理约束，可以先用本地结构化 JSONL 和仓内 CLI 达成 80% 价值。
- `godot-scene-test` skill 可以在 Log CLI 成熟前保留 fallback，但必须标记 result source。

### Design Defects To Avoid

- 只实现 structured entry，不实现 analyzer/query，会导致 AI 仍要读全量 JSONL。
- 只实现 analyzer，不迁 Validation helper，会导致 PASS/FAIL 事实源继续分裂。
- 只写 owner `Log.md` 模板，不补实际第一批 owner，会导致后续实现自由发挥。
- 只在 DocsAI 写方向，不同步 `.ai-config` skill，会让不同工具恢复时仍使用旧 runner/analyzer 心智。

### Better Options

比“一步到所有 owner”更好的执行方式是：先完成 Logger/Validation/runner/analyzer 骨架，再迁第一批核心测试和 3~5 个高价值 flow，最后用 grep gate 列出剩余 legacy fallback。

### Recommendation

执行时严格按任务分批，并把每批验证摘要写入 `progress.md`。如果 Godot runner 不可用，应只把场景验证标为 blocked，不把 SDD done。

## Verification

文档/准备阶段：

```bash
python3 Workspace/SDD/sdd.py validate SDD-0040
python3 Workspace/SDD/sdd.py validate --all
python3 Workspace/SDD/sdd.py index
git diff --check
```

代码实施阶段至少需要：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
python3 Workspace/SDD/sdd.py validate SDD-0040
```

Godot runner 可用时还需要在承载游戏仓运行目标 scene，并验证 `resultSource=artifact|structured-log`，`logctl analyze/query` 产出 `analysis/ai-context.md`。
