# 2026-05-14 OpenSpec 第一轮重构计划评审

## 总体评分

- 战略合理性：6 / 10
- 技术深度：6 / 10
- AI 可执行性：5 / 10
- 风险控制：4 / 10
- 文档质量：6 / 10
- 综合：5.4 / 10

结论：建议 **修改后部分实施**。这 5 个 change 抓住了 SlimeAI 第一轮 AI-first 重构的主要病灶，但当前骨架不能按 `P2 -> P1/P3 -> P4 -> P5` 直接进入实现。最核心的问题不是 OpenSpec schema，而是 artifact 之间存在顺序冲突、部分设计仍是 stringly-typed、跨仓边界有反向依赖风险、P4 语义变更过大且验证不足。

## 证据范围

- 已读取 5 个 active change 的 `proposal.md / design.md / tasks.md / specs/*/spec.md`。
- 已运行并确认 5 个 change 均通过 `openspec validate --strict`。
- 已对照当前代码搜索 `RelationshipManager / EntityId / IRuntimeSystem / RuntimeCommandBuffer / MouseSelection / InputUseSkill / GodotPlayerInputComponent` 等符号。
- 已对照官方外部资料：Bevy `Commands`、Unity Entities `EntityCommandBuffer`、Flecs relationships / defer、Unreal GAS。

关键本地证据：

- `SlimeAI/DocsAI/ProjectState.md:13` 和 `:23` 写的是 P2 优先实施。
- `openspec/changes/refactor-runtime-entity-identity-and-world/proposal.md:49` 写明 P2 必须在 P1 archive 之后开始。
- `openspec/changes/refactor-runtime-schedule-with-phases-and-command-buffer/proposal.md:55` 写明 P4 必须在 P2 archive 之后开始。
- `openspec/changes/refactor-runtime-events-purge-game-leakage/tasks.md:31` 要求框架 `MouseSelection` capability 改用 `BrotatoLike.Game.Events`。
- 当前代码中并不存在 `SlimeAI/GameOS/Capabilities/MouseSelection/`，只有 `Runtime/Events/Global/MouseSelection*.cs`。
- `openspec/changes/refactor-runtime-schedule-with-phases-and-command-buffer/tasks.md:22` 把 `DeferredRuntimeCommand` 设计为单个 `PayloadKey / PayloadValue` 字符串。
- `openspec/changes/refactor-runtime-schedule-with-phases-and-command-buffer/proposal.md:17` 与 `tasks.md:4-6` 对 `ProcessRecord / ProcessRuntimeInfo` 的职责描述不一致。
- `openspec/changes/enhance-ai-feature-development-skill/proposal.md:36` 说 references 为 8 个文件，`spec.md:36-48` 实际列出 11 个，`tasks.md:86` 又写 9-10 个。

## 主要优点

- **问题选择基本正确**：Relationship 弱类型图、raw string EntityId、静态全局 world、框架事件混入 BrotatoLike、RuntimeCommandBuffer spec 未落地、skill 源同步纪律，都是当前 AI-first 路由稳定性的真实痛点。
- **P1 的方向是对的**：生命周期树与业务引用拆开，生命周期只负责 parent-child 和 destroy policy，业务 owner/source/target 交给 Capability-owned DataKey，这是比 Flecs-style 通用 relationship graph 更符合本项目定位的取舍。
- **P3 的边界意识是对的**：Wave、Game lifecycle、InputSkill、BrotatoLike input map 不应该留在框架 Runtime/Events，尤其 `GodotPlayerInputComponent` hardcode 游戏输入动作名，确实会误导后续 AI 路由。
- **P5 放最后的理由成立**：P5 reference 文件如果引用 P1-P4 未落地概念，会把 spec 假设写成事实。最后基于 archive 后事实固化 skill 是合理的。
- **OpenSpec 四件套完整度较高**：每个 change 都有 Why、What、Impact、Design decisions、Tasks 和 capability delta，并通过 strict validate，说明 skeleton 层面不是随意文本。

## 主要问题

### 🔴 严重 / 阻塞性

#### 1. 实施顺序与 artifact 自身依赖冲突

当前 ProjectState 和用户摘要建议 `P2 -> P1/P3 -> P4 -> P5`，但 P2 artifact 自己写明 “必须在 P1 archive 之后开始”。P4 又依赖 P2。按现有 artifact，实际 DAG 至少是：

