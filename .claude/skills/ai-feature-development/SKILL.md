---
name: ai-feature-development
description: SlimeAI AI-first 框架任务兼容入口。用于新功能、重构、迁移、SDD task、DataOS/GodotBridge/游戏切片接入，以及用户要求从设计生成执行文档、FeatureSpec、实现草案或代码落点说明时；先路由到 Workspace/SystemAgent/，再选择 owner capability skill。
---

# AI Feature Development

## 定位

本 skill 是兼容入口，不是 SystemAgent workflow 正文。SystemAgent 正文事实源在 `Workspace/SystemAgent/`；owner capability skill 仍按任务影响面在各自分类中读取。

## 入口顺序

1. 读取 `Workspace/SystemAgent/README.md`、`Workspace/SystemAgent/README.md` 和 `Workspace/SystemAgent/Registry/workflow-catalog.yaml`。
2. 选择 primary workflow，并读取对应 `Workspace/SystemAgent/Routes/<Workflow>.md`。
3. 汇报 selected workflow，以及 `workflow-catalog.yaml` 中 must_read 清单的“已读 / 未读”状态；未读项必须说明原因。
4. 大型功能、架构变更、跨模块重构、长期设计决策、迁移账本或跨目录文档治理必须进入 SDD；执行 active SDD 前读取 `Workspace/SDD/README.md` 与 `Workspace/SDD/docs/SDDFormat.md`，并用 `python3 Workspace/SDD/sdd.py show <sdd-id>` 确认上下文。若当前任务预计跨多个 task、多个文件、多个 git boundary，必须维护当前 SDD 的 `tasks.md` 和 `progress.md` 状态面板。
5. 若任务涉及功能实现或行为验收，优先读取或编写设计旁 `.FeatureSpec.md`；用户说“执行文档 / 执行规格 / 实现草案 / 代码怎么改 / 看不懂怎么落地”时，也按 FeatureSpec 处理。FeatureSpec 文件名可以按执行主题命名，不要求和设计文档同名；BDD 只作为 FeatureSpec 的行为场景格式，按 `Workspace/SystemAgent/Tools/BDDSceneFormat.md` 检查验收标准。
6. 完成 SystemAgent route 后，再选择 owner skill：ECS Runtime / Capability 使用对应 `ecs-*` 或 `*-system`，DataOS 使用 `data-authoring`，GodotBridge adapter 使用 `ecs-component`，测试/场景使用 `test-system` 或 `godot-scene-test`，AI 配置使用 `ai-config-management`。
7. 功能实现改变 owner skill 的路由、源码位置、验证命令或收尾门禁时，必须更新对应 `.ai-config/skills/<category>/<owner>/SKILL.md` 源，并运行 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` 与 skill-test lint；禁止只更新同步副本。

## 功能收尾

收尾阶段委托 `ai-process-retrospective` 或 `systemagent-retrospective`，并读取 `Workspace/SystemAgent/Actors/Retrospective.md` 与 `Workspace/SystemAgent/Routes/WorkflowIteration.md`。若任务使用 SDD，收尾前先补齐 `progress.md` 的 State / Next / Blocker / Validation summary。不要在本兼容入口内复制 retrospective 正文。

## 验证

进入验证前解析 review-mode：命令参数 `--review {mode}` → `Workspace/SystemAgent/Registry/review-mode.txt` → 默认 `lean`；合法值为 `full`、`lean`、`solo`。

按影响面运行最小完整验证：文档/skill change 至少运行 SDD validate/文件清单/sync/lint；代码 change 追加框架 build/tests；Godot change 追加游戏 build/scene/analyzer。SDD 长任务还要检查 `progress.md` 是否与 `tasks.md` 同步。

新功能涉及 GodotBridge、Godot Node 生命周期、Physics、Input、Resource、UI、动画或游戏侧 glue 时，必须新增或更新独立 Godot 验证场景，并检查 README 五字段、`index.json`、`result.json`、scene artifact 和 ValidationCatalog；`run-main-smoke` 只能作为回归补充。

## 禁止

- 不把 `Workspace/DocsAI/AgentWorkflow/` 当作当前 SystemAgent 事实源。
- 不复制 `Workspace/SystemAgent/Routes/`、`Roles/`、`Gates/` 或 `Policies/` 正文。
- 不直接修改同步副本作为源；skill/rule/command 只改 `.ai-config/` 后运行 sync。
- 不在 SDD 长任务里把改变方向、阻塞或最终验证结论只留在聊天上下文，而不写入 `progress.md`。
