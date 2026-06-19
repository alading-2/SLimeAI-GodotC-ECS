# 外部非ECS框架：实体异构属性管理方案调研

> 状态：research / reference only
> 日期：2026-06-19
> 目的：为 SlimeAIFramework 的 Data 系统设计提供 5-10 个具体框架案例，说明非 ECS 框架如何解决"一个实体上有大量异构属性"的问题。

## 当前采纳结论

本页是参考资料，不是实现规格。当前 SlimeAI 裁决以 [`README.md`](./README.md)、[`07-OOP中数据定义与运行时管理方案.md`](./07-OOP中数据定义与运行时管理方案.md) 和 [`08-Command与数据修改入口.md`](./08-Command与数据修改入口.md) 为准：

- 不回退旧 `DataMeta`，继续使用 DataOS descriptor / runtime snapshot / generated `DataKey<T>`。
- 字段定义集中，运行时值按 Data / Profile / Component / System 分区承载。
- QFramework 的 Command / Query / Event 只采纳语义边界，不引入 Architecture 或 `AbstractCommand` 对象体系。
- DataModifier 只保留在 attribute-like numeric Data 字段。
- Component/System authoritative 字段进入 Data 时应标记为 projection。

## 调研问题

SlimeAIFramework 已裁决：
- Entity → Object（OOP 运行时对象）
- Component 可持有内部字段
- Data = 受控共享状态 + 表格驱动 + modifier/computed + 可观察同步
- DataBinding 显式绑定，不靠反射
- DataModifier 只用于 attribute-like numeric 字段

核心问题是：**一个 Object 上有 20-50 个异构属性（HP、速度、攻击力、冷却、AI状态、动画状态等），这些属性有不同的共享需求、不同的生命周期、不同的修改来源，非ECS框架怎么管理？**

---

## 案例 1：Unreal Gameplay Ability System（GAS）

**框架类型**：Unreal Engine 内建能力系统，非 ECS，OOP + 属性系统

**核心机制**：
- `UAttributeSet`：挂在 Actor 上的属性容器，内含多个 `FGameplayAttributeData`（base value + current value）
- `UGameplayEffect`：修改属性的效果包，支持 Instant/Duration/Infinite，modifier add/multiply/override
- `FActiveGameplayEffectHandle`：运行时效果句柄，可查询、移除、堆叠
- 属性初始化通过 `DataTable`（`FAttributeMetaData` 行：BaseValue, MinValue, MaxValue）

**如何解决异构属性问题**：
```
只把 gameplay 属性（HP, Attack, MoveSpeed）放进 AttributeSet
Actor 其他状态（动画、AI黑板、UI状态）不在 AttributeSet 中
GameplayEffect 只修改 AttributeSet 中的属性
Attribute 变化触发委托回调，UI/逻辑/音效订阅
```

**SlimeAI 可借鉴**：
- ✅ 属性只覆盖"需要modifier/共享"的字段，不覆盖所有状态
- ✅ base/current 双值模型
- ✅ Effect 有 source 生命周期，移除 source 自动回滚
- ✅ DataTable 初始化属性默认值
- ⚠️ GAS 绑定 Unreal 类型系统（UObject/UPROPERTY），SlimeAI 不能照搬
- ⚠️ GAS 的 AttributeSet 遍历所有属性的模式，不如 SlimeAI 的 DataKey<T> 类型安全

**关键代码模式（伪代码）**：
```csharp
// 只有 gameplay 属性才继承 AttributeSet
class HealthAttributeSet : UAttributeSet
{
    FGameplayAttributeData CurrentHp;  // base + current
    FGameplayAttributeData MaxHp;
    FGameplayAttributeData DamageReduction;
}

// GameplayEffect 修改属性
class HealEffect : UGameplayEffect
{
    Modifiers = [
        (Attribute=CurrentHp, Op=Add, Magnitude=50)
    ];
    DurationPolicy = Instant;
}

// 属性变化回调
OnAttributeChanged(Attribute, OldValue, NewValue)
    -> UI 更新血条
    -> 死亡判定
    -> Debug panel
```

---

## 案例 2：QFramework 的 BindableProperty + Model 模式

**框架类型**：Unity C# 架构框架（GitHub 5.3k★），OOP，非 ECS