```text
clarify-gameos-terminology archive?
        │
        ▼
P1 Relationship/Lifecycle
        │
        ▼
P2 EntityId + RuntimeWorld
        │
        ▼
P4 Schedule + CommandBuffer

P3 Events 可独立，但会与 P2 的 EntityId payload 迁移互相重触
P5 必须最后
```

建议修改：

- 立即修正 `SlimeAI/DocsAI/ProjectState.md` 的顺序说明，避免后续 AI 按错误路线执行。
- 如果坚持 P2 先做，应把 P2 拆成 `P2a: EntityId typed wrapper` 和 `P2b: RuntimeWorld facade`。`P2a` 可以先于 P1，P1 直接使用 typed `EntityId`，避免先做 `EntityIdList<string>` 再马上改为 `EntityIdList<EntityId>`。
- 如果不拆 P2，则执行顺序应改为 `P1 -> P2 -> P3 -> P4 -> P5`，其中 P3 可以早做，但要接受后续 P2 再改 game-side event payload。

#### 2. P3 的 MouseSelection 处理会制造框架到游戏的反向依赖

P3 spec 说框架 `Runtime/Events` 不持有 game-specific event，这是正确的。但 `tasks.md:31` 要求 `SlimeAI/GameOS/Capabilities/MouseSelection/` 若引用旧事件，就改为 `using BrotatoLike.Game.Events;`。这会让框架仓引用游戏仓 namespace，违反多仓单向依赖。更严重的是，当前代码根本没有 `SlimeAI/GameOS/Capabilities/MouseSelection/` 目录，只有未使用的 `MouseSelection*.cs` event 文件。

建议修改：

- 删除 P3 中关于 “framework MouseSelection capability 继续留在框架但发布 BrotatoLike 事件” 的方案。
- 对当前实际代码，MouseSelection/Wave/GameStart 这些 global event 没有业务引用，应优先判断是 **直接删除** 还是迁到游戏侧作为未来占位。没有 producer/consumer 的事件不应为“迁移”创建新游戏侧 API。
- `GodotPlayerInputComponent` 和 `InputUseSkill/InputPreviousSkill/InputNextSkill` 是真实调用链，P3 应聚焦这些实际生产消费路径。
- 如果未来确实要 MouseSelection capability，应该单独 OpenSpec 决定 capability owner，不能在框架 capability 里引用 game event。

#### 3. P4 `DeferredRuntimeCommand` 仍然是 stringly-typed payload，不满足本轮 typed-value 目标

P4 的 `DeferredCommandKind` 封闭 8 种是可以接受的第一阶段 scope，但 `DeferredRuntimeCommand(... string PayloadKey, string PayloadValue ...)` 不够表达 spawn / queued event / resource request / Godot instantiate。它把弱类型字典换成了弱类型字符串协议，仍然需要运行时解析、错误原因膨胀和文档同步。

外部对照：

- Bevy `Commands` 是 command queue，命令在 `ApplyDeferred` 时顺序应用，且内建 command 是 typed method，扩展 command 也是实现 `Command` trait 的 typed 行为。
- Unity ECB 通过 `CreateEntity / DestroyEntity / SetComponent<T> / AddComponent<T>` 这种 typed API 记录命令，playback 前不生效，并明确 placeholder entity 只在同一个 ECB 内有意义。
- Flecs defer 支持自动延迟，但文档明确 deferred 操作在 `defer_end` 前不可见，并列出删除实体、子实体等边界规则。

建议修改：

- 保留 `DeferredCommandKind` 作为 discriminant，但为每种 kind 定义 command-specific payload record，例如 `SpawnCommandPayload`、`DestroyCommandPayload`、`AttachCommandPayload`、`QueuedEventCommandPayload`、`GodotNodeInstantiatePayload`。
- 如果为了不引入继承，可用 `DeferredRuntimeCommand` 持有 nullable typed payload fields，并在构造器保证 kind 与 payload 匹配。
- queued event 不应只存 `EventName` 字符串。至少要定义第一阶段支持范围：只支持 framework-known event record，或只支持 observation/report，不支持任意事件重放。
- spawn captured-id 语义必须像 Unity ECB 一样明确 placeholder/reserved id 的作用域：是否只允许同一 command buffer 后续命令引用，是否允许调用方拿去 `EntityManager.Get`，playback 后如何 remap/report。

