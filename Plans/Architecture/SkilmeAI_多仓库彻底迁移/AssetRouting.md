# 当前仓库资产分流清单

> 日期：2026-05-04
> 输入仓库：`/home/slime/Code/Godot/Games/MyGames/brotato-my`

## 分流目标

| 资产类型 | 当前路径 | 目标仓库 | 目标路径 |
| --- | --- | --- | --- |
| Runtime 内核 | `Src/ECS/Base/Entity`、`Component`、`Data`、`Event` | `SkilmeAI` | `GameOS/Runtime` |
| Runtime 工具 | `Src/ECS/Tools`、`Data/ResourceManagement` | `SkilmeAI` | `GameOS/Runtime`、`GameOS/GodotBridge` |
| Gameplay 能力 | `Src/ECS/Base/System/*`、业务 Component | `SkilmeAI` | `GameOS/Capabilities/<Name>` |
| AI 执行协议 | `DocsAI/Protocols`、`DocsAI/Workflows` | `SkilmeAI` | `DocsAI/Protocols`、`Agent/Protocols` |
| 测试基础设施 | `.codex/skills/GodotSkill`、`DocsAI/Tests` | `SkilmeAI` | `GameOS/Validation`、`Agent/SkillsSource` |
| 数据表和 schema 输入 | `Data/DataNew`、`Data/DataKey`、`Data/EventType` | `SkilmeAI` | `DataOS`、`GameOS/Authoring` |
| 游戏资产 | `assets` | `BrotatoLike` | `Assets` |
| 游戏入口 | `project.godot`、`Brotato_my.csproj`、`Src/Main` | `BrotatoLike` | 仓库根、`Src/Game` |
| Godot 插件 | `addons/DataConfigEditor` 等 | 待定 | DataOS 工具或游戏本地插件 |
| 历史计划和经验 | `Plans`、`DocsAI/Experience` | `SkilmeAI` 和当前仓库 | 当前仓库保留输入，新仓库保留可执行结论 |

## 不直接迁移为新入口

- 旧 `.tres` 运行时 Resource 数据入口。
- 旧 `Src/ECS` 路径名和命名空间。
- 旧 `.claude` 专属入口说明。
- 已完成历史计划的执行细节。

## 验收

```bash
rg -n "Src/ECS/Base|Data/DataNew|ResourceManagement|GodotSkill" Plans/Architecture/Godot_AI_Game_OS_Migration Plans/Architecture/SkilmeAI_多仓库彻底迁移
```

