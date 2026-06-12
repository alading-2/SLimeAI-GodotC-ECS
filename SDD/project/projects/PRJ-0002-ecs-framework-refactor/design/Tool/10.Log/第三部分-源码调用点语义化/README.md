# 第三部分：源码调用点语义化

> 更新：2026-06-11
> 状态：draft direction，未实现，方向未冻结
> 作用：回答“为什么运行游戏时打印仍然分离”，并把后续 `Src/ECS` 调用点迁移定义成可确认、可审查的大任务。

## 用户原始问题

> 我看打印还是分离的，原因是不是 src/代码还没有改 log 的形式，这个都没有完成你说已经改完了？

这个判断成立。之前把 analyzer 默认入口完成说成“Log 改完了”是不准确的。当前只能说：

- 记录层完成了第一版：`LogEntry`、sink、profile、budget、`OperationTrace`、`ValidationSession` 已存在。
- 离线整理层完成了第一版：`logctl analyze` 默认输出 flow conclusion、success template、failure-first digest，不再默认 raw 分桶。
- 源码调用点语义化没有完成：大量 `Src/ECS` 业务、测试、调试调用点仍按“逐行文本消息”写日志，所以 live stdout / 新 run 仍可能看起来分离。

## 真实问题

这不是“再加几个字段”能解决的问题。真实问题是 Log 被拆成三层以后，第三层还没有按 AI-first 方式迁移：

| 层 | 解决的问题 | 当前状态 | 用户看到的影响 |
| --- | --- | --- | --- |
| 记录层 | 一条日志怎么成为结构化事实 | 已有基础实现 | raw JSONL 能写，但不代表语义好 |
| 离线整理层 | 已产生的 raw 怎么提炼成 AI 默认入口 | analyzer 默认入口已修正 | 旧 run 能被压缩成摘要、模板和 flow conclusion |
| 源码调用点语义化 | 源码到底应该在什么时机、以什么粒度写什么事实 | 未完成 | 运行游戏时仍能看到分散 `_log.Info`、测试说明、逐步成功文本 |

如果只做前两层，AI 能更好地分析已有日志，但不能保证新运行时的现场打印已经变成“一个 flow 一条结论”。这就是当前偏差。

## 本地证据

2026-06-11 在 `Src/ECS` 粗略扫描：

```text
_log.Trace/Debug/Info/Success/Warn/Error/Validation 命中约 547 处
GD.Print / GD.Push* / Console.WriteLine / PrintRich 命中约 5 处
BeginTrace / CompleteTrace / OperationTrace 命中约 13 处
```

这组数字不是最终缺陷数量，因为里面包含测试、Logger 自身 sink 和少量注释；但它足以证明：源码调用点迁移还不是“收尾”，而是一个独立的大阶段。

当前高风险路径包括：

| 路径 | 现象 | 后续判断 |
| --- | --- | --- |
| `Src/ECS/Test/GlobalTest/MainTest/MainTest.cs` | 操作说明、步骤、生成成功逐行 `_log.Info` | 应改为测试准备 flow / Validation artifact / debug-only instructions |
| `Src/ECS/Runtime/Tests/ECSTest/ECSTest.cs` | 仍有 `GD.Print(msg)` | 测试事实应迁到 `ValidationSession` 或 artifact |
| `Src/ECS/Runtime/System/SystemManager.cs` | 启动和状态报告逐系统多行 `_log.Info` | 应输出 `SystemStartup` / `SystemStatusSnapshot` summary |
| `Src/ECS/Capabilities/Ability/**` | 仍有普通 debug/info 与部分 trace 混用 | 应围绕 `AbilityCast` / `AbilityTrigger` 固定 flow 字段 |
| `Src/ECS/Capabilities/TestSystem/**` | Debug UI 操作逐行 info | 应归入 debug profile，默认不进入 AI live stdout |
| `Src/ECS/Tools/TargetSelector` / `ObjectPool` | 已有预算和 analyzer 模板，但运行时窗口 aggregate 仍可加强 | 高频成功路径应优先 summary / template / sample |

## 为什么之前会误判

之前的错误不是代码完全没做，而是完成边界说错了：

