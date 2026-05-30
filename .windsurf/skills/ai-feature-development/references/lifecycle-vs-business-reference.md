# Lifecycle vs Business Reference

> Baseline 由当前 SDD design、相关 Capability contract 与 `Src/ECS/**` 旁文档管理。历史基线见 `openspec/specs/runtime-relationship-lifecycle/spec.md` 与 `openspec/specs/runtime-business-entity-references/spec.md`。

读取时机：需要表达 owner、source、target、equipped-by、spawned-by、affected-by、UI mapping、entity-to-component mapping 或父子生命周期时读取。

## 边界

Lifecycle tree 只表达父子生命周期：

- parent entity
- child entity
- destroy policy
- priority

Business reference 表达玩法 / 能力语义：

- 技能拥有者
- 投射物来源
- effect 来源 / 目标
- 装备目标
- UI / adapter 映射
- 生成者 / 被影响者

业务引用必须由 owner Capability 定义 typed `DataKey<EntityId?>`、`DataKey<EntityIdList>` 或专用 typed record。不要把业务字段塞进 lifecycle link。

## 正确入口

- 生命周期绑定：`world.Lifecycle.Attach(parent, child, policy, priority)` 或 `EntitySpawnConfig.ParentEntityId / ParentDestroyPolicy`。
- 多个 owned child：`EntityIdList` + owner cleanup hook。
- 单一 source / target：owner Capability 的 typed `DataKey<EntityId?>`。
- adapter / node 映射：GodotBridge owner registry，不复用 lifecycle tree 表达业务关系。

## 反模式

- 新增 `RelationshipType.Owner / Target / Source / EquippedBy` 等业务关系类型。
- 在 `LifecycleLink` 或 lifecycle record 里塞 `Dictionary<string, object>`。
- 用 lifecycle parent 表示技能拥有者、投射物来源或 UI owner。
- 为了查询方便把所有关系塞回通用 relationship manager。

## 检查命令

```bash
rg -n "RelationshipManager|RelationshipType|RelationshipRecord|RelationshipLifecycle" SlimeAI Games/BrotatoLike -S
```

代码中出现旧符号时，先判断是 migration note、spec 反例还是真实调用。真实调用必须迁移。
