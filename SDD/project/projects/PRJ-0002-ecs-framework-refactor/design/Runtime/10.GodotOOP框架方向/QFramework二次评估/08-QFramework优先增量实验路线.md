# QFramework 优先增量实验路线

> 状态：candidate route / not frozen  
> 日期：2026-06-19  
> 用户原始问题：[`source-request.md`](./source-request.md)  
> 前置裁决：[`07-框架根缺失与魔改QFramework可行性.md`](./07-框架根缺失与魔改QFramework可行性.md)

## 这次新思路是什么

用户这次把路线进一步收窄为：

```text
先深度了解 QFramework。
不要一上来把 SlimeAI 的 Data/Event/Entity/Component/System/ObjectPool/Log 全迁到新框架。
而是把 SlimeAI 概念一点点加到 QFramework 里。
每加一个概念，就观察 QFramework 的清晰性哪里被破坏、哪里需要改。
等理念冲突暴露清楚后，再决定 SlimeAI 自己应该怎么改。
```

这个思路比“直接重写 SlimeAI”更适合当前阶段。

原因不是 QFramework 能直接解决 SlimeAI 的所有问题，而是：**QFramework 足够小、足够清晰、约束足够短，适合作为框架感训练器和压力测试母版**。当一个 SlimeAI 概念加入 QFramework 后，如果 QFramework 的短规则立刻变复杂，就能反推这个概念在 SlimeAI 里到底是基础规则、上层功能，还是错误抽象。

## 我的裁决

推荐采用这个路线，但要改一句话：

```text
不是“把 SlimeAI 框架一点点加到 QFramework 里并长期留在那里”。
而是“用 QFramework 承接 SlimeAI 概念压力测试，逼出 SlimeAI-native Architecture Contract”。
```

这条路线对你当前最有价值，因为它不会要求你先拥有成熟框架经验。它允许你先站在一个清晰框架里做小改动，再观察清晰性如何被破坏。

但它必须有边界：

- QFramework 原型是学习原型，不是正式 SlimeAI 源码。
- 每次只加一个 SlimeAI 压力源，不做全量迁移。
- 每次实验都要记录“QFramework 原规则 vs SlimeAI 需求 vs 冲突点 vs 萃取规则”。
- 一旦 QFramework 为了容纳 SlimeAI 概念变得解释不清，就停止继续往里塞，转而把冲突写入 SlimeAI Architecture Contract。
- 原型结束后，不把 QFramework 原型整包搬回 SlimeAI；只搬回规则、命名、接口边界和验证门禁。

## 为什么这比直接迁移 SlimeAI 概念更好

直接迁移有一个隐藏风险：你会把旧 SlimeAI 的混乱也一起搬过去。

SlimeAI 当前不是完全没有框架，而是有很多 framework-grade 部件：

```text
DataOS descriptor / runtime snapshot / generated DataKey<T>
typed Event
Object / Component / System / Feature
ObjectPool
Log / Validation / SDD / SystemAgent
Damage / Ability / AI 等 capability
```

问题是这些部件缺少同一个短规则统领。直接把它们迁到“新的 SlimeAI”里，很容易只是换目录、换名字、换一套文档，根本矛盾仍然存在：

- Data 还是会膨胀。
- Event 还是会混用成请求、事实、查询触发器。
- System、Feature、Component 的优先级还是不清楚。
- ObjectPool、Log、Validation 还是像外挂能力，而不是框架契约自然展开。

而 QFramework 的好处是它的初始规则很短：

```text
Architecture 是架构图和容器。
IController 接输入和表现。
IModel 放共享数据。
ISystem 放跨表现层逻辑。
IUtility 放基础设施。
Command 表达写意图。
Query 表达只读查询。
Event / BindableProperty 表达状态变化通知。
ICanXxx 用接口限制能力。
```

你每加一个 SlimeAI 概念，都能马上问：

```text
这个概念是让规则更清楚了，还是更模糊了？
它属于 QFramework 哪个角色？
如果哪个角色都放不下，是 QFramework 不够，还是 SlimeAI 概念本身边界没想清楚？
```

