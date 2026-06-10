# SDD-0040 T2 Execution Prompt

你是 SDD-0040 的当前执行者。目标不是继续证明 `LogEntry`、sink 和最小 `logctl` 能跑，而是补完用户明确要求的 **打印信息整理闭环**：让 `.ai-temp/log-runs/20260610-013907/raw/scene-log.jsonl` 经过 `logctl analyze` 后，不需要 AI 直接读 raw JSONL，也能判断 gate 可信度、top noise、flow、缺字段和下一步修复任务。

## 必读

1. `AGENTS.md`
2. `DocsAI/README.md`
3. `DocsAI/ECS/README.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/README.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/source-request.md`
7. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/07-当前样本日志问题与整理方案.md`
8. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/029-SDD-0040-log-ai-first-observation-hard-cutover/README.md`
9. 同目录下 `tasks.md`、`bdd.md`、`progress.md`、`notes.md`
10. `DocsAI/ECS/Tools/Logger/README.md`

## 当前裁决

- 用户质疑成立：T1 完成的是结构化记录管道，不是完整的打印信息整理。
- 不新建 SDD-0041；继续使用 SDD-0040 的 T2.1~T2.7，避免 Log hard cutover 出现两个互相覆盖的事实源。
- SDD 当前 `blocked` 只表示最终 Godot scene smoke 没有有效承载游戏 runner；它不阻止 T2 analyzer/owner 整理在现有样本上推进。
- 当前样本 `.ai-temp/log-runs/20260610-013907` 是 T2 第一验收样本。
- `passed` 只能由 artifact 或 Validation channel 明确通过得出；只有 structured log 且没有 failure 时必须是 `no-failure-observed`，不能伪装成通过。
- raw JSONL 是证据源，不是默认提示词输入。默认入口必须是 `summary.md`、`ai-context.md`、`noise/top-contexts.md`、`missing-fields/index.md`、`flows/index.md`。

## 执行范围

优先完成 `tasks.md` 的 T2.1~T2.4：

1. T2.1：升级 `Workspace/Tools/logctl/logctl.mjs analyze`，生成 markdown digest。
2. T2.2：修正 gate status 语义，区分 `passed / failed / no-failure-observed / stdout-pattern-fallback / invalid-input`。
3. T2.3：修正 flow 边界，不再把普通 `operation` 当 flow；按 `channel=Flow`、显式 `entryType` 或完整 OperationTrace 契约聚合。
4. T2.4：实现 semantic missing-fields，检测 `fields:{}`、`operation==context`、缺 `durationMs/reasonCode/entityId/sourceFile/sourceLine`、unknown owner/phase。

T2.5~T2.6 在 analyzer digest 能暴露问题后再做：

- T2.5：只处理样本 top hot-spot：TargetSelector、ObjectPool、HealthBarUI、Damage、System。
- T2.6：Validation artifact adoption；没有 artifact 的 run 不能报告 `passed`。

## 第一批代码入口

先读后改：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
sed -n '1,360p' Workspace/Tools/logctl/logctl.mjs
rg -n "buildGateReport|aiContext|flowEntries|missing-fields|suggest|writeJsonl|analyze" Workspace/Tools/logctl/logctl.mjs
sed -n '1,320p' Src/ECS/Tools/Logger/Log.cs
sed -n '1,260p' Src/ECS/Tools/Logger/ValidationSession.cs
sed -n '1,260p' Src/ECS/Tools/TargetSelector/TargetQueryEngine.cs
sed -n '1,260p' Src/ECS/Tools/ObjectPool/Core/ObjectPool.cs
sed -n '1,240p' Src/ECS/UI/UI/HealthBarUI/HealthBarUI.cs
sed -n '1,260p' Src/ECS/Capabilities/Damage/System/DamageService.cs
```

不要先全仓清理所有 `_log.Info`。先让 analyzer 能把当前样本整理清楚，再按 digest 处理 owner hot-spot。

## T2.1 产物契约

`logctl analyze --run-dir .ai-temp/log-runs/20260610-013907 --out .ai-temp/log-runs/20260610-013907/analysis-next` 后必须至少生成：

```text
analysis-next/
  summary.md
  ai-context.md
  gate-report.json
  raw/
  by-owner/
  by-phase/
  flows/
    flows.json
    index.md
  failures/
    failures.json
    index.md
  noise/
    summary.json
    top-contexts.md
  missing-fields/
    missing-fields.json
    index.md