**核心机制**：
- `Architecture` → `System` + ``Model` + `Utility`
- `Model` 持有 `BindableProperty<T>`（可观察属性）
- `BindableProperty<T>` 支持 `.Register(callback)` 监听变化
- `Command` 修改 Model，`Query` 读取 Model
- `Event` 用于跨 System 通信

**如何解决异构属性问题**：
```
每个 Model 管一组相关属性
属性类型化：BindableProperty<int>, BindableProperty<float>
变化监听自动触发 UI 更新
Command 是唯一写入入口，保证可追溯
```

**SlimeAI 可借鉴**：
- ✅ 强类型属性容器（类比 DataKey<T>）
- ✅ 变化监听是属性的内在能力（类比 DataChanged<T>）
- ✅ Command 作为写入入口保证可追溯（类比 owner API / Data.Set 权威）
- ✅ Model 不需要包含所有状态，只包含需要共享的
- ⚠️ QFramework 的全局 Architecture 注册模式不适合 SlimeAI
- ⚠️ QFramework 没有 modifier/computed 链路

**关键代码模式**：
```csharp
// Model 持有可观察属性
class GameModel : AbstractModel
{
    public BindableProperty<int> Gold = new BindableProperty<int> { Value = 0 };
    public BindableProperty<int> Hp = new BindableProperty<int> { Value = 100 };
}

// Command 修改属性
class AddGoldCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var model = this.GetModel<GameModel>();
        model.Gold.Value += 10;
    }
}

// UI 注册监听
model.Hp.Register(newHp => { healthBar.SetValue(newHp); });
```

---

## 案例 3：Godot Resource 作为数据容器模式

**框架类型**：Godot 原生模式，Resource 是引擎级数据容器

**核心机制**：
- `Resource` 是 Godot 的可序列化数据容器
- 自定义 Resource 子类定义强类型字段（`[Export]` 属性）
- Resource 可从 `.tres` 文件加载，可在 Inspector 编辑
- Resource 可被多个 Node 共享引用
- Resource 有 `Changed` 信号

**如何解决异构属性问题**：
```
每种数据类型一个 Resource 子类
实体持有 Resource 引用，不自己存储配置数据
多个实体可共享同一个 Resource 实例（共享配置）
运行时修改 Resource 会触发 Changed 信号
```

**SlimeAI 可借鉴**：
- ✅ Resource 作为"不随实体销毁的配置数据"的天然载体
- ✅ Godot Inspector 可视化编辑
- ✅ 多实例共享同一份配置数据
- ⚠️ Resource 不适合做运行时状态（会被序列化/共享）
- ⚠️ 缺少 modifier/computed/validator 链路
- ⚠️ 不能替代 DataOS 的 SQLite authoring 和 AI 可读 descriptor

**关键代码模式**：
```csharp
// 定义数据容器
public partial class UnitStats : Resource
{
    [Export] public float MaxHp = 100f;
    [Export] public float MoveSpeed = 3.5f;
    [Export] public float Attack = 10f;
    [Export] public float AttackSpeed = 1.0f;
}

// 实体引用 Resource
public partial class Unit : Node2D
{
    [Export] public UnitStats Stats;  // 从 Inspector 赋值或代码加载
}

// 多个同类 Unit 共享同一 Stats 实例
// 运行时修改 Stats 会影响所有引用它的 Unit
```

**与 SlimeAI Data 的对比**：
SlimeAI 的 DataOS record → runtime snapshot → DataKey<T> 链路比 Godot Resource 更强大，因为有：
- SQLite authoring（不是手写 .tres）
- typed DataKey<T>（不是 untyped [Export]）
- descriptor 元数据（description, range, allowed values）
- DataModifier 管线
- validator + AI 可读

---

## 案例 4：Unity ScriptableObject 数据驱动模式

**框架类型**：Unity 社区广泛使用的数据驱动模式，非 ECS

**核心机制**：
- ScriptableObject 子类定义数据 schema
- 实例从 Asset 文件加载
- 运行时 ScriptableObject 可持有状态（但这会导致共享污染）
- 社区模式：ScriptableObject 只存配置，运行时状态在 MonoBehaviour 字段

**典型模式（Ryan Hipple 的 ScriptableObject Architecture）**：
```
SO 定义变量模板：FloatVariable, IntVariable
SO 有 Initial Value（不变）和 Runtime Value（运行时覆盖）
多个 Component 引用同一个 FloatVariable SO
监听者注册 ValueChanged 事件
```

**如何解决异构属性问题**：
```
配置数据放 ScriptableObject（base values from tables）
运行时状态放 Component 字段（instance-specific）
SO 作为"共享引用"让多个系统读同一份数据
Event 通知变化
```

**SlimeAI 可借鉴**：
- ✅ 配置/运行时分离（类比 DataOS record vs Object.Data runtime）
- ✅ 多系统共享同一数据引用（类比 Data 作为共享状态）
- ✅ 变化事件通知（类比 DataChanged<T>）
- ⚠️ ScriptableObject 运行时修改会影响所有引用者（污染风险）
- ⚠️ 没有 modifier/computed 链路
- ⚠️ 没有类型安全的 key 系统

**关键代码模式**：
```csharp
// ScriptableObject 作为共享数据容器
[CreateAssetMenu]
public class FloatVariable : ScriptableObject
{
    public float InitialValue;
    private float _runtimeValue;
    public float Value
    {
        get => _runtimeValue;
        set
        {
            _runtimeValue = value;
            OnValueChanged?.Invoke(_runtimeValue);
        }
    }
    public event Action<float> OnValueChanged;
}