这比在 SlimeAI 自己复杂系统里继续改来改去更容易学习。

## 实验总原则

### 原则 1：先保持 QFramework 原味

第一轮不要急着改 QFramework。

先用它原本的方式写一个最小例子：

```text
Counter / Health / Damage
Architecture.Init 注册 Model/System/Utility。
Controller 只 SendCommand / RegisterEvent / RegisterWithInitValue。
Model 保存共享状态。
System 处理跨对象逻辑。
Command 修改状态。
Event 通知事实。
Query 做只读查询。
```

目的不是做游戏，而是把 QFramework 的“短规则肌肉记忆”建立起来。

### 原则 2：一次只加一个 SlimeAI 压力源

每次实验只引入一个问题。

不要同时引入 DataOS、对象池、Godot 生命周期、Feature、Log、AI 验证。否则你看不清到底是谁破坏了清晰性。

### 原则 3：用冲突表记录学习结果

每个实验都记录：

| 字段 | 说明 |
| --- | --- |
| QFramework 原规则 | 原本应该怎么写。 |
| SlimeAI 压力源 | 本轮加入了什么 SlimeAI 需求。 |
| 冲突表现 | 哪个类开始变胖、哪个事件变混、哪个状态归属变不清。 |
| 临时改法 | 原型里怎么改 QFramework。 |
| 萃取结论 | 正式 SlimeAI 应该吸收什么、拒绝什么。 |

这个表比“我觉得某概念好不好”更可靠。

### 原则 4：保护 QFramework 的清晰性作为检测仪表

QFramework 的价值就是清晰。一旦你发现为了塞入某个 SlimeAI 概念，需要大量例外解释，说明已经得到答案了：

```text
这个概念不能照这个形态进入框架根。
```

不要继续硬塞。停下来，把冲突写成 SlimeAI 自己的规则。

### 原则 5：只萃取规则，不复制身份

最终不要得到：

```text
SlimeAI = QFramework fork
```

而应该得到：

```text
SlimeAI Architecture Contract
  学到了 QFramework 的少规则、角色边界、Command/Query/Event 语义、ICanXxx 权限限制；
  保留 SlimeAI 自己的 Godot lifecycle、DataOS、Feature、ObjectPool、Log/Test/AI-first 验证。
```

## 建议实验阶梯

### Stage 0：纯 QFramework 复刻

目标：先不要创新，只复刻 QFramework 原始架构。

建议样例：

```text
CounterApp
HealthApp
DamageApp
```

观察点：

- `Architecture.Init()` 是否真的像架构图。
- `Model` 放共享数据时是否清楚。
- `Command` 是否让写入口更好 grep。
- `RegisterWithInitValue` 是否让 UI / debug 绑定变简单。
- `ICanXxx` 是否让“不该有的能力”编译不过。

通过标准：

```text
你能不看 SlimeAI 现有文档，只用 5-8 条规则解释这个小项目。
```

### Stage 1：加入 Godot 生命周期压力

问题：

```text
QFramework 的 IController 默认很像 Unity MonoBehaviour。
SlimeAI 的 Godot Node 不能变成万能 gameplay controller。
```

实验：

- 把 Controller 改成 Godot Node adapter。
- Node 只接输入、展示、订阅、释放订阅。
- gameplay 写入口仍走 Command / owner handler。

要观察的冲突：

- Node 是否开始直接改 Model/Data。
- Controller 是否变成业务中心。
- 生命周期释放是否能覆盖 Node exit tree、disable、pool release。

萃取目标：

```text
SlimeAI 的 Node = Adapter / View / Lifecycle carrier。
不是 gameplay controller。
```

### Stage 2：加入 per-object Health / Damage

问题：

```text
QFramework Model 更适合应用级共享数据。
SlimeAI 有大量对象实例状态。
```

实验：

- 写 10 个对象，每个对象有 CurrentHp / MaxHp。
- 尝试把 HP 放 QFramework Model。
- 再尝试放 Object/Component state，并只把共享可观察字段投影出来。

要观察的冲突：

