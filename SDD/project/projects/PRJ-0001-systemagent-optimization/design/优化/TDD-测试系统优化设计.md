# TDD 与测试系统优化设计

> 来源：用户纠正脱离 Godot 运行语义的测试框架方向后的 SystemAgent 深度分析
> 日期：2026-06-12
> 优先级：P0

## 用户原始问题

用户指出：脱离 Godot 运行语义的测试框架方向错了。SlimeAI 是 Godot C# 项目，Runtime/Data 也要在 Godot 运行环境、资源路径、Node 生命周期、Log / Validation artifact 语义下验证；不能为了追求“纯代码快速测试”硬写一套不可控测试。TDD/Test 最重要的是先和用户确认行为、标准答案和试点范围，再写测试。

## 真实问题

用户判断成立。旧方案把“测试跑得快”当成了主目标，把“能证明真实行为”放到了次要位置。

当前真正的问题不是“缺某个测试框架”，而是：

1. **测试设计先行不足**：AI 容易直接写测试代码，但没有先确认行为、标准答案、输入、观察点、失败标准和 artifact 路径。
2. **测试证据不稳定**：旧测试里仍有 `PASS/FAIL` 文本、分散 Log、退出码和人工判断混用。
3. **ValidationSession 没有成为默认断言载体**：结构化 artifact 才适合 AI 审查 RED/GREEN，而不是 stdout。
4. **BDD 与设计文档位置割裂**：BDD 写在 SDD 里，常常变成任务归档文档，没有跟设计变更实时同步。
5. **TDD 被流程化口号化**：强调 RED/GREEN，但没有把“先和用户确认要测什么”作为写测试前的门槛。

因此，脱离 Godot 运行语义的新测试框架不应作为当前方向。后续如有极小工具库确实脱离 Godot，也必须单独论证，不作为 SystemAgent TDD 默认路线。

## 新定位

TDD/Test 在 SlimeAI 里不是“先选测试框架”，而是先固定四件事：

1. **行为**：这个功能到底应该发生什么。
2. **标准答案**：输入、观察、通过条件、失败条件、artifact 路径是什么。
3. **运行环境**：是否必须通过 Godot headless scene、DataOS validator、build 或 logctl 分析证明。
4. **证据形态**：RED/GREEN 是否有同一个 check 的结构化 artifact 支撑。

测试代码只是这些标准答案的实现，不是需求分析的替代品。

## Data 试点方向

用户已指定先从 `Src/ECS/Runtime/Data` 试点。新的试点不新增测试框架，而是选一个 Data 行为，在 Godot headless scene 中验证。

推荐试点范围：

