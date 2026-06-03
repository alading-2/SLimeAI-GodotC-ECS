# Context Object Pattern

> 类型：概念文档
> 范围：`Src/ECS/Runtime/Event/EventContext.cs`
> 事实源：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/1.Event系统优化/`

## 1. 核心问题

事件系统天然单向（发布者 -> 订阅者）。如何让订阅者向发布者"返回"数据？

## 2. 解决方案：Context 对象

发布者创建可变 Context -> Emit -> 订阅者修改 Context -> 发布者读取结果。

```csharp
var context = new EventContext();
ability.Events.Emit(new GameEventType.Ability.CheckCanUse(context));
if (!context.Success) return;  // 订阅者标记了失败
```

## 3. Context vs EventData

| 特性 | EventData | Context |
|------|-----------|---------|
| 类型 | `readonly record struct` | `class` |
| 可变性 | 不可变 | 可变 |
| 用途 | 单向通知 | 请求-响应 |
| GC | 零 GC（栈分配） | 有 GC（堆分配） |
| 场景 | 高频事件 | 低频检查/请求 |

## 4. EventContext API

| API | 用途 |
|-----|------|
| `SetFailed(reason)` | 标记失败并记录原因 |
| `SetResult<T>(result)` | 写入强类型结果 |
| `GetResult<T>()` | 读取结果 |
| `StopPropagation()` | 设置停止传播标志 |

注意：当前 EventBus 不会自动解析 payload 内的 `EventContext`，`StopPropagation()` 只是上下文标志，不会强制跳过后续 handler。需要 handler 约定检查。

## 5. 最佳实践

**推荐：**
- Context 用于低频"检查"或"请求"
- 检查完后立即使用结果
- EventData 用于高频通知

**避免：**
- 在热路径（`_Process`）中创建 Context（堆分配）
- 把 Context 用于纯通知场景