#### 4. P4 把 CommandBuffer、phase、RuntimeWorld 整合、IRuntimeSystem rename、ProcessRecord 合并塞进一个 change，风险过大

P4 同时做语义引入和命名重构。`IRuntimeSystem -> IRuntimeProcess` 是术语治理，不是 RuntimeCommandBuffer 的必要条件。当前 P4 内部还出现了 `ProcessRecord` 语义不一致：proposal 说 `GetRuntimeInfo()` 直接返回 `IReadOnlyList<ProcessRecord>`，tasks 又要求 `ProcessRuntimeInfo` 持有 `ProcessRecord + runtime state`。

建议修改：

- 拆成 `P4a: RuntimeCommandBuffer + SchedulePhase + guard` 和 `P4b: Schedule terminology rename`。
- P4a 不要重命名 `IRuntimeSystem`，先把 buffer 行为落地并验证。
- P4b 只有在证据显示 `IRuntimeSystem` 名称继续误导 AI 时再做。当前 `clarify-gameos-terminology` change 甚至写过“代码级 rename 需要单独 migration evidence”，且该 change 还未 archive 到 baseline。
- 若要保留 rename，先统一 `ProcessRecord / ProcessRuntimeInfo / ProcessConfig` 的边界：配置是 immutable record，运行态是 runtime info，不要把 runtime mutable state 放进 public record struct 再说它是不可变配置。

#### 5. RuntimeWorld dispose 顺序没有稳定事实，且 P2/P4/P5 三处互相矛盾

P2 tasks 写 `Pools -> Resources -> Lifecycle -> Entities -> Events`。P4 tasks 又说在 `Lifecycle` 与 `Entities` 之间插入 `Schedule` 与 `Commands`。P5 reference 草案写 `Pools -> Resources -> Schedule -> Commands -> Lifecycle -> Entities -> Events`。三者不能同时为真。

建议修改：

- 在 P2 或 P4 前先写一个 dispose invariant 表：哪些 subsystem 在清理时可能发布事件、入队命令、访问 entity、访问 resource、访问 pool。
- 明确 dispose 时 pending commands 是 drain、fail report，还是 discard。现在没有说。
- 建议顺序先以行为不丢证据为原则：停止 schedule/process 产生新工作，决定 Commands drain/discard 策略，销毁 entities/lifecycle 时保持 Events 存活，最后清空 Events。Pools/Resources 具体位置要由 Godot node pool 和 resource catalog 当前引用关系证明，而不是口头固定。

### 🟡 重要 / 需讨论

#### 6. P2 的 `EntityId` 实际是 `string Value` wrapper，不是摘要中的 `ulong Value`

用户摘要写 `readonly record struct EntityId(ulong Value)`，实际 proposal/tasks 是 `EntityId(string Value)`。这不是小差异：`string wrapper` 只解决编译期类型误传，不解决 generation/stale handle，也不改善存储紧凑性。这个取舍可以接受，但必须明说。

建议修改：

- 在 P2 proposal 中明确 “本轮只做 typed string boundary，不做 generational handle”。
- 删除或弱化 “Bevy/Unity typed handle” 的类比，避免让读者以为已经获得 generation stale safety。
- 如果长期想要 `ulong` 或 `(index, generation)`，应新增 future requirement，不要混在本轮。

#### 7. P1/P2 现在制造了可避免的重复迁移

P1 引入 `EntityIdList(IReadOnlyList<string>)`，P2 立刻改为 `EntityIdList(IReadOnlyList<EntityId>)`。P1 新增 static `LifecycleTree`，P2 立刻拆成 `RuntimeWorld.Default.Lifecycle` 的实例 facade。这个重复能降低单 change 爆炸，但对 AI 执行来说会增加两轮全仓 rewrite。

建议修改：

- 首选拆出 `P0/P2a EntityId typed value`，然后 P1 直接用 typed `EntityId`。
- 如果不拆，P1 tasks 应显式标记临时类型，避免 docs/skills 在 P1 archive 后把 `EntityIdList<string>` 当长期事实。

#### 8. P1 把 owner-side list 一致性压给各 Capability，但缺少 destroy 回收机制

P1 spec 要求 “child destroy 后 owner-side list must update”，但 LifecycleTree 不改业务 DataKey，EntityManager 也不知道具体 Capability 的 owner lists。仅靠各 Capability 手动维护，很容易出现 projectile/effect 被 `EntityManager.Destroy` 或 lifecycle recursive destroy 后 owner list stale。

