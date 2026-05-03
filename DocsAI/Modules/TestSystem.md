# TestSystem 模块契约

本文是 AI 修改运行时测试系统时的执行契约。详细人类说明见 `Docs/框架/ECS/System/TestSystem/TestSystem.md`。

## 系统定位

`TestSystem` 是运行时调试宿主，负责：

- 调试 UI 入口
- 选中实体
- 模块注册与切换
- 刷新当前测试模块

它不负责：

- 正式玩家 UI
- 技能主动执行前台
- 复制独立 Feature / Ability 生命周期
- 视觉资源预览场景

涉及能力生命周期时，必须复用：

- `EntityManager`
- `FeatureSystem`
- `AbilitySystem`
- `FeatureDebugService`

## 核心入口

- 宿主：`Src/ECS/Base/System/TestSystem/TestSystem.cs`
- 模块基类：`Src/ECS/Base/System/TestSystem/TestModuleBase.cs`
- 模块核心：`Src/ECS/Base/System/TestSystem/Core/`
- 属性模块：`Src/ECS/Base/System/TestSystem/Attribute/`
- 技能模块：`Src/ECS/Base/System/TestSystem/Ability/`
- 系统监控：`Src/ECS/Base/System/TestSystem/System/`
- 对象池信息：`Src/ECS/Base/System/TestSystem/Info/`
- 资源目录：`Src/ECS/Base/System/TestSystem/ResourceCatalog/`
- 敌人生成：`Src/ECS/Base/System/TestSystem/Spawn/`

视觉预览已迁出 TestSystem，运行：

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn --build
```

## 宿主原则

- `TestSystem` 只负责面板骨架、模块清单、模块切换、选中实体和局部事件。
- 不把具体业务测试逻辑堆进 `TestSystem.cs`。
- 模块初始化失败必须隔离：单个模块失败不能拖垮整个宿主。
- 模块路径使用完整点分路径，如 `信息.对象池`、`技能.技能测试`。
- 模块顺序使用 `TestModuleSceneRegistry` 清单顺序。

## 模块开发流程

1. 新建 `MyTestModule.cs`，继承 `TestModuleBase`。
2. 新建 `MyTestModule.tscn`，固定 UI 骨架放场景里。
3. 实现稳定 `Definition.Id` 和完整 `Definition.ModulePath`。
4. 实现 `Initialize(...)`、`Refresh()`。
5. 必要时实现 `OnSelectedEntityChanged / OnActivated / OnDeactivated`。
6. 在 `TestModuleSceneRegistry` 登记模块场景。
7. 如需调用正式系统能力，先新增 Service / Adapter，不要把系统调用散落进按钮回调。

## 关键边界

- 属性测试：
  - 状态覆写走 `entity.Data.Set(...)`。
  - 临时加成走 `FeatureDebugService.ApplyTemporaryModifier(...)`。
  - 临时加成只展示 `IsNumeric && SupportModifiers && !IsComputed` 的 DataMeta。
- 技能测试：
  - 只做添加、移除、启用、禁用技能。
  - 不新增主动触发技能按钮或测试版 AbilityExecutor。
  - 分组优先使用完整 `AbilityData.FeatureGroupId`。
- 资源目录：
  - 使用 `ResourceCatalog.GetEntries(...)`、`ResourcePaths.Resources`、`ResourceManagement.Load<T>(...)`。
  - 不在运行时测试面板全盘扫描 `res://` 作为主数据源。
- 系统监控：
  - 只通过 `SystemInfoService` 汇总展示数据。
  - 系统操作走 `SystemManager.TryAddSystem / TryRemoveSystem / TrySetSystemEnabled`。
  - `Required == true` 或被依赖系统禁止禁用 / 移除。
- 敌人生成：
  - 通过 `SystemManager.Execute<SpawnSystem, SpawnBatchRequest, SpawnBatchResult>(...)`。
  - 不直接 `new EnemyEntity()` 或 `QueueFree()`。

## TestSystem 特殊规则

- `Data.Get/Set` 必须显式使用 `DataKey.XXX.Key`，不要依赖 `DataMeta` 到 `string` 的隐式转换。
- UI 控件日志使用 `Info / Warn / Error`，不要使用 `Debug`。
- 后台模块失活后不要继续响应实体事件。
- 复杂 UI 条目应拆成独立 `.tscn`，不要在代码里大量 `new Label / Button / SpinBox` 拼布局。

## 推荐测试

- `dotnet build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build`
- 涉及视觉预览时运行 `VisualPreviewScene.tscn`

## 禁止事项

- 不要把 TestSystem 做成正式玩家 UI。
- 不要在 TestSystem 内新增技能主动触发前台。
- 不要绕开 `FeatureSystem` / `EntityManager` 复制能力生命周期。
- 不要让 TestSystem 模块实现 `IFeatureHandler` 或伪造 FeatureEntity。
- 不要恢复宿主级 `TestRefreshScheduler`。
- 不要直接编辑计算属性。
- 不要让后台模块持续保留事件订阅。