1. **把 analyzer 成功当成整体 Log 成功**：`analysis/summary.md` 变短，不等于源码以后不再分散打印。
2. **把少量 owner trace 当成全局迁移**：Ability、Damage、TargetSelector、ObjectPool 等有第一批 `OperationTrace`，但大量普通 `_log.Info` 还在。
3. **验收没有包含 live run 形态**：之前主要验证 `logctl analyze` 产物和构建，没有以“用户运行游戏后 stdout / 新 analysis 是否符合 AI-first”为完成门禁。
4. **设计文档没有显眼区分三层状态**：第一部分、第二部分写了记录和整理，但入口没有明确告诉后续 AI“源码调用点迁移未完成”。

这个偏差必须写进文档和 SDD，否则下一轮 AI 仍可能从“analyzer DONE”恢复，并继续错误声称 Log 已完成。

## AI-first 的目标形态

源码调用点语义化不是让日志更少，也不是让每条日志都变成更长 JSON。目标是让一次业务动作有一个 AI 能直接判断的结论：

```text
用户释放技能
  -> AbilityCast flow conclusion
     outcome=Succeeded/Failed/Skipped
     failedStep=...
     reasonCode=...
     keyFields={caster, abilityId, target, cost, cooldown, spawnedEffects}
     rawRef=...
```

AI 默认应该先看到“这次释放成功了吗、失败在哪一步、证据在哪”，而不是看到十几条自然语言顺序日志。

### Live stdout 草案

默认 live stdout 只显示：

- `Warn` / `Error` / `Fatal`
- Validation 最终 verdict
- 关键 flow completion summary
- run / scene / system startup 的短 summary
- budget suppressed summary

默认 live stdout 不显示：

- 普通 `_log.Info` 步骤说明
- 每次成功查询、成功绑定、成功租借、成功释放
- Test UI 点击说明
- 每帧或高频状态
- 用 message 承载的字段串

完整细节写 JSONL / artifact；人工调试时通过 debug profile 打开 owner / context，而不是把默认输出长期放开。

## 设计选项

### 方案 A：只收紧 stdout 策略

做法：保持大多数调用点不变，仅把 `StdoutSummarySink` 或 profile 改得更严格。

优点：改动小，能快速减少屏幕输出。

问题：这只能“少显示”，不能让日志更语义化。raw 里仍然是分散消息，AI 需要靠 analyzer 猜语义。它可以作为临时止血，但不能作为最终 AI-first 方案。

### 方案 B：按 owner 做源码调用点语义化迁移

做法：先冻结 live stdout policy 和 owner flow contract，再按 owner 分批迁移调用点。

优点：能把“释放技能、生成怪物、系统启动、目标查询、对象池租还、验证场景”改成稳定语义结论，符合用户要的 AI debug 目标。

代价：这是大任务，需要设计门禁、迁移顺序、新 run 验证和回归检查。

推荐采用这个方案。

### 方案 C：重写 Log API，禁止普通 `_log.Info`

做法：新增更强类型的 API，强制所有调用点走 flow/event/validation 分类。

优点：长期最干净。

问题：当前方向和调用点语义尚未冻结，直接重写 API 风险过大；而且会把大量 owner 行为判断压进底层 Logger，反而可能模糊边界。可以作为后续演进，不作为下一步默认方案。

## 推荐迁移路线

### T3.0 方向冻结

先确认三个问题：

- 默认 live stdout 是否严格到只显示 warn/error/validation/flow summary。
- 第一批迁移是否覆盖所有 live 可见路径，还是先覆盖主链路和最吵 owner。
- 测试说明、Debug UI 操作、人工引导文本是否默认转入 artifact/debug profile，而不是 stdout。

未确认前不进入大规模源码修改。

### T3.1 调用点盘点

把当前 `_log.*`、`GD.Print`、`Console.WriteLine` 调用点按 owner 分类：

| 分类 | 处理方向 |
| --- | --- |
| 流程型 | 改为 `OperationTrace` 或 flow summary |
| 验证型 | 改为 `ValidationSession` / artifact |
| 高频成功型 | 改为 window aggregate / success template / sample |
| 启动快照型 | 改为一次 summary / diagnostics snapshot |
| Debug UI 型 | 默认 debug profile，不进 AI live stdout |
| 真异常型 | 保留 warn/error，但补 `reasonCode` 和关键字段 |

盘点产物应该是 owner 清单，不是简单 grep 列表。

### T3.2 Owner flow contract

每个第一批 owner 必须先写或更新 README `## Log`：

