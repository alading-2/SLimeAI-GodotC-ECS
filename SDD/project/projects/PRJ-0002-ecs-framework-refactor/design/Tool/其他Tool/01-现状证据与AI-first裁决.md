# 现状证据与 AI-first 裁决

> 更新：2026-06-03
> 状态：current research decision
> 裁决：剩余 Tools 不需要整体推翻；真正问题是 owner 边界、文档漂移、查询/加载/随机缺少结构化 diagnostics，以及部分工具仍以自由字符串和静态全局状态作为事实源。

## 1. Goal

本轮要解决：

- `Input`、`ObjectPool`、`Timer` 已改，`Log` 跳过后，剩余 Tools 是否需要保留。
- 每个 Tool 对 AI 是否必要，是否符合 AI-first ECS 原则。
- 当前缺陷、隐藏风险和推荐完善路线。
- 在 PRJ-0002 的 `design/Tool/其他Tool/` 中生成可恢复的设计事实源。

非目标：

- 不直接修改源码。
- 不创建新的执行型 SDD。
- 不处理 `Input`、`ObjectPool`、`Timer` 和 `Logger/Log`。
- 不把 BrotatoLike 专属玩法上提成框架默认。
- 不引入新依赖或第三方 ECS/数学/查询库。

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
- 项目 SDD：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`、`design/INDEX.md`、`progress.md`

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

### 2.3 外部校准

通过 Context7 读取 Godot 官方文档片段：

- Godot thread-safe APIs：跨线程或不安全时机修改 SceneTree 应使用 `CallDeferred(Node.MethodName.AddChild, childNode)`。
- Godot groups：节点可以加入 group 并由 SceneTree 查询，但 group 只是引擎级集合，不提供 SlimeAI 的 Entity/Data/owner/purpose 语义。
- Godot resources / PackedScene：C# 侧使用 `ResourceLoader.Load<PackedScene>()` / `GD.Load<PackedScene>()` 后实例化；SlimeAI 应继续用 `ResourceManagement` 包装加载入口。

### 2.4 Git boundary

当前仓库边界：`/home/slime/Code/SlimeAI/SlimeAI`。

工作区已有大量 `.uid` 删除和若干未跟踪缓存目录，不属于本轮范围。本轮只新增 PRJ-0002 设计文档，并同步项目设计索引入口。

## 3. Problem Shape

剩余 Tools 的共性问题不是“工具太多”，而是：

- **owner 语义不稳定**：`CommonTool` 这种名字会诱导 AI 把杂项继续塞进去；`NodeLifecycle` 和 `ParentManager` 又容易被误当成 Entity/Relationship/System owner。
- **文档与源码漂移**：`NodeLifecycle` 文档示例仍写 `Register(node, "Enemy")` 和 `GetNodesByType<T>("Enemy")`，源码已经没有这些签名。
- **全局静态状态太多**：`NodeLifecycleManager`、`ParentManager`、`ResourceCatalog`、`EntityTargetSelector` 都是静态入口，缺少 reset/diagnostics/scope 说明时，AI 很难判断场景切换、测试隔离和生命周期边界。
- **自由字符串仍存在**：资源 key、parent name、catalog prefix、usageName、entity id string 都还在若干工具里承担语义。
- **查询与随机不可复现**：`TargetSelector` 和 `Math.CheckProbability` / `PositionTargetSelector` 依赖全局或即时随机源，测试和 AI debug 难稳定复现。
- **可观察性不足**：选不到目标、加载失败、节点泄漏、挂载点创建延迟、概率边界异常等问题没有结构化 artifact。

## 4. Main Risks

| 风险 | 说明 | 后果 |
| --- | --- | --- |
| TargetSelector 继续全局扫描 | 当前从 `NodeLifecycleManager.GetNodesByInterface<Node2D>()` 拿全部 Node2D，再几何过滤。 | 敌人规模上升后热路径成本和 GC/排序成本不可控。 |
| `OriginProvider` 被忽略 | `TargetSelectorQuery.ResolveOrigin()` 存在，但几何和排序仍传 `query.Origin`。 | 光环/持续范围/跟随目标查询会出现“文档说跟随，实际不跟随”的隐性 bug。 |
| NodeLifecycle 成为业务事实源 | Entity/UI/Component 都委托它，TargetSelector 也直接扫它。 | AI 容易绕过 EntityRegistry / LifecycleTree / UI owner，重新制造旧全局查询问题。 |
| ParentManager 挂载点隐式创建 | `EnsurePath` 在 root 下用 deferred add，并立即返回 pending node。 | 同帧访问、场景切换、重复 Init、Root scope 不清时难诊断。 |
| ResourceManagement fallback 太宽 | `Load<T>` 找不到精确 key 时用 `Contains` 匹配。 | 重名或近似名时可能加载错误资源，AI 无法从日志知道是否走了 fallback。 |
| Math 混合业务公式和纯数学 | `MyMath` 同时有冷却、护甲、概率，且使用 `GlobalConfig` 和 `GD.Randf()`。 | 公式 owner 不清，概率测试不稳定，跨能力复用边界模糊。 |

## 5. Options

### Option A：只补文档，不动后续实现

优点：

- 成本最低。
- 不影响当前 SDD-0029 和已有 dirty workspace。

缺点：

- 只能降低误路由，不能解决 TargetSelector 查询、Resource fallback、随机复现和 diagnostics 缺口。
- AI 未来实现时仍会踩同样的源码问题。

适合：当前只需要设计包，不急着进入实现。

### Option B：按工具风险做增量 hardening

优点：

- 保留现有 ECS/Godot 主线。
- 优先修 `TargetSelector`、`NodeLifecycle`、`ParentManager` 的 AI debug 痛点。
- 不需要重写 Tools 目录，也不引入新依赖。

缺点：

- 需要拆后续执行型 SDD。
- 部分旧 API 会经历迁移和文档同步。

适合：推荐方案。

### Option C：Tools 整体重构为新 Runtime Tool Kernel

优点：

- 可一次性统一 ToolId、manifest、diagnostics、scope。

缺点：

- 把问题扩大，和当前 AI-first ECS 纠偏方向冲突。
- 会把 `Math`、`ResourceManagement`、`TargetSelector`、`NodeLifecycle` 等不同性质工具强行塞进同一抽象。

适合：不推荐。

## 6. Recommendation

采用 **Option B：按风险增量 hardening**。

推荐顺序：

1. `TargetSelector` 先做查询契约硬化，因为它被 Ability、AI、AbilityImpact、Data feature handler 频繁调用，且直接影响 gameplay 正确性。
2. `NodeLifecycle` 与 `ParentManager` 做边界收口，先补文档和 diagnostics，再决定是否改名或 facade 化。
3. `Math` 补 deterministic RNG、公式 owner、几何 ownership 和测试边界，不做大重写。
4. `CommonTool` 冻结，不再扩展；资源加载问题进入 `ResourceManagement` strict loading / diagnostics。

## 7. Must Confirm

这些问题不阻塞本轮设计文档，但进入实现型 SDD 前必须确认：

- 是否接受 `CommonTool` 不再新增方法，并在后续迁移中把 `LoadPackedScene` 收口到 `ResourceManagement` 或 `ResourceLoadingTool`。
- 是否接受 `TargetSelector` 作为下一轮 Tools 实现优先级最高的切片。
- 是否接受 `NodeLifecycle` 不再作为业务模块可随意调用的公共查询入口，后续新业务默认走 Entity/UI/TargetSelector typed facade。
- 是否接受 `ParentManager` 后续可能改名或 facade 化为 `RuntimeMountRegistry` / `SceneMountRegistry`，但第一阶段保留兼容 API。

## 8. Should Confirm

- `Math.MyMath` 中护甲、冷却、概率公式是否继续归 Math，还是后续按 Damage/Ability/Random 分 owner。
- `TargetSelector` 是否需要在第一阶段加入空间索引，还是先只做 query validation、diagnostics 和 RNG/source hardening。
- 资源 `LoadPath` 是否只允许 DataOS snapshot/resource ref 和明确 `ResourceLoadSource`，业务代码继续禁止直接 `res://`。

