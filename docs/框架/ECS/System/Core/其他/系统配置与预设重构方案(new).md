# 系统配置与预设重构方案

> 文档类型：历史重构草案  
> 当前状态：已按“一个 `SystemConfig` + 一个 `SystemPreset` + 三域 `ProjectState`”落地；旧 `SystemProfile / SystemKind / SystemLifetime / 四 Phase` 表述仅保留为问题背景，不作为当前实现依据。

## 背景与目标

### 当前问题
1. **SystemProfile 机制冗余**：Profile 只是补充信息，与 SystemDescriptor 重复
2. **配置分散**：系统元数据在代码中注册，缺乏统一的可视化配置
3. **分组单一**：SystemLifetime 只能单选，无法支持多维度分类
4. **缺少游戏类型预设**：无法快速切换不同游戏模式的系统配置
5. **调试不便**：缺少运行时查看和筛选系统的工具

### 设计目标
1. **数据驱动**：系统配置从代码迁移到表格资源，支持可视化编辑
2. **多维度分组**：支持分组（挂载）+ 标签（逻辑分类）双维度
3. **游戏类型预设**：支持预设配置，一键切换游戏模式
4. **统一管理**：SystemDescriptor 作为唯一信息源，移除 Profile 冗余
5. **调试友好**：TestSystem 支持分组/标签筛选和系统信息查看

## 核心设计

### 1. 分组 + 标签双维度

**SystemGroup（分组）**：用于挂载位置
- 多选 Flags 枚举，按位从小到大优先
- 取最低位作为挂载路径
- Base 分组优先级最高，强制加载，禁止移除

**SystemTag（标签）**：用于逻辑分类
- 多选 Flags 枚举
- 用于游戏类型预设的批量开启
- 支持运行时筛选和查询

```csharp
[Flags]
public enum SystemGroup : ulong
{
    None        = 0,
    Base        = 1 << 0,  // 基础系统（优先级最高，强制加载）
    Combat      = 1 << 1,
    Movement    = 1 << 2,
    AI          = 1 << 3,
    UI          = 1 << 4,
    Audio       = 1 << 5,
    VFX         = 1 << 6,
    Loot        = 1 << 7,
    Progression = 1 << 8,
    Debug       = 1 << 9,
    Test        = 1 << 10,
    Else        = 1 << 63, // 未分组系统（兜底）
}

[Flags]
public enum SystemTag : ulong
{
    None       = 0,
    Core       = 1 << 0,  // 核心系统
    Roguelike  = 1 << 1,
    Survival   = 1 << 2,
    Tower      = 1 << 3,
    Multiplayer= 1 << 4,
    Singleplayer=1<<5,
    Editor     = 1 << 6,
    Runtime    = 1 << 7,
}
```

### 2. Phase 枚举改为 Flags

```csharp
[Flags]
public enum AppPhase : byte
{
    None         = 0,
    Boot         = 1 << 0,
    FrontEnd     = 1 << 1,
    InSession    = 1 << 2,
    ShuttingDown = 1 << 3,
    
    // 预设组合
    All          = Boot | FrontEnd | InSession | ShuttingDown,
    GameplayPhases = InSession,
    MenuPhases   = Boot | FrontEnd,
}

// SessionPhase、OverlayPhase、ExecutionPhase 同理
```

### 3. 系统名字枚举

```csharp
/// <summary>
/// 系统 Id 枚举。
/// <para>用于 Dependencies、SystemPreset 等需要引用系统的地方。</para>
/// <para>手动维护，与实际系统类名保持一致。</para>
/// </summary>
public enum SystemId
{
    // 基础设施
    ObjectPoolInit,
    TimerManager,
    ProjectStateBridge,
    
    // 战斗系统
    DamageService,
    RecoverySystem,
    DamageStatisticsSystem,
    
    // 生成系统
    SpawnSystem,
    
    // 目标系统
    TargetingManager,
    
    // UI 系统
    UIManager,
    DamageNumberSystem,
    
    // 暂停菜单
    PauseMenuSystem,
    
    // 调试系统
    TestSystem,
    MouseSelectionSystem,
}
```

### 4. 数据驱动架构

