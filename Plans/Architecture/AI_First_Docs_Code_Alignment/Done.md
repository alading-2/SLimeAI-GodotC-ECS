# Done

## 已完成项

- 新建 `Plans/Architecture/AI_First_Docs_Code_Alignment/`。
- 记录本轮目标、阶段计划、审计事实和验证清单。
- 新增 `DocsAI/Modules/FeatureSystem.md`。
- 新增 `DocsAI/Modules/DataAuthoring.md`。
- 更新 `DocsAI/INDEX.md`、`DocsAI/ProjectState.md`、`DocsAI/Skills/Skill到DocsAI映射.md`。
- 压缩 `damage-system`、`data-authoring`、`ecs-data`、`ecs-entity`、`ecs-event`、`feature-system`、`tools`、`ui-bind` 等 Skill。
- 补充 `ability-system` 对 FeatureSystem 契约的入口。
- 更新 `Docs/README.md` 与 `Docs/框架/项目索引.md`，指向本轮计划并移除旧 Skill 入口。
- 清理 `Src/**/*.md` 中本轮发现的旧 Windows 绝对链接。
- 第二批修正：
  - `.codex/skills/ability-system/references/ability-logic-parameters.md` 的旧 FeatureId / enum 读取示例。
  - `Src/ECS/Test/SingleTest/ECS/Data/README.md` 的旧测试路径和 Data 文档链接。
  - `Src/ECS/Base/System/TestSystem/README.md` 的旧 `.tres` 系统配置说明。
  - `Plans/Architecture/AI_First_Program_Migration/Progress.md` 的历史计划状态分叉。
- 第三批修正：
  - `Src/ECS/Test/SingleTest/ECS/ECSTest/README.md` 和 `Src/ECS/Test/SingleTest/Tools/ObjectPool/README.md` 的旧 `Src/Test` 路径。
  - `Src/ECS/Base/Entity/Core/EntityManager.md` 的 DataNew 配置来源和 `QueueFree` 边界说明。
  - `Src/ECS/Base/Component/Ability/CostComponent/README.md` 的旧 `AbilityExecutor` 扩展示例和 DataKey 写法。
  - `Src/ECS/Base/Data/README.md` 的旧 `GetTree().CreateTimer` 示例、普通 `const string` DataKey 示例和 DataNew 分工。
  - `Src/ECS/Base/Component/Component规范.md` 的 Spawn 时序。
- 第四批修正：
  - `Src/ECS/Tools/TargetSelector/README.md` 的旧 `AbilityTargetTeamFilter` 示例。
  - `Src/ECS/Tools/ObjectPool/ObjectPool.md` 的旧 AutoLoad 初始化、旧 `LoadScene` 示例、旧“静态归还仅支持 Node”语义和 `PoolNames` 说法。
  - `Src/ECS/Tools/Timer/TimerManager.md` 的可空 Timer 生命周期示例。
- 第五批修正：
  - `Src/ECS/Base/Component/Component规范.md` 的旧 `.tres` 配置来源。
  - `Src/ECS/Base/Component/Unit/Common/AttackComponent/AttackComponent.md` 的 AttackInterval / 追加冷却语义。
  - `Src/ECS/Base/Component/Collision/PickupComponent/PickupComponent.md` 的当前注释归档状态。
  - `DocsAI/Modules/Component.md` 的 PickupComponent 禁用契约。
  - `Src/ECS/UI/README.md` 的旧 `LoadScene<T>()`、旧 `Src/UI/Core` 路径、旧 AutoLoad 单例说明。
  - `DocsAI/Modules/UI.md` 的 UI 加载和 UIManager 注册契约。
  - `Src/ECS/Base/Event/README_EventBus.md` 的旧事件定义路径和事件名格式说明。

## 验证结果

