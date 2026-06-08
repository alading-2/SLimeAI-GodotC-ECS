# 现状证据与 AI-first 裁决

> 更新：2026-06-07
> 状态：current consolidated overview
> 裁决：剩余 Tools 的功能都按 AI-first 重新定义；执行时只保功能，不保旧 API 兼容。`ParentManager` 功能升级为 Runtime mount 能力；`TargetSelector` 不做兼容桥，完全迁到 `TargetQueryEngine`；`NodeLifecycle` 迁到 Runtime registry；`CommonTool.LoadPackedScene` 迁入 ResourceLoading；Common Utilities 固定放 `Src/ECS/Tools/CommonUtilities/`；不保留 ResourceManagement 的“管理器”心智，后续收敛为极薄 `ResourceLoading` 统一加载工具 + generated catalog + diagnostics。

## 1. Goal

本轮要解决：

- `Input`、`ObjectPool`、`Timer` 已改，`Log` 跳过后，剩余 Tools 是否需要保留。
- 每个 Tool 对 AI 是否必要，是否符合 AI-first ECS 原则。
- 当前缺陷、隐藏风险和推荐完善路线。
- 在 PRJ-0002 的 `design/Tool/其他Tool/` 中生成可恢复的设计事实源。
- 按用户最新要求校准：AI 框架以 AI 为主，服务 AI 快速理解功能；需要重构就完全重构，不因旧代码形状保留兼容链。

非目标：

- 不直接修改源码。
- 不创建新的执行型 SDD。
- 不处理 `Input`、`ObjectPool`、`Timer` 和 `Logger/Log`。
- 不把 BrotatoLike 专属玩法上提成框架默认。
- 不引入新依赖或第三方 ECS/数学/查询库。
- 不再按每次用户确认新增独立时间线文档；后续确认应回写本总览或对应功能文档。

## 2. Context Read

### 2.1 本地事实源