**代码注册**：只提供 SystemId + Factory
```csharp
SystemRegistry.Register(nameof(DamageSystem), () => new DamageSystem());
```

**表格配置**：`Data/Config/System/System/DamageSystem.tres`
```csharp
[GlobalClass]
public partial class SystemConfig : Resource
{
    // 基础信息
    [Export] public string SystemId { get; set; }
    [Export] public SystemKind Kind { get; set; }
    [Export] public SystemGroup Groups { get; set; }
    [Export] public SystemTag Tags { get; set; }
    
    // 加载配置
    [Export] public bool DefaultAutoAdd { get; set; } = true;
    [Export] public bool DefaultEnabled { get; set; } = true;
    [Export] public int Priority { get; set; } = 0;
    
    // 运行条件（替代 SystemRunCondition）
    [Export] public AppPhase AllowedAppPhases { get; set; }
    [Export] public SessionPhase AllowedSessionPhases { get; set; }
    [Export] public OverlayPhase AllowedOverlayPhases { get; set; }
    [Export] public OverlayPhase BlockedOverlayPhases { get; set; }
    [Export] public ExecutionPhase AllowedExecutionPhases { get; set; }
    
    // 依赖关系
    [Export] public SystemId[] Dependencies { get; set; }
    
    // 说明
    [Export] public string Description { get; set; }
}
```

**SystemPreset 配置**：`Data/Config/System/SystemPreset/RoguelikePreset.tres`
```csharp
[GlobalClass]
public partial class SystemPreset : Resource
{
    [Export] public string PresetName { get; set; }
    [Export] public bool IsActive { get; set; } = false;
    [Export] public SystemGroup EnabledGroups { get; set; }
    [Export] public SystemTag EnabledTags { get; set; }
    [Export] public SystemId[] EnabledSystemIds { get; set; }
    [Export] public SystemId[] DisabledSystemIds { get; set; }
    [Export] public string Description { get; set; }
}
```

### 5. 加载逻辑

```
1. 读取所有 SystemPreset，找到 IsActive = true 的预设
   - 只允许一个 Active（启动时检查，多个则报错）
   
2. 如果有 ActivePreset：
   - 收集 EnabledGroups 对应的所有系统
   - 收集 EnabledTags 对应的所有系统
   - 收集 EnabledSystemIds 列表
   - 排除 DisabledSystemIds 列表
   
3. 读取所有 SystemConfig 表格
   - 收集 DefaultAutoAdd = true 的系统
   
4. 合并去重，得到最终加载列表

5. Base 分组的系统强制加载（不受 Preset 影响）

6. 按 Priority 排序后依次 EnsureSystem
```

### 6. 挂载路径规则

```csharp
// 取 Groups 的最低位作为挂载路径
SystemGroup mountGroup = GetLowestBitGroup(descriptor.Groups);
string parentPath = mountGroup switch {
    SystemGroup.Base => "Base",
    SystemGroup.Combat => "Combat",
    SystemGroup.Movement => "Movement",
    SystemGroup.Else => "Else",
    _ => mountGroup.ToString()
};
```

### 7. 系统信息接口

**新接口**：`ISystemFunction`
```csharp
public interface ISystemFunction
{
    void OnProjectStateChanged(ProjectStateSnapshot snapshot);
    SystemRuntimeInfo GetSystemRuntimeInfo();
}

public interface ISystem : ISystemLifecycle, ISystemFunction { }
```

**SystemRuntimeInfo**：
```csharp
public class SystemRuntimeInfo
{
    public string SystemId { get; set; }
    public bool IsAdded { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsRunning { get; set; }
    public SystemGroup Groups { get; set; }
    public SystemTag Tags { get; set; }
    public List<SystemStat> CustomStats { get; set; }
}

public class SystemStat
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Category { get; set; }
}
```

### 8. TestSystem UI 设计

