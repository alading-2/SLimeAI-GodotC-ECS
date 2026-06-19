# SlimeAI 采纳与拒绝清单

## Adopt Now

| QFramework 机制 | SlimeAI 落点 | 理由 |
| --- | --- | --- |
| 少规则分层 | Data 进入条件、Feature owner 边界、Command / Event / Query 术语 | SlimeAI 当前文档正确但偏长，需要更短的硬规则。 |
| 共享数据判断 | Data / Profile / Component / System 字段归属审计 | QFramework 的“持久化、跨界面、配置表”可翻译为 SlimeAI 的“共享、表格驱动、可观察治理”。 |
| Command / Query / Event 语义 | typed request/result、owner handler、Service Pipeline、readonly Query facade、typed fact Event | 解决“不能直接拿 Component 改字段，也不能到处 Data.Set”的问题。 |
| ICanXxx 编译期能力接口 | CommandContext / QueryContext / FeatureHandlerContext 的能力限制 | 用编译器减少 AI 和人绕过边界的机会。 |
| RegisterWithInitValue | `Data.SubscribeWithCurrentValue` / UI binding / debug panel | UI 和 Debug 订阅状态时不应手动先读一次再订阅。 |
| Init 清单像架构图 | FeatureManifest / KernelManifest / SystemManifest | 让 AI 一眼看到 owner、handler、event、query、data projection 和验证入口。 |
| IUnRegister 生命周期 token | Event subscription token / Data binding token | 对象池、Node exit tree、feature disable 时统一清理订阅。 |

## Adopt Later

| QFramework 机制 | 条件 | 说明 |
| --- | --- | --- |
| Command queue / middleware | Command dispatcher 原型稳定后 | SlimeAI 可在 Log / Validation 中记录 command trace，但不急着做 undo / replay。 |
| OrEvent | UI binding 需要组合多个 DataChanged / Event 时 | 可作为 UI utility，不进入 gameplay 核心。 |
| Toolkits ActionKit | Godot C# 时序动作需求明确后 | 适合单独 SDD 研究，不和核心 Architecture 绑定。 |
| Toolkits UIKit | SlimeAI UI stack 需求明确后 | 当前 UI 先按 Godot 场景和现有 UI owner 处理。 |
| CodeGenKit 思路 | Godot scene binding 代码重复明显后 | 可转为 Godot partial binding generator，但不复制 Unity 代码。 |
| 小型 IOC registry | Feature 内部存在多实现服务查找需求时 | 只做 owner 内 registry，不做全局 Architecture 根。 |

## Reject

| QFramework 机制 | 拒绝原因 |
| --- | --- |
| 直接引入 QFramework 依赖 | 会把外部 public API 变成 SlimeAI 长期事实源，后续改造实际变 fork 维护。 |
| `Architecture<T>` static singleton | 不适合 Godot 多场景、测试隔离、对象池和多 runtime scope。 |
| `IController` 让 Godot Node 承担 gameplay controller | SlimeAI Node 是 bridge / view / lifecycle 承载者；gameplay owner 应在 Feature / Component / System。 |
| `AbstractCommand` 引用对象体系 | 与 typed request/result + handler/pipeline 重叠，增加分配和文件膨胀。 |
| `TypeEventSystem.Global` | 不表达 Object / Feature / Runtime scope，容易变全局广播。 |
| `BindableProperty<T>` 替代 Data | 缺 DataOS、descriptor、modifier、computed、authority、projection、validation。 |
| 每个模块默认一个接口 | 单实现接口会增加仪式感；只在多实现、mock、权限限制或 public contract 清楚时使用。 |
| `Model` 作为所有共享状态容器 | SlimeAI 需要 Data / Profile / Component / System 分区，不回到大 Model。 |

## 对旧裁决的修订

旧文档已经判断“QFramework 可学习，不直接接入”。本轮新问题是“如果我愿意改底层，是否可以直接用”。修订后的表达是：

```text
如果直接用 = 引入依赖或以 Architecture<T> 为根：仍然不建议。
如果直接用 = 以 QFramework 核心为草稿 hard fork：可以做实验，但必须先删改关键 API。
如果用 = 采用少规则和能力接口重建 SlimeAI Kernel：推荐。
```

这比“只学习不接入”更积极，但仍不允许 QFramework 覆盖 SlimeAI 事实源。

## 与当前 Data 方案的关系

QFramework 强化当前 Data 方案，而不是推翻它：

- QFramework 说“只有共享数据进 Model”；SlimeAI 应继续收窄 Data 进入条件。
- QFramework 说“敌人当前生命通常脚本自己管理”；SlimeAI 要进一步区分 `CurrentHp` 这种跨 Damage/UI/AI/Test 的共享字段，与 `AttackComponent._state` 这种 Component authoritative 字段。
- QFramework 的 `BindableProperty` 证明变化通知必要；SlimeAI 用 `DataChanged<T>` 和 DataBinding，而不是换成 BindableProperty。
- QFramework 的 Command 证明写入口要集中；SlimeAI 用 typed command/request + owner handler/pipeline。