// 使用
public class HealthBar : MonoBehaviour
{
    [SerializeField] FloatVariable CurrentHp;  // 引用共享 SO
    void OnEnable() => CurrentHp.OnValueChanged += UpdateBar;
    void OnDisable() => CurrentHp.OnValueChanged -= UpdateBar;
}
```

---

## 案例 5：Defold 引擎的 Game Object Properties + Factories

**框架类型**：Defold（C/Lua），轻量级 2D 引擎，非 ECS

**核心机制**：
- Game Object 上可附加多个 Components（Sprite, Collision, Script, Factory）
- Script Component 通过 `go.property()` 声明属性，可在 Editor 编辑
- Properties 是强类型的（number, hash, vector3, vector4, quat, resource）
- Properties 变化可在 script 中响应
- Factory 从模板创建 Game Object，支持参数覆盖

**如何解决异构属性问题**：
```
每个 script component 声明自己需要的属性
属性可在 editor 中可视化配置
Factory 创建时可覆盖默认属性值
属性变化可被监听
```

**SlimeAI 可借鉴**：
- ✅ 属性声明式定义（类比 DataKey<T> 声明）
- ✅ Editor 可视化配置（类比 DataOS descriptor + Inspector）
- ✅ 工厂创建时参数覆盖（类比 DataOS record apply）
- ⚠️ 属性分散在各个 script 中，没有统一属性系统
- ⚠️ 没有 modifier/computed

---

## 案例 6：Stride3D 的 Entity-Component（非 ECS）模式

**框架类型**：Stride3D（C#），开源 3D 引擎，OOP Entity-Component

**核心机制**：
- `Entity` 是 Node 的子类，可包含多个 `EntityComponent`
- `EntityComponent` 是普通 C# 类，可持有任意字段
- `EntityComponent` 可以通过 `[DataMember]` 标记序列化字段
- 变化监听通过 `PropertyChanged` 事件
- 编辑器自动显示 `[DataMember]` 字段

**如何解决异构属性问题**：
```
每个 Component 自己管理自己的字段
序列化字段通过 DataMember 自动进入编辑器
没有全局属性系统——状态就在 Component 里
需要共享时通过 Component 引用或 Event
```

**SlimeAI 可借鉴**：
- ✅ Component 可持有内部字段（已采纳）
- ✅ 序列化/编辑器集成通过标记（类比 [Export] 或 descriptor）
- ⚠️ 没有统一的共享状态管理——这正是 SlimeAI Data 要解决的问题

---

## 案例 7：Cocos Creator 的 Component 属性 + 属性装饰器

**框架类型**：Cocos Creator（TypeScript），OOP Component-based

**核心机制**：
- Component 通过 `@property` 装饰器声明属性
- 属性自动在编辑器 Inspector 中显示
- 属性类型推断（number, string, Vec3, Node, etc.）
- 属性有默认值，可在编辑器覆盖
- `@ccclass` 注册组件类型

**如何解决异构属性问题**：
```
属性通过装饰器声明在 Component 上
编辑器自动展示并可编辑
没有全局属性系统
数据从 Prefab/Scene 序列化加载
```

**关键代码模式**：
```typescript
@ccclass('HealthComponent')
export class HealthComponent extends Component {
    @property maxHp: number = 100;
    @property currentHp: number = 100;

