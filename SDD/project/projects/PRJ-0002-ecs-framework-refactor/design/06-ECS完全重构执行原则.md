# ECS 完全重构执行原则

> 更新：2026-05-31  
> 状态：current，PRJ-0002 后续 hard cutover / 无兼容重构的项目级原则。  
> 来源：Data 系统 SDD-0012~SDD-0022 执行复盘、`2.Data系统优化/04~06`、`DocsAI/ECS/Data/Data系统说明.md`、当前源码 grep gate。  
> 用途：后续 Entity / Relationship / Event / Data 补丁进入执行前必须先读本文，避免“口头完全重构，实际兼容迁移”。

## 1. 结论

Data 这次不是设计方向错了，而是“完全重构”的执行定义不够硬。

早期设计已经反复写清楚：

```text
DataOS SQLite authoring
  -> runtime_snapshot.json descriptors / records
  -> generated typed handle
  -> catalog-bound Data
  -> business callsites
```

但执行过程把很多旧入口当作“暂时能跑的过渡形态”留了下来。直到 SDD-0021 前，完成标准主要证明了主链路能跑，没有证明旧链路已经失去进入运行时、测试和文档推荐面的能力。结果是旧写法继续从以下缝隙进入：

- generated `DataKey.Xxx` compatibility alias。
- `DataKey<T> -> string` 隐式转换。
- `Data` public string-key API。
- record field type 由 generator hardcode，和 descriptor 形成第二事实源。
- validator 检查中间层，不检查最终 `runtime_snapshot.json`。
- 旧 DocsAI / DocsNew / roadmap 中曾有“已完成旧路径退出”的过早结论或旧写法说明；当前 `DocsAI/` 已重新作为 SlimeAI 框架统一文档入口，必须纳入 hard cutover 门禁。

因此，后续所有标记为“完全重构 / hard cutover / 无兼容”的任务，完成标准必须从“新主链路可用”升级为：

```text
新主链路可用
旧入口不可编译 / 不可运行 / 不可被测试复用 / 不在 current 文档中被推荐
最终交付物有红灯门禁证明
```

## 2. Data 为什么还是兼容了

### 2.1 “完整重构”被写成最终状态，不是每个切片的硬约束

证据：

- `2.Data系统优化/03-完全重构范围与TDD测试计划.md` 明确写“不是兼容迁移，而是完整重构”，并要求 `DataOS descriptor / runtime_snapshot.json.descriptors` 成为单一字段定义事实源。
- `data-rewrite-execution-prompt.md` 也写了“禁止兼容层”，但同时把旧路径删除放到 SDD-0019。
- SDD-0012~SDD-0018 多个切片的边界写着“不删除旧路径”“最终由 SDD-0019 删除”。

推论：

这些表述对顺序执行是合理的，但缺少“临时旧入口账本”。旧入口被延期时，没有强制记录：

```text
旧入口是什么
为什么短期保留
哪个 SDD 必删
对应 grep gate 是什么
如果未删，当前 SDD 不能 done
```

结果是每个切片都可以认为“我这里不删，后面删”，后面切片又只删除了最显眼的旧目录，没有删除全部语义兼容口。

### 2.2 删除清单偏向目录和类名，漏掉了语义兼容口

证据：

- SDD-0019 的任务重点是删除 `SlimeAI/Data/Data`、`DataNew`、旧 Data 测试、`DataMeta/DataRegistry` 定义事实源。
- SDD-0020 的任务重点是 RuntimeTables、DataTable 反射、Entity config 推断、DataRegistry/DataMeta fallback。
- 运行报错后，`06-无兼容完全重构总审计/README.md` 才把 `DataKey<T>` 隐式 string、generated alias、非标量类型降级、public string-key API、final snapshot mismatch、`legacyTable/legacyData` 这些列成 P0/P1。

推论：

执行者删除了“看得见的旧系统”，但没有系统审计“旧系统的能力是否仍可达”。兼容残留往往不是一个旧目录，而是编译器隐式转换、public overload、generator alias、validator 漏层和文档推荐入口。

### 2.3 验收验证证明的是主链路成功，不是旧链路死亡

