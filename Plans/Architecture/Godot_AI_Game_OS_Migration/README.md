# Godot AI Game OS Migration

> 日期：2026-05-04
> 状态：执行中
> 上游总方案：`Plans/Architecture/框架整体迁移/迁移.md`
> 多仓库计划：`Plans/Architecture/SkilmeAI_多仓库彻底迁移/README.md`

## 目标

把当前 `brotato-my` 从长期主项目降级为迁移输入仓库，并按 `Godot AI Game OS` 目标结构迁出 Runtime、Capabilities、Authoring、Validation、Observation、DocsAI 和 Skill。

本计划不做旧结构兼容。旧 `Src/ECS`、`Data/DataNew`、`.tres` 运行时入口和旧 Skill 只作为迁移输入，迁完后在新仓库内按新结构重新建立入口。

## 当前执行边界

本计划目录只做迁移控制面：

- 冻结旧仓库的定位和入口。
- 盘点旧资产与目标分流。
- 建立新工作区骨架。
- 建立可从聊天上下文丢失后恢复的计划状态。

实际 Runtime、Capability、DataOS、游戏项目和 Skill 迁移发生在新工作区：

```text
/home/slime/Code/SkilmeAI/SkilmeAI
/home/slime/Code/SkilmeAI/Games/BrotatoLike
```

不要在本目录内寻找 Runtime 源码改动；本目录只记录阶段、风险和恢复入口。

## 执行策略纠偏

- 后续不再按旧仓库 Skill 一个个迁移后停顿。
- 框架仓库已采用批量入口型 Skill：一次性建立 `.codex/skills`，每个 Skill 只指向新仓库 Contracts / ApiIndex / ProjectState / 源码位置。
- 框架仓库 `Agent/SkillsSource` 已作为 Skill 源头，`.codex/skills` 是当前可触发安装入口。
- 游戏仓库只保留游戏入口型 Skill：`project-index / game-development / gameos-reference / data-authoring / godot-scene-test`。
- 任务继续推进时，优先打开真实目标仓库执行，不在旧输入仓库重复盘点。

## 阶段

| 阶段 | 状态 | 输出 |
| --- | --- | --- |
| Phase 00：冻结旧世界 | 已完成 | 本计划、`Progress.md`、`Backlog.md`、`Done.md` |
| Phase 01：资产盘点 | 已完成 | `00_Inventory.md` |
| Phase 02：目标骨架 | 已完成 | `/home/slime/Code/SkilmeAI/` 工作区骨架 |
| Phase 03：迁移 Runtime 内核 | 已完成最小闭环 | `SkilmeAI/GameOS/Runtime` |
| Phase 04：迁移第一批 Capability | 已完成第一批 | Movement / Collision / Damage / Feature / Ability |
| Phase 05：迁移第二批 Capability | 部分完成 | AIBehavior / Projectile / Attack / Effect 已有闭环；Spawn 已在 BrotatoLike 接入；UIHud 未完成 |
| Phase 06：迁移 Authoring/DataOS | 已完成第一批并持续扩大 | SQLite schema、生成快照、数据校验、BrotatoLike seed / snapshot |
| Phase 07：External Research + Experience | 已有基础 | 外部资料协议、经验库、research skill |
| Phase 08：Validation + Observation | 已完成第一批 | Runtime tests、BrotatoLike Godot scene runner、日志摘要 |
| Phase 09：重写 DocsAI + Skill | 已完成入口型第一批 | 框架 `.codex/skills`、游戏 `.codex/skills` |
| Phase 10：删除旧结构 | 未开始 | 删除旧当前入口 |
| Phase 11：AI 生成 Demo 验证 | 未开始 | Brotato-like Demo 验证 |

## 验证命令

```bash
git status --short
find Plans/Architecture/Godot_AI_Game_OS_Migration -maxdepth 1 -type f | sort
find /home/slime/Code/SkilmeAI -maxdepth 3 -type d | sort
git -C /home/slime/Code/SkilmeAI/SkilmeAI status --short
git -C /home/slime/Code/SkilmeAI/Games/BrotatoLike status --short
rg -n "Godot_AI_Game_OS_Migration|SkilmeAI|迁移输入仓库" AGENTS.md DocsAI Docs Plans
```
