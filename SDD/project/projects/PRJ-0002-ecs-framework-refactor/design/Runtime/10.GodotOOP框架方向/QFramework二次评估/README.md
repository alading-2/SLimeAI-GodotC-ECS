# QFramework 二次评估

> 状态：candidate analysis / not frozen  
> 日期：2026-06-19  
> 用户原始问题：[`source-request.md`](./source-request.md)  
> 上级方向：[`../README.md`](../README.md)  

## 一句话结论

你的新判断更接近根因：SlimeAI 现在更像“一组完成度不低的底层和功能集合”，还不是一个足够清晰的框架。Data 和 Event 把很多东西串起来了，但它们不是框架根；它们更像共享状态协议和通信协议。真正缺的是一套让所有东西围绕它展开的 **Foundation Contract / Architecture Contract**。

所以，**应该深入学习 QFramework 的概念**。尤其要学：

```text
Architecture 是架构图。
Model/System/Utility/Controller 是角色。
Command/Query/Event 是交互语法。
ICanXxx 是编译期边界。
共享数据有进入条件。
```

但如果目标是长期 SlimeAIFramework，**不建议保留“我是在魔改 QFramework”这个身份**。更好的判断是：

```text
可以用“大改 QFramework”作为学习方法和原型路径；
长期产物应是 SlimeAI-native Architecture Contract；
QFramework 是思想母版，不是事实源和依赖根。
```

如果你“完全用 QFramework”，SlimeAI 会变清晰，但会变成一个 Unity/MVC 应用层框架的 Godot 变体，DataOS、Feature、对象池、System diagnostics、AI-first 验证会被挤到框架外。  
如果你“魔改 QFramework”，可行，但改完关键点后已经是 SlimeAI 架构重建：

```text
需要改掉：
  static Architecture<T> 单例
  AbstractCommand 引用对象执行器
  TypeEventSystem.Global 无 scope 广播
  BindableProperty 替代 Data 的倾向
  IController 让 Godot Node 变 gameplay controller
  Model/System/Utility 固定四层对 SlimeAI Feature/DataOS 的挤压

需要保留：
  少规则分层
  读写分离
  Command 是写意图，Event 是事实通知，Query 是只读查询
  ICanXxx 编译期权限接口
  RegisterWithInitValue 式订阅当前值
  Init 清单像架构图一样可读
```

推荐路线更新为：

```text
第一阶段：深入学习 QFramework，写出 SlimeAI 对应概念表。
第二阶段：拿 QFramework 核心做 disposable fork / prototype，亲手改 Architecture、Command、Model。
第三阶段：把验证出的规则落成 SlimeAI-native Architecture Contract，不保留 QFramework 依赖身份。
```

也就是说，“大改 QFramework”适合作为训练轮和原型轮；“新的 SlimeAIFramework”应该是吸收 QFramework 清晰性的自有框架根。

用户最新补充的“先深度了解 QFramework，再把 SlimeAI 概念一点点加到 QFramework 里，通过冲突暴露理念问题”的路线，推荐采纳为 **Architecture Contract discovery**。这不是正式迁移路线，而是学习和压力测试路线：先保护 QFramework 的短规则，再逐个加入 Godot 生命周期、per-object state、DataOS、Feature、ObjectPool、Log/Test 压力，最后只把仍然清晰的规则萃取回 SlimeAI。

## 为什么不是直接用

QFramework 是应用层分层框架。它解决的是：

```text
代码放在哪里。
谁能改状态。
谁能读状态。
状态变化怎么通知。
```

SlimeAIFramework 要解决的是：

```text
Godot Node / scene / object pool 生命周期。
per-object runtime state。
DataOS authoring -> runtime snapshot -> generated DataKey<T>。
Feature 可组合、可裁剪、可启停。
System 运行条件、阻断原因和 diagnostics。
typed scoped Event。
Log / Test / SDD / SystemAgent 的 AI-first 闭环。
```

这两组问题重叠，但不是同一个层级。QFramework 的小，主要来自它没有承担 SlimeAI 已经承担的 DataOS、GodotBridge、System diagnostics、对象池、碰撞、AI 验证和多游戏 profile 问题。不能把“少代码”直接等价成“更适合做 SlimeAI 底层”。

## 推荐方向

推荐走 **方案 C：SlimeAI-native QFramework-inspired Kernel**。

| 方案 | 判断 | 说明 |
| --- | --- | --- |
| A. 直接引入 QFramework 并局部 patch | 不推荐 | 依赖和 API 形态会倒逼 SlimeAI 接受 `Architecture<T>` / `AbstractCommand` / 全局事件；后续改动会变成 fork 维护。 |
| B. hard fork QFramework 核心源码后大改 | 有条件可做 | 可以快速得到命名和结构，但需要先删改核心设计；适合作为临时原型，不适合作为长期事实源。 |
| C. 用 QFramework 原则重建 SlimeAI Kernel | 推荐 | 保留少规则、Command/Query/Event、ICanXxx、当前值订阅；底层继续以 Godot AutoLoad、SystemManager、DataOS、Feature scope 为事实源。 |
| D. 先魔改 QFramework 作为训练原型，再迁移成 SlimeAI Architecture Contract | 最推荐 | 对“缺框架经验”的风险最低：先学会清晰框架怎么长，再决定哪些规则进入 SlimeAI。 |

## 本目录文档

| 文档 | 职责 |
| --- | --- |
| [`01-问题复盘与重新裁决.md`](./01-问题复盘与重新裁决.md) | 回答“这个问题是否真实存在、用户思路是否成立、推荐方向是什么”。 |
| [`02-QFramework源码证据.md`](./02-QFramework源码证据.md) | 汇总本地源码、官方文档、Context7/Web 核对到的事实。 |
| [`03-SlimeAI采纳与拒绝清单.md`](./03-SlimeAI采纳与拒绝清单.md) | 按 Adopt Now / Later / Reject 给出采纳边界。 |
| [`04-Architecture与Command改造方案.md`](./04-Architecture与Command改造方案.md) | 专门分析如果改 `Architecture` 和 `Command`，SlimeAI 应该怎么改。 |
| [`05-迁移路线与验证门禁.md`](./05-迁移路线与验证门禁.md) | 给出最小试验切片、迁移顺序、失败条件和验证入口。 |
| [`06-SlimeAIKernel执行草案.FeatureSpec.md`](./06-SlimeAIKernel执行草案.FeatureSpec.md) | 如果后续要实施，第一刀怎么切到代码的候选执行规格。 |
| [`07-框架根缺失与魔改QFramework可行性.md`](./07-框架根缺失与魔改QFramework可行性.md) | 回答“SlimeAI 是否缺框架根、是否应该深学/魔改 QFramework”。 |
| [`08-QFramework优先增量实验路线.md`](./08-QFramework优先增量实验路线.md) | 回答“先学 QFramework，再逐步加入 SlimeAI 概念暴露理念冲突”是否可行，并给出实验阶梯。 |

## 当前不改变的裁决

- 不改变 `SlimeAIFramework` 的 Godot/OOP 方向。
- 不把 QFramework 升级为 SlimeAI 事实源。
- 不回退 DataOS descriptor / runtime snapshot / generated `DataKey<T>`。
- 不把 `Data` 改名为 `Model` 或 `BindableProperty`。
- 不让 Godot Node 默认实现 QFramework `IController`。
- 不把 QFramework Toolkits 直接引入框架仓。
