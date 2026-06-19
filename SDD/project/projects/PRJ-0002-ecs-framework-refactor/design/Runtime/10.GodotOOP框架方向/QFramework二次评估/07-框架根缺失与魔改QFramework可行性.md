# 框架根缺失与魔改 QFramework 可行性

## 这次问题的关键变化

用户这次补充的核心不是“QFramework 能不能用”，而是：

```text
我没有太多做框架的经验。
QFramework 小而清晰。
SlimeAI 越写越像一堆底层和功能集合，不像一个框架。
能不能通过深入学习或大改 QFramework，做一个新的 SlimeAI 框架？
```

这个判断比上一轮更接近根因。

上一轮重点是“不要直接接入 QFramework”。这仍然成立。但它没有充分回应“SlimeAI 缺清晰框架根”这个问题。这里必须补一个更明确的裁决：

```text
SlimeAI 需要重建框架根。
QFramework 很值得深学。
魔改 QFramework 可行，但更适合作为训练原型和架构对照。
最终正式框架应是 SlimeAI-native，不应长期以 QFramework fork 的身份存在。
```

## SlimeAI 现在为什么不像框架

一个框架不只是功能多，也不是底层工具多。框架应该提供一个清晰的展开方式：

```text
新功能进来时，先回答角色归属。
状态放在哪里有规则。
写入口在哪里有规则。
读入口在哪里有规则。
事件怎么命名有规则。
生命周期谁接管有规则。
验证如何证明有规则。
```

SlimeAI 现在的问题是：很多子系统已经做得很认真，但它们之间主要靠 Data / Event 连接。

```text
Data:
  负责共享状态、表格输入、约束、modifier、computed、UI/debug/test/AI 观察。

Event:
  负责 typed message、DataChanged、Feature 通知、Component 通信。
```

这两者很重要，但它们不应该承担“框架根”的全部职责。结果是：

- Data 容易膨胀成所有状态的入口。
- Event 容易混用成 command / fact / query trigger。
- System、Feature、Component 的关系需要靠很多文档解释。
- ObjectPool、Log、Validation、DataOS 都有价值，但不像围绕同一个框架语法展开。
- 新功能不知道先写 Feature、System、Component、Data descriptor、Event，还是 Log/Test。

所以用户说“不是框架，是功能集合”，不是完全准确，但方向正确。更准确的说法是：

```text
SlimeAI 有框架级部件，但缺架构级契约。
```

## QFramework 为什么显得清晰

QFramework 清晰，不是因为它功能比 SlimeAI 强，而是因为它的架构语法很短：

```text
Architecture:
  注册 Model / System / Utility。

Controller:
  接收输入、更新表现、发送 Command、查询 Model/System。

Model:
  保存共享数据，提供增删查改，状态变化发 Event 或 BindableProperty。

System:
  处理跨 Controller 的业务逻辑，监听/发送 Event，访问 Model。

Utility:
  基础设施，不依赖上层。

Command:
  状态变更入口。

Query:
  复杂只读查询入口。

Event:
  下层通知上层的事实消息。
```

它把问题压成一组角色和一组交互动词。新功能进来时，人先问：

```text
这是共享数据吗？进 Model。
这是跨表现层逻辑吗？进 System。
这是状态变更吗？写 Command。
这是读取组合数据吗？写 Query。
这是底层通知表现吗？发 Event。
这是平台能力吗？写 Utility。
```

这就是框架感。

## 是否应该深入学习 QFramework

应该，而且不是泛泛看 API，而是按“框架根训练”去学。

必须学：

1. `Architecture<T>` 为什么让人一眼看懂项目。
2. `IController / ISystem / IModel / IUtility` 的角色划分。
3. Command / Query / Event 的语义边界。
4. `ICanXxx` 如何用编译期限制强制规则。
5. “什么是共享数据”的判断。
6. `BindableProperty.RegisterWithInitValue` 为什么让 UI binding 简洁。
7. QFramework 的局限：全局单例、Command 对象分配、Event 无 scope、Model 容易变大容器。

学习目标不是背 QFramework API，而是能把 SlimeAI 映射成同样短的规则。

## 能不能通过大改 QFramework 做新的 SlimeAI 框架

可以，但要分两种含义。

### 含义 A：正式 fork QFramework，长期在 fork 上做 SlimeAI

不推荐。

理由：

- QFramework 根语义是 Unity/MVC 应用层，不是 Godot/Feature/DataOS runtime。
- `Architecture<T>` 静态单例会和 Godot runtime instance、测试隔离、对象池生命周期冲突。
- `Model` 会诱导 Data 重新变成大共享状态容器。
- `TypeEventSystem.Global` 会诱导全局广播。
- `AbstractCommand` 会诱导每个操作一个对象类，和 SlimeAI typed request/pipeline 方向冲突。
- 一旦 fork 改得足够多，外部 QFramework 文档不再能指导你的框架，反而产生名字误导。

### 含义 B：把 QFramework 核心当训练材料，魔改出一个原型

推荐。

理由：

- 它小，改得动。
- 它清晰，改坏了也容易知道哪里坏。
- 它能逼你回答 SlimeAI 的角色映射。
- 它能帮你摆脱“Data/Event 串所有东西”的惯性。
- 原型可以快速验证：SlimeAI 到底需要 `Architecture`、`Command`、`Model` 这些概念的哪一部分。