建议修改：

- 为每个 owned-id-list 定义 owner Capability 的销毁订阅或 cleanup hook，例如 Projectile/Effect 订阅 `EntityDestroyed` 后移除对应 id。
- 增加 “child 被非 Capability 自己销毁” 的测试场景。
- 对 “投射物源 entity 既是业务引用又影响生命周期” 明确双事实规则：`SourceEntity` 只表达伤害归因等业务语义；是否随源销毁由独立 lifecycle attach + destroy policy 决定，不允许从 source 自动推断 lifecycle。

#### 9. P4 的 guard 自动入队会改变现存 API 语义，tasks 中只写“评估”不够

`EntityManager.Spawn` 在 guarded 区域返回 reserved id，注册延迟到 phase playback。现有调用大量假设 spawn 后立即可读 data、立即可 `EntityManager.Get`、立即启动 movement。P4 `tasks.md:75` 只说 “评估 BrotatoLike spawn 调用面是否依赖同步生效”，这个颗粒度太弱。

建议修改：

- 增加强制审计任务：列出所有 `Spawn / Destroy / Attach / Publish` 调用点，标记是否可能处于 event dispatch / lifecycle callback / Godot callback / schedule phase guard 内。
- 在 spec 中明确 guarded `Spawn` 返回类型。如果仍返回 `IEntity`，那就不是 “reserved id”，必须有 deferred entity proxy 的语义；如果只返回 `EntityId`，那需要 breaking API。
- 提供显式 API，如 `SpawnDeferred` 或 `world.Commands.Spawn(...)`，避免调用方看不出同步/延迟差异。自动检测 `IsGuarded` 可以保留，但必须有 observation report 和 test。

#### 10. P3 的“迁回游戏侧”有些事件实际上是“删除死代码”

当前 `WaveStarted/WaveCompleted/GameStart/GameOver/GamePause/GameResume/MouseSelection*` 在框架和游戏源中几乎只有定义，无真实消费者。把它们迁到游戏侧可能制造新的无用 API。与其迁移，不如按使用证据分类：

- `InputUseSkill/InputPreviousSkill/InputNextSkill`：真实使用，迁移。
- `GodotPlayerInputComponent`：真实使用，迁移。
- `GameStarted`：游戏侧已有，框架 `GameStart` 可以删除并改 docs。
- `Wave* / GameOver/Pause/Resume / MouseSelection*`：如果无引用，直接删除或留作游戏侧 backlog，不要自动新增文件。

#### 11. tasks 粒度仍偏大，50 个 checkbox 不等于可执行

P1/P2/P4 的 tasks 多处是 “修改所有调用点”“跑 build 修复编译错误”“覆盖全部 17 个 requirement”。这类任务对 AI 单会话过粗，失败后很难定位回滚边界。

建议修改：

- 每个 change 拆成 3-6 个 implementation batch，每批有明确可构建边界。
- 对大型 rename 增加 “grep inventory artifact”：先输出旧符号清单和目标映射，再改。
- build 修复不要作为一个任务，改为 “先框架编译到 N 类错误 -> 修类型 A -> 修类型 B -> 修 tests -> 修游戏仓”。

#### 12. spec 中很多 “AI MUST reject” 是流程约束，不是 runtime contract

这类条款放在 spec 不是绝对错误，因为 SlimeAI 的 spec 也承载 AI-first 协议。但需要区分：

- Runtime 行为可自动测试：例如 `LifecycleTree.Attach` single-parent、CommandBuffer playback report。
- AI 路由规范靠 review/lint：例如 “AI 提议 X 必须拒绝”。

建议修改：

- 在每个 capability spec 中加 enforcement 类型标记：`Verified by tests`、`Verified by grep/lint`、`Verified by review`。
- AI 行为约束可保留在 spec，但 should link 到 skill/DocsAI，而不是把所有流程细节复制进 runtime spec。

#### 13. P5 有价值，但 reference 数量和计数已漂移

P5 proposal 说 8 个 reference，tasks 说新增 8 个、已有 2 个、总计 9-10 个，spec 列表实际是 11 个。这个漂移恰好证明“skill spec 化”本身也需要更克制。

建议修改：

