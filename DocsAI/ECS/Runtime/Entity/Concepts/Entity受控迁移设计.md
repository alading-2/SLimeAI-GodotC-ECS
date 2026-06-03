# Entity 受控迁移设计

> 类型：概念文档
> 范围：`Src/ECS/Runtime/Entity/Migration/`
> 事实源：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/`
> 状态：概念已确定，API `EntityManager.Migrate` 尚未实现

## 1. 核心语义

迁移不是"把一个实体原封不动变成另一个实体"。Godot Scene 型 Entity 的根节点脚本类型是写死的，不能像纯 ECS 那样靠增删组件直接变 archetype。

迁移的固定语义：
```
新建目标 Entity -> 迁移受控 Data -> 不迁移 Events/Component/视觉 -> 销毁源 Entity
```

## 2. 为什么这样设计

- `Entity.Events` 是当前实例上的订阅表，直接复制会把旧闭包和旧节点引用一起带过去
- Component 的真实运行状态依赖目标实体自己的组件树和注册时序，不适合生搬
- 视觉场景不能迁移，只能重新实例化目标实体自己的视觉

迁移的连续性来自 **Data contract**，不是来自节点对象本身。

## 3. 迁移边界

**迁移：**
- 受控 Data 字段（数值、布尔、字符串、枚举、Vector2）
- 直接 lifecycle parent（可选）
- 位置/旋转（默认继承）

**不迁移：**
- EntityId（目标实体拿新 ID）
- Entity.Events 订阅表
- Component 私有字段和闭包状态
- 视觉节点树 / VisualRoot
- 整张关系图
- 旧实体的 owned children

## 4. 适用场景

**适合：**
- 单位死亡后替换成尸体/掉落物
- 投射物结束后替换成地面效果
- 召唤物卵成熟后替换成单位

**不适合：**
- 同一实体只是状态变化（用 Data + Component）
- 同一实体只是换皮（用 Visual 更新）
- 只是增减少量能力（用 Feature / Ability）

## 5. 溯源

不保留旧 EntityId。如玩法需要溯源，只在 Data 写 `SourceEntityId` / `OriginEntityId`。

## 6. 历史判断

成熟 ECS 的主流做法也不是"对象级变身"：
- Unity Entities：增删组件导致 archetype 变化，实体被移动到新 archetype
- Bevy：显式选择复制什么组件、是否移动、是否重映射引用
- EnTT：选择性拷贝组件数据并重建标识映射

本项目采用"受控替换"而非"万能克隆"，复杂度可控。
