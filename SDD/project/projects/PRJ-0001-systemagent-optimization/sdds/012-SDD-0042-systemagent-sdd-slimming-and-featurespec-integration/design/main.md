# SDD-0042 SystemAgent SDD Slimming and FeatureSpec Integration

## 用户原始问题

> 就按你的顺序先生成SDD

用户接受前序深度分析给出的执行顺序：第一批先做 `SDD精简设计` 与 `FeatureSpec` 集成；`Worktree` 后续单独做；`TDD` 等 Log/Validation 证据链一起确定；`Hook` 只记录节点不启用。

## 真实问题

方向已经冻结，但当前实现仍存在“文档新、工具旧”的断层：

- SystemAgent 文档、规则和部分 skill 已写入 SDD 精简与 FeatureSpec 口径。
- SDD CLI 模板仍生成 `Latest Resume + Timeline + P001` 样板。
- SDD CLI 状态命令仍写入逐任务流水账。
- SDD validate 仍要求 `bdd.md` 必须出现 `Scenario:`，没有真正接受 FeatureSpec Source / Executed features 作为任务摘录入口。

因此本 SDD 的核心不是再讨论方向，而是把已冻结设计落到 CLI、模板、validate、测试、docs 和 skill 同步上。

## 解决思路

本 SDD 合并执行两份设计：

- `../../design/优化/SDD精简设计.md`
- `../../design/优化/FeatureSpec-功能实现规格设计.md`

执行方式以 `../../design/优化/SDD精简与FeatureSpec集成.FeatureSpec.md` 为实现规格源。长期功能目标写在 FeatureSpec，当前 SDD 只保存任务状态、执行范围和验证摘要。

## 非目标

- 不实现 `Worktree激活设计.md` 中的 worktree skill 或 SDD CLI worktree 参数。
- 不执行 `TDD-测试系统优化设计.md` 的 Data Godot/Validation 试点。
- 不启用 Hook，不新增自动提醒。
- 不批量重写历史 done SDD，只修模板、CLI 和 validate 默认行为。
- 不把 SDD validate 描述成业务验证通过。

## 执行阶段

1. 为当前旧行为补失败先行测试：模板、CLI 写入、BDD/FeatureSpec validate、项目子 SDD shared refs。
2. 修改 SDD 模板、progress 写入和 validate 规则。
3. 更新 SDD 文档、SystemAgent 文档/规则和 skill 源。
4. 运行 SDD CLI 单元测试、目标 SDD validate、全量 validate、ai-config sync 和 skill-test。

## 验证入口

具体验证命令写在 `tasks.md` 和 `notes.md`，完成时汇总到 `progress.md` 的 Validation 面板。
