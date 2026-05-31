# ECS 框架文档

> 状态：current
> 定位：SlimeAI ECS 框架核心文档，按分类 + owner 聚合。
> 更新：2026-05-31

## 阅读顺序

1. **方向定位**：读 [`../ECS框架与AIFirst方向决策.md`](../ECS框架与AIFirst方向决策.md)。
2. **核心 owner**：按需读 `Entity/`、`Data/`、`Event/`、`Collision/`、`Component/`。
3. **系统层**：按需读 `System/` 下对应 owner 文档。
4. **工具层和 UI 层**：按需读 `Tools/`、`UI/` 下对应 owner 文档。
5. **具体组件**：按 `Component/` 下与 `Src/ECS/Base/Component/` 对齐的目录读取迁移全文。
6. **执行前**：先读对应 owner 的完整迁移文档，再进入 `Src/ECS/` 阅读源码。

## 分层规则

`DocsAI/ECS` 是 ECS 功能文档事实源。原 `Src/ECS/**.md` 长文档已迁入这里；`Src/ECS` 不再保留框架 Markdown 文档。

`DocsAI/ECS` 当前按功能 owner 管理，但 Component 文档暂时例外：`DocsAI/ECS/Component/` 与 `Src/ECS/Base/Component/` 保持目录对齐，方便迁移审计和源码就近查找。

- `Base/` 不作为 DocsAI 分类；`Src/ECS/Base/Component/**` 映射为 `DocsAI/ECS/Component/**`。
- Component 文档先保持原目录和原文件名；后续如果要按功能 owner 拆分，必须先更新 `DocsAI/管理/` 规则。
- `Src/ECS/` 仍不保留 Markdown 文档，迁移来源只用于追溯。

| 文件 | 用途 |
| ---- | ---- |
| `Concept.md` | 设计定位、契约、职责边界、依赖、红线；可选，不强制 |
| `Usage.md` | 从原 `Src/ECS` 迁入的使用说明、API、示例、扩展步骤；可选，不强制 |
| `Tests.md` | 验证入口和测试覆盖（有需要时建立） |
| `Debug.md` | 排错流程和日志说明（有需要时建立） |
| 原文件名 | 原文整体迁移更清晰时保留 |

完整治理规则见 [`../管理/DocsAI统一管理与索引规则.md`](../管理/DocsAI统一管理与索引规则.md)。

## 目录

### 核心 owner

| Owner | 文档 | 一句话说明 |
| ----- | ---- | ---- |
| Entity | [Entity/](Entity/) | 实体身份容器、生命周期、EntityManager、IComponent 规范 |
| Data | [Data/](Data/) | DataOS 管道、DataKey、运行时存储 |
| Event | [Event/](Event/) | EventBus 架构、Context 模式 |
| Collision | [Collision/](Collision/) | 碰撞桥接层、碰撞层级、对象池碰撞兼容 |
| Component | [Component/](Component/) | 与 `Src/ECS/Base/Component/` 对齐的组件迁移文档 |

### System/ — 系统层

| Owner | 文档 | 一句话说明 |
| ----- | ---- | ---- |
| Core | [System/Core/](System/Core/) | 系统注册、生命周期、分层总览 |
| AbilitySystem | [System/AbilitySystem/](System/AbilitySystem/) | 技能系统三层架构 |
| FeatureSystem | [System/FeatureSystem/](System/FeatureSystem/) | 通用能力层（Grant/Enable/Disable/Remove） |
| DamageSystem | [System/DamageSystem/](System/DamageSystem/) | 10 阶段伤害管线 |
| Movement | [System/Movement/](System/Movement/) | 策略模式移动系统 |
| Movement/ScalarDriver | [System/Movement/ScalarDriver.md](System/Movement/ScalarDriver.md) | 标量参数驱动 |
| Movement/Strategies | [System/Movement/Strategies.md](System/Movement/Strategies.md) | 运动策略扩展说明 |
| AI | [System/AI/](System/AI/) | 行为树框架 |
| Status | [System/Status/](System/Status/) | 实体状态效果系统 |
| Effect | [System/Effect/](System/Effect/) | 特效系统 |
| Spawn | [System/Spawn/](System/Spawn/) | 程序化敌人生成 |
| TargetingSystem | [System/TargetingSystem/](System/TargetingSystem/) | 异步目标选择会话 |
| MouseSelection | [System/MouseSelection/](System/MouseSelection/) | 鼠标点击/框选目标系统 |
| TestSystem | [System/TestSystem/](System/TestSystem/) | 运行时调试系统 |