| Owner | 必须定义的 flow / summary |
| --- | --- |
| Runtime/System | `SystemStartup`、`SystemStatusSnapshot`、`SystemLoadFailure` |
| Ability | `AbilityCast`、`AbilityTrigger`、`AbilityInventoryChange` |
| Spawn | `WaveSpawn`、`EntitySpawnBatch`、`SpawnFailure` |
| TargetSelector | `TargetQuerySummary`、异常 query detail |
| ObjectPool | `PoolAcquireSummary`、`PoolReleaseSummary`、异常 detail |
| Test / Validation | `ValidationSceneRun`、`CheckResult` |
| TestSystem / Debug UI | `DebugAction`，默认 debug profile |

没有 owner contract 的调用点不进入大规模实现。

### T3.3 第一批源码迁移

优先迁移实际影响用户运行观感和 AI debug 的路径：

1. `MainTest` / `ECSTest`：测试说明、PASS/FAIL、`GD.Print`。
2. `SystemManager`：启动和状态报告。
3. `Ability` + `Spawn`：释放技能、生成实体、波次流程。
4. `TargetSelector` + `ObjectPool`：高频成功路径运行时聚合。
5. `TestSystem` / Debug UI：默认 debug profile。

迁移原则：

- 不把所有 `_log.Info` 机械替换成 `BeginTrace`。
- 单次操作内多条日志合成一个 flow conclusion。
- 成功路径默认 summary，失败路径保留 detail。
- 字段进入 `fields`，message 只保留短人类摘要。
- 测试断言进入 `ValidationSession`，不再靠文本 pattern。

### T3.4 新 run 验收

必须用用户实际运行或 Godot runner 产生的新 run 验证，而不是只用旧样本：

- live stdout 默认行数和内容符合 policy。
- `analysis/summary.md` 第一屏能判断 run 状态。
- `flows/flows.jsonl` 中能看到关键业务 flow conclusion。
- `noise/templates.jsonl` 能聚合高频成功路径。
- `missing-fields/index.md` 不再把第一批 owner 标为严重缺字段。
- 没有 Validation artifact 时仍不能报告 `passed`。

## Definition of Done 草案

第三部分不能用“构建通过”或“grep 少了几行”证明完成。完成条件至少是：

- `Src/ECS` 直接 `GD.Print` / `Console.WriteLine` 只剩 Logger sink 或明确注释示例，测试事实不再用直接打印。
- 第一批 owner 的普通流程日志不再默认刷 live stdout。
- Ability / Spawn / System / TargetSelector / ObjectPool / Validation 至少各有一个稳定 flow 或 summary contract。
- 新 run 的 live stdout 默认只包含 warn/error、validation、flow summary 和 run summary。
- `logctl analyze` 对新 run 的默认可读入口仍小于 raw，并能按 flow 判断成功、失败、跳过和缺字段。
- owner README / `Log.md` 与源码调用点一致，不能只有文档没有实现。

## 必须确认

### 思路问题

1. **默认 stdout 要不要严格收口？**
   - 为什么问：如果默认仍显示普通 info，用户运行时仍会觉得分离。
   - 推荐默认：严格收口，只显示 warn/error/validation/flow summary/run summary。

2. **第三部分是继续放在 SDD-0040，还是新建 SDD？**
   - 为什么问：它和现有 Log hard cutover 同源，但范围已经大到跨 owner 源码迁移。
   - 推荐默认：继续放在 SDD-0040 的 T3，因为它是前两部分未完成的同一目标；如果后续规模失控，再拆新 SDD。

### 信息缺口

1. **需要一份新运行日志样本。**
   - 为什么问：旧样本能证明 analyzer 修正，不能证明当前源码运行效果。
   - 默认处理：先按本地静态盘点设计，不声称 live 效果通过。

2. **用户最关心的 live 场景是哪一个？**
   - 为什么问：第一批迁移应围绕真实游戏运行，而不是按 grep 数量排序。
   - 推荐默认：以 `MainTest` / 主场景启动 / 释放技能 / 生成怪物为第一验收链路。

### 决策未定

1. **Debug UI / TestSystem 的用户操作日志默认是否可见？**
   - 为什么问：这些日志对人工调试有用，但会污染 AI 默认上下文。
   - 推荐默认：默认不进 AI live stdout，debug profile 打开。

2. **是否允许短期保留部分 `_log.Info`？**
   - 为什么问：全仓一次改完风险大。
   - 推荐默认：允许保留低频、非默认 stdout 的 info；但 live 可见路径和第一批 owner 必须完成语义化。