```
┌─────────────────────────────────────────────────────────────┐
│ 系统测试面板                                                  │
├─────────────────────────────────────────────────────────────┤
│ 分组筛选: [Base][Combat][Movement][AI]...                    │
│ 标签筛选: [Core][Roguelike][Survival]...                     │
├──────────────────────┬──────────────────────────────────────┤
│ 系统列表              │ 系统详情                              │
│                      │                                      │
│ ✅ DamageSystem      │ SystemId: DamageSystem               │
│ ✅ MovementSystem    │ Groups: Base | Combat                │
│ ⏸️ AISystem          │ Tags: Core | Roguelike               │
│ ❌ LootSystem        │ IsAdded: true                        │
│                      │ IsEnabled: true                      │
│ 图例:                │ IsRunning: true                      │
│ ✅ Added+Enabled     │                                      │
│ ⏸️ Added+Disabled    │ 自定义统计:                           │
│ ❌ Not Added         │ - 总伤害: 12345                       │
│                      │ - 活跃实体: 56                        │
└──────────────────────┴──────────────────────────────────────┘
```

## 实施步骤

### Phase 1: 核心枚举和数据结构（优先）

**1.1 创建枚举**
- `Src/ECS/Base/System/Core/Lifecycle/SystemGroup.cs`
- `Src/ECS/Base/System/Core/Lifecycle/SystemTag.cs`
- `Src/ECS/Base/System/Core/Lifecycle/SystemId.cs`

**1.2 修改 Phase 枚举为 Flags**
- `Src/ECS/Base/System/Core/State/Phase/AppPhase.cs`
- `Src/ECS/Base/System/Core/State/Phase/SessionPhase.cs`
- `Src/ECS/Base/System/Core/State/Phase/OverlayPhase.cs`
- `Src/ECS/Base/System/Core/State/Phase/ExecutionPhase.cs`

**1.3 创建资源类**
- `Src/ECS/Base/System/Core/Config/SystemConfig.cs`
- `Src/ECS/Base/System/Core/Config/SystemPreset.cs`

**1.4 创建配置服务**
- `Src/ECS/Base/System/Core/Config/SystemConfigService.cs`（加载和解析 SystemConfig）
- `Src/ECS/Base/System/Core/Config/SystemPresetService.cs`（加载和解析 SystemPreset）

### Phase 2: 重构核心架构

**2.1 重构 SystemDescriptor**
- 简化为只包含 SystemId + Factory
- 其他字段从 SystemConfig 读取

**2.2 重构 SystemRegistry**
- 修改 Register 签名为 `Register(string systemId, Func<object> factory)`
- 添加 `GetDescriptorsByGroup(SystemGroup)` 查询方法
- 添加 `GetDescriptorsByTag(SystemTag)` 查询方法

**2.3 重构 SystemManager**
- 修改 Bootstrap 流程，先加载 SystemConfig 和 SystemPreset
- 实现 Preset 应用逻辑
- 修改挂载路径生成逻辑（按 Groups 最低位）
- 实现 Base 分组强制加载逻辑

**2.4 移除 Profile 机制**
- 删除 `Src/ECS/Base/System/Core/Profile/SystemProfile.cs`
- 删除 `Src/ECS/Base/System/Core/Profile/SystemProfileEntry.cs`
- 删除 `Src/ECS/Base/System/Core/Profile/SystemProfileService.cs`

**2.5 修改 SystemRunCondition**
- 改为从 SystemConfig 读取 Phase Flags
- 修改 Evaluate 逻辑支持按位判断

### Phase 3: 接口重构

**3.1 创建新接口文件夹**
- `Src/ECS/Base/System/Core/ISystem/`

**3.2 创建 ISystemFunction 接口**
- `Src/ECS/Base/System/Core/ISystem/ISystemFunction.cs`
- 包含 `OnProjectStateChanged` 和 `GetSystemRuntimeInfo`

**3.3 移动和重构接口**
- 移动 `ISystemLifecycle.cs` 到 `ISystem/` 文件夹
- 移动 `ISystem.cs` 到 `ISystem/` 文件夹
- 修改 `ISystem` 继承 `ISystemLifecycle + ISystemFunction`

**3.4 创建 SystemRuntimeInfo**
- `Src/ECS/Base/System/Core/ISystem/SystemRuntimeInfo.cs`
- `Src/ECS/Base/System/Core/ISystem/SystemStat.cs`

### Phase 4: 迁移现有系统注册