证据：

- `05-Data重构运行报错根因分析.md` 记录 `validate-dataos.sh` passed，但 `runtime_snapshot.json` 中 `AbilityIcon` record type 和 descriptor type 不一致。
- `06-无兼容完全重构总审计/README.md` 明确指出 validator 检查对象不是最终生成物。
- SDD-0020 完成后仍需要 SDD-0021 补 final snapshot mismatch gate、generated handle gate、implicit string gate。

推论：

构建通过、DataOS validate 通过、场景 smoke 通过，都不足以证明“无兼容”。无兼容必须有反向门禁：旧入口搜索结果为 0，或者只出现在 history / audit / gate 文档中。

### 2.4 “保留旧 ECS 主线”与“删除旧 API”没有被明确区分

证据：

- 项目主设计要求保留旧 ECS 主线结构，不做整体替换。
- Data 子系统又被裁决为完整重构。
- 后续 Entity / Relationship 也被裁决为 hard cutover。

推论：

如果文档只写“保留旧 ECS”，执行时很容易变成“保留旧 ECS 的 API 形状”。正确边界应是：

```text
保留概念：Entity / Component / Data / Event / System / Relationship
不自动保留旧实现：string key / static facade / fallback / relationship string graph / config object inference
```

概念可以保留，旧 API 不能因此获得兼容豁免。

### 2.5 current 文档和历史文档的边界不够硬

证据：

- `design/INDEX.md` 中 `01-Data系统问题分析.md` 标为 current，但 notes 写“历史入口”。
- `06-无兼容完全重构总审计/README.md` 指出 roadmap / ProjectState / DocsNew 曾继续写旧路径已退出或兼容入口可见。
- 当前 `DocsAI/` 是框架统一文档入口；必须防止 `DocsAI/ECS/**`、SDD current 文档和 owner skill 继续推荐旧写法。`Src/ECS` 不再保存框架 Markdown 文档。

推论：

对 AI 来说，`current` 比“历史入口”四个字更强。只要旧文档还在 current 索引里，或者 current 文档中保留旧用法示例，后续执行就会把它当成可用事实。

## 3. 后续完全重构的硬原则

### P1：先定义“保留概念”和“删除实现”

每个重构设计必须先写两张表：

| 类别 | 必须保留 | 必须删除 |
| --- | --- | --- |
| 概念层 | 例如 Entity 生命周期、Data 字段、Event 通知 | 不适用 |
| API 层 | 新 typed handle / explicit record / scoped service | 旧 static facade、string key、implicit conversion、fallback overload |
| 数据层 | 新唯一事实源 | 旧手写表、旧配置对象、旧 snapshot mirror |
| 文档层 | current 新路径 | current 旧示例、模糊“迁移期可用” |

没有这张表，不允许进入执行。

### P2：禁止“未命名的临时兼容”

hard cutover 中如果短期必须保留旧入口，只能以 temporary exception 存在，并必须同时写：

```text
Exception ID:
旧入口:
保留原因:
允许调用范围:
删除任务:
删除 grep gate:
过期 SDD / 日期:
未删除时是否阻塞 done: yes
```

没有 exception ID 的兼容入口，默认就是 bug。

### P3：完成标准必须包含 kill gates

每个 hard cutover SDD 的 `tasks.md` 和 `execution-prompt.md` 必须包含三类门禁：

1. **正向验证**：新主链路 build / test / scene 通过。
2. **反向验证**：旧入口 grep gate 为 0 或命中白名单明确。
3. **最终交付物验证**：检查 runtime 最终生成物，而不是只检查中间表、fixture 或局部单测。

Data 的教训是：只检查 `dataos_runtime_field_stream` 不够，必须检查 `runtime_snapshot.json`。同理：

- Entity 重构不能只检查 `EntityId` 类型存在，必须检查旧 `EntityRelationshipManager` / string relation / static facade 不再可达。
- Event 重构不能只检查新 event key 可用，必须检查旧 const string / payload-as-key / untyped subscribe 不再作为业务入口。
- Relationship 重构不能只检查新 LifecycleTree 可用，必须检查旧关系图不再承担业务归属或清理职责。