- `Workspace/SystemAgent/Actors/DeepThink.md`
- `Workspace/SystemAgent/Actors/DesignCritic.md`
- `.codex/skills/tools/SKILL.md`
- `DocsAI/README.md`
- `DocsAI/ECS/README.md`
- `DocsAI/ECS框架与AIFirst方向决策.md`
- `DocsAI/ECS/Runtime/Data/Data系统说明.md`
- `DocsAI/ECS/Tools/Math/*`
- `DocsAI/ECS/Tools/TargetSelector/*`
- `DocsAI/ECS/Tools/NodeLifecycle/*`
- `DocsAI/ECS/Tools/ParentManager/*`
- `Data/ResourceManagement/*`
- `Src/ECS/Tools/CommonTool.cs`
- `Src/ECS/Tools/Math/**`
- `Src/ECS/Tools/TargetSelector/**`
- `Src/ECS/Tools/NodeLifecycle/NodeLifecycleManager.cs`
- `Src/ECS/Tools/ParentManager/**`
- `Src/ECS/Runtime/System/SystemManager.cs`
- `Src/ECS/Runtime/Entity/Manager/EntityManager.cs`
- `Src/ECS/UI/Core/UIManager.cs`
- 项目 SDD：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`、`design/INDEX.md`、`Core/progress.md`
- 已合并的历史确认输入：2026-06-04 至 2026-06-07 按时间追加的用户裁决、ResourceManagement 深度分析和 ResourcePathMigration 设计。

### 2.2 广泛搜索范围

本轮使用 `find` / `rg` 搜索了：

- `Src/ECS/Tools` 当前文件清单。
- `DocsAI/ECS/Tools` 当前文档清单。
- `CommonTool`、`LoadPackedScene` 调用点。
- `MyMath`、`WaveMath`、`BezierCurve`、`BezierTemplateBuilder`、`Geometry2D`、`GeometryCalculator` 调用点。
- `NodeLifecycleManager`、`GetNodesByInterface`、`GetNodesByType` 调用点。
- `ParentManager`、`ParentNames`、`GetOrRegister`、`EnsurePath` 调用点。
- `EntityTargetSelector`、`PositionTargetSelector`、`TargetSelectorQuery`、`GeometryType`、`TargetSorting`、`TeamFilter` 调用点。
- `ResourceManagement`、`ResourceCatalog`、`LoadPath` 调用点。
- `ResourceGenerator`、`ResourcePaths`、project directory / `project-filesystem` workflow 设计与路径残留检查。

### 2.3 外部校准

通过 Context7 读取 Godot 官方文档片段：

- Godot thread-safe APIs：跨线程或不安全时机修改 SceneTree 应使用 `CallDeferred(Node.MethodName.AddChild, childNode)`。
- Godot groups：节点可以加入 group 并由 SceneTree 查询，但 group 只是引擎级集合，不提供 SlimeAI 的 Entity/Data/owner/purpose 语义。
- Godot resources / PackedScene：C# 侧使用 `ResourceLoader.Load<PackedScene>()` / `GD.Load<PackedScene>()` 后实例化；SlimeAI 应继续用 `ResourceManagement` 包装加载入口。
- Godot Node / SceneTree：child 随 parent 生命周期管理，group 查询只返回当前 tree 中属于 group 的节点；这支持 SlimeAI 保留统一 mount registry，但不支持把 group 当作 gameplay 查询事实源。
- Godot RandomNumberGenerator：seed/state 支持可复现伪随机序列；这支持 TargetSelector / Math / Spawn 的 deterministic RNG 方向。

### 2.4 本地 Resources 校准

启用 `Resources/Engine/Docs/FrameworkAnalysis/Reports` 最小范围搜索：

- EnTT / DefaultEcs / Arch / Flecs / Gaia 报告都支持“Capability-owned selector / service 查询”方向，拒绝暴露通用 world query DSL、pair graph、archetype/chunk storage 或 registry public API。
- EnTT resource cache 只采纳 stable key、loader、diagnostics 的观察思想，不复制 C++ handle/cache 模型。
- Arch / DefaultEcs hierarchy 只作为 parent / hierarchy 派生数据与结构变更边界参考，不恢复通用 Relationship graph。

### 2.5 Git boundary

当前仓库边界：`/home/slime/Code/SlimeAI/SlimeAI`。

工作区已有大量 `.uid` 删除和若干未跟踪缓存目录，不属于本轮范围。本轮只新增 PRJ-0002 设计文档，并同步项目设计索引入口。

## 3. Problem Shape

剩余 Tools 的共性问题不是“工具太多”，也不是“是否还保留旧代码”，而是 AI 不能从入口快速判断功能、owner、scope、生命周期、失败原因和验证方式：

- **owner 语义不稳定**：`CommonTool` 这种名字会诱导 AI 把杂项继续塞进去；`NodeLifecycle` 和 `ParentManager` 又容易被误当成 Entity/Relationship/System owner；`ResourceManagement` 这个名字会把极薄加载 facade 误导成“资源管理系统”。
- **文档与源码漂移**：`NodeLifecycle` 文档示例仍写 `Register(node, "Enemy")` 和 `GetNodesByType<T>("Enemy")`，源码已经没有这些签名。
- **全局静态状态太多**：`NodeLifecycleManager`、`ParentManager`、`ResourceCatalog`、`EntityTargetSelector` 都是静态入口，缺少 reset/diagnostics/scope 说明时，AI 很难判断场景切换、测试隔离和生命周期边界。
- **自由字符串仍存在**：资源 key、parent name、catalog prefix、usageName、entity id string 都还在若干工具里承担语义。
- **查询与随机不可复现**：`TargetSelector` 和 `Math.CheckProbability` / `PositionTargetSelector` 依赖全局或即时随机源，测试和 AI debug 难稳定复现。
- **可观察性不足**：选不到目标、加载失败、节点泄漏、挂载点创建延迟、概率边界异常等问题没有结构化 artifact。
- **执行策略过保守**：旧设计包里“第一阶段保留兼容 API / 只补 diagnostics”的表述容易让后续实现继续沿旧入口叠补丁；用户已明确不需要为了代码兼任而保留旧 API。

## 4. Main Risks

| 风险 | 说明 | 后果 |
| --- | --- | --- |
| TargetSelector 继续全局扫描 | 当前从 `NodeLifecycleManager.GetNodesByInterface<Node2D>()` 拿全部 Node2D，再几何过滤。 | 敌人规模上升后热路径成本和 GC/排序成本不可控。 |
| `OriginProvider` 被忽略 | `TargetSelectorQuery.ResolveOrigin()` 存在，但几何和排序仍传 `query.Origin`。 | 光环/持续范围/跟随目标查询会出现“文档说跟随，实际不跟随”的隐性 bug。 |
| NodeLifecycle 成为业务事实源 | Entity/UI/Component 都委托它，TargetSelector 也直接扫它。 | AI 容易绕过 EntityRegistry / LifecycleTree / UI owner，重新制造旧全局查询问题。 |
| ParentManager 挂载点隐式创建 | `EnsurePath` 在 root 下用 deferred add，并立即返回 pending node。 | 同帧访问、场景切换、重复 Init、Root scope 不清时难诊断；但这说明 mount 功能重要，不是可删除工具。 |
| ResourceManagement fallback 太宽 | `Load<T>` 找不到精确 key 时用 `Contains` 匹配，且类名暗示它能管理路径移动。 | 重名或近似名时可能加载错误资源；AI 会误以为目录移动稳定性应由运行时 loader 解决。 |
| Math 混合业务公式和纯数学 | `MyMath` 同时有冷却、护甲、概率，且使用 `GlobalConfig` 和 `GD.Randf()`。 | 公式 owner 不清，概率测试不稳定，跨能力复用边界模糊。 |
| 长期兼容 facade 扩散 | 旧入口保留下来后，AI 会继续复制旧调用。 | 重构无法真正完成，DocsAI 与源码入口继续漂移。 |

## 5. Options

### Option A：只补文档，不动后续实现

优点：

- 成本最低。
- 不影响当前 SDD-0029 和已有 dirty workspace。

缺点：

- 只能降低误路由，不能解决 TargetSelector 查询、Resource fallback、随机复现和 diagnostics 缺口。
- AI 未来实现时仍会踩同样的源码问题。

适合：当前只需要设计包，不急着进入实现。

### Option B：按功能切片 hard cutover

优点：

- 保留 ECS/Godot 功能主线，但不保旧 API 形状。
- 每个切片内部可以改名、删旧方法、迁移调用点、重建测试，切片结束前删除临时兼容入口。
- 优先解决 AI 最难判断的 mount、query、resource loading、random/diagnostics 问题。

缺点：

- 需要拆后续执行型 SDD。
- 验证成本更高，需要 grep gate、DocsAI/skill sync 和 Godot scene artifact。

适合：推荐方案。

### Option C：Tools 整体重构为新 Runtime Tool Kernel

优点：

- 可一次性统一 ToolId、manifest、diagnostics、scope。

缺点：

- 把问题扩大，和当前 AI-first ECS 纠偏方向冲突。
- 会把 `Math`、`ResourceManagement`、`TargetSelector`、`NodeLifecycle` 等不同性质工具强行塞进同一抽象。

适合：不推荐。

## 6. Recommendation

采用 **Option B：按功能切片 hard cutover**。

推荐顺序：

1. `RuntimeMountRegistry` / `SceneMountRegistry`：把 `ParentManager` 功能升级为 mount manifest、scope、pending/in-tree diagnostics，解决大量 Entity / Pool / UI / Tool 节点路径统一问题。
2. `TargetQueryEngine`：替换静态全局扫描式 `EntityTargetSelector`，补 query validation、resolved origin/forward、candidate source、diagnostics、deterministic RNG 和 safe sorting。
3. `ResourceLoading`：迁出 `CommonTool.LoadPackedScene`，删除或重命名当前 `ResourceManagement` 管理器入口，保留极薄加载工具；strict lookup、source policy、structured result、catalog diagnostics。
4. `MathFormula` / deterministic random：拆 `MyMath` owner，保留 `Geometry2D` 纯算法，概率与采样支持 seed/RNG。
5. `NodeLifecycleRegistry`：从 Tools helper 语义迁到 Runtime registry，保留统一 Node 注册、注销、id、owner metadata、snapshot diagnostics 和 invalid cleanup；业务查询迁走后删除 public global scan 入口。
6. `Common Utilities`：保留通用工具区域，最终放在 `Src/ECS/Tools/CommonUtilities/` + `DocsAI/ECS/Tools/CommonUtilities/`，但不保留无约束 `CommonTool.SomeHelper()` 杂物箱。

## 7. Confirmed Decisions

这里记录已经解决的方向问题。后续实现不再重复询问这些问题。

用户已确认：

- `ParentManager` 默认挂载 scope 采用 `/root/SlimeAIRuntime`。
- `ResourceManagement` 不作为长期“资源管理器”概念保留；后续应重命名或收敛为极薄 `ResourceLoading` 统一加载工具。
- 统一加载工具接受 strict fail-fast：删除 contains fallback，缺精确 key / 缺 `ResourceLoadSource` 直接让验证失败。
- `res://` 本身不是问题；它是 Godot project root 路径。要治理的是无 owner 裸加载、路径移动后的引用迁移和 diagnostics。
- ResourceManagement 不应被描述为“移动目录不出错”的万能路径管理器；当前名称和文档会误导，执行期应改为 `ResourceLoading` 或等价极薄 facade。
- 目录 / 资源移动后通过 project directory / project-filesystem workflow 替换 old/new path，运行 ResourceGenerator，并用 `rg` / diagnostics 检查旧路径残留。
- 单独目录操作 skill 是必要的：AI-first 框架不应靠人工全仓搜索处理目录生成、删除、移动、检查和字符串路径替换。
- 未来框架仓和游戏仓分离后，框架 catalog 不默认拥有游戏资源；游戏仓需要自己的 game-local catalog 或 generator root/output 能力。
- Common Utilities 最终目录为 `Src/ECS/Tools/CommonUtilities/` + `DocsAI/ECS/Tools/CommonUtilities/`。
- `NodeLifecycle` 从 `Src/ECS/Tools/NodeLifecycle/` 迁到 `Src/ECS/Runtime/NodeLifecycle/`。
- `TargetSelector` 不做兼容桥；`EntityTargetSelector.Query(query)` 不作为执行期或最终 public/current API 保留，执行型 SDD 应完全迁移到 `TargetQueryEngine`。

