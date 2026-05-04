# Godot AI Game OS Migration

> 日期：2026-05-04
> 状态：执行中
> 上游总方案：`Plans/Architecture/框架整体迁移/迁移.md`
> 多仓库计划：`Plans/Architecture/SkilmeAI_多仓库彻底迁移/README.md`

## 目标

把当前 `brotato-my` 从长期主项目降级为迁移输入仓库，并按 `Godot AI Game OS` 目标结构迁出 Runtime、Capabilities、Authoring、Validation、Observation、DocsAI 和 Skill。

本计划不做旧结构兼容。旧 `Src/ECS`、`Data/DataNew`、`.tres` 运行时入口和旧 Skill 只作为迁移输入，迁完后在新仓库内按新结构重新建立入口。

## 当前执行边界

本阶段只做迁移控制面：

- 冻结旧仓库的定位和入口。
- 盘点旧资产与目标分流。
- 建立新工作区骨架。
- 建立可从聊天上下文丢失后恢复的计划状态。

暂不迁移 C# Runtime，不改 Godot 场景，不删除旧文件。

## 阶段

| 阶段 | 状态 | 输出 |
| --- | --- | --- |
| Phase 00：冻结旧世界 | 进行中 | 本计划、`Progress.md`、`Backlog.md`、`Done.md` |
| Phase 01：资产盘点 | 进行中 | `00_Inventory.md` |
| Phase 02：目标骨架 | 进行中 | `/home/slime/Code/SkilmeAI/` 工作区骨架 |
| Phase 03：迁移 Runtime 内核 | 未开始 | `SkilmeAI/GameOS/Runtime` |
| Phase 04：迁移第一批 Capability | 未开始 | Movement / Collision / Damage / Feature / Ability |
| Phase 05：迁移第二批 Capability | 未开始 | AIBehavior / Projectile / Spawn / UIHud / VisualEffect / TestSystem |
| Phase 06：迁移 Authoring/DataOS | 未开始 | SQLite schema、生成快照、数据校验 |
| Phase 07：External Research + Experience | 已有基础 | 外部资料协议、经验库、research skill |
| Phase 08：Validation + Observation | 未开始 | 统一测试包装、日志摘要、dump 工具 |
| Phase 09：重写 DocsAI + Skill | 未开始 | 新结构入口和短 Skill |
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

