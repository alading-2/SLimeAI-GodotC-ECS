# SystemAgent Information Architecture Refresh

## Goal

先刷新 `Workspace/SystemAgent` 的信息架构，让正式事实源能表达最新分层模型：Workflow 负责编排，Capability/Skill 提供可复用能力，Role 提供执行视角，SDD/validation/report 作为 Artifact，Gate 只做阶段检查，Subagent 是可选执行基座。

本 SDD 的目标不是一次性重写所有流程，而是先回答“长期规则、能力正文、wrapper policy、subagent policy、catalog 和验证入口应该放在哪里”。后续 Hook P0、Workflow 分层、DesignDiscovery 和 Worktree/Subagent 安全策略都应基于这个目录结构继续实施。

## Context

当前 `Workspace/SystemAgent` 已经从旧嵌套目录迁移为唯一正文事实源根，包含 `Workflows/`、`Roles/`、`Protocols/`、`Gates/`、`Policies/`、`Catalog/`、`Config/`、`Tools/` 和 `Skills/`。但项目设计已经进一步明确：

- `Skills/` 现在只说明 wrapper skill policy，不应成为 `.ai-config/skills/` 源。
- `DesignDiscovery`、`SDDManagement`、`ValidationRelease` 这类能力需要作为可复用 capability 被 workflow 调用，也可单独触发。
- Subagent 不应成为新的顶层默认流程，也不应默认并行写代码；当前更适合作为只读搜索、独立评审和验证设计辅助能力。
- Hook/Gate 后续要依赖稳定的目录、catalog 和 SDD evidence 输入，不能继续靠临场解释事实源。

## Design

推荐先采用保守的信息架构刷新：

1. 保留 `README.md` 和 `INDEX.md` 作为最短入口，不复制 workflow、role、gate 或 policy 正文。
2. 保留 `Workflows/` 作为端到端流程编排目录，但要求入口第一屏表达 trigger、task size、SDD mode、called capabilities、roles、artifacts、gates 和 completion criteria。
3. 新增或明确 `Capabilities/` 作为 SystemAgent-owned capability 正文位置，用于 DesignDiscovery、SDDManagement、ValidationRelease 等可复用能力；`.ai-config/skills/*` 仍然只是工具入口或 wrapper 源。
4. 保留 `Roles/` 作为执行视角目录；是否新增 `DesignCritic` 留到后续 DesignDiscovery SDD 决定。
5. 保留 `Gates/` 作为阶段检查目录；Gate 不生成设计、不替代 SDD 记录，只检查 evidence 是否存在。
6. 在 `Policies/` 中补充或登记 Subagent / Git / Documentation 边界，明确 subagent 默认只读、写操作由主对话执行。
7. 同步 `Catalog/manifest.yaml`、目录级 `INDEX.md` 和文档治理策略，使机器索引与人工入口一致。

非目标：

- 不修改 hook 脚本或 hook 配置。
- 不修改 `.ai-config/skills/`，除非实施中发现目录刷新必须同步 wrapper 入口；如发生，需单独记录并运行 sync/lint。
- 不实现 tackle 式 agent dispatcher。
- 不移动 SDD 实例或项目级设计文档。

## Verification

完成本 SDD 时至少验证：

- `python3 Workspace/SDD/sdd.py validate --all`
- `git diff --check`
- 目标路径旧引用搜索和分类，确保没有把过期 SystemAgent 入口当作当前事实源。

如果修改 `.ai-config/skills/` 或规则源，还必须运行：

- `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`