- P5 最后做是正确的，但先在 P1-P4 的每个 `tasks.md` 顶部放一个短执行纪律段，覆盖 rename pipeline、grep inventory、framework/game boundary、spec-code alignment。
- P5 archive 前统一 reference 清单，以实际文件为准。建议只把 “必须存在的核心入口” spec 化，不 spec 每个参考文件名，或把文件清单生成校验交给 sync 脚本/manifest。

### 🟢 改进建议 / 可选

#### 14. `RuntimeWorld` 不需要引入通用 DI 容器

当前选择 facade 而非 Service Locator/DI 容器是合理的。这个框架目标是 AI 路由稳定，不是插件式 IoC。建议保持 `RuntimeWorld` 显式持有少量 subsystem。需要注意的是，不要让 `RuntimeWorld.Services.Get<T>()` 这种泛型 service locator 混进来。

#### 15. `IRuntimeSystem -> IRuntimeProcess` 不急于代码级 rename

术语层面 `Runtime Process` 更符合 SlimeAI “不是 ECS” 的自我定位。但 Bevy/Unity/Flecs 都使用 `System` 作为主流术语，完全改名会增加外部学习成本。建议先文档映射，代码 rename 等下一轮有证据再做。

#### 16. 缺少 DataKey/DataCatalog 迁移专项

P2 触及 `DataKey<IEntity?> -> DataKey<EntityId?>`，但 DataOS snapshot loader、descriptor default、generated typed snapshot、observation serialization 都可能受影响。本轮没有独立列一个 DataKey/DataCatalog migration change。建议至少在 P2 加一组 DataOS validator tasks，验证 snapshot manifest、descriptor default 和 loader reconstruction。

#### 17. GodotBridge 抽象与对象池可能需要后续专项

P3/P4 都碰到 GodotBridge，但没有系统解决 bridge adapter ownership、node instantiate/free command 的边界、pool clear 与 entity/resource 生命周期关系。建议 P4 前加一个小 spike：只画 GodotBridge -> RuntimeWorld -> CommandBuffer 的调用边界，不写代码。

#### 18. 回滚策略应按可独立 revert 的 batch 写

当前 Migration Plan 的 Phase 看起来像逻辑阶段，但不是一定能独立 commit/revert。大 rename 删除旧符号后，回滚会同时跨框架仓、游戏仓、submodule 指针、DocsAI、skills。建议每个 phase 写 “可编译状态 / 可测试状态 / revert 单元”。

## 对评审维度的逐项回答摘要

### A. 战略合理性

- 范围划分覆盖了正确主题，但 P2 应拆成 EntityId typed value 与 RuntimeWorld facade，P4 应拆出 Process rename。
- 当前顺序不是最优，而且与 P2 artifact 冲突。按现有文件，P2 不能先做。
- P5 最后做是正确的，但应给 P1-P4 加临时执行纪律，不要等 P5 才有 rename/boundary 指导。
- 遗漏项：DataKey/DataCatalog 迁移验证、GodotBridge command boundary、对象池/资源/实体 dispose invariant、测试 fixture 统一细化。

### B. 技术深度

- `RuntimeWorld.Default + CreateScoped()` 方向稳，但 dispose 顺序不稳，且 schedule/commands 策略缺失。
- 8 种 command kind 作为第一阶段合理，但 `PayloadKey/PayloadValue` 不合理，应换 typed payload。
- `using var guard` 比调用栈检测/attribute 更可审计，建议保留显式 guard，但不要让原同步 API 静默变成 deferred。
- lifecycle 与 business reference 可严格分离，但需要双事实场景：同一对 entity 可以同时有 `SourceEntity` 和 lifecycle attach，二者语义不同。
- `IRuntimeProcess` 有 AI-first 优势，但代码级 rename 的收益低于成本，建议延后或拆分。

### C. AI 可执行性

- 27-53 个 task 对单会话偏大，尤其 P1/P2/P4。需要 batch 化。
- tasks 中 “所有调用点”“全部 17 个 requirement” 仍过粗。
- proposal/design/spec/tasks 信息充分但重复较多，且存在 P2/P4/P5 内部不一致。
- P5 references 按需加载可以控制 token，但当前数量/计数漂移，说明需要 manifest 或减少强制清单。

### D. 风险与回滚

- `string EntityId -> EntityId` 回滚成本高，建议先做 typed wrapper inventory 或双阶段迁移。
- P3 会破坏 smoke 的主要风险在 input component 和 input events，非实际使用的 global events 可以删而非迁。
- P4 的 captured-id 语义是最大行为风险。必须先用测试证明现有调用不依赖即时注册，或显式 API 改名。
- Migration Plan 的 phase 不等于独立 rollback 单元，应补 “可编译点”。

