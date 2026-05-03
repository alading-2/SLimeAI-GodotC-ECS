---
name: ecs-entity
description: 创建新 Entity、管理 Entity 生命周期（Spawn/Register/Destroy）、实现 IEntity 接口、配置对象池时使用。适用于：新建敌人/子弹/玩家/技能等实体，处理 Entity 的生成销毁，实现 IPoolable 接口。触发关键词：新建实体、Entity、IEntity、IPoolable、对象池生成、EntityManager.Spawn。
---

# ECS Entity 入口

## 什么时候用

- 新建或修改 Entity 场景 / 脚本。
- 使用 `EntityManager.Spawn/Register/Destroy`。
- 修改 EntityManager、关系、迁移或对象池激活流程。
- 处理 `IEntity`、`IPoolable`、父子关系、迁移。

## 转向其它 Skill

- Component 逻辑 -> `@ecs-component`
- Data 容器读写 -> `@ecs-data`
- 对象池工具细节 -> `@tools`
- 伤害结算 -> `@damage-system`

## 必读

- `DocsAI/Modules/Entity.md`
- 涉及 EntityManager、关系、迁移、对象池激活时读 `DocsAI/Workflows/ECS核心修改门禁.md`
- 测试选择读 `DocsAI/Tests/测试矩阵.md`

## 最短流程

1. 判断是否真的需要改 Entity 核心；普通功能优先放 Component / System。
2. 查相近 Entity 和场景结构。
3. 生成走 `EntityManager.Spawn`，销毁走 `EntityManager.Destroy`。
4. 对象池 Entity 检查 `IPoolable`、Data 清理、事件清理和碰撞激活时序。
5. 关系和迁移改动先列影响范围，再做最小改动。
6. 运行构建和 ECS / ObjectPool 相关场景。
7. 更新 `DocsAI/Modules/Entity.md`、项目索引或相关文档。

## 禁止事项

- 不要 `new Entity()`。
- 不要直接 `QueueFree()` 销毁实体。
- 不要业务层手写父销毁级联。
- 不要绕过 `EntityRelationshipTraversal` 手写关系溯源。
- 不要把业务流程写进 Entity `_Process` / `_PhysicsProcess`。

## 推荐验证

```bash
dotnet build
node .claude/skills/GodotSkill/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/ECSTest/ECSTestScene.tscn --build
node .claude/skills/GodotSkill/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/Tools/ObjectPool/ObjectPoolManagerTest.tscn --build
```