- 只选 1 个 Data 行为，例如 typed `DataKey<T>` 写入读取、range policy、snapshot record apply 或 modifier effective value。
- 先写设计旁 BDD / 标准答案，确认 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath`。
- 再改或新增现有 Data Godot 测试场景，让目标 check 先 RED。
- 最小实现后让同一个 check GREEN。
- 不扩到 Entity/Event/System，不重构整套测试目录。

为什么仍选 Data：

- Data 是 Runtime 核心，验证价值高。
- Data 已有 Godot 场景测试入口，可在现有运行方式上试点，不引入新依赖。
- Data 行为和 DataOS / snapshot / generated handle 有强事实源关系，适合验证“标准答案先行”是否有效。

## 测试设计流程

### Step 1：先问行为，不先写测试

AI 必须先把用户需求压成可确认行为：

```text
行为：Data 写入超出 range 的值时应该失败，并保留旧值。
输入：catalog 中有 key=CurrentHp，range=[0,100]，初始值=50，写入值=120。
观察：TrySet 返回 false；CurrentHp 仍为 50；artifact 有 reasonCode=range_violation。
通过：目标 check 为 pass，failureReasons 为空。
失败：写入成功、旧值被覆盖、没有 reasonCode 或 artifact 缺字段。
```

用户确认后才进入测试实现。若用户未确认，只能用明确默认假设推进，并在回复里写清影响。

### Step 2：把标准答案放在设计旁

BDD 不应远离设计文档。新的建议是：重要行为场景优先放在对应设计文档附近，或在设计文档中直接维护“行为验收”小节；SDD 的 `bdd.md` 只引用或摘录当前任务真正要执行的场景。

这样设计变了，BDD 跟着变；SDD 归档后不会变成过期事实源。

### Step 3：写 RED check

RED 的目标是证明 check 能抓住缺失行为。

可接受 RED：

- Godot runner 指向目标 scene failed。
- `result.json` 或 scene artifact 中目标 check 为 fail。
- fail reason 指向目标行为缺失，而不是编译错误、资源路径错误或 runner 环境错误。

不可接受 RED：

- 只有 stderr。
- 只有 exit code 非 0。
- stdout 有 `FAIL` 文本但没有 artifact。
- 测试因为语法错、资源缺失或环境坏而失败。

### Step 4：最小实现后 GREEN

GREEN 必须引用同一个 scene、同一个 artifact、同一个 check。

可接受 GREEN：

- `index.json` / `result.json` 显示 scene passed。
- artifact `status=pass`、`failureReasons=[]`。
- 标准答案五字段非空。
- 目标 check 从 fail 变为 pass。

`无 error`、`exit code 0`、stdout clean 只能作为辅助，不能单独证明通过。

## 测试工具边界

| 工具 | 用途 | 不做什么 |
| --- | --- | --- |
| `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` | 编译门禁 | 不证明行为正确 |
| DataOS validator | descriptor / authoring / snapshot 数据规则 | 不替代 Runtime/Godot 行为测试 |
| Godot headless scene | Runtime / Capability / Godot 生命周期下的行为验证 | 不用裸 stdout 当主证据 |
| `ValidationSession` / artifact | 测试标准答案和 RED/GREEN 主证据 | 不写成自然语言日志堆 |
| `logctl analyze/query` | 运行后证据整理和下钻 | 不替代测试断言 |

## 与 Log 的关系

Log / Validation 是 evidence plane。TDD 不能只看 terminal 输出，应默认读：

- scene runner 的 `index.json`
- per-scene `result.json`
- Validation artifact
- 必要时再用 `logctl analyze` 的 `summary.md / ai-context.md / flows/ / failures/`

Log 第三层“源码调用点语义化”未完成时，测试仍可能有分散 live stdout；这不影响 artifact 作为主证据，但后续 Log 重构要继续收口调用点。

## 与 Code Review 的关系

TDD/Test 产物可以作为 Code Review 的 `evidenceRef`，但不替代读代码。

Code Review 第一问题仍是：功能是否按用户需求、设计和行为场景实现。测试失败暴露实现错误时会影响判定；测试缺失或 artifact 不足通常作为证据缺口记录。

## 实施路径

| 阶段 | 内容 | 边界 |
| --- | --- | --- |
| Phase 1 | 选 Data 1 个行为，写设计旁标准答案 | 必须先给用户看思路 |
| Phase 2 | 在现有 Godot Data 测试场景做 RED check | 不新增测试框架 |
| Phase 3 | 最小实现或测试基础设施调整，让同一 check GREEN | 不扩散到其他 Runtime owner |
| Phase 4 | 总结 Data 试点是否可推广 | 用户确认后再改规则/模板 |
| Phase 5 | 将成功模式推广到 Entity/Event/System 或 Capability | 每个 owner 单独试点 |

## 不做什么

- 不新增脱离 Godot 运行语义的测试框架。
- 不创建新的测试项目来绕开 Godot 场景验证。
- 不把“纯 Runtime”理解成“脱离 Godot 环境”。
- 不硬写测试代码来倒推需求。
- 不把 BDD 写成归档摆设。
- 不用 stdout PASS/FAIL 当主要证据。