### Tools/ — 工具层

| Owner | 文档 | 一句话说明 |
| ----- | ---- | ---- |
| ObjectPool | [Tools/ObjectPool/](Tools/ObjectPool/) | 通用 C# 对象池 |
| Timer | [Tools/Timer/](Tools/Timer/) | 高性能定时器系统 |
| Math | [Tools/Math/](Tools/Math/) | 数学工具层 |
| TargetSelector | [Tools/TargetSelector/](Tools/TargetSelector/) | 目标查询三层架构 |
| Input | [Tools/Input/](Tools/Input/) | 输入管理 |
| Logger | [Tools/Logger/](Tools/Logger/) | 日志系统 |
| NodeLifecycle | [Tools/NodeLifecycle/](Tools/NodeLifecycle/) | 节点生命周期管理 |
| ParentManager | [Tools/ParentManager/](Tools/ParentManager/) | 父子节点管理工具 |

### UI/ — UI 层

| Owner | 文档 | 一句话说明 |
| ----- | ---- | ---- |
| UI | [UI/](UI/) | Binding Pattern，UI 不是 Entity Component |

### Component/ — 组件层

`Component/` 暂时与 `Src/ECS/Base/Component/` 保持一致，避免迁移阶段丢失源码上下文。

| Src 对齐目录 | DocsAI 文档 |
| ---- | ---- |
| `Component规范.md` | [Component/Component规范.md](Component/Component规范.md) |
| `Ability/ChargeComponent/README.md` | [Component/Ability/ChargeComponent/README.md](Component/Ability/ChargeComponent/README.md) |
| `Ability/CooldownComponent/README.md` | [Component/Ability/CooldownComponent/README.md](Component/Ability/CooldownComponent/README.md) |
| `Ability/CostComponent/README.md` | [Component/Ability/CostComponent/README.md](Component/Ability/CostComponent/README.md) |
| `Ability/TriggerComponent/README.md` | [Component/Ability/TriggerComponent/README.md](Component/Ability/TriggerComponent/README.md) |
| `Collision/CollisionComponent/CollisionComponent.md` | [Component/Collision/CollisionComponent/CollisionComponent.md](Component/Collision/CollisionComponent/CollisionComponent.md) |
| `Collision/ContactDamageComponent/ContactDamageComponent.md` | [Component/Collision/ContactDamageComponent/ContactDamageComponent.md](Component/Collision/ContactDamageComponent/ContactDamageComponent.md) |
| `Collision/PickupComponent/PickupComponent.md` | [Component/Collision/PickupComponent/PickupComponent.md](Component/Collision/PickupComponent/PickupComponent.md) |
| `Movement/EntityMovementComponent说明.md` | [Component/Movement/EntityMovementComponent说明.md](Component/Movement/EntityMovementComponent说明.md) |
| `Unit/Common/AttackComponent/AttackComponent.md` | [Component/Unit/Common/AttackComponent/AttackComponent.md](Component/Unit/Common/AttackComponent/AttackComponent.md) |
| `Unit/Common/DataInitComponent/DataInitComponent.md` | [Component/Unit/Common/DataInitComponent/DataInitComponent.md](Component/Unit/Common/DataInitComponent/DataInitComponent.md) |
