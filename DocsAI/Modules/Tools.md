# Tools 模块契约

本文是 AI 使用或修改 TimerManager、ObjectPool、TargetSelector、ResourceManagement 和通用工具时必须阅读的执行契约。

## 职责边界

Tools 用来消除重复、易错和高频流程。

Tools 负责：

- 统一计时器。
- 高频对象池。
- 目标查询。
- 资源加载。
- 数学、日志、节点生命周期等通用工具。

Tools 不负责：

- 承载具体玩法业务。
- 绕过 EntityManager / SystemManager。
- 作为临时杂物桶收纳跨模块逻辑。

## 核心入口

- `Src/ECS/Tools/Timer/TimerManager.cs`
- `Src/ECS/Tools/ObjectPool/ObjectPool.cs`
- `Src/ECS/Tools/ObjectPool/ObjectPoolManager.cs`
- `Src/ECS/Tools/TargetSelector/TargetSelector.cs`
- `Src/ECS/Tools/TargetSelector/TargetSelectorQuery.cs`
- `Data/ResourceManagement/ResourceManagement.cs`
- `Data/ResourceManagement/ResourcePaths.cs`
- `Src/ECS/Tools/Math/`

## 数据 / 事件 / 生命周期

- 计时统一用 `TimerManager`。
- 高频 Entity / UI / 特效统一走对象池。
- Entity 对象池生成统一通过 `EntityManager.Spawn`。
- 范围查找统一用 `EntityTargetSelector.Query`。
- 资源加载统一用 `ResourceManagement` 或明确允许的底层工具。
- 运行时数据配置从 DataNew 读取，不通过 ResourceManagement 读旧 `.tres` 主数据。

## 禁止事项

- 禁止 `new Timer()` / `GetTree().CreateTimer()`。
- 禁止高频 `new` + `QueueFree()`。
- 禁止 `GetTree().GetNodesInGroup()` 手写查目标。
- 禁止业务代码直接 `GD.Load<T>("res://...")`。
- 禁止把技能专用逻辑塞进通用 Tools。

## 修改流程

1. 判断需求是否已有现成 Tool。
2. 新 Tool 必须有明确复用场景，不要为单个功能过早抽象。
3. 涉及对象池时检查 `IPoolable`、激活时序和复用统计。
4. 涉及资源加载时检查 ResourceGenerator / ResourcePaths 是否需要更新。
5. 更新对应 Skill、DocsAI 和测试矩阵。

## 推荐测试

- `dotnet build`
- ObjectPool：`node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/Tools/ObjectPool/ObjectPoolManagerTest.tscn --build`
- TargetSelector：`node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/Tools/TargetSelector/TargetSelectorTest.tscn --build`
- Input / Logger / Math 修改时运行对应 `SingleTest/Tools` 场景。

## 人工审查重点

- 是否绕过框架统一工具。
- 计时器是否在释放时取消。
- 对象池是否存在脏状态、旧碰撞、重复订阅。
- 目标查询是否保持阵营、范围和排序语义。
- 资源路径是否硬编码或绕过 ResourceManagement。