- `Model` 是否变成按 objectId 索引的大表。
- `Command` 是否需要携带 objectId。
- `Event` 是否需要 scope，否则所有对象都收同一类事件。

萃取目标：

```text
SlimeAI 不能把 Model 翻译成 Data 大容器。
正式规则应区分：
  Component authoritative state
  Data authoritative state
  Data projection
  Profile/config state
  System/service state
```

### Stage 3：加入 DataOS / 表格驱动压力

问题：

```text
QFramework 的 Model 是 C# 手写状态。
SlimeAI 的字段定义来自 DataOS descriptor / runtime snapshot / generated DataKey<T>。
```

实验：

- 不直接手写 `HealthModel.CurrentHp` 字段。
- 模拟一份 descriptor，把字段定义和 runtime holder 分开。
- 让 Architecture/Manifest 只显示字段清单和路由，不拥有字段定义。

要观察的冲突：

- QFramework `Model` 是否会诱导回到手写字段。
- `BindableProperty<T>` 是否会替代 DataChanged / DataBinding。
- 字段定义、运行时值、修改入口是否又混在一起。

萃取目标：

```text
DataOS 是 SlimeAI 字段定义事实源。
Architecture/Manifest 只读展示和路由，不替代 DataOS。
DataBinding 保留 RegisterWithCurrentValue 语义，但不改成 BindableProperty 事实源。
```

### Stage 4：加入 Feature owner 压力

问题：

```text
QFramework 的四层是应用结构。
SlimeAI 的 Feature 是玩法能力 owner，负责组件、Data、Event、System、验证入口的聚合。
```

实验：

- 做 `CombatFeature`。
- 列出它拥有的 components、commands、queries、events、data fields、bindings、tests。
- 不让 `Architecture.Init()` 变成一个全局巨型注册表。

要观察的冲突：

- Feature 是否能成为 owner。
- Architecture 是否开始保存所有业务细节。
- System / Model / Utility 是否足够表达 Feature 归属。

萃取目标：

```text
SlimeAI 需要 FeatureManifest。
它像 QFramework Init() 一样可读，但不是全局注册表。
```

### Stage 5：加入 ObjectPool / 复用生命周期压力

问题：

```text
QFramework 主要处理注册和订阅释放。
SlimeAI 对象会 pool release / acquire，同一个 Node 多次复用。
```

实验：

- 一个对象死亡后进入池，再作为新单位复用。
- 检查 Data、Event、Timer、Binding、Modifier source 是否清理。
- 检查 current value binding 是否在 acquire 后重新发初始值。

要观察的冲突：

- `UnRegisterWhenGameObjectDestroyed` 式思路是否不足。
- 订阅是否只绑定销毁，不绑定 disable/release。
- Data modifier / event handler 是否残留到下一次复用。

萃取目标：

```text
SlimeAI lifecycle token 必须覆盖：
  Node exit tree
  Component disable
  Feature disable
  Pool release
  Runtime destroy
```

### Stage 6：加入 Log / Test / AI-first 验证压力

问题：

```text
QFramework 强调代码组织。
SlimeAI 还要求 AI 能追踪、验证、复盘。
```

实验：

- 每个 Command / Request 产出 trace。
- 每个失败有 blocked reason。
- 每个 Event / DataChanged 可被测试和 logctl 观察。
- 每个 FeatureManifest 指向测试入口。

要观察的冲突：

- QFramework Command 对象是否方便记录 trace。
- Event 是否有足够 payload 和 scope 供验证。
- Query 是否返回可验证只读视图。

萃取目标：

```text
SlimeAI 的 Architecture Contract 必须把 Validation / Observation 当一等规则。
这不是 QFramework 原生能力，但可以接在 Command / Event / Query 语义上。
```

### Stage 7：萃取 SlimeAI Architecture Contract

当前面实验完成后，才开始写正式 SlimeAI 规则。

产物建议：

```text
ArchitectureContract/
  README.md
  01-角色与依赖方向.md
  02-状态归属规则.md
  03-Command-Query-Event语义.md
  04-FeatureManifest规则.md
  05-Godot生命周期与ObjectPool规则.md
  06-Validation与Observation规则.md
```