### P4：让错误在生成/编译阶段变红

完全重构不能依赖运行时才发现旧用法。优先级应是：

```text
generator validator 红灯
compile-time type error
unit / scene test 红灯
runtime smoke error
```

Data 的 `DataKey<T> -> string` 隐式转换把本应编译失败的错误拖到运行期，是典型反例。后续重构应主动删除隐式转换、宽泛 overload、object 参数、string fallback，让旧调用点尽早编译失败。

### P5：文档同步不是收尾，是门禁

current 文档必须和代码门禁一起验收。hard cutover 完成时，以下文档不能留旧入口推荐：

- PRJ design index / roadmap / progress。
- 当前系统 `DocsAI/ECS/**` 说明。
- SDD current 文档、owner skill 和 `DocsAI/ECS/**` 模块文档。
- skill / rule / execution prompt。
- 测试 README 和示例代码。

如果旧文档需要保留引用，必须标为 `historical-input` / `archived` / `rejected`，并写明“不可作为当前实现入口”。

### P6：测试要重写旧行为，不是修补旧测试

旧测试如果验证旧行为，保留它就等于保留旧 API。hard cutover 需要：

```text
旧测试验证旧行为 -> 删除或改成历史 fixture
新测试验证新契约 -> RED/GREEN/REFACTOR
grep gate 验证旧测试 helper 不再复活旧入口
```

Data 的旧 `new Data()` / `DataKey.Xxx` / `DataMeta` 测试辅助就是风险来源。后续 Entity / Relationship 测试不能继续用旧 string relationship helper 证明新系统正确。

## 4. SDD 模板补充要求

后续 hard cutover SDD 的 `tasks.md` 至少应包含这些任务组：

1. **Readiness baseline**：列出旧入口命中、旧文档命中、旧测试命中、最终生成物/运行物命中。
2. **Target contract**：写明唯一事实源、唯一 public API、唯一初始化/生命周期路径。
3. **Compile breaker**：删除隐式转换、fallback overload、object/string 弱类型入口，让旧调用点变红。
4. **Callsite migration**：按 owner 模块迁移调用点，不通过 facade 包旧 API。
5. **Legacy removal**：删除旧 runtime/source/test/docs 推荐面。
6. **Kill gates**：grep / jq / script / analyzer / scene gate，新鲜执行并写入 progress。
7. **Docs and skills**：同步 current 文档、相关 skill/rule 和源码旁 README。
8. **Done guard**：如果 kill gate 未清零，只能 blocked，不能 done。

## 5. Entity / Relationship 下一步特别注意

Entity / Relationship 已经裁决为 hard cutover，因此不能重复 Data 的错误：

- 不能把旧 `EntityManager` 做成长期兼容 facade。
- 不能保留旧 `EntityRelationshipManager` 作为新 LifecycleTree 的 fallback。
- 不能让 string relationship type 和 typed reference 并存为双事实源。
- 不能让业务归属、UI 绑定、伤害归因继续从旧关系图推断。
- 不能让测试为了省事继续创建旧 relationship graph。
- 不能在 current 文档里同时推荐旧 Relationship API 和新 LifecycleTree API。

推荐完成门禁示例：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
rg -n "EntityRelationshipManager|EntityRelationshipType|ParentRelationTypes|BindParentRelationships|RelationshipType|relationshipType" Src/ECS Data DocsAI
rg -n "GetRelation|GetChildren\\(|GetParent\\(|AddRelationship|RemoveRelationship" Src/ECS Data
rg -n "兼容 facade|fallback|legacy relationship|旧 Relationship.*当前入口" DocsAI SDD/project/projects/PRJ-0002-ecs-framework-refactor -g "*.md"
```

允许命中只能是历史文档、审计文档、grep gate 本身或明确标记为不可执行入口的迁移说明。

## 6. 执行者一句话

不要把 hard cutover 理解成“先让新系统跑起来，再慢慢删旧系统”。

hard cutover 的最小闭环是：

```text
新系统跑起来
旧入口进不来
旧文档不推荐
最终交付物被验证
未完成项不能标 done
```

这五件事必须同时成立。
