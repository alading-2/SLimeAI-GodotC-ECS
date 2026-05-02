# Done

## 已完成阶段

- 01：建立 `Docs/` 与 `DocsAI/` 分层入口。
- 02：增强 Godot 场景测试 runner，支持过滤、输出文件、日志摘要和失败原因。
- 03：重构高频 Skill 为短入口，并建立 Skill 到 DocsAI 映射。
- 04：补齐 Entity / Component / Data / Event / SystemCore / AbilitySystem / DamageSystem / TestSystem / UI / Tools 模块契约。
- 05：新增长任务上下文协议，项目级计划统一放在根目录 `Plans/`。
- 06：根据原始代码修正试点方向，确认已有 `LifecycleComponent` 承担 MaxLifeTime 生命周期，不新增重复 `LifetimeComponent`。
- 07：新增 ECS 核心修改门禁和回归矩阵。

## 后续计划

- 文档与 Skill 按当前代码继续收敛：`Plans/Architecture/AI_First_Docs_Code_Alignment/README.md`

## 已知验证结果

- `node --check .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs` 通过。
- `godot-scene-runner list` 能列出测试场景。
- `list --filter Movement` 能筛选 Movement 场景。
- `MainTest --build --max-log-lines 80` 当前失败：`failed=true`、`failureReason=TimedOut`。关键日志为 `Cannot instantiate C# script ... PickupComponent.cs ... class could not be found`，并伴随大量 `ERROR: Condition "!is_inside_tree()" is true.`。该问题作为独立修复项保留。
