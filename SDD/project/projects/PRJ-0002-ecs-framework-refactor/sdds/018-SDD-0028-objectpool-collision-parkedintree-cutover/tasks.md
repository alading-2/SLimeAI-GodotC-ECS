# Tasks

## Progress

- **Status**: active
- **Completed**: 0/12
- **Current**: T1.1

## Task List

- [ ] T1.1 Readiness baseline
  - **Scope**: 确认 git boundary、dirty baseline、当前 ObjectPool 旧脱树实现、Collision/Movement/ContactDamage guard 缺口、ObjectPool tests/demo 现状、DocsAI/SDD 引用和 SDD validate 基线。
  - **Validation**: `git status --short`；目标 grep gates；`python3 Workspace/SDD/sdd.py validate SDD-0028`。

- [ ] T1.2 Runtime state tests first
  - **Scope**: 为 `ObjectPoolRuntimeStateStore`、`PoolRuntimeState`、parking position、ready frame、release/acquire frame、fallback flag 写 RED tests 或等价 headless checks。
  - **Validation**: runtime contract checks 先出现缺类型/缺行为失败，再实现转绿。

- [ ] T1.3 ObjectPool internal strategy split
  - **Scope**: 在 `Src/ECS/Tools/ObjectPool/` 拆出 `PoolNodeLifecycleStrategy`、`PoolParkingStrategy`、`ObjectPoolRuntimeStateStore`、`CollisionLogicGuard`、`DetachFallbackStrategy`、`PoolLifecycleContext`。
  - **Validation**: public API 不变；纯 C# / Node pool contract 通过；无新增依赖。

- [ ] T1.4 ParkedInTree default cutover
  - **Scope**: 修改 `ObjectPool<T>` 默认 release/activate：不默认脱树、不默认关碰撞、不改 layer/mask/shape；release 隐藏、停处理、停车并写 state；activate 写 ready frame。
  - **Validation**: grep gate 证明旧默认路径只剩 fallback/control 或历史注释；contract tests 覆盖 `Area2D` / `CharacterBody2D` 根节点仍在树中。

- [ ] T1.5 CollisionLogicGuard API and owner integration
  - **Scope**: 提供 `CanProcessCollision` 入口，并接入 `CollisionComponent`、`HurtboxComponent`、`PickupComponent` 和 MovementCollision 入口。
  - **Validation**: 回池对象、未到 ready frame、无效 self/target 不发业务事件。

- [ ] T1.6 ContactDamage stale attacker cleanup
  - **Scope**: `ContactDamageComponent` 在 `HurtboxEntered`、timer tick、resume 时查 attacker pool state；guard 失败时取消 timer 并清理 `_contactBodies/_bodyTimers`。
  - **Validation**: 旧 attacker 回池或复用后不会继续对受害者造成接触伤害。

- [ ] T1.7 Runtime observability
  - **Scope**: 扩展 `ObjectPoolObservability` 或新增 snapshot registry，输出 pool/node state、parking position、ready frame、last acquire/release frame、fallback 状态。
  - **Validation**: TestSystem 或 validation artifact 可读取节点级状态；不进入 `Entity.Data`。

- [ ] T1.8 ObjectPool runtime contract tests
  - **Scope**: 新增 `ObjectPoolContractRuntimeTest`，覆盖 warmup、stats、重复归还、capacity discard、static return、plain object mapping、active snapshot、测试池隔离。
  - **Validation**: 可自动运行，不依赖 UI、鼠标、随机位置或人工观察。

- [ ] T1.9 Godot collision validation scene
  - **Scope**: 新增 `ObjectPoolCollisionIsolationValidation.cs/.tscn/README.md`，自动覆盖 Area2D / CharacterBody2D release、activate first-frame embargo、ready frame、same-frame reuse、parking pressure、fallback detach control。
  - **Validation**: README 五字段 + PASS artifact `.ai-temp/scene-tests/artifacts/objectpool-collision-isolation-validation.json`，artifact `checks[]` 完整。

- [ ] T1.10 Manual demo isolation
  - **Scope**: 保留 `ObjectPoolVisualTest` / `ObjectPoolManagerTest` 为 legacy/manual demo；测试池名改为 `Demo/ObjectPool/...`；避免污染真实 `ObjectPoolNames` 和全局 `DestroyAll()`。
  - **Validation**: demo 不作为 PASS/FAIL；测试池名 grep 不覆盖真实池名。

- [ ] T1.11 DocsAI and skill sync
  - **Scope**: 更新 ObjectPool、Collision、Movement/Damage 相关 DocsAI；若实现/验证规则变化，更新 `.ai-config/skills` 源并运行 sync/skill-test。
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`。

- [ ] T1.12 Final gates and SDD closeout
  - **Scope**: 跑 build、DataOS validator、ObjectPool contract、Godot scene validation、scene-gate、BrotatoLike smoke、SDD validate；回填 tasks/progress/项目 roadmap/progress。
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`；`python3 Workspace/SDD/sdd.py validate SDD-0028`；`python3 Workspace/SDD/sdd.py validate --all`；所有 gate 有新鲜证据。