推荐做法：

```text
不要直接改正式 SlimeAI。
新建 disposable prototype。
从 QFramework.cs 复制核心思想或最小代码。
逐步改名和改语义：
  Architecture -> SlimeArchitecture / RuntimeKernel
  Model -> State / DataModel / ProfileModel
  System -> Service / Coordinator / RuntimeSystem
  Controller -> Adapter / ViewController / GodotBridge
  Command -> typed request + handler
  Event -> scoped typed fact event
```

原型成功后，不把原型整包搬回 SlimeAI，而是提炼规则和接口，写成 SlimeAI Architecture Contract。

## SlimeAI 应该补的框架根

候选名字：

```text
SlimeArchitecture
SlimeRuntimeKernel
SlimeGameFramework
SlimeApplication
```

比名字更重要的是它必须定义角色和交互语法。

推荐框架根：

```text
SlimeArchitecture
  Role:
    Object       Godot Node/scene 中的游戏对象根
    Component    单对象功能单元，可持有内部状态
    Feature      玩法能力 owner，声明组件、状态、命令、事件、验证
    Service      跨对象或跨功能协调器
    Data         共享/表格/可观察/可约束状态协议
    Profile      一组配置或初始化参数
    Adapter      Godot/UI/Input/Resource 等外部桥
    Utility      无上层依赖的基础设施

  Interaction:
    Command      请做某事，必须有 owner handler
    Query        只读查询，不返回 mutable internal
    Event        已发生事实，可无人监听
    Binding      Data/Profile 到 Component/UI 的同步
    Validation   行为标准答案和证据
```

这才是 SlimeAI 缺的“清晰框架”。

## QFramework 角色如何翻译到 SlimeAI

| QFramework | SlimeAI 不应直接翻译为 | 推荐翻译 |
| --- | --- | --- |
| Architecture | 一个大 static singleton | `SlimeArchitecture`：只读架构图 + runtime kernel facade |
| Model | Data 大容器 | Data / Profile / ComponentState / ServiceState 分区 |
| System | 所有业务逻辑入口 | Service / Coordinator，只处理跨对象、调度、查询、批处理 |
| Controller | Godot Node 万能业务入口 | Adapter / UI / Input bridge |
| Utility | 任意工具箱 | 无上层依赖的基础设施 owner |
| Command | `AbstractCommand` 对象 | typed request + owner handler / pipeline |
| Query | 到处 new Query | readonly facade / query service |
| Event | 全局广播 | scoped typed fact event |
| BindableProperty | 替代 Data | DataObservable / SubscribeWithCurrentValue |
| ICanXxx | 全套复制接口 | 用在关键上下文的能力接口 |

## 推荐学习和改造路线

### Step 1：QFramework 概念学习

产出：

```text
QFrameworkConceptMap.md
```

内容：

- 每个 QFramework 概念解决什么问题。
- SlimeAI 中对应问题是什么。
- 直接复制会出什么问题。
- 魔改后的 SlimeAI 概念名是什么。

### Step 2：一次性原型

产出：

```text
.ai-temp/prototypes/slime-architecture-qf/
```

内容：

- 一个最小 SlimeArchitecture。
- 一个 Health/Damage 示例。
- 一个 UI binding 示例。
- 一个 Command/Query/Event 示例。

注意：原型不进入正式事实源，不直接替换 Src。

### Step 3：Architecture Contract 文档

产出：

```text
SDD/.../Runtime/10.GodotOOP框架方向/ArchitectureContract/
```

内容：

- 角色定义。
- 状态归属规则。
- Command / Query / Event 语义。
- Feature manifest 格式。
- 新功能落点流程。
- 反例和禁止路径。

### Step 4：正式最小切片

候选：

```text
Health / Damage / Recovery
```

成功标准：

- 不靠长文档也能说明：状态在哪、谁能写、谁能读、事件是什么。
- Data / Event / System / Log / Test 都围绕 Architecture Contract 展开。
- 代码入口减少，而不是更多。

## 我对“完全用 QFramework”的最终判断

不建议。

不是因为 QFramework 差，而是它的清晰来自它解决的问题更窄。SlimeAI 如果完全用它，会获得短期清晰，但会丢掉你已经做出来的一些真正底层资产：

- DataOS descriptor / runtime snapshot / generated `DataKey<T>`。
- SystemManager run condition / blocked reason / diagnostics。
- typed scoped EventBus。
- ObjectPool / NodeLifecycle / RuntimeMount。
- Log / Test / SDD / SystemAgent 的 AI-first 闭环。

真正应该做的是：

```text
学 QFramework 的“清晰框架语法”，
用它训练你的框架感，
然后重建 SlimeAI 的 Architecture Contract。
```

## 需要确认

- 你是否接受先做一个一次性 QFramework 魔改原型，而不是直接动正式 SlimeAI 源码？  
  默认建议：接受。这样风险最低。

- 新框架根的名字，你更倾向 `SlimeArchitecture` 还是 `SlimeRuntimeKernel`？  
  默认建议：文档层叫 `SlimeArchitecture`，代码核心叫 `SlimeRuntimeKernel`。

- 第一刀是否用 `Health/Damage/Recovery` 做样例？  
  默认建议：是。它能同时验证 Data、Command、Event、System、Log、Test。