## 8. Must Confirm

当前没有阻塞进入设计整理或创建后续执行型 SDD 的 Must Confirm。

ResourceLoading 实现前仍有独立功能决策需要在该执行型 SDD 内确认：

- DataOS resource ref 是否未来从 `res://` 迁到 `ResourceKey + Category` 或 `uid://`。默认先不改 DataOS schema。
- Godot `uid://` 是否纳入下一阶段验证。默认只作为研究项，不作为当前主存储。
- 资源加载失败策略的具体实现边界：默认 structured result + startup preflight fail-fast，不在 gameplay 热路径到处抛未捕获异常。
- 游戏仓分离后 game-local catalog 的输出位置和生成命令。默认框架 generator 不拥有游戏资源。

## 9. Should Confirm

- `ParentManager` 执行时是否直接改名为 `RuntimeMountRegistry` / `SceneMountRegistry`。
- `MyMath` 是否拆为 `AttributeFormula` / `CooldownFormula` / `DamageFormula` / `ProbabilityTool`。

已不再需要确认：

- `CommonTool.LoadPackedScene` 迁出后当前 `CommonTool` 删除或 internal 化。
- `NodeLifecycleManager.GetAllNodes()` / `GetNodesByInterface<T>()` 从业务可见 API 删除。

## 10. Defaults I Will Use