**4.1 更新所有系统注册代码**（16 个文件）
- 从 `new SystemDescriptor(...)` 改为 `Register(nameof(XXX), () => ...)`
- 移除所有元数据参数

**4.2 创建 SystemConfig 表格**
- 为每个系统创建 `.tres` 文件
- 路径：`Data/Config/System/System/`
- 按现有系统分组创建子文件夹（可选）

**4.3 创建默认 SystemPreset**
- `Data/Config/System/SystemPreset/DefaultPreset.tres`
- `IsActive = true`

**4.4 更新 ResourceGenerator**
- 修改路径配置支持 `Data/Config/System/System/` 和 `Data/Config/System/SystemPreset/`

### Phase 5: 实现系统信息接口

**5.1 为现有系统实现 GetSystemRuntimeInfo**
- 优先实现核心系统（DamageService、SpawnSystem、RecoverySystem）
- 其他系统返回基础信息即可

**5.2 扩展 SystemManager**
- 添加 `GetAllSystems()` 查询接口
- 添加 `GetSystemsByGroup(SystemGroup)` 查询接口
- 添加 `GetSystemsByTag(SystemTag)` 查询接口
- 添加 `GetSystemRuntimeInfo(string systemId)` 查询接口

### Phase 6: TestSystem 重构

**6.1 创建 SystemTestModule**
- `Src/ECS/Base/System/TestSystem/Modules/SystemTestModule.cs`

**6.2 实现 UI 组件**
- 分组筛选器（CheckButton 多选）
- 标签筛选器（CheckButton 多选）
- 系统列表（颜色区分状态）
- 系统详情面板

**6.3 实现运行时操作**
- Add/Remove 系统
- Enable/Disable 系统
- 实时刷新系统状态

### Phase 7: 文档更新

**7.1 更新核心文档**
- `docs/框架/ECS/System/Core/系统与状态分层总览.md`
- `docs/框架/ECS/System/Core/其他/系统生命周期与项目状态设计.md`

**7.2 创建新文档**
- `docs/框架/ECS/System/Core/系统配置与预设.md`（详细设计文档）
- `Src/ECS/Base/System/Core/README.md`（使用指南）
- `Data/Config/System/README.md`（配置说明）

**7.3 更新项目索引**
- `Docs/框架/项目索引.md`

### Phase 8: TODO（后续优化）

**8.1 为所有系统实现自定义统计信息**
- 每个系统根据业务需求实现 CustomStats

**8.2 优化 resources_table 插件**
- 支持 Enum 按列筛选
- 提升表格编辑体验

## 关键文件路径

### 新增文件
```
Src/ECS/Base/System/Core/
├── Lifecycle/
│   ├── SystemGroup.cs          # 新增：分组枚举
│   ├── SystemTag.cs            # 新增：标签枚举
│   └── SystemId.cs             # 新增：系统 Id 枚举
├── Config/
│   ├── SystemConfig.cs         # 新增：系统配置资源
│   ├── SystemPreset.cs         # 新增：系统预设资源
│   ├── SystemConfigService.cs  # 新增：配置服务
│   └── SystemPresetService.cs  # 新增：预设服务
├── ISystem/
│   ├── ISystemLifecycle.cs     # 移动：生命周期接口
│   ├── ISystemFunction.cs      # 新增：功能接口
│   ├── ISystem.cs              # 移动：主接口
│   ├── SystemRuntimeInfo.cs    # 新增：运行时信息
│   └── SystemStat.cs           # 新增：统计信息
└── README.md                   # 更新：使用指南

Data/Config/System/
├── System/                     # 新增：系统配置目录
│   ├── Combat/
│   │   ├── DamageSystem.tres
│   │   └── ...
│   ├── Movement/
│   └── ...
├── SystemPreset/               # 新增：预设配置目录
│   ├── DefaultPreset.tres
│   ├── RoguelikePreset.tres
│   └── ...
└── README.md                   # 新增：配置说明
```

