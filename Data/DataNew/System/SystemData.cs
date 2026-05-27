using slime.data;
using System.Linq;

namespace slime.data.Systems;

/// <summary>
/// 系统配置（纯 POCO，不继承 Resource）。
/// <para>用于让旧调用继续通过 DataNew DTO 读取 DataOS runtime snapshot。</para>
/// </summary>
public class SystemData
{
    /// <summary>全部系统配置实例。</summary>
    public static SystemData[] All => DataTable.GetAll<SystemData>().ToArray();

    /// <summary>按 SystemId 获取系统配置，找不到返回 null 并记录日志。</summary>
    public static SystemData? Get(string name) => DataTable.GetByName<SystemData>(name);

    /// <summary>对象池初始化系统。</summary>
    public static SystemData ObjectPoolInit => DataTable.GetRequiredByName<SystemData>("ObjectPoolInit");

    /// <summary>定时器管理系统。</summary>
    public static SystemData TimerManager => DataTable.GetRequiredByName<SystemData>("TimerManager");

    /// <summary>项目状态桥接系统。</summary>
    public static SystemData ProjectStateBridge => DataTable.GetRequiredByName<SystemData>("ProjectStateBridge");

    /// <summary>实体管理器。</summary>
    public static SystemData EntityManager => DataTable.GetRequiredByName<SystemData>("EntityManager");

    /// <summary>伤害处理服务。</summary>
    public static SystemData DamageService => DataTable.GetRequiredByName<SystemData>("DamageService");

    /// <summary>伤害统计系统。</summary>
    public static SystemData DamageStatisticsSystem => DataTable.GetRequiredByName<SystemData>("DamageStatisticsSystem");

    /// <summary>恢复系统。</summary>
    public static SystemData RecoverySystem => DataTable.GetRequiredByName<SystemData>("RecoverySystem");

    /// <summary>生成系统。</summary>
    public static SystemData SpawnSystem => DataTable.GetRequiredByName<SystemData>("SpawnSystem");

    /// <summary>目标选择管理系统。</summary>
    public static SystemData TargetingManagerRuntime => DataTable.GetRequiredByName<SystemData>("TargetingManagerRuntime");

    /// <summary>暂停菜单系统。</summary>
    public static SystemData PauseMenuSystem => DataTable.GetRequiredByName<SystemData>("PauseMenuSystem");

    /// <summary>UI 管理系统。</summary>
    public static SystemData UIManager => DataTable.GetRequiredByName<SystemData>("UIManager");

    /// <summary>伤害数字 UI 桥接系统。</summary>
    public static SystemData DamageNumberRuntimeBridge => DataTable.GetRequiredByName<SystemData>("DamageNumberRuntimeBridge");

    /// <summary>测试系统。</summary>
    public static SystemData TestSystem => DataTable.GetRequiredByName<SystemData>("TestSystem");

    /// <summary>鼠标选择系统。</summary>
    public static SystemData MouseSelectionSystem => DataTable.GetRequiredByName<SystemData>("MouseSelectionSystem");

    /// <summary>系统唯一 Id。</summary>
    public string SystemId { get; set; } = "";

    /// <summary>系统挂载分组（Host 位置，单选）。</summary>
    public SystemGroup MountGroup { get; set; } = SystemGroup.Else;

    /// <summary>系统标签（逻辑分类和预设筛选），多选 Flags。</summary>
    public SystemTag Tags { get; set; } = SystemTag.None;

    /// <summary>是否为必需系统（无论预设如何都装载）。</summary>
    public bool Required { get; set; }

    /// <summary>默认是否自动装载（没有激活预设时使用）。</summary>
    public bool AutoLoad { get; set; } = true;

    /// <summary>首次纳入管理时的人工开关默认值。</summary>
    public bool StartEnabled { get; set; } = true;

    /// <summary>加载优先级（数值越小越优先，用于依赖排序）。</summary>
    public int Priority { get; set; }

    /// <summary>允许的流程状态（Flags 组合，为 None 表示不限制；优先使用 GameFlowState 预设组合）。</summary>
    public GameFlowState AllowedFlowStates { get; set; } = GameFlowState.None;

    /// <summary>要求存在的覆盖层（Flags 组合，为 None 表示不要求覆盖层；优先使用 OverlayFlags 预设组合）。</summary>
    public OverlayFlags RequiredOverlays { get; set; } = OverlayFlags.None;

    /// <summary>禁止的覆盖层（Flags 组合，为 None 表示不屏蔽覆盖层；优先使用 OverlayFlags 预设组合）。</summary>
    public OverlayFlags BlockedOverlays { get; set; } = OverlayFlags.None;

    /// <summary>允许的模拟状态（Flags 组合，为 None 表示不限制；优先使用 SimulationState 预设组合）。</summary>
    public SimulationState AllowedSimulationStates { get; set; } = SimulationState.None;

    /// <summary>依赖系统 Id 列表。</summary>
    public string[] Dependencies { get; set; } = System.Array.Empty<string>();

    /// <summary>系统描述（用于文档和调试）。</summary>
    public string Description { get; set; } = "";

}
