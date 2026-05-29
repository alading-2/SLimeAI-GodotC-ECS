# 项目规则 - Godot 4.6 C# (.NET 8.0)

> 详细架构文档: [项目索引](Docs/框架/项目索引.md)
> 详细操作规范已迁移到 Skills（`@skill名称` 或描述任务时自动触发）
> 当前纠偏入口: `/home/slime/Code/SlimeAI/Workspace/DocsAI/SlimeAINewReorientation/00-README.md`
> 当前 OpenSpec change: `/home/slime/Code/SlimeAI/openspec/changes/reorient-slimeainew-ecs-framework/`

## 当前仓库定位

- 当前仓库是旧 Godot C# ECS 框架主线，包含 `Src/ECS/`、旧数据、旧游戏工程、DocsAI 和本地 skill 入口。
- 旧“作为 `SkilmeAI` / Godot AI Game OS 迁移输入仓库”的说法保留为历史方向，不再作为默认任务入口。
- `SlimeAI/` 新 GameOS 现在是冻结参考实现；可以借鉴 typed DataKey、typed event、Observation、scene artifact gate、DataOS 分层和 SystemAgent gates，但不把新增 Runtime、Capability、DataOS 工作默认迁出到 `SlimeAI/GameOS`。
- 大型 ECS 改造前先读工作区纠偏文档和 OpenSpec change，再从 `DocsAI/INDEX.md` 进入本仓模块契约与验证文档。
- 本 change 只改路由和文档；`Src/ECS` 核心代码不在本次修改范围。

## 0.游戏开发
- 以资深游戏开发者结合现代游戏设计框架深度思考

## 1. 交互规则
- 必须使用中文回复
- 避免删除再创建文件，尽量修改文件
- 禁止使用PowerShell命令
- 新增加或修改的代码要增加适当注释，如果是传递参数需要在后面加//注释
- 新增功能后必须更新 [Docs/框架/项目索引.md](../../Docs/框架/项目索引.md)
- 修改框架相关实现/接口/流程后，必须同步更新对应 Skill 文档，禁止 Skill 与代码脱节
- 触碰 `Src/ECS/`、`DocsAI/Modules/`、`DocsAI/Tests/` 或本仓 skill 前，先从本仓运行 `git status --short`，确认 nested git 边界。
- 不把 `SlimeAI/GameOS` 作为默认代码落点；只有用户明确要求或新的 OpenSpec change 授权时才修改新 GameOS。

## 2. C# 代码硬约束
- **概率值**: 统一 0-100（计算时 /100）
- **注释**: 统一使用 `< >` 而非转义字符 `&lt;` `&gt;`
- **性能红线**: `_Process` 中禁止 `new` 对象和 LINQ
- **"不限制"语义**: 数值型参数表示"无限制"时统一用 -1（如 MoveMaxDuration / MoveMaxDistance），不用 0
- **角度输入语义**: 对外输入参数一律使用“度”（如 Angle / Orbit* / WavePhase / Rotation）；Godot 2D 约定统一按 `0=右、90=下、180=左、正值顺时针`；仅在内部三角函数或旋转计算时转为弧度；Node2D 旋转优先使用 `RotationDegrees/GlobalRotationDegrees`

## 3. 架构红线（绝对禁止）

**Entity 生命周期**
- ❌ 直接 `new Entity()` → 必须 `EntityManager.Spawn/Register`
- ❌ 直接 `entity.QueueFree()` → 必须 `EntityManager.Destroy`

**数据存储**
- ❌ Component 私有业务状态字段（`_currentHp`、`_moveSpeed`）→ 存 `Data`
- ❌ `Data.On()` 监听数据变化 → 用 `Entity.Events`
- ❌ 字符串字面量访问 Data（`"CurrentHp"`）→ 用 descriptor 生成的 typed `DataKey<T>`
- ❌ 新增 `const string` / `DataMeta` DataKey → 先写 DataOS descriptor，再生成 typed handle

**通信**
- ❌ Godot Signal 处理核心逻辑 → 用 `EventBus`
- ❌ 直接调用其他 Component 方法 → 用 `Entity.Events`

**资源加载**
- ❌ `GD.Load<T>("res://...")` / `ResourceLoader.Load(...)` → 用 `ResourceManagement.Load`

**系统调用**
- ❌ `new Timer()` / `GetTree().CreateTimer()` → 用 `TimerManager`
- ❌ `GetTree().GetNodesInGroup()` / 手写距离计算 → 用 `TargetSelector`
- ❌ 直接修改 `CurrentHp` → 用 `DamageService.Instance.Process()`
- ❌ 手写暴击/闪避/冷却/充能/范围检测 → 用对应系统组件
- ❌ 手动 `new` + `QueueFree()` 高频对象 → 用对象池

## 4. Skill 速查（@mention 或描述任务自动触发）

| 任务场景 | Skill |
|----------|-------|
| 查找任意模块文档/源码/模板文件 | `@project-index` |
| 新建/管理 Entity、对象池 | `@ecs-entity` |
| 新建/修改 Component | `@ecs-component` |
| 读写 Data、定义 DataKey | `@ecs-data` |
| 事件发布/订阅、定义事件类型 | `@ecs-event` |
| 实现技能功能 | `@ability-system` |
| 造成伤害、扩展伤害处理器 | `@damage-system` |
| 开发 UI、绑定 Entity | `@ui-bind` |
| Timer/ObjectPool/TargetSelector/ResourceManagement | `@tools` |
