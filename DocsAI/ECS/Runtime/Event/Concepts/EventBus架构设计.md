# EventBus 架构设计

> 类型：概念文档
> 范围：`Src/ECS/Runtime/Event/`
> 事实源：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/1.Event系统优化/`

## 1. 为什么自定义 EventBus

C# 原生 `event` 有三个核心问题：

| 问题 | 原生 event | EventBus |
|------|-----------|----------|
| 生命周期陷阱 | 静态事件易内存泄漏 | 实例总线 + `Clear()` 一键清理 |
| 无优先级 | Delegate 执行顺序不确定 | `Priority` 数值排序 |
| 对象池不友好 | 需逐个字段解绑 | `Events.Clear()` 一次清空 |

## 2. 三层架构

```
GlobalEventBus.Global    跨实体低频广播
Entity.Events            单实体内部组件通信
EventBus (引擎)          On/Off/Once/Emit/Priority
```

分层规则：
- 只服务单个实体内部 -> `entity.Events`
- 跨实体/跨系统/全局 -> `GlobalEventBus.Global`
- 禁止所有事件都上提到全局（订阅膨胀 + 清理困难）

## 3. 事件标识：payload 类型即 key

旧模式：`const string EventName + XxxEventData`（双写，易漂移）
新模式：`readonly record struct` 类型本身即 key

```csharp
// 事件定义
public readonly record struct Damaged(IEntity Victim, float Amount);

// 订阅与触发
entity.Events.On<Damaged>(OnDamaged);
entity.Events.Emit(new Damaged(victim, 10f));
```

`typeof(Damaged)` 是 EventBus 字典 key。无字符串事件名，无 `XxxEventData` 后缀。

## 4. 核心运行时能力

| 能力 | 说明 |
|------|------|
| 优先级 | 数值越大越先执行 |
| 一次性订阅 | `Once<T>()`，触发后自动移除 |
| 派发中安全移除 | 标记延迟清理，不抛集合修改异常 |
| 同类型重入保护 | 正在派发时再次 `Emit` 同类型 -> 阻止并 warning |
| 异常隔离 | 单个 handler 抛异常不中断后续派发 |
| 动态事件 | 不提供 dynamic object 主入口；数据驱动场景必须先落成 typed payload / typed action wrapper |

## 5. 与现代框架对比

| 特性 | Unity DOTS | Unreal GAS | Godot Signal | 本项目 EventBus |
|------|-----------|-----------|-------------|----------------|
| 类型安全 | 编译期 | 反射 | 弱类型 | 编译期 + 运行时检查 |
| 运行期路由 | 不支持 | 支持 | 支持 | 按 payload Type 路由 |
| 优先级 | 无 | 支持 | 无 | 支持 |
| 性能 | Burst 极致 | 反射开销 | 信号开销 | 零反射 + 零 GC (struct) |
| 学习成本 | 高 | 中 | 低 | 低 |

## 6. 设计权衡

**优势：**
- 类型安全：`readonly record struct` + 编译期检查
- 零 GC：栈分配事件数据
- 零反射：泛型 `Emit<T>` 直接调用 `Action<T>`
- 分层：全局 + 局部隔离

**代价：**
- 运行时字典查找有轻微开销（`Dictionary<Type, List<Subscription>>`）
- GlobalEventBus 长期监听者需手动 `Off<T>` 清理

## 7. 不会做

- 不支持跨线程事件（Godot 单线程模型）
- 不支持事件冒泡（过度设计）
- 不恢复 `DynamicInvoke`（性能倒退）

## 8. 历史判断

本轮 Event 优化的关键判断是"删掉字符串事件名，而不是重做一个更大的事件系统"。保留 `On/Off/Once/Emit`、优先级、重入保护、异常隔离；删除的是 `const string` 与 payload 双写。

后续维护重点：守住协议边界（事件少、payload 清、作用域明、订阅方能清理）。
