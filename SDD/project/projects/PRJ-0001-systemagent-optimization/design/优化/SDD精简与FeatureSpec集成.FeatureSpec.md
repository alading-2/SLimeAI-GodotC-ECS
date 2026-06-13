# SDD 精简与 FeatureSpec 集成 FeatureSpec

## Source Design

- `SDD精简设计.md`
- `FeatureSpec-功能实现规格设计.md`
- `Code-Review优化设计.md`

## Feature List

| ID | Feature | Priority | Status | Notes |
| --- | --- | --- | --- | --- |
| FS-1 | SDD progress 默认状态面板 | P0 | planned | 模板和 CLI 写入都停止生成逐任务 timeline |
| FS-2 | SDD CLI 状态写入精简 | P0 | planned | `start/block/done/note/task` 不再追加样板流水账 |
| FS-3 | FeatureSpec / BDD validate 支持 | P0 | planned | `bdd.md` 可用 FeatureSpec Source、Executed features 或 not-required reason 通过 |
| FS-4 | 项目子 SDD 设计引用轻量化 | P0 | planned | 项目子 SDD 默认引用 project/design，不因未复制完整设计而被误报 |
| FS-5 | SystemAgent 文档和 skill 同步 | P1 | planned | SDD、TDD、TestDesigner、Code Review、workflow skill 口径一致 |
| FS-6 | 验证证据闭环 | P1 | planned | 单元测试、目标 SDD validate、全量 validate、skill sync/lint 可复查 |

## FS-1: SDD progress 默认状态面板

### Goal

新建 SDD 和手动模板默认生成薄状态面板，而不是 `Latest Resume + Timeline + P001` 的旧恢复日志。

### Behavior

Given 用户创建新的 SDD  
When CLI 写入 `progress.md`  
Then 文件默认包含 `State / Decisions / Validation`，不包含 `Timeline` 和 `P001` 样板。

### Implementation Guidance

- Owner: `Workspace/SDD`
- Key files / areas: `Workspace/SDD/Src/templates.py`, `Workspace/SDD/templates/`
- Public API: `python3 Workspace/SDD/sdd.py new`
- Constraints: 保留 AI 可恢复的 current / next / blocker 信息。
- Forbidden: 不把完整命令输出、文件清单或 task done 流水账写进 progress。

### TDD Handoff

- expectedInputs: 新 SDD 标题、项目 ID、scope、tags。
- expectedObservations: 生成的 `progress.md` 有 State 面板，无 `### P001`。
- passCriteria: 单元测试断言新模板结构；目标 SDD validate 无 error。
- failCriteria: 新 progress 仍包含 `Timeline`、`Context / Conclusion / Evidence / Impact / Resume` 样板。
- artifactPath: `Workspace/SDD/tests/test_sdd_cli.py` 测试输出。

### Review Checklist

- `README.md` 仍能显示当前任务和下一步。
- `progress.md` 不再重复 `tasks.md` 可推导的信息。

## FS-2: SDD CLI 状态写入精简

### Goal

CLI 状态命令只更新 metadata、README、tasks header 和状态面板，不追加每步流水账。

### Behavior

Given 已存在 SDD  
When 执行 `start`、`task done`、`note`、`block` 或 `done`  
Then CLI 更新当前状态和必要裁决/验证摘要，不自动追加 task command timeline。

### Implementation Guidance

- Owner: `Workspace/SDD`
- Key files / areas: `Workspace/SDD/Src/commands.py`, `Workspace/SDD/Src/progress.py`, `Workspace/SDD/Src/instance_ops.py`
- Public API: `start`, `block`, `done`, `note`, `task`
- Constraints: `done` 必须保留最终 validation summary；`block` 必须保留 blocker。
- Forbidden: 不保留旧 timeline fallback 作为默认写入路径。

### TDD Handoff

- expectedInputs: 临时 SDD、若干 task checkbox、validation summary。
- expectedObservations: 执行命令后 `progress.md` 状态更新但无新增 `### Pxxx`。
- passCriteria: 单元测试覆盖 task / done / block / note 关键路径。
- failCriteria: 任一命令继续写入 `task command` 或 `继续处理下一个未完成任务` 样板。
- artifactPath: `Workspace/SDD/tests/test_sdd_cli.py`。

### Review Checklist

- CLI 输出仍足够定位 SDD 路径和结果。
- `metadata.progress` 与 `tasks.md` 仍一致。

## FS-3: FeatureSpec / BDD validate 支持

### Goal

SDD validate 接受 FeatureSpec 作为长期功能事实源，`bdd.md` 只做任务摘录或引用入口。

### Behavior

Given `bdd.md` 标记 required=true  
When 文件包含 `Source:` 指向 `.FeatureSpec.md`，并列出 `Executed features` 或 `Executed scenarios`  
Then validate 通过，不要求复制 `Scenario:` 正文。

### Implementation Guidance

