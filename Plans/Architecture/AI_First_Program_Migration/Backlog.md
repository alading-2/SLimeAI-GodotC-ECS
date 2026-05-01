# Backlog

## AI-First 迁移后续任务

- 逐步迁移 `Src/**/*.md` 中的旧绝对路径和历史 API 说明。
- 将 `ecs-entity`、`ecs-data`、`ecs-event`、`damage-system`、`ui-bind`、`tools` 继续压缩为更短 Skill。
- 为 `LifecycleComponent + MaxLifeTime` 补最小自动化场景测试。
- 修复 `MainTest` 中 `PickupComponent.cs` C# script instantiate 失败。
- 建立外部成熟 ECS / Godot 框架本地 clone 研究协议，避免只看搜索摘要。
- 为 DataConfigEditor 和 DataNew 建立 AI 填表流程。
- 为资源生成工作流建立单独 DocsAI / Skill，不混入当前程序闭环。

## 暂不做

- 不删除旧 `Src/**/*.md`。
- 不一次性重构 ECS 核心。
- 不在 DocsAI 下新建第二套 Plans。
- 不把美术、策划、资源生成混进当前程序开发闭环。
