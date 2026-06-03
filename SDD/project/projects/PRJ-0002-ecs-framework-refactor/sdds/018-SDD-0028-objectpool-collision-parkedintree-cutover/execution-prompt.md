# SDD-0028 Execution Prompt

把本文件整体交给新的执行会话。目标是一次性完成 `SDD-0028 ObjectPool Collision ParkedInTree Cutover`，不是只改文档或只做对象池局部补丁。

## 角色定位

你是 SDD-0028 的主执行者和集成者。默认中文回答；命令、代码、错误信息保留原文。大任务先计划，再执行。改文件前先读相关文件，改完总结改动和验证结果。不要 push。不要随意加依赖、大重构或跨 git 边界混提交。

执行时必须使用相关 skill：

- `sdd-workflow` / `sdd-management`：恢复和更新 SDD。
- `tools`：ObjectPool 属于 ECS Tools owner。
- `collision-system`：Collision、Hurtbox、ContactDamage、碰撞层、pool guard 属于 Collision owner。
- `movement-system` / `damage-system`：如果接入 MovementCollision 或 Damage/ContactDamage 入口 guard。
- `test-system`：新增 contract tests、validation artifact、日志分析。
- `godot-scene-test` 和 `scene-gate`：新增或运行 ObjectPoolCollisionIsolationValidation 场景。
- `ai-config-management` / `skill-test`：仅当 `.ai-config` skill 源需要同步时使用。

## 工作区

- **Framework Git Boundary**: `/home/slime/Code/SlimeAI/SlimeAI`
- **Game Validation Git Boundary**: `/home/slime/Code/SlimeAI/Games/BrotatoLike`
- **Project**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`
- **Current SDD**: `sdds/018-SDD-0028-objectpool-collision-parkedintree-cutover/`
- **Shared Design Package**: `design/Tool/ObjectPool/`

每次执行 git 操作前先确认边界：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
```

当前工作区可能已有 unrelated `.uid`、AI 配置、DocsAI、Timer、ObjectPool、Collision 设计文档或 `__pycache__` 改动。不要清理、回滚、覆盖或混入无关改动。

## 必读顺序

先读规则和项目入口：

1. `AGENTS.md`
2. `DocsAI/README.md`
3. `DocsAI/ECS/README.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/roadmap.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/progress.md`

再读 ObjectPool / Collision 共享设计：

1. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/ObjectPool/README.md`
2. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/ObjectPool/01-现状证据与AI-first裁决.md`
3. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/ObjectPool/02-目标架构与重构路线.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/ObjectPool/03-碰撞停放与逻辑验证结论草案.md`
5. `DocsAI/ECS/Tools/ObjectPool/README.md`
6. `DocsAI/ECS/Capabilities/Collision/README.md`
7. `DocsAI/ECS/Capabilities/Collision/Concepts/README.md`
8. `DocsAI/ECS/Capabilities/Collision/Concepts/Godot物理时序与对象池碰撞.md`
9. `DocsAI/ECS/Capabilities/Collision/Concepts/Node2D父链约束.md`

再读本 SDD：

1. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/018-SDD-0028-objectpool-collision-parkedintree-cutover/README.md`
2. `design/main.md`
3. `tasks.md`
4. `bdd.md`
5. `progress.md`
6. `notes.md`

最后读源码：

1. `Src/ECS/Tools/ObjectPool/ObjectPool.cs`
2. `Src/ECS/Tools/ObjectPool/ObjectPoolManager.cs`
3. `Src/ECS/Tools/ObjectPool/ObjectPoolObservability.cs`
4. `Src/ECS/Tools/ObjectPool/Tests/`
5. `Src/ECS/Capabilities/Collision/Component/CollisionComponent/CollisionComponent.cs`
6. `Src/ECS/Capabilities/Collision/Component/HurtboxComponent/HurtboxComponent.cs`
7. `Src/ECS/Capabilities/Collision/Component/ContactDamageComponent/ContactDamageComponent.cs`
8. MovementCollision 相关源码

## 核心裁决

- 保留 ObjectPool，不取消对象池，不把 ObjectPool 升级为 EntityManager。
- 保留 `Get(false)` / `Activate()` 两阶段语义。
- 根节点是 `CollisionObject2D` 的池化对象默认 `ParkedInTree`。
- 默认不 `RemoveChild`，不 `SetCollisionTreeActive(false)`，不关 `Monitoring/Monitorable`，不禁用 shape，不改 layer/mask。
- 回池只写 runtime state、隐藏、停处理、移动到分散 parking grid。
- `Activate()` 设置 `CollisionReadyPhysicsFrame = currentPhysicsFrame + 1`。
- 激活第一 physics frame 默认不处理业务碰撞。
- `ObjectPoolRuntimeStateStore` 不进入 `Entity.Data`，不走 DataOS。
- Collision / Movement / ContactDamage / Damage 业务入口必须查 `CollisionLogicGuard`。
- `Detach` / `disable_mode=REMOVE` 只作为 fallback / control check。
- Godot validation 必须产出机器可读 PASS artifact，不能只靠 stdout 或 exit code。

## 禁止结果

- 不把旧 `NeedsTreeDetach(node is CollisionObject2D)` 继续作为默认策略。
- 不默认调用 `SetCollisionTreeActive(false)`。
- 不通过 layer/mask/shape/monitoring 切换模拟对象池退场。
- 不把 pool lifecycle state 写进 `Entity.Data` 或 DataOS descriptor。
- 不只改 ObjectPool 而不接 Collision / Movement / ContactDamage guard。
- 不只接 guard 而不提供可观测 runtime state。
- 不用等待一帧后“没有报错”替代 artifact oracle。
- 不把 `Node2D` 父链结构 bug 继续归因给对象池。

