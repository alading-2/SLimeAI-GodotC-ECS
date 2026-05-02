---
name: ecs-component
description: 创建新 Component、实现 IComponent 接口、在组件中读写 Data、订阅 Entity.Events 事件时使用。适用于：新建功能组件（移动/攻击/血量/动画等），组件间通信，组件生命周期管理。触发关键词：新建组件、Component、IComponent、OnComponentRegistered、组件通信。
---

# ECS Component 入口

## 什么时候用

- 新建或修改 Component。
- 实现 `IComponent` 生命周期。
- 在组件中读写 `Entity.Data`。
- 订阅或发布 `Entity.Events`。
- 修改碰撞组件、运动组件、朝向组件等组件侧逻辑。

## 转向其它 Skill

- Entity 生成、销毁、对象池生命周期 -> `@ecs-entity`
- DataKey 定义、Data 容器规则 -> `@ecs-data`
- EventBus 协议或事件类型 -> `@ecs-event`
- 伤害处理 -> `@damage-system`
- Godot 场景测试 -> `@godot-scene-test`

## 必读

- `DocsAI/Modules/Component.md`
- 涉及 Entity 生命周期或组件注册核心时读 `DocsAI/Workflows/ECS核心修改门禁.md`
- `Src/ECS/Base/Component/TemplateComponent.cs`
- `Src/ECS/Base/Component/IComponent.cs`
- `Src/ECS/Base/Component/Component规范.md`

## 最短流程

1. 查是否已有类似 Component。
2. 读 `DocsAI/Modules/Component.md`。
3. 从模板或相近组件复制结构。
4. 在 `OnComponentRegistered` 绑定 `IEntity` / `Data` 并订阅事件。
5. 在 `OnComponentUnregistered` 清理引用。
6. 运行 `dotnet build` 和相关场景测试。
7. 更新 `Docs/框架/项目索引.md`、`DocsAI/Modules/Component.md` 或相关文档。

## 关键禁止事项

- 不要私有字段保存共享业务状态。
- 不要在 `_Ready()` 订阅核心事件。
- 不要直接调用其它 Component 方法。
- 不要用 Godot Signal 承载核心逻辑。
- 不要在 `_Process` 中 `new` 对象或使用 LINQ。

## 推荐验证

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list --filter Movement
```