### 修改文件
```
Src/ECS/Base/System/Core/
├── SystemDescriptor.cs         # 简化为 SystemId + Factory
├── SystemRegistry.cs           # 修改 Register 签名，添加查询方法
├── SystemManager.cs            # 重构 Bootstrap 流程
├── Lifecycle/SystemRunCondition.cs  # 改为从 SystemConfig 读取
└── State/Phase/
    ├── AppPhase.cs             # 改为 Flags
    ├── SessionPhase.cs         # 改为 Flags
    ├── OverlayPhase.cs         # 改为 Flags
    └── ExecutionPhase.cs       # 改为 Flags
```

### 删除文件
```
Src/ECS/Base/System/Core/Profile/
├── SystemProfile.cs            # 删除
├── SystemProfileEntry.cs       # 删除
└── SystemProfileService.cs     # 删除
```

### 需要更新注册的系统文件（16 个）
```
Src/ECS/Tools/ObjectPool/ObjectPoolInit.cs
Src/ECS/Tools/Timer/TimerManager.cs
Src/ECS/Base/System/Core/State/ProjectStateBridge.cs
Src/ECS/Base/System/TargetingSystem/TargetingManagerRuntime.cs
Src/ECS/Base/System/DamageSystem/DamageService.cs
Src/ECS/Base/System/RecoverySystem/RecoverySystem.cs
Src/ECS/Base/System/Spawn/SpawnSystem.cs
Src/ECS/Base/System/DamageSystem/DamageStatisticsSystem.cs
Src/ECS/Base/System/PauseMenu/PauseMenuSystem.cs
Src/ECS/Base/System/TestSystem/TestSystem.cs
Src/ECS/Base/System/MouseSelection/MouseSelectionSystem.cs
Src/ECS/UI/Core/UIManager.cs
Src/ECS/UI/UI/DamageNumberUI/DamageNumberRuntimeBridge.cs
```

## 重要约束

### Base 分组规则
- Base 分组的系统强制加载，不受 Preset 和 SystemConfig 的 DefaultAutoAdd 影响
- Base 分组的系统强制启用，外部无法 Disable
- Base 分组的系统禁止 Remove
- Base 分组要谨慎使用，只用于核心基础设施

### SystemPreset 规则
- 只允许一个 Preset 的 IsActive = true
- 启动时检查，多个 Active 则报错并停止启动
- Preset 的优先级：EnabledSystemIds > EnabledTags > EnabledGroups
- DisabledSystemIds 优先级最高，可以覆盖其他规则

### Phase 组合枚举
- 在各自的 Phase 枚举中定义预设组合
- 用户可以直接选择预设组合，无需在代码中手动组合
- 详细说明写入 `Data/Config/System/README.md`

### 系统 Id 枚举维护
- 手动维护，与实际系统类名保持一致
- 新增系统时必须同步更新枚举
- 用于 Dependencies、SystemPreset 等需要引用系统的地方

## 验证计划

### 功能验证
1. 创建测试 SystemConfig 和 SystemPreset
2. 验证 Base 分组强制加载
3. 验证 Preset 切换功能
4. 验证分组/标签筛选
5. 验证系统信息查询

### 性能验证
1. 测试 Bootstrap 启动时间
2. 测试 Phase 切换性能
3. 测试系统查询性能

### 兼容性验证
1. 验证现有系统迁移后功能正常
2. 验证依赖关系正确解析
3. 验证运行条件正确评估

## 风险与缓解

### 风险 1：迁移工作量大
- **缓解**：分阶段实施，先完成核心架构，再逐步迁移系统

### 风险 2：表格配置复杂
- **缓解**：提供详细文档和示例，优化 resources_table 插件

### 风险 3：Base 分组滥用
- **缓解**：在文档中明确说明使用场景，代码审查时严格把关

### 风险 4：SystemId 枚举维护遗漏
- **缓解**：在 SystemRegistry.Register 中检查枚举是否存在，不存在则报错

## 参考资料

### 成熟引擎方案
- Unity ECS：SystemGroup + ComponentSystemGroup
- Bevy：SystemSet + Label + Plugin
- Unreal：Subsystem + Module + GameFeature

### 项目现有文档
- `docs/框架/ECS/System/Core/系统与状态分层总览.md`
- `docs/框架/ECS/System/Core/其他/系统生命周期与项目状态设计.md`
- `docs/框架/ECS/System/Core/其他/系统生命周期三案设计.md`