    @property(Node) healthBar: Node = null;  // UI 引用

    takeDamage(amount: number) {
        this.currentHp -= amount;
        if (this.currentHp <= 0) this.die();
    }
}
```

**SlimeAI 可借鉴**：
- ✅ 声明式属性定义，自动进入编辑器
- ⚠️ 没有跨组件共享属性的能力
- ⚠️ 没有 modifier/computed

---

## 案例 8：ArcheType（非 Unity ECS）—— 组件式但非数据导向

**框架类型**：社区 C# 游戏框架模式，OOP Entity-Component

**核心机制**：
- Entity 是 ID + Dictionary<Type, Component>
- Component 是普通 C# 对象，可持有任何字段
- `entity.Get<T>()` 获取组件
- `entity.Get<T>().SomeField` 直接访问字段
- 没有 System、没有 query、没有 archetype storage

**如何解决异构属性问题**：
```
属性就在 Component 字段里
需要共享属性时，多个 Component 读同一个 Component 的字段
或者通过 Entity-level 的属性容器（类似 Blackboard）
```

**关键代码模式**：
```csharp
// Entity 级别的属性容器（Blackboard 模式）
public class EntityBlackboard
{
    private Dictionary<string, object> _data = new();

    public T Get<T>(string key) => (T)_data[key];
    public void Set<T>(string key, T value) { _data[key] = value; OnChanged?.Invoke(key); }
    public event Action<string> OnChanged;
}