## 9. Defaults I Will Use

如果用户不补充，后续设计默认采用：

- 第一阶段不做任何工具改名 hard cutover，只补契约、diagnostics、测试和文档。
- `TargetSelector` 先不引入空间索引；当目标数量、查询次数或 profiling artifact 证明全局扫描成为瓶颈时再加。
- `Math` 不引入第三方库，不迁到 `System.Numerics`，继续使用 Godot `Vector2`。
- `CommonTool` 只作为现有兼容入口保留，不再新增杂项方法。
- `ParentManager` 继续由 `SystemManager._EnterTree()` 初始化，但后续 mount path 需要 manifest 化。

## 10. Not Recommended

- 不建议把剩余 Tools 全部塞进一个大 `ToolManager`。
- 不建议删除 `TargetSelector`，让 Ability/AI 回到手写距离扫描。
- 不建议把 `NodeLifecycleManager` 当作 Entity hard cutover 后的最终 Entity 查询系统。
- 不建议让 `ParentManager` 管 Entity 生命周期、对象池状态或业务关系。
- 不建议把 `CommonTool` 扩成“任何模块都能放一点”的杂项仓库。
- 不建议为了 AI-first 去掉 ECS / Godot 的成熟心智模型。

## 11. DesignCritic

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
- `ParentManager` 返回 pending root child，缺少 mount diagnostics。

### Better Options Checked

更小方案是只更新 DocsAI/Tools 文档。这能降低误路由，但不能给未来实现者足够的代码级风险清单。因此本轮选择项目级设计包，后续再决定是否拆执行型 SDD。

## 12. Artifact Updates

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