- `git status --short` 已执行；工作区仍包含进入本轮前已有的 Godot 场景测试 Skill / DocsAI Tests / Docs 测试目录改动，本轮未回滚。
- `find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l`：总计 742 行。
- `find DocsAI -maxdepth 3 -type f | sort`：已包含 `DocsAI/Modules/DataAuthoring.md` 和 `DocsAI/Modules/FeatureSystem.md`。
- `rg -n "/mnt/e|file:///|复刻土豆兄弟|\\.windsurf" DocsAI .codex/skills Docs/框架/项目索引.md Src -g "*.md"`：无命中。
- `rg -n "DocsAI/Modules|DocsAI/Tests|DocsAI/Workflows" .codex/skills DocsAI`：有预期入口和映射命中。
- `dotnet build`：通过，0 Warning，0 Error。
- 第二批验证：
  - `rg -n "Ability[_]Movement|Src/Test/ECS/Data|Data/Config/System/System/Resource/TestSystem[.]tres|[.][.]/[.][.]/[.][.]/ECS/Data" .codex Src DocsAI Docs -g "*.md"`：无命中。
  - `rg -n "/mnt/e|file:///|复刻土豆兄弟|\\.windsurf" DocsAI .codex/skills Docs/框架/项目索引.md Src -g "*.md"`：无命中。
  - `dotnet build`：通过，0 Warning，0 Error。
- 第三批验证：
  - `rg -n "AbilityExecutor|IAbilityExecutor|Src/Test|GetTree\\(\\)\\.CreateTimer|public const string ManaRegen|public const string MaxMana|public const string EffectiveMana|通常来自 \\.tres|Data/Config/System/System/Resource/TestSystem\\.tres" Src/ECS -g "*.md"`：仅剩 3 处禁止性说明（`不要通过旧 AbilityExecutor` / `不要新增 IAbilityExecutor` / `不要恢复 AbilityExecutorRegistry`）。
  - `rg -n "/mnt/e|file:///|复刻土豆兄弟|\\.windsurf" DocsAI .codex/skills Docs/框架/项目索引.md Src -g "*.md"`：无命中。
  - `find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l`：总计 742 行。
  - `find DocsAI -maxdepth 3 -type f | sort`：已包含 `DocsAI/Modules/DataAuthoring.md` 和 `DocsAI/Modules/FeatureSystem.md`。
  - `rg -n "DocsAI/Modules|DocsAI/Tests|DocsAI/Workflows" .codex/skills DocsAI`：有预期入口和映射命中。
  - `dotnet build`：通过，0 Warning，0 Error。
- 第四批验证：
  - `rg -n "AbilityTargetTeamFilter|LoadScene|\\bPoolNames\\b|AutoLoad|_EnterTree\\(\\).*ObjectPoolInit|_Ready\\(\\).*ObjectPoolInit|仅支持 Godot Node|不支持静态归还|GetTree\\(\\)\\.CreateTimer|new Timer\\(" Src/ECS/Tools -g "*.md"`：无命中。
  - `dotnet build`：通过，0 Warning，0 Error。
- 第五批验证待执行：
  - `rg -n "LoadScene|Src/UI/Core|AutoLoad 单例|Unit\\.Created|Spawn Config \\(\\.tres\\)|外部系统（如 AI 的 CD 控制|所有频率控制 100%|WindowUp|被回收/QueueFree|PickupComponent\\` 添加|AbilityTargetTeamFilter|GetTree\\(\\)\\.CreateTimer|new Timer\\(|GD\\.Load|ResourceLoader\\.Load|GetNodesInGroup|Data\\.On\\(|public const string" Src/ECS/Base/Component Src/ECS/AI Src/ECS/UI -g "*.md"`：无命中。
  - `rg -n "LoadScene|Src/UI/Core|Src/ECS/Base/Event/Type|小写下划线|AutoLoad 单例|Unit\\.Created|Spawn Config \\(\\.tres\\)|外部系统（如 AI 的 CD 控制|所有频率控制 100%|WindowUp|被回收/QueueFree|PickupComponent\\` 添加|AbilityTargetTeamFilter|GetTree\\(\\)\\.CreateTimer|new Timer\\(|GD\\.Load|ResourceLoader\\.Load|GetNodesInGroup|Data\\.On\\(|public const string" Src -g "*.md"`：仅剩禁止性 `GetNodesInGroup`、事件名 `public const string` 和特殊引用键示例。
  - `rg -n "/mnt/e|file:///|复刻土豆兄弟|\\.windsurf" DocsAI .codex/skills Docs/框架/项目索引.md Src -g "*.md"`：无命中。
  - `dotnet build`：通过，0 Warning，0 Error。