// 或者类型安全版本
public class EntityProperties
{
    public float MoveSpeed { get; set; }
    public float CurrentHp { get; set; }
    public float MaxHp { get; set; }
    public bool IsDead { get; set; }
}
```

**SlimeAI 可借鉴**：
- ⚠️ Dictionary<string, object> 黑板模式缺乏类型安全——这正是 SlimeAI 用 DataKey<T> 避免的
- ✅ Entity-level 属性容器的思路（类比 Object.Data）

---

## 案例 9：Path of Exile 的属性系统（工业参考）

**框架类型**：工业级 ARPG 属性系统，非 ECS

**核心机制**：
- 属性有 Tags（Fire, Cold, Physical, etc.）
- Modifier 有 Operations（Add, Increase, More, Override）
- 计算顺序：Base → +Add → ×Increase → ×More → Override → Cap
- 装备/Buff/天赋各自提供 modifier，通过 tag 匹配到目标属性
- 属性只有"需要共享计算"的才进入属性系统，其他状态（动画、AI）不在其中

**如何解决异构属性问题**：
```
只有 gameplay 属性（生命、抗性、伤害、速度等）进入属性系统
每个属性独立计算 modifier 链
Tag 系统决定哪些 modifier 影响哪些属性
其他状态（位置、动画、UI、AI）用常规方式管理
```

**SlimeAI 可借鉴**：
- ✅ 属性系统只覆盖需要 modifier 的字段（已采纳）
- ✅ 分层计算：base → additive → multiplicative → final → cap（类比 DataModifier 管线）
- ✅ Tag 系统可用于 future extension（Skill tag → Data tag 匹配）
- ⚠️ PoE 的 tag 系统过于复杂，SlimeAI 当前不需要

---

## 案例 10：Starbound/Stardew Valley 式的数据表 + 实例覆盖

**框架类型**：独立游戏常用模式，数据表驱动 + 实例覆盖

**核心机制**：
- 配置数据从 JSON/CSV/XML 加载（物品表、怪物表、技能表）
- 实体创建时从数据表读取默认值
- 运行时字段在实体对象上直接修改
- 没有全局属性系统，没有 modifier 链
- 需要"装备加成"时直接在计算函数里读装备属性

**如何解决异构属性问题**：
```
数据表定义：{ monster: "slime", hp: 50, speed: 2.0, attack: 5 }
创建实例时：instance.hp = table.hp
运行时修改：instance.hp -= damage
没有共享属性系统——直接字段访问
```

**SlimeAI 可借鉴**：
- ✅ 数据表初始化（已采纳 DataOS SQLite → runtime snapshot）
- ✅ 简单直接——对于不共享的属性，放在 Component 字段里就好（已采纳）
- ⚠️ 没有 modifier/computed——当需要 Buff/装备系统时会痛苦
- ⚠️ 没有变化通知——UI/Debug 需要轮询

---

## 综合对比表

| 框架 | 属性存储 | 共享机制 | Modifier | 变化通知 | 数据源 | 类型安全 |
| --- | --- | --- | --- | --- | --- | --- |
| Unreal GAS | AttributeSet | Actor 内共享 | ✅ GameplayEffect | ✅ 委托 | DataTable | 中（FName） |
| QFramework | Model.BindableProperty | Architecture 全局 | ❌ | ✅ Register | 代码 | ✅ 泛型 |
| Godot Resource | Resource 字段 | 引用共享 | ❌ | ✅ Changed | .tres 文件 | 中（[Export]） |
| Unity SO | ScriptableObject | Asset 引用 | ❌ | ✅ Event | .asset 文件 | 中 |
| Defold | go.property | 局限 | ❌ | 有限 | Editor 配置 | ✅ 声明式 |
| Stride3D | Component 字段 | 组件引用/Event | ❌ | PropertyChanged | 序列化 | ✅ C# |
| Cocos Creator | @property | 局限 | ❌ | 有限 | Prefab 序列化 | ✅ 装饰器 |
| Blackboard | Dictionary | Entity 级 | ❌ | key 变化 | 代码 | ❌ |
| PoE 式 | 属性系统 | Tag 匹配 | ✅ Add/More | 无标准 | 数据表 | 中 |
| 数据表模式 | 字段直接访问 | 无 | ❌ | 无 | JSON/CSV | ✅ |

## 对 SlimeAI Data 设计的具体建议

基于以上调研，SlimeAI 的双层状态模型是正确的方向：

### 1. Data 只覆盖需要共享/modifier/表格驱动的字段 ✅
所有成熟框架都这样做：Unreal GAS 的 AttributeSet 只含 gameplay 属性，PoE 的属性系统只含需要 modifier 计算的属性。SlimeAI 的"进入条件"表已经定义了正确边界。

### 2. 显式 DataBinding 优于反射/黑板 ✅
QFramework 的 BindableProperty、Unity SO 的引用模式都需要显式声明绑定。SlimeAI 的 DataBinding 方案比黑板模式（Dictionary<string, object>）类型安全，比反射扫描可靠。

### 3. DataModifier 只用于 numeric attribute ✅
Unreal GAS 和 PoE 都只对数值属性做 modifier 计算。SlimeAI 的 DataModifier 收窄到 attribute-like numeric 字段是正确方向。

### 4. DataChanged<T> 作为同步机制 ✅
QFramework 的 BindableProperty.Register、Unreal GAS 的 AttributeChanged delegate、Godot Resource 的 Changed signal 都证明"属性变化通知"是必要能力。SlimeAI 的 DataChanged<T> 已覆盖。

### 5. Component 内部字段是默认选择 ✅
Stride3D、Cocos Creator、Defold 都证明：大部分组件状态不需要全局共享，放在组件字段里最简单。只有真正需要共享/表格/modifier/追踪的字段才进入 Data。

### 6. 表格初始化 + 运行时覆盖 ✅
Unreal DataTable、PoE 数据表、SlimeAI 的 DataOS → runtime snapshot 都证明"表格提供默认值，运行时实例可覆盖"是成熟模式。

---

## 附：可进一步研究的 GitHub 项目

以下项目可在后续深入研究：

1. **Arch** (github.com/genaray/Arch) - C# 轻量级 ECS，但其 Component 存储模式可参考
2. **DefaultEcs** (github.com/Doraku/DefaultEcs) - C# ECS，其属性系统设计可参考
3. **LeoECSLite** (github.com/Leopotam/ecslite) - 轻量 ECS，Component 就是普通 struct
4. **MonoGame.Extended** (github.com/craftworkgames/MonoGame.Extended) - Entity-Component 非 ECS
5. **Nez** (github.com/prime31/Nez) - C# 2D 框架，Entity-Component 模式
6. **Otter2D** (github.com/NoelFB/Otter) - C# 2D 框架，传统 Entity-Component
7. **Stride** (github.com/stride3d/stride) - C# 3D 引擎，Entity-Component 非 ECS
8. **MoonSharp + Game frameworks** - Lua 数据驱动模式参考

（注：以上项目需实际访问 GitHub 确认当前版本和 API）
