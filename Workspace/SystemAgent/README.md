# SystemAgent

`Workspace/SystemAgent/` 是 SlimeAI 工作区 AI 执行的唯一正文事实源。

## 入口

README → `Docs/README.md`（系统说明）→ 选 Route → 按 Route phase 读 Actors / Rules → 用 Tools 验

> 当前建议把 SystemAgent 理解成 **control plane**：负责 workflow / actor / gate / retrospective。
> `DocsAI/ECS/Tools/Logger/` 中的 Log / Validation / Test 属于 **evidence plane**：负责给 Debug、Review、Verify 提供可复查证据。

| 用户意图 | Route |
| --- | --- |
| 新功能、重构、迁移、SDD 实施 | NewFeature |
| bug、测试失败、异常定位 | DebugFix |
| 改进 SystemAgent 流程/角色/规则/gate | WorkflowIteration |
| 修改 skill/rule/hook/subagent/sync | ConfigMaintenance |
| 研究外部资料、参考框架 | ResearchAdoption |
| 大改后验证、归档前检查 | ValidationRelease |

## 目录

| 目录 | 职责 |
| --- | --- |
| Docs/ | SystemAgent 说明文档入口（理念、架构、Workflow、Actor、Session Adapter、SDD、TDD、Worktree、完成度分析、Debug 证据链） |
| Routes/ | 6 个执行路由 |
| Actors/ | 15 个角色定义（含 DeepThink / DesignCritic / Senior reviewer） |
| Rules/ | 行为约束：ReviewGates、VerdictVocabulary、Git、Subagent、AIConfig、Boundary、TDD、Philosophy、Documentation、DesignDocument |
| Tools/ | skill-test lint、hook smoke、BDD 场景格式、session-adapter 会话整理 |
| Registry/ | 机器索引（manifest、catalog）+ 运行配置 |

## 边界

- 正文在 `Workspace/SystemAgent/`；`.ai-config/` 只保存 skill 源
- `.ai-config/skills/systemagent-skill/` 保存可单独触发的 SystemAgent capability skill，例如 `systemagent-deepthink` 和 `systemagent-design-document`；`.ai-config/skills/systemagent-workflow/` 保存 workflow entry skill。
- Skill/rule/command 只改 `.ai-config/`，改后运行 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- Hook/subagent 配置直接维护 `.claude/.codex`，不走 `.ai-config` 同步
- SDD 任务实例在 `SDD/`；`Workspace/SDD/` 只保存 CLI 和模板
