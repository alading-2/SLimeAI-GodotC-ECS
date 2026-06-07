# Entity Spawn 统一与业务 Facade 重构

> 更新：2026-05-31
> 状态：current design package
> 入口：`main.md`
> 范围：`Src/ECS/Base/Entity/Core/`、`Src/ECS/Base/System/EffectSystem/`、`Src/ECS/Base/System/ProjectileSystem/`、`Src/ECS/Base/System/AbilitySystem/`、`Src/ECS/Base/System/Spawn/`、`Src/ECS/Base/System/TargetingSystem/`、相关 SingleTest 场景。

## 0. 这份设计回答什么

本目录只回答一件事：

**Entity 生成到底怎么统一，才能对 AI 友好，又不会把 EntityManager 做成更大的业务巨物。**

结论先写在前面：

- 统一的是底层创建管线，不是把所有业务参数塞进一个万能 `Spawn`。
- 业务入口可以分开，但必须收口到同一条 `EntitySpawnPipeline`。
- `EntityManager.Spawn<T>` 保留，但只能做薄 facade，不再承载领域语义。
- `EffectTool`、`ProjectileTool`、`AbilityService`、`SpawnSystem`、`TargetingManager` 这类业务入口继续存在，但它们只能组装请求，不能自己做池化、实例化、注册、组件扫描、生命周期 attach。
- `PauseMenuSystem`、`TestSceneHelper` 这类非 Entity 场景实例化，不属于 Entity spawn，不应该强行纳入同一套 API。

## 1. 阅读顺序

1. `main.md` - 本目录主设计。
2. `../1.初级修改/03-LifecycleTree与业务引用设计.md` - 生命周期 parent 与业务引用边界。
3. `../1.初级修改/02-代码实现说明.md` - 当前目标代码形状。
4. `../1.初级修改/04-完全重构范围与TDD测试计划.md` - TDD 任务序和门禁。
5. `../1.初级修改/05-源码调用点迁移清单.md` - 当前散点调用位置。

## 2. 你会在这份设计里得到什么

- 为什么 `EffectTool.cs` 现在的写法最容易让人误判“Spawn 已经统一”，但实际上并没有。
- 为什么 `ProjectileTool.cs`、`EntityManager_Ability.cs`、`SpawnSystem.cs`、`TargetingManager.cs` 不能继续各写各的创建细节。
- `EntityManager.Spawn<T>` 应该保留到什么程度。
- 哪些东西属于 Entity spawn，哪些东西只是 Godot scene instantiate。
- 迁移时应该先收哪一层，后收哪一层。

