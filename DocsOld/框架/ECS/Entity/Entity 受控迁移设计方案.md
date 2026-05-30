# Entity 受控迁移设计方案

## Summary

当前这套 Godot Scene 型 ECS，不适合做“Entity 直接变成另一种 Entity”。成熟 ECS 的主流做法也不是这样：

- Unity Entities 把“类型变化”建模为 增删组件导致 archetype 变化，实体会被移动到新 archetype，而不是换 OO 类。
- Bevy 的 EntityClonerBuilder 是 显式选择复制什么组件、是否移动、是否重映射引用。
- EnTT 的 snapshot / continuous_loader 是 选择性拷贝组件数据并重建标识映射，不是万能克隆。
- Flecs 也是 实体 + 组件 + 关系 / prefab 继承，强调显式结构，不强调对象级“变身”。

结合你当前实现，v1 应定义为：

- 新建目标 Entity
- 迁移受控的 Data
- 不迁移局部 EventBus 订阅
- 只接管主归属链和少量显式声明关系
- 迁移成功后默认销毁源实体

这不是“100% 迁移”，而是“玩法可用的受控替换”。

## Implementation Changes

### 1. 公共入口定义

在 EntityManager 增加统一迁移入口，保持生命周期入口统一：

- EntityManager.Migrate<TTarget>(Node source, EntityMigrationConfig config)
- 新增 EntityMigrationConfig
- 新增 EntityMigrationProfile

EntityMigrationConfig 只保留 v1 必需字段：

- TargetSpawn：复用现有 EntitySpawnConfig
- Profile：数据/关系迁移规则；默认使用安全默认值
- DataOverrides：迁移后覆写到目标实体的数据
- InheritDirectParent：默认 true
- 不做 KeepSource / DisableSource / PreserveId 这些扩展，v1 固定为“替换并销毁”

### 2. 迁移语义固定

迁移流程固定为：

1. 校验源实体已注册，且实现 IEntity
2. 拍源快照：
    - Data.GetAll()
    - 直接 PARENT
    - PARENT 上的 ParentDestroyPolicy
    - Node2D 位置/旋转
3. 组装目标生成配置：
    - 位置/旋转默认继承源实体
    - 若 TargetSpawn.ParentEntity 未显式指定，且 InheritDirectParent=true，则继承源实体直接父级
4. EntityManager.Spawn<TTarget>()
5. 过滤并迁移 Data
6. 迁移允许接管的关系
7. 发迁移事件
8. EntityManager.Destroy(source)

失败处理固定为：

- 目标生成失败：源实体保持原状
- 目标生成后、数据/关系迁移失败：销毁目标实体并保留源实体
- 不允许半迁移状态留在场景里

### 3. Data 迁移边界

v1 的核心是“Data 可迁移，且只迁可解释的数据”。

默认规则：

- 不迁移 Id
- 不迁移 Entity.Events
- 不迁移 Component 私有字段
- 不迁移视觉场景 / 节点树 / 组件树运行时状态
- 默认只迁移“安全值”：
    - 数值、布尔、字符串、枚举
    - 常见 Godot 值类型，如 Vector2
    - Resource
- 默认跳过危险引用：
    - Node
    - IEntity
    - IComponent
    - 委托 / 事件 / EventBus
    - 其他明显绑定旧实例生命周期的对象引用

为避免把通用迁移做成硬编码黑名单，建议在 DataMeta 增加一个最小迁移属性，而不是复杂策略机：

- CanMigrate，默认 true
- 框架保留键如 Id 标记为 false
- Profile 再做 IncludeKeys / ExcludeKeys / Overrides

Profile 默认策略建议是：

- 先按 CanMigrate 过滤
- 再按安全值类型过滤
- 再按 ExcludeKeys
- IncludeKeys 允许显式放行
- DataOverrides 最后应用

不要做“默认完整复制所有 key 并覆盖目标一切”的方案。跨类型迁移时，这会把旧类型的运行时脏数据灌进新类型。

### 4. Event / Component / Visual 不迁移

这一条必须写进规范，而不是留成“默认大家都懂”：

