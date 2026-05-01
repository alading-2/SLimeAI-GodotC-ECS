# ECS 核心修改门禁

本文定义 AI 修改 ECS 核心前后的强制检查。普通 Component / System 功能开发不需要全量门禁，但只要触碰核心高风险区就必须执行。

## 核心高风险区

- Entity 创建、注册、销毁：`Src/ECS/Base/Entity/Core/EntityManager*.cs`
- Component 注册、注销、动态添加移除：`EntityManager_Component.cs`
- Entity 关系：`EntityRelationshipManager.cs`、`EntityRelationshipTraversal.cs`、`EntityRelationshipLifecycle.cs`
- Entity 迁移：`EntityManager_Migration.cs`
- EventBus / GlobalEventBus：`Src/ECS/Base/Event/`
- SystemManager / SystemRegistry / ProjectState：`Src/ECS/Base/System/Core/`
- ObjectPool 激活、回收和碰撞隔离：`Src/ECS/Tools/ObjectPool/`
- TimerManager 项目级暂停：`Src/ECS/Tools/Timer/`
- ResourceManagement 资源索引：`Data/ResourceManagement/`

## 修改前必须回答

- 为什么必须改核心？
- 有没有用 Component / System / Event / Data 解决的替代方案？
- 会影响哪些 System / Component / Entity？
- 会影响对象池、碰撞、关系、事件清理或系统门禁吗？
- 旧场景或旧数据是否需要迁移？
- 如何回滚？

## 修改中规则

- 小步改动，不做无关重构。
- 不删除旧兼容路径，除非明确验证所有引用。
- 不改变公开 API 语义，除非同步更新文档、Skill 和测试。
- 核心错误日志必须能定位模块、实体或系统。

## 修改后回归矩阵

最低验证：

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/ECSTest/ECSTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/SystemCore/SystemCoreRuntimeTest.tscn --build
```

按影响范围追加：

- Data：`DataTestScene.tscn`、`TestDataKeyMapping.tscn`
- Event / Entity：`ECSTestScene.tscn`
- ObjectPool：`ObjectPoolManagerTest.tscn`
- Damage：`DamageSystemTest.tscn`
- Ability：`AbilitySystemPipelineTest.tscn`、`ActiveSkillInputTest.tscn`
- Movement：`MovementComponentTestScene.tscn`、`MovementCollisionRuntimeTest.tscn`

大范围核心改动：

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-all --build --continue-on-fail
```

## 最终回复必须包含

- 核心高风险区是否被修改。
- 修改前替代方案判断。
- 已运行验证命令。
- 失败日志摘要。
- 建议人工重点审查文件。

## 人工审查重点

- 生命周期顺序是否改变。
- 对象池复用是否会产生脏状态。
- 事件订阅是否泄漏或重复。
- 系统运行态门禁是否绕过。
- 资源加载是否出现新硬编码路径。
