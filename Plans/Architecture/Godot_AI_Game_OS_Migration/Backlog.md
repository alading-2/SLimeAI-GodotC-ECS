# Godot AI Game OS Migration Backlog

## P0

- 继续迁移剩余 Feature actions 和具体 Ability handler 执行逻辑；`target_point_skill` 和 `aura_shield` 已完成真实执行闭环。
- 从 `MigrationInput/` 适配真实 UI / 输入 / 游戏场景内容。
- 把真实主场景 / UI / SpawnSystem 后续 smoke 接入 `Games/BrotatoLike/Tools/run-godot-scene.sh`。
- 建立 `SkilmeAI.GameOS.Contracts.md` 和 `ApiIndex.md` 生成规则，减少手工维护。

## P1

- 建立 Capability 模板：`capability.json`、`Contract.md`、`Debug.md`、`Tests/`。
- 建立 `Tools/run-capability-test.sh` 包装命令。
- 推进 Godot PhysicsServer2D trace 和对象池碰撞问题 Observation。
- 将框架入口型 Skill 和 Capability Contract 继续收敛，避免重复读取旧仓库。

## P2

- DataOS 继续覆盖旧 DataNew 剩余字段和编辑器 authoring UI。
- 框架 package 从 project-reference 迁移到 local-nuget 流程。
- 删除旧结构前建立归档清单和最终验证门禁。