## T1.1 Readiness Baseline

先只读，不改实现。记录摘要到 `progress.md`，不要复制完整 dirty 列表。

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
sed -n '1,420p' Src/ECS/Tools/ObjectPool/ObjectPool.cs
sed -n '1,260p' Src/ECS/Tools/ObjectPool/ObjectPoolManager.cs
sed -n '1,220p' Src/ECS/Tools/ObjectPool/ObjectPoolObservability.cs
sed -n '1,240p' Src/ECS/Capabilities/Collision/Component/CollisionComponent/CollisionComponent.cs
sed -n '1,260p' Src/ECS/Capabilities/Collision/Component/HurtboxComponent/HurtboxComponent.cs
sed -n '1,360p' Src/ECS/Capabilities/Collision/Component/ContactDamageComponent/ContactDamageComponent.cs
find Src/ECS/Tools/ObjectPool/Tests -maxdepth 2 -type f -print | sort
rg -n "NeedsTreeDetach|SetCollisionTreeActive|RemoveChild\\(|ReattachToTree|ForceDisableCollisionsDirect|CollisionShape2D\\.PropertyName\\.Disabled|Area2D\\.PropertyName\\.Monitoring|Area2D\\.PropertyName\\.Monitorable" Src/ECS/Tools/ObjectPool Src/ECS/Capabilities/Collision
rg -n "ObjectPoolRuntimeStateStore|CollisionLogicGuard|CollisionReadyPhysicsFrame|CollisionLogicActive|ParkedInTree" Src/ECS DocsAI SDD
python3 Workspace/SDD/sdd.py validate SDD-0028
```

完成后勾选 `T1.1`，追加 progress：Context / Conclusion / Evidence / Impact / Resume。

## 实现顺序

严格按 `tasks.md` 推进：

1. T1.2 先补 runtime state RED tests。
2. T1.3 拆内部策略和 runtime state store。
3. T1.4 迁移 ObjectPool 默认行为到 `ParkedInTree`。
4. T1.5 接 Collision / Movement guard。
5. T1.6 修 ContactDamage stale attacker timer。
6. T1.7 补节点级观测。
7. T1.8 补 runtime contract tests。
8. T1.9 补 Godot collision validation scene、README 五字段和 PASS artifact。
9. T1.10 降级并隔离 manual demo。
10. T1.11 同步 DocsAI 和 skill。
11. T1.12 跑最终 gates，回填 SDD 和项目状态。

每完成一项任务就更新 `tasks.md` 和 `progress.md`。不要等到最后一次性补状态。

## 目标代码形态

推荐新增或拆分：

```text
Src/ECS/Tools/ObjectPool/PoolLifecycleContext.cs
Src/ECS/Tools/ObjectPool/PoolRuntimeState.cs
Src/ECS/Tools/ObjectPool/ObjectPoolRuntimeStateStore.cs
Src/ECS/Tools/ObjectPool/PoolParkingStrategy.cs
Src/ECS/Tools/ObjectPool/PoolNodeLifecycleStrategy.cs
Src/ECS/Tools/ObjectPool/CollisionLogicGuard.cs
Src/ECS/Tools/ObjectPool/DetachFallbackStrategy.cs
Src/ECS/Tools/ObjectPool/PoolNodeStateSnapshot.cs
```

最小 API 形态：

```csharp
public readonly record struct PoolRuntimeState(
    string PoolName,
    bool IsInPool,
    bool CollisionLogicActive,
    long CollisionReadyPhysicsFrame,
    long LastAcquirePhysicsFrame,
    long LastReleasePhysicsFrame,
    Vector2 ParkingPosition,
    bool DetachFallbackEnabled);

public static bool CanProcessCollision(Node self, long currentPhysicsFrame);
public static bool CanProcessCollision(Node self, Node target, long currentPhysicsFrame);
```

## ObjectPoolCollisionIsolationValidation

建议新增：

```text
Src/ECS/Tools/ObjectPool/Tests/ObjectPoolCollisionIsolationValidation.cs
Src/ECS/Tools/ObjectPool/Tests/ObjectPoolCollisionIsolationValidation.tscn
Src/ECS/Tools/ObjectPool/Tests/README.md
```

README 必须包含：

- `expectedInputs`
- `expectedObservations`
- `passCriteria`
- `failCriteria`
- `artifactPath`

artifact 建议：

```text
.ai-temp/scene-tests/artifacts/objectpool-collision-isolation-validation.json
```

checks 至少覆盖：

- `collision_area_release_parked_in_tree`
- `collision_character_release_parked_in_tree`
- `collision_activate_first_frame_embargo`
- `collision_activate_after_ready_frame`
- `collision_immediate_reuse_same_frame`
- `collision_parking_grid_pressure`
- `collision_detach_fallback_control`
- `collision_artifact_oracle_complete`

运行命令：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/ObjectPool/Tests/ObjectPoolCollisionIsolationValidation.tscn --timeout 20 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

如果当前承载游戏没有 runner 或没有 Godot CLI，必须写入 blocker，不能用 BrotatoLikeOld 或旧游戏证据替代。

## 最终验证

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
python3 Workspace/SDD/sdd.py validate SDD-0028
python3 Workspace/SDD/sdd.py validate --all
git diff --check
```

若改 `.ai-config/skills`：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```
