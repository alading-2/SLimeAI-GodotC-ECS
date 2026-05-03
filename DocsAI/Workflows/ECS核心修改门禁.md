# ECS 核心修改门禁

本文定义 AI 修改 ECS 核心前后的强制检查。普通 Component / System 功能开发不需要全量门禁，但只要触碰核心高风险区就必须执行。

## 核心高风险区

| 风险等级 | 目录/文件 | 影响范围 |
|----------|----------|----------|
| 🔴 极高 | `Src/ECS/Base/Entity/Core/EntityManager*.cs` | Entity 生成/注册/销毁，错误导致泄漏、崩溃 |
| 🔴 极高 | `Src/ECS/Base/Entity/Core/EntityRelationshipManager.cs` | 父子关系/级联销毁/迁移，错误导致孤立节点 |
| 🔴 极高 | `Src/ECS/Base/Entity/Core/EntityRelationshipTraversal.cs` | 关系溯源，错误导致 FindAncestorOfType 失效 |
| 🔴 极高 | `Src/ECS/Base/Entity/Core/EntityRelationshipLifecycle.cs` | 销毁策略、级联策略 |
| 🔴 极高 | `Src/ECS/Base/Entity/Core/EntityManager_Migration.cs` | 实体迁移，错误导致数据丢失 |
| 🔴 极高 | `Src/ECS/Base/Event/` | EventBus / GlobalEventBus，错误导致事件丢失、内存泄漏 |
| 🔴 极高 | `Src/ECS/Base/System/Core/SystemManager.cs` | 系统启停/状态切换，错误导致系统无法启动 |
| 🔴 极高 | `Src/ECS/Base/System/Core/SystemRegistry.cs` | 系统注册/发现 |
| 🔴 极高 | `Src/ECS/Tools/ObjectPool/ObjectPool.cs` | 对象池激活/回收/碰撞时序 |
| 🟡 高 | `Src/ECS/Tools/Timer/` | TimerManager，错误导致定时器泄漏或回调丢失 |
| 🟡 高 | `Data/ResourceManagement/` | ResourceManagement / ResourcePaths，错误导致资源加载失败 |
| 🟡 高 | `Src/ECS/Base/Component/IComponent.cs` | Component 注册/注销接口 |

## 修改前必须回答

- 为什么必须改核心？有没有用 Component / System / Event / Data 解决的替代方案？
- 会影响哪些 System / Component / Entity？列出具体类型。
- 会影响对象池、碰撞、关系、事件清理或系统门禁吗？
- 旧场景或旧数据是否需要迁移？
- 如何回滚？

## 修改中规则

- 小步改动，不做无关重构。
- 不删除旧兼容路径，除非明确验证所有引用。
- 不改变公开 API 语义，除非同步更新文档、Skill 和测试。
- 核心错误日志必须能定位模块、实体或系统。

## 修改后回归矩阵

### 最低验证（所有核心修改）

```bash
dotnet build
node .claude/skills/GodotSkill/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/ECSTest/ECSTestScene.tscn --build --attempts 2 --errors-only --log-dir .ai-temp/scene-tests/runs
node .claude/skills/GodotSkill/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/SystemCore/SystemCoreRuntimeTest.tscn --build --attempts 2 --errors-only --log-dir .ai-temp/scene-tests/runs
```

### 按影响范围追加

| 修改范围 | 追加测试场景 |
|----------|-------------|
| Data / DataKey | `DataTestScene`, `TestDataKeyMapping` |
| Event / Entity 生命周期 | `ECSTestScene` |
| ObjectPool | `ObjectPoolManagerTest` |
| Damage / 伤害管道 | `DamageSystemTest` |
| Ability / Feature | `AbilitySystemPipelineTest`, `ActiveSkillInputTest` |
| Movement / 碰撞 | `MovementComponentTestScene`, `MovementCollisionRuntimeTest` |
| Spawn | `SpawnTestScene` |
| EntityManager.Relationship | `MovementCollisionRuntimeTest` (级联销毁/迁移) |

### 大范围核心改动（发布前）

```bash
node .claude/skills/GodotSkill/scripts/godot-scene-runner.mjs run-all --build --continue-on-fail --attempts 2 --errors-only --log-dir .ai-temp/scene-tests/runs
```

## 最终回复必须包含

- 核心高风险区是否被修改。
- 修改前替代方案判断。
- 已运行验证命令和结果。
- 失败日志摘要（如有）。
- 建议人工重点审查文件。

## 门禁豁免

以下情况可跳过部分验证：
- 纯文档修改（Docs/、DocsAI/、Plans/、.md 文件）
- 纯 DataKey 新增（不涉及 Data 容器核心逻辑）
- 测试场景本身的新增或修改（不影响核心代码）
- `.codex/` 目录修改（用户自行管理）

## 人工审查重点

- 生命周期顺序是否改变。
- 对象池复用是否会产生脏状态。
- 事件订阅是否泄漏或重复。
- 系统运行态门禁是否绕过。
- 资源加载是否出现新硬编码路径。
- `ForceUpdateTransform` 是否有 `IsInsideTree` 守卫。