如果用户不补充，后续设计默认采用：

- 只保功能兼容，不保 API 兼容；旧类名、旧方法、旧文件可删除。
- `ParentManager` 执行时升级为 `RuntimeMountRegistry` / `SceneMountRegistry` 语义，默认挂到 `/root/SlimeAIRuntime`，测试可注入 test root。
- `TargetSelector` 先不引入空间索引；先完成 query contract、candidate source、diagnostics 和 deterministic RNG。当目标数量、查询次数或 profiling artifact 证明全局扫描成为瓶颈时再加。
- `Math` 不引入第三方库，不迁到 `System.Numerics`，继续使用 Godot `Vector2`。
- `CommonTool.LoadPackedScene` 迁入 `ResourceManagement` / `ResourceLoading`；当前 `CommonTool` 删除或 internal 化。
- 保留受约束 Common Utilities 区域，位置固定为 `Src/ECS/Tools/CommonUtilities/` + `DocsAI/ECS/Tools/CommonUtilities/`，但不允许 `CommonTool.SomeHelper()` 杂物箱。
- `ResourceManagement.Load<T>` 删除 contains fallback；`LoadPath` 必须携带 `ResourceLoadSource`。
- `ResourceManagement` 命名默认在执行型 SDD 中改为 `ResourceLoading` facade，或至少让 public 文档只暴露 ResourceLoading 语义；ResourceCatalog 和 ResourceGenerator 保留为 catalog / diagnostics 工具。
- 目录增删改查默认使用 project directory / `project-filesystem` skill：先确认 git boundary，先 dry-run，再 apply，再检查 old path residue。
- `NodeLifecycle` 迁到 Runtime registry 语义，只保底层注册和 diagnostics，不作为新业务查询入口。

