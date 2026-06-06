# 单例工具设计

> 状态：current
> 范围：`Src/ECS/Tools/Singleton/`
> 更新：2026-06-06

## 1. 背景

当前框架中存在少量 Godot Node 级单例入口：

- `SystemManager.Instance`：项目唯一 Autoload 根，负责 System Core 启动和系统托管。
- `TimerManager.Instance`：高频基础设施入口，供 timer facade 和组件 cancel point 使用。
- `DamageService.Instance`：伤害服务兼容入口和统计处理器内部入口。
- `TestSystem.Instance`：运行时测试面板入口。
- `SpawnSystem.Instance`、`RecoverySystem.Instance`：仍存在的业务系统访问口，后续更适合逐步迁到 `SystemManager.Execute`。

这些单例的核心问题不是少写几行 `Instance = this`，而是生命周期策略不一致：

- 有的重复实例会 `QueueFree`，有的直接覆盖。
- 有的 `_ExitTree` 直接置空，有的先判断 `Instance == this`。
- 有的在 `_EnterTree` 绑定，有的在 `_Ready` 绑定。
- `CanvasLayer`、`Control`、`Node2D` 等类型已经占用基类，不能统一继承 `Singleton<T> : Node`。

## 2. 目标

新增一个非继承式单例守卫工具，用于统一“绑定、重复检测、退出释放”三件事：

```text
SingletonInstanceGuard  // 纯 C# 引用绑定规则，可 TDD
NodeSingletonGuard      // Godot Node 包装，负责日志和重复节点 QueueFree
```

目标边界：

- 不创建继承式 `Singleton<T>` 基类。
- 不替代 `SystemManager` / `SystemRegistry` / `ISystem` 生命周期。
- 不负责实例化场景、Autoload、lazy create 或系统注册。
- 不鼓励业务代码新增 `XxxSystem.Instance` 调用；业务系统命令仍优先走 `SystemManager.Execute`。

## 3. 方案

### 3.1 纯规则层

`SingletonInstanceGuard` 只处理引用：

- `TryBind(candidate, ref instance)`：当前实例为空或就是自己时绑定成功。
- 当前实例为另一个对象时，不覆盖旧实例，并通过 `onDuplicate` 回调暴露重复候选。
- `Release(candidate, ref instance)`：只有当前实例就是 `candidate` 时才清空。

这一层不依赖 Godot API，可以用普通 `dotnet run` 做 RED/GREEN 测试。

### 3.2 Godot Node 包装层

`NodeSingletonGuard` 面向 Godot Node：

- 检测到重复 Node 时输出日志。
- 默认对重复候选调用 `QueueFree()`。
- 释放时复用纯规则层，避免旧节点离树时误清掉新实例。

示例：

```csharp
private static DamageService? _instance;
public static DamageService? Instance => _instance;

public override void _EnterTree()
{
    if (!NodeSingletonGuard.TryBind(this, ref _instance, _log))
    {
        return;
    }

    // 当前系统自己的初始化逻辑
}

public override void _ExitTree()
{
    NodeSingletonGuard.Release(this, ref _instance);
}
```

## 4. 首批迁移范围

本切片只迁移代表性且风险可控的三处：

- `TimerManager`：补重复实例保护，保持非 nullable `Instance` 兼容既有高频调用。
- `DamageService`：用 guard 替代手写重复实例逻辑。
- `TestSystem`：证明 `CanvasLayer` 场景不需要继承式单例基类也能复用单例守卫。

不迁移：

- `SystemManager`：它是唯一 Autoload 根和系统生命周期总控，保留显式逻辑更清楚。
- `SpawnSystem` / `RecoverySystem`：后续优先考虑减少业务系统 `Instance` 依赖，迁往 `SystemManager.Execute`。
- `GlobalEventBus.Global`、`DataRuntimeBootstrap.Default`、各类 `static class` 工具：它们不是 Godot Node 单例。

## 5. 风险和约束

- `TimerManager.Instance` 保持原 API 形态，避免大量调用点改成 nullable；调用方仍需保证系统启动后再使用。
- 重复实例默认销毁重复候选，不会覆盖旧实例。
- `_ExitTree` 只释放当前实例，防止旧节点退出时清空新实例。
- 工具只做生命周期守卫，不解决系统执行入口治理；新增业务能力仍应优先设计 command handler。

## 6. 验证

新增轻量 TDD 入口：

```bash
dotnet run --project Tools/SingletonGuardTdd/SingletonGuardTdd.csproj
```

框架构建验证：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

AI 配置同步和 skill lint：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```