- Entity.Events 不迁移订阅表
- 目标实体的事件订阅，必须依赖目标实体自己的 Spawn -> RegisterComponents -> OnComponentRegistered
- 任何想跨迁移保留的状态，都必须先落进 Data
- 视觉场景不能迁移，只能重新实例化目标实体自己的视觉
- Component 私有状态不保证连续；如确实需要连续，必须显式数据化

也就是说，迁移的连续性来自 Data contract，不是来自节点对象本身。

### 5. 关系迁移只做主归属链

v1 默认只做这些：

- 接管源实体的直接 PARENT
- 继承该 PARENT 上的 ParentDestroyPolicy
- 可选接管少量显式声明的“父 -> 子”业务关系类型

v1 默认不做这些：

- 不自动迁移所有入边
- 不自动迁移所有出边
- 不自动接管源实体的 owned children
- 不重写整个关系图中所有指向源实体的引用

原因很直接：

- 你现在的 PARENT 已经承担归属和生命周期语义
- 旧实体 owned children 默认应跟着旧实体销毁
- 如果要“整图替换节点”，那已经不是 v1 迁移，是图重写器了，复杂度会失控

### 6. 适用场景与非适用场景

适合先支持的真实场景：

- 单位死亡后替换成尸体/掉落物
- 投射物结束后替换成地面效果/陷阱
- 召唤物卵/种子成熟后替换成单位
- 需要完全不同组件树和根节点类型的阶段切换

不建议用迁移解决的场景：

- 同一实体只是状态变化
- 同一实体只是换皮/换视觉
- 只是增减少量能力
- 只是临时挂载效果

这些场景优先用：

- Data 状态切换
- Component 启停
- 视觉场景更新
- Ability / Feature / Status 机制

## Public APIs / Types

建议新增或调整的公开接口：

- EntityManager.Migrate<TTarget>(Node source, EntityMigrationConfig config)
- EntityMigrationConfig
- EntityMigrationProfile
- DataMeta.CanMigrate

建议新增迁移事件：

- GameEventType.Global.EntityMigrating
- GameEventType.Global.EntityMigrated

事件负载最少包含：

- SourceEntity
- TargetEntity
- ProfileName 或 ProfileId

不要新增“保持旧 EntityId”接口。v1 固定：

- 目标实体拿自己的新 EntityId
- 如玩法需要溯源，只在 Data 写 SourceEntityId / OriginEntityId

## Test Plan

至少覆盖这些运行时/单测场景：
- Id 不被复制，目标实体保留新 EntityId
- Entity.Events 订阅不被复制
- 目标实体的组件通过正常注册流程重新订阅
- 源实体直接 PARENT 能接管到目标实体
- 目标生成失败时，源实体保持不变
- 关系迁移失败时，目标实体回滚销毁，源实体保持不变

- Src/ECS/Base/Entity/Entity规范.md
- Docs/框架/ECS/Entity/Entity架构设计理念.md
- Docs/框架/ECS/Entity/EntityManager设计说明.md

另外同步更新：

- ecs-entity skill
- 项目索引

## Assumptions

- v1 目标是“玩法替换迁移”，不是快照系统、存档系统、网络复制系统
- v1 默认策略是“新实体 + 受控 Data 迁移 + 销毁旧实体”
- v1 不保留旧 EntityId
- v1 不重写整张关系图
- 需要连续迁移的状态，今后统一要求写入 Data
- 若某场景只是状态变化，不应滥用迁移

## Sources

- Unity Entities archetypes: https://docs.unity.cn/Packages/com.unity.entities%401.0/manual/concepts-archetypes.html
- Unity EntityManager.Instantiate: https://docs.unity.cn/Packages/com.unity.entities%401.0/api/Unity.Entities.EntityManager.Instantiate.html
- Bevy EntityClonerBuilder: https://docs.rs/bevy/latest/bevy/ecs/entity/struct.EntityClonerBuilder.html
- EnTT entity docs / snapshot loader: https://skypjack.github.io/entt/md_docs_2md_2entity.html
- Flecs entities/components: https://www.flecs.dev/flecs/md_docs_2EntitiesComponents.html
- Flecs prefabs manual: https://www.flecs.dev/flecs/md_docs_2PrefabsManual.html