这一阶段才决定正式代码怎么切。

## 每阶段的失败信号

这些信号一出现，就说明不能继续按原方向硬塞：

| 失败信号 | 含义 |
| --- | --- |
| `Model` 变成所有对象状态的大字典 | Data / Component / Profile / System 分区没想清楚。 |
| `Architecture` 开始保存业务字段 | 框架根变成 God Object。 |
| `Event` 既表示请求又表示事实 | Command / Event 边界失效。 |
| `Command` 类数量快速爆炸 | QFramework 对象式 Command 不适合 SlimeAI 默认路径。 |
| Godot Node 到处直接改状态 | Adapter / gameplay owner 边界失败。 |
| 每个规则都需要例外说明 | 框架根不够清晰，应该停止扩张。 |
| 为了兼容 QFramework 名字而扭曲 SlimeAI 概念 | 原型身份反客为主。 |
| Log/Test/Validation 挂不上 Command/Event/Data | AI-first 是后补外挂，不是框架契约。 |

## 我对这条路线的质疑

这条路线可行，但有三个风险。

### 风险 1：学习原型变成正式 fork

最危险的是你越改越顺手，最后把 QFramework fork 当成正式框架。短期会很爽，长期会出现两个问题：

- 官方 QFramework 文档不能再指导你，因为你已经改掉核心语义。
- SlimeAI 文档会被 QFramework 名字误导，AI 也会把 `Architecture<T>`、`Model`、`IController` 当事实源。

处理方式：

```text
原型目录必须带 prototype / disposable 字样。
正式文档只写 SlimeAI Architecture Contract，不写“基于 QFramework fork”。
```

### 风险 2：你会过度学习 QFramework API，而不是学习框架判断

QFramework 值得学的是：

```text
角色划分。
依赖方向。
写入口。
读入口。
通知方向。
共享数据进入条件。
能力接口。
```

不值得投入太深的是：

```text
每个 API 细节。
Toolkits 全家桶。
Unity UI 绑定方式。
具体示例项目写法。
```

处理方式：

```text
学习笔记必须始终回答“这个概念解决什么框架问题”，而不是“这个 API 怎么调用”。
```

### 风险 3：小例子会低估 SlimeAI 的真实复杂度

CounterApp 很清晰，是因为它没有：

- 对象池复用。
- per-object 状态。
- 表格驱动。
- modifier/computed。
- AI 验证。
- 多 Feature 组合。
- Godot scene lifecycle。

处理方式：

```text
必须按 Stage 1-6 逐步加压力。
只做 CounterApp 得出的结论不能直接进入 SlimeAI 正式架构。
```

## 推荐下一步

我建议下一步不要直接改正式 `Src/`。

先做一个只读/实验性质的学习包：

```text
.ai-temp/prototypes/qframework-slimeai-pressure/
```

里面只放：

```text
00-QFrameworkConceptMap.md
01-PureQFrameworkCounter/
02-GodotLifecyclePressure/
03-HealthDamagePerObject/
04-DataOSDescriptorPressure/
05-FeatureManifestPressure/
06-ObjectPoolLifecyclePressure/
07-ValidationObservationPressure/
conflict-log.md
```

如果要进 SDD，则只为这件事建一个“Architecture Contract discovery”任务，不把它命名成“迁移 QFramework”。

## 最终结论

你的新路线成立，而且我认为它比“先设计一个完美 SlimeAI 新框架”更稳。

它的核心价值是：

```text
先借 QFramework 的清晰性当标尺。
再用 SlimeAI 的真实复杂度逐步施压。
通过冲突暴露框架理念问题。
最后萃取 SlimeAI 自己的框架根。
```

我不建议你现在追求“直接用 QFramework 做 SlimeAI”。  
我建议你追求：

```text
用 QFramework 学会什么叫清晰框架；
用增量压力实验知道 SlimeAI 到底需要哪些不同规则；
最后写出 SlimeAI-native Architecture Contract。
```

这对“框架经验不足”的状态是合理路线：先模仿清晰结构，再制造受控冲突，最后萃取自己的规则。