- Owner: `Workspace/SDD`
- Key files / areas: `Workspace/SDD/Src/validation.py`, `Workspace/SDD/docs/ValidationRules.md`
- Public API: `python3 Workspace/SDD/sdd.py validate`
- Constraints: required=false 仍必须有 Reason。
- Forbidden: 不用“写更多 BDD 正文”作为通过条件。

### TDD Handoff

- expectedInputs: 三类 bdd.md：FeatureSpec Source、Executed features、not required。
- expectedObservations: 三类都按规则返回 error/warn。
- passCriteria: required=true + Source/Executed features 无 error；空壳 required=true 仍 error。
- failCriteria: 只有缺 `Scenario:` 就报错。
- artifactPath: `Workspace/SDD/tests/test_sdd_cli.py`。

### Review Checklist

- 规则不会放过空壳 BDD。
- 规则不会鼓励复制完整 FeatureSpec。

## FS-4: 项目子 SDD 设计引用轻量化

### Goal

项目子 SDD 可以引用项目级共享设计和 FeatureSpec，不再复制完整设计快照。

### Behavior

Given 项目子 SDD 的 `design/INDEX.md` 或 `sdd.json.shared_design_refs` 指向 project/design  
When 本地 `design/main.md` 只写任务差异  
Then validate 不应把它误判为 thin design 或 external refs 错误。

### Implementation Guidance

- Owner: `Workspace/SDD`
- Key files / areas: `Workspace/SDD/Src/validation.py`, `Workspace/SDD/docs/SDDFormat.md`
- Constraints: 独立 SDD 仍需要可恢复设计入口。
- Forbidden: 不恢复“复制项目设计到子 SDD”的默认流程。

### TDD Handoff

- expectedInputs: 项目子 SDD、独立 SDD、shared_design_refs。
- expectedObservations: 项目子 SDD 引用有效时通过；独立空壳仍警告或错误。
- passCriteria: validate 区分 shared refs 和真正空壳。
- failCriteria: 项目子 SDD 只因引用 project/design 就报 SDD025。
- artifactPath: `Workspace/SDD/tests/test_sdd_cli.py`。

### Review Checklist

- 共享设计引用路径真实存在。
- 本 SDD 的 `design/main.md` 至少说明局部目标和不做什么。

## FS-5: SystemAgent 文档和 skill 同步

### Goal

SystemAgent、SDD docs、`.ai-config` skill 和同步副本都使用同一套 SDD / FeatureSpec 口径。

### Behavior

Given AI 准备执行 SDD 或功能实现  
When 读取 SDD workflow、TDD/TestDesigner、Code Review 或 DesignDocument skill  
Then 它们都把 FeatureSpec 作为设计冻结后的实现规格，并把 SDD 定位为轻量任务状态容器。

### Implementation Guidance

- Owner: `Workspace/SystemAgent`, `.ai-config`
- Key files / areas: `Workspace/SystemAgent/Docs`, `Workspace/SystemAgent/Rules`, `.ai-config/skills/sdd`, `.ai-config/skills/systemagent-skill`
- Constraints: 改 `.ai-config` 后必须运行 sync 和 skill-test。
- Forbidden: 不直接改同步副本作为源。

### TDD Handoff

- expectedInputs: 修改后的 skill/rule/docs。
- expectedObservations: sync 后 `.codex/skills` 副本一致；skill-test 无 critical。
- passCriteria: `sync-ai-config.sh` 成功；skill-test Critical 0。
- failCriteria: 源和副本漂移，或 skill 仍要求旧 BDD / timeline。
- artifactPath: skill-test summary 输出。

### Review Checklist

- Code Review 仍以功能实现度优先，不退化为测试 gate。
- TDD 仍消费 FeatureSpec，不临场发明标准答案。

## FS-6: 验证证据闭环

### Goal

本 SDD 完成时有可复查的 CLI、单元测试、同步和 lint 证据。

### Behavior

Given SDD-0042 声称完成  
When Reviewer 检查验证摘要  
Then 能看到命令、结果、影响范围和任何剩余 warning 的解释。

### Implementation Guidance

- Owner: `Workspace/SDD`, `Workspace/SystemAgent`
- Key files / areas: tests, docs, skills
- Constraints: `validate --all` 只证明 SDD artifact，不证明业务代码行为。
- Forbidden: 不用 “ok / passed” 作为完整验证摘要。

### TDD Handoff

- expectedInputs: 本轮改动后的仓库。
- expectedObservations: 目标测试和校验命令输出可复查。
- passCriteria: 无 error；warning 有解释；skill sync/lint 结果明确。
- failCriteria: 未运行目标测试，或验证摘要缺命令和结果。
- artifactPath: SDD-0042 `progress.md` Validation 摘要。

### Review Checklist

- 最终汇报区分本轮改动和既有 dirty workspace。
- 不把 SDD validate 当成业务实现正确性证明。