## 11. Not Recommended

- 不建议把剩余 Tools 全部塞进一个大 `ToolManager`。
- 不建议删除 `TargetSelector`，让 Ability/AI 回到手写距离扫描。
- 不建议把 `NodeLifecycleManager` 当作 Entity hard cutover 后的最终 Entity 查询系统。
- 不建议让 `ParentManager` 管 Entity 生命周期、对象池状态或业务关系。
- 不建议把 `CommonTool` 扩成“任何模块都能放一点”的杂项仓库。
- 不建议继续写长期兼容 facade。
- 不建议继续为每一次确认新增一份按日期命名的 current 设计文档；确认应合并到总览或对应功能文档。
- 不建议把 `res://` 当成错误格式；它是 Godot 项目根路径。
- 不建议声称 ResourceManagement 能自动修复资源移动；移动后的修复属于 project directory / migration workflow + generator + diagnostics。
- 不建议保留 `ResourceManagement` 作为大而泛的“资源管理系统”概念；它现在代码量很小，最终只需要 ResourceLoading 加载工具。
- 不建议为了 AI-first 去掉 ECS / Godot 的成熟心智模型；要重构的是 SlimeAI 的 AI 可读契约，不是引擎事实。

## 12. DesignCritic

### Assumptions

- 当前 `Src/ECS/Tools` 剩余 owner 就是本轮范围，`Input/ObjectPool/Timer/Log` 不再展开。
- 当前工作只落设计，不进入代码实现。
- 现有 dirty workspace 中 `.uid` 删除和未跟踪缓存目录不是本轮改动。

### Missing Context

- 未运行 Godot 场景验证，原因是本轮不改运行时代码。
- 未检查 BrotatoLike 当前 runner 状态，原因是本轮只写框架设计文档。
- 未读取所有外部引擎源码，只用本地源码、DocsAI/SDD 和 Godot 官方文档校准关键边界。

### Design Defects Found

- `TargetSelectorQuery.ResolveOrigin()` 没有被当前 selector 主路径使用。
- `NodeLifecycle` DocsAI 文档和源码 API 不一致。
- `CommonTool` 名称与 AI-first owner 边界冲突。
- `ResourceManagement.Load<T>` 的 contains fallback 对 AI debug 不透明。
- `ResourceManagement` 名称与当前代码量和职责不匹配，容易把目录移动、catalog、加载、修复都塞回一个“管理器”。
- `ParentManager` 返回 pending root child，缺少 mount diagnostics。

### Better Options Checked

更小方案是只更新 DocsAI/Tools 文档。这能降低误路由，但不能给未来实现者足够的代码级风险清单，也不符合用户“功能优先、可完全重构”的裁决。因此本轮保留项目级设计包，但收敛为固定结构：总览、功能说明和实施验证，不再让日期确认文档成为 current 入口。

## 13. Artifact Updates

本轮写入：

- `design/Tool/其他Tool/README.md`
- `design/Tool/其他Tool/01-现状证据与AI-first裁决.md`
- `design/Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md`
- `design/Tool/其他Tool/03-Math目标架构与验证.md`
- `design/Tool/其他Tool/04-NodeLifecycle与ParentManager边界.md`
- `design/Tool/其他Tool/05-TargetSelector查询契约.md`
- `design/Tool/其他Tool/06-实施路线与验证门禁.md`
- `design/INDEX.md`
- `README.md`
