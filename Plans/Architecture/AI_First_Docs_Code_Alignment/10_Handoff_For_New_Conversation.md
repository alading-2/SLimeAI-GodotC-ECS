# 10 Handoff For New Conversation

## 当前结论

本计划尚未彻底完成，但已经完成 AI 可执行入口和明显过期内容的主体校准。

当前可交接状态：

- `.codex/skills/*` 已压缩为短入口，当前总计 742 行。
- `DocsAI/Modules/FeatureSystem.md` 与 `DocsAI/Modules/DataAuthoring.md` 已新增。
- `DocsAI/INDEX.md`、`DocsAI/ProjectState.md`、Skill 映射已指向新计划和新增模块。
- `Src/**/*.md` 已按模块族清理多批会直接误导 AI 写错代码的旧路径、旧 API、旧数据源和旧生命周期说明。
- `dotnet build` 最近通过，0 Warning，0 Error。
- 旧机器路径扫描最近无命中：`/mnt/e`、`file:///`、`复刻土豆兄弟`、`.windsurf`。

## 已完成到哪里

已完成计划主体：

- 01 审计当前 DocsAI / Skill / Src 文档。
- 02 更新 DocsAI 模块契约，新增 FeatureSystem / DataAuthoring。
- 03 压缩 Skill 为 AI 短入口。
- 04 开始治理 Src 源码旁文档。
- 05 做第一轮验证和交接。

后续追加批次：

- 06 第二批代码对齐：Ability 参考示例、Data 测试 README、TestSystem README、旧迁移计划状态。
- 07 第三批：CostComponent、Data README、Component 规范、EntityManager、测试 README。
- 08 第四批：Tools 族 ObjectPool、TargetSelector、TimerManager。
- 09 第五批：Component / Attack / Collision / UI / EventBus。

## 明确未完成

- `Src/**/*.md` 还没有做完整迁移，只清理了已确认会误导 AI 的内容。
- Movement 长设计文档、AI 行为树细节、Collision 设计总览还需要后续深层审计。
- `Docs/` 中对应人类设计文档还没有系统归档 / 拆分 / 迁移。
- 没有修运行时代码。
- 历史 `MainTest` 失败不是本轮范围，需要独立 Debug。
- 没有恢复或处理 `.claude/skills`、`.opencode/skills`、`.windsurf/skills` 的删除状态。

## 新对话推荐入口

新对话先读：

1. `Plans/Architecture/AI_First_Docs_Code_Alignment/README.md`
2. `Plans/Architecture/AI_First_Docs_Code_Alignment/Progress.md`
3. `Plans/Architecture/AI_First_Docs_Code_Alignment/Done.md`
4. `Plans/Architecture/AI_First_Docs_Code_Alignment/Backlog.md`
5. `Plans/Architecture/AI_First_Docs_Code_Alignment/10_Handoff_For_New_Conversation.md`
6. `DocsAI/ProjectState.md`

## 新对话建议继续做

优先顺序：

1. 深审 `Src/ECS/Base/System/Movement/**/*.md`，判断哪些长设计内容迁入 `Docs/`，哪些执行规则下沉到 `DocsAI/Modules/Component.md` 或新增 Movement 契约。
2. 深审 `Src/ECS/AI/README.md` 与 AI 行为树源码，确认 AI 节点是否需要独立 `DocsAI/Modules/AI.md` 或 Skill。
3. 深审 Collision 文档族和 `Docs/框架/ECS/Collision/*`，统一 CollisionComponent / Hurtbox / MovementCollision / ContactDamage 分工。
4. 再做 `Docs/` 人类文档的归档和索引整理，不要一次性删除 `Src/**/*.md`。

## 最近验证命令

```bash
rg -n "/mnt/e|file:///|复刻土豆兄弟|\\.windsurf" DocsAI .codex/skills Docs/框架/项目索引.md Src -g "*.md"
rg -n "LoadScene|Src/UI/Core|Src/ECS/Base/Event/Type|小写下划线|AutoLoad 单例|Unit\\.Created|Spawn Config \\(\\.tres\\)|外部系统（如 AI 的 CD 控制|所有频率控制 100%|WindowUp|被回收/QueueFree|PickupComponent\\` 添加|AbilityTargetTeamFilter|GetTree\\(\\)\\.CreateTimer|new Timer\\(|GD\\.Load|ResourceLoader\\.Load|GetNodesInGroup|Data\\.On\\(|public const string" Src -g "*.md"
find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l
dotnet build
```

第二条宽扫描当前只剩预期项：

- `GetNodesInGroup` 出现在禁止性说明中。
- `public const string` 出现在事件名示例或特殊引用键示例中。

## 工作区注意事项

- 工作区有大量进入本轮前已有改动。
- 不要回滚 `.claude/skills`、`.opencode/skills`、`.windsurf/skills` 删除状态，除非用户明确要求。
- 不要覆盖 Godot 场景测试 Skill、DocsAI Tests、Docs README、项目索引等已有改动；继续修改时要合并。
