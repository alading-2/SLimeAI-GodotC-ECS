# Entity 模块契约

本文是 AI 修改 Entity 生命周期、对象池、关系和迁移逻辑前必须阅读的执行契约。原始说明见 `Src/ECS/Base/Entity/Entity规范.md`、`Src/ECS/Base/Entity/Core/EntityManager.md` 和 `Docs/框架/项目索引.md`。

## 职责边界

Entity 是实现 `IEntity` 的 Godot Scene，不是业务逻辑类。

Entity 负责：

- 持有 `Data`。
- 持有局部事件总线 `Events`。
- 作为 Component 的挂载点。
- 提供 `EntityId`、场景节点和必要接口。

Entity 不负责：

- 写核心战斗、移动、死亡、AI 流程。
- 直接管理其他实体生命周期。
- 绕过 `EntityManager` 注册、销毁或查询组件。

## 核心入口

- `Src/ECS/Base/Entity/Core/IEntity.cs`
- `Src/ECS/Base/Entity/TemplateEntity.cs`
- `Src/ECS/Base/Entity/Core/EntityManager.cs`
- `Src/ECS/Base/Entity/Core/EntityManager_Component.cs`
- `Src/ECS/Base/Entity/Core/EntityManager_Relationship.cs`
- `Src/ECS/Base/Entity/Core/EntityManager_Migration.cs`
- `Src/ECS/Base/Entity/Core/EntityRelationshipManager.cs`
- `Src/ECS/Base/Entity/Core/EntityRelationshipTraversal.cs`
- `Src/ECS/Base/Entity/Core/EntityRelationshipLifecycle.cs`

## 数据 / 事件 / 生命周期

- 生成统一走 `EntityManager.Spawn<T>(EntitySpawnConfig)`。
- 注册已存在节点统一走 `EntityManager.Register`。
- 销毁统一走 `EntityManager.Destroy`。
- `Spawn` 内部完成 Config 注入、VisualRoot 注入、位置旋转、组件注册和对象池激活。
- `ParentEntity + AutoAddParentRelation + ParentDestroyPolicy + ParentRelationTypes` 是生成阶段绑定父子关系的主入口。
- `PARENT` 是唯一归属主链，业务关系只做分类查询。
- Entity 迁移固定语义是“新建目标 Entity -> 迁移受控 Data -> 销毁源 Entity”。
- 迁移不复制 `Entity.Events` 订阅、Component 私有状态、`VisualRoot` 或整张关系图。

## 禁止事项

- 禁止 `new Entity()` 生成实体。
- 禁止直接 `QueueFree()` 销毁实体。
- 禁止业务层手写父销毁时顺手销毁子实体。
- 禁止手写关系溯源替代 `EntityRelationshipTraversal`。
- 禁止把业务流程写进 Entity `_Process` / `_PhysicsProcess`。
- 禁止把对象池 Entity 出池后立即恢复碰撞，必须走 EntityManager 的两阶段激活。

## 修改流程

1. 判断是否真的需要改 Entity 核心；普通功能优先用 Component / System / Event。
2. 涉及 `EntityManager`、关系、对象池、迁移时先读 `DocsAI/Workflows/ECS核心修改门禁.md`。
3. 列出影响范围：对象池、组件注册、事件清理、父子关系、迁移、VisualRoot。
4. 做最小改动，不顺手重构 unrelated 模块。
5. 更新本契约、项目索引或相关 Skill。

## 推荐测试

- `dotnet build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/ECSTest/ECSTestScene.tscn --build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/Tools/ObjectPool/ObjectPoolManagerTest.tscn --build`
- 核心改动后运行 `run-all --build --continue-on-fail`。

## 人工审查重点

- 是否绕过 `EntityManager`。
- 是否破坏对象池复用、碰撞禁用和激活时序。
- 是否破坏 `PARENT` 主链和父销毁策略。
- 是否把迁移做成深拷贝或万能克隆。
- 是否让 Entity 重新承担业务逻辑。
