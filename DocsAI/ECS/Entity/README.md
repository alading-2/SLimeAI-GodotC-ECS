# Entity 文档入口

> 状态：current
> 更新：2026-05-31
> 范围：`Src/ECS/Base/Entity/`、`Src/ECS/Base/Event/`、`Data/DataKey/Generated/`、`Data/EventType/`。
> 设计事实源：`../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/`。

## 1. 先读什么

当前 Entity 文档按下面顺序读取：

1. `Entity使用说明.md`：当前可执行用法和 hard cutover 边界。
2. `../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/06-2026-05-31-DataEventDocsAI同步校准.md`：Data / Event / DocsAI 更新后的 Entity 执行前 override。
3. `../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/README.md`：Entity / Relationship hard cutover 设计包入口。
4. `Entity规范.md`、`EntityManager.md`：从 `Src/ECS` 迁入的历史原文，只用于理解旧实现，不作为新代码示例。

## 2. 当前裁决

Entity hard cutover 方向不变：

- Entity 仍是纯身份和运行时状态容器；业务逻辑放 Component / System / Service。
- 创建统一走 `EntityManager.Spawn/Register`，销毁统一走 `EntityManager.Destroy`。
- 旧 `EntityRelationshipManager / EntityRelationshipType / ParentRelationTypes / EntityManager_Ability` 不再作为新设计入口。
- Entity identity 的目标 runtime API 是 typed `EntityId`；`GeneratedDataKey.Id` 只作为 DataOS / snapshot / observation 的字符串投影。
- 当前 DataOS 还没有原生 `entity_id/entity_id_list` valueType；默认用 generated string / string_array projection，转换集中在 owner service / helper 内。
- Event 当前以 payload 类型作为事件 key；新增 Entity lifecycle 事件必须用 `readonly record struct`，不新增字符串事件名或 `XxxEventData`。

## 3. 不再采用的旧写法

下面写法只能作为旧代码审计对象：

- 在 Entity 模板里手写 `public string EntityId` 并用 `GetInstanceId().ToString()` 生成身份。
- 用 `DataKey.Id` 当 Entity identity 入口。
- 用 `EntityRelationshipManager` 表达 projectile / effect / ability / UI / component owner。
- 用 `GameEventType.Unit.DeadEventData`、`PropertyChangedEventData` 这类 `XxxEventData` 双写事件。
- 在 current 文档或新代码中恢复 `DocsNew`、`Src/ECS/**.md` 作为框架文档入口。

## 4. 验证入口

文档类更新至少运行：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
python3 Workspace/SDD/sdd.py validate --all
find Src/ECS -type f -name '*.md' | sort
```

代码实现阶段再按影响面运行：

```bash
Tools/run-build.sh
Tools/run-tests.sh
```
