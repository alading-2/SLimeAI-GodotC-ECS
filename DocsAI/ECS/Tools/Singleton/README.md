# Singleton Guard 工具

> 状态：current
> sourcePaths: `Src/ECS/Tools/Singleton/`
> relatedDesign: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/ECS框架优化/2.单例工具/README.md`
> lastReviewed: 2026-06-06

## 定位

Singleton Guard 是非继承式单例守卫工具，用于统一 Godot Node 单例的绑定、重复实例处理和退出释放。

它不是 `Singleton<T> : Node` 基类。框架内的 `Node`、`CanvasLayer`、`Control`、`Node2D` 等类型可能已经占用继承位，因此单例复用必须通过工具函数完成。

## API

```csharp
SingletonInstanceGuard.TryBind(candidate, ref instance);
SingletonInstanceGuard.Release(candidate, ref instance);

NodeSingletonGuard.TryBind(this, ref _instance, _log);
NodeSingletonGuard.Release(this, ref _instance);
```

`SingletonInstanceGuard` 是纯 C# 引用绑定规则，不依赖 Godot API。

`NodeSingletonGuard` 是 Godot Node 包装，检测到重复实例时默认输出日志并 `QueueFree()` 重复节点。

## 使用规则

推荐写法：

```csharp
private static MySystem? _instance;
public static MySystem? Instance => _instance;

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

保留非 nullable `Instance` 只用于已有高频兼容入口，例如 `TimerManager.Instance`。新增系统默认不要公开非 nullable `Instance`。

## 边界

Singleton Guard 做：

- 绑定首次实例。
- 拒绝重复实例覆盖旧实例。
- 默认销毁重复 Node。
- 只在当前实例退出时清空引用。

Singleton Guard 不做：

- 不创建场景实例。
- 不注册 `SystemRegistry`。
- 不处理 Autoload。
- 不做 lazy create。
- 不替代 `SystemManager.Execute`。
- 不管理纯静态工具类或 `GlobalEventBus.Global`。

## 当前迁移状态

首批已接入：

- `Src/ECS/Tools/Timer/TimerManager.cs`
- `Src/ECS/Capabilities/Damage/System/DamageService.cs`
- `Src/ECS/Capabilities/TestSystem/System/TestSystem.cs`

暂不迁移：

- `SystemManager`：唯一 Autoload 根，保留显式逻辑。
- `SpawnSystem` / `RecoverySystem`：后续优先减少业务系统直接 `Instance` 调用。

## 验证

```bash
dotnet run --project Tools/SingletonGuardTdd/SingletonGuardTdd.csproj
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```
