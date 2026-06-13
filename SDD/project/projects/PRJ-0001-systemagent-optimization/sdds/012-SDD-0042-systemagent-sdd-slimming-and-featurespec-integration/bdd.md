# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 改变 SDD CLI、模板、validate 和 SystemAgent workflow/skill 口径。
- **Source**: `../../design/优化/SDD精简与FeatureSpec集成.FeatureSpec.md`
- **Executed features**: FS-1, FS-2, FS-3, FS-4, FS-5, FS-6

## Scenarios

### Scenario: 新 SDD 默认使用状态面板

Given 用户通过 SDD CLI 创建新的项目子 SDD
When CLI 写入 `progress.md`
Then `progress.md` 包含 State / Decisions / Validation
And 不包含 `Timeline` 或 `P001` 样板

### Scenario: task 命令不追加逐任务流水账

Given 一个 SDD 已有多个 task
When 用户执行 `python3 Workspace/SDD/sdd.py task <id> done T1.1`
Then `tasks.md` 和 metadata progress 更新
And `progress.md` 不新增 `task command` timeline entry

### Scenario: FeatureSpec Source 可作为 BDD 摘录入口

Given `bdd.md` 包含 `Source:` 指向 `.FeatureSpec.md`
And `bdd.md` 包含 `Executed features`
When 用户运行 `python3 Workspace/SDD/sdd.py validate <id>`
Then validate 不因缺少复制的 `Scenario:` 正文报错