```

`summary.md` 第一屏必须回答：

- gate status、confidence、resultSource。
- entries、invalid JSONL、validationEntries、artifacts、structuredFailures。
- top owner、top operation、top phase、top noise。
- `fields:{}`、`operation==context`、unknown owner/phase、缺 source 是否存在。
- 下一步先读哪些文件，哪些 raw 不应该直接读。

`ai-context.md` 必须回答：

- 没有 artifact / Validation 时，只能说 `no-failure-observed`。
- top noisy owner/context/operation 和动作：`budget`、`sample`、`aggregate`、`owner field contract`、`move to Validation`。
- flow digest，只展示真正 flow summary 和异常 flow。
- semantic missing-fields digest。
- owner 文档链接和下一轮 query / profile override 建议。

## T2.2 Gate 语义

当前错误样本不能再出现：

```text
resultSource=structured-log
status=passed
validationEntries=0
artifacts=0
```

目标规则：

| status | 条件 |
| --- | --- |
| `passed` | artifact pass 或 Validation channel 明确 pass，且没有 structured failure |
| `failed` | artifact fail、Validation fail、structured error/fail |
| `no-failure-observed` | 有 structured log，但没有 Validation/artifact，也没有失败 |
| `stdout-pattern-fallback` | 只能靠 legacy stdout pattern |
| `invalid-input` | raw JSONL 截断或关键 artifact 解析失败，且严重到不能信任本 run |

当前样本至少应从 `passed` 改为 `no-failure-observed`，并在 summary/gate warning 中写出 1 条 invalid JSONL。

## T2.3 Flow 边界

禁止：

```js
const flowEntries = run.entries.filter((entry) => normalizeStatus(entry.channel) === "flow" || entry.operation);
```

目标：

- 普通 `operation` 只进 `by-owner`、`by-phase`、`noise`，不能自动进入 `flows`。
- `flows/index.md` 只纳入 `channel=Flow`、显式 `entryType=flow_*` 或完整 OperationTrace 契约。
- 按 `correlationId` / `flowId` / owner-context-operation 聚合 start、step、complete；无法聚合时标记为 `Log gap`。
- 高频成功 flow 以 summary / sample / aggregate 呈现，不让 AI 默认读全部 completion。

## T2.4 Semantic Missing Fields

`missing-fields` 不只检查 envelope required fields。它要输出 AI 为什么不能判断问题：

- `fields:{}`。
- `operation == context`。
- flow complete 缺 `durationMs`。
- warn/fail 缺 `reasonCode`。
- owner 相关日志缺 `entityId`、`targetId`、`poolName`、`processorCount` 等关键字段。
- 缺 `sourceFile/sourceLine`。
- unknown owner / phase。

`missing-fields/index.md` 必须按 owner/context/operation 分组，给出分类和任务，例如：

```text
## Runtime / HealthBarUI
- 问题：operation==context，fields 为空，无法按 entity 聚合 bind 过程。
- 样本：83 x OnUnitCreated / 83 x 准备绑定 / 83 x 成功绑定。
- 分类：Log gap / Owner field gap。
- 任务：补 HealthBarBind operation，fields={entityId, entityType, outcome, reasonCode}。
```

## 验证命令

每完成 T2.1~T2.4 一批后运行：

```bash
node --check Workspace/Tools/logctl/logctl.mjs
Workspace/Tools/logctl/logctl analyze --run-dir .ai-temp/log-runs/20260610-013907 --out .ai-temp/log-runs/20260610-013907/analysis-next
test -f .ai-temp/log-runs/20260610-013907/analysis-next/summary.md
test -f .ai-temp/log-runs/20260610-013907/analysis-next/ai-context.md
test -f .ai-temp/log-runs/20260610-013907/analysis-next/noise/top-contexts.md
test -f .ai-temp/log-runs/20260610-013907/analysis-next/missing-fields/index.md
test -f .ai-temp/log-runs/20260610-013907/analysis-next/flows/index.md
Workspace/Tools/logctl/logctl query --analysis-dir .ai-temp/log-runs/20260610-013907/analysis-next owner=TargetSelector operation=TargetQueryEntities --format md
Workspace/Tools/logctl/logctl suggest --run-dir .ai-temp/log-runs/20260610-013907 --dry-run
python3 Workspace/SDD/sdd.py validate SDD-0040
git diff --check -- SDD/project/projects/PRJ-0002-ecs-framework-refactor Workspace/Tools/logctl DocsAI/ECS/Tools/Logger
```

注意：`python3 Workspace/SDD/sdd.py validate PRJ-0002` 当前 CLI 不支持 project id 作为 validate target，不要把这个命令写入通过证据。

## SDD 同步要求

- 完成 T2 子任务时，同步更新 `tasks.md` checkbox、`progress.md` Latest Resume 和必要的 `bdd.md` / `notes.md`。
- 如果改 `DocsAI/ECS/Tools/Logger` 或 owner 文档，保持项目级 `design/Tool/10.Log` 与 SDD-0040 恢复点一致。
- 如果改 `.ai-config/skills/`，必须只改 `.ai-config` 源，随后运行 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` 和 `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`。
- 不把无 artifact / Validation 的 run 记录为通过；可以记录为 `no-failure-observed`。

## 完成定义

T2.1~T2.4 的完成定义：

- 当前样本生成完整 markdown digest。
- `gate-report.json` 不再误报 `passed`。
- `flows/index.md` 不再包含全部普通 operation。
- `missing-fields/index.md` 明确列出 `fields:{}`、`operation==context` 和第一批 owner 字段任务。
- `ai-context.md` 足以让下一轮 AI 从摘要和 query 入口继续，不需要直接读取 4915 行 raw JSONL。

T2 全部完成定义：

- T2.1~T2.7 全部 checkbox 完成。
- 当前样本和可用本地门禁通过。
- 最终 Godot scene smoke 如果仍无有效 runner，必须保留 blocker，不能把 SDD 标记为 done。