### E. 可验证性

- `run-build.sh 修复编译错误` 太乐观，应拆错误类型和调用面 inventory。
- spec scenarios 多数有 2+ 场景，但自动化覆盖差异很大。需要标注 test/grep/review。
- P3 的功能等价应以 Godot smoke、input probe、Ability trigger、eventbus dump 证明。对于未使用事件，等价证明应是 “删除前后无引用 + build/smoke pass”。

### F. 外部成熟方案对照

- Bevy：本计划借鉴 deferred command 和 world/schedule 是合理的，但 Bevy command 扩展是 typed command 行为，不是 string payload。
- Flecs：P1 拒绝通用 relationship graph 是合理的，因为 SlimeAI 明确不是 ECS/query DSL。Flecs relationship 很强，但会把 AI 路由重新带回 pair/wildcard/query 心智。
- Unity Entities：P4 应借鉴 ECB 的 placeholder entity 作用域和 deterministic playback，尤其不能让 reserved id 在 buffer 外被误用。
- Unreal GAS：SlimeAI 的 Ability/Feature/Effect 分层接近 GAS 的 ability/effect/attribute 分工，但本轮 Runtime 重构不要把 GAS 的复杂 replication/prediction/task 模型提前引入。

### G. 文档与协议

- OpenSpec 写法基本符合项目现有规范，strict validate 通过。
- “AI 提议 X 必须拒绝” 可以在 AI-first spec 中存在，但应标注 review/lint enforcement，并链接 skill。
- P5 不算完全过度工程，因为 skill 漂移是项目事实风险；但当前 spec 化粒度过细，文件清单已漂移，应收敛。

## 是否建议进入实施

建议：**修改后部分实施**。

推荐调整后的实施路线：

1. `P0` 修正计划骨架：修 ProjectState 顺序、修 P3 MouseSelection 反向依赖、修 P4 command payload、统一 P5 reference 数量、明确 dispose invariant。
2. `P2a` typed EntityId wrapper 先行，或如果不拆则先做 P1。
3. `P1` lifecycle/business split，直接使用最终 EntityId 类型更好。
4. `P3` 删除/迁移真实使用的 game leakage，非使用 event 直接删除，不制造游戏侧空 API。
5. `P2b` RuntimeWorld facade + CreateScoped 测试隔离。
6. `P4a` CommandBuffer + SchedulePhase + explicit guard，不做 Process rename。
7. `P4b` 仅在必要时做 `IRuntimeSystem -> IRuntimeProcess` rename。
8. `P5` 最后 archive 后固化 skill，保留按需 reference，但降低 spec 化清单刚性。

## 对原始用户请求的回应

这份计划 **大体回应了用户原始意图**：它确实把 Entity 标识弱类型、Relationship 与业务引用混淆、框架事件泄漏游戏逻辑、CommandBuffer 缺失、skill baseline 漂移放进了 OpenSpec skeleton，并且没有直接改代码。

但它也有明显走偏：

- P4 把术语 rename 与 CommandBuffer 绑在一起，偏向“命名洁癖式大重构”，会稀释真正要落地的结构变更语义。
- P3 试图处理不存在的 MouseSelection capability，并提出框架引用游戏 namespace 的过渡状态，这是边界方向错误。
- P5 的 meta-level 管理有价值，但当前文件清单和段落骨架 spec 化过重，且已经出现计数漂移。
- P2/P1 顺序在文档和 artifact 之间冲突，说明计划尚未达到“AI 可直接按顺序执行”的质量门槛。

最终判断：**目标没有跑偏，执行骨架还没到可实施质量**。先修上述阻塞点，再分批实施。

## 外部资料

- Bevy `Commands`: <https://docs.rs/bevy/latest/bevy/prelude/struct.Commands.html>
- Unity Entities `EntityCommandBuffer`: <https://docs.unity.cn/Packages/com.unity.entities%400.51/manual/entity_command_buffer.html>
- Flecs Relationships: <https://www.flecs.dev/flecs/md_docs_2Relationships.html>
- Flecs deferred commands: <https://www.flecs.dev/flecs/group__commands.html>
- Unreal Gameplay Ability: <https://dev.epicgames.com/documentation/unreal-engine/using-gameplay-abilities-in-unreal-engine>
