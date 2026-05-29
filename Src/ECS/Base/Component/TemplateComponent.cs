using Godot;

/// <summary>
/// Component 标准模板
/// 
/// 核心职责：[描述组件的核心功能]
/// 
/// 设计原则：
/// - 单一职责：只做一件事
/// - 数据驱动：通过 Data 容器读写数据
/// - 事件驱动：监听 Entity.Events 响应变化
/// </summary>
public partial class TemplateComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(TemplateComponent));

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;

    // ================= 属性访问 =================

    // 【重要】数据存储规则：
    // ✅ 必须存 Data：运行时状态（HP、State、Velocity、计时器等）
    // ✅ 必须存 Data：需要被其他 Component/System 读取的共享状态或对外发布结果
    // ❌ 不需要存 Data：固定配置（ReviveDuration）、临时引用（Target、Collector）
    // ❌ 不需要存 Data：仅服务当前组件内部算法推进的运行态（如累计角度、阶段缓存、当前角速度）

    // 【强制】使用 DataKey 常量访问数据，禁止使用字符串字面量
    // ❌ 错误：_data.Get<float>("CurrentHp")
    // ✅ 正确：_data.Get<float>(GeneratedDataKey.CurrentHp)

    // 属性读取示例：
    // public float CurrentHp => _data.Get<float>(GeneratedDataKey.CurrentHp);

    // 属性写入示例（在方法中使用，不要直接赋值属性）：
    // _data.Set(GeneratedDataKey.CurrentHp, 80f);
    // _data.Add(GeneratedDataKey.Score, 10);

    // 固定配置示例（无需存 Data）：
    // public float ReviveDuration { get; set; } = Config.HeroReviveTime;

    // 组件内部算法运行态示例（无需存 Data）：
    // private float _currentAngularSpeed;
    // private float _accumulatedAngle;

    // 对外共享结果示例（必须存 Data）：
    // _data.Set(GeneratedDataKey.MovementFacingDirection, facingDirection);

    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _entity = iEntity;
            _data = iEntity.Data;

            // ✅ 在此订阅事件

            // 示例1:监听 Data 属性变化(响应 Spawn 后设置的初始数据)
            // ⚠️ 关键: 许多数据(如 SkillLevel, Target)是在 Spawn 之后才设置的
            // 所以必须监听 PropertyChanged 事件,而不是假设它们已经存在
            _entity.Events.On<GameEventType.Data.PropertyChanged>(
                OnDataChanged);

            // 示例2:跨组件通信 - 监听治疗请求事件
            _entity.Events.On<GameEventType.Unit.HealRequest>(
                OnHealRequest);
        }
    }

    public void OnComponentUnregistered()
    {
        // ✅ 无需手动解绑事件(EntityManager会自动调用Events.Clear())

        // 清理引用
        _data = null;
        _entity = null;
    }

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        // ❌ 不要在此订阅Data或Entity.Events事件(应在OnComponentRegistered)
    }

    public override void _Process(double delta)
    {

    }

    // ================= 核心API =================

    /// <summary>
    /// 示例:公开方法
    /// </summary>
    public void DoSomething()
    {
        if (_data == null || _entity == null) return;

        // 业务逻辑
    }

    // ================= 私有方法 =================

    /// <summary>
    /// 示例:监听Data属性变化
    /// </summary>
    private void OnDataChanged(GameEventType.Data.PropertyChanged evt)
    {
        if (evt.Key != GeneratedDataKey.Name) return;

        // 响应数据变化
    }

    /// <summary>
    /// 示例:跨组件通信 - 处理治疗请求
    /// 通过事件而非 GetComponent 实现解耦通信
    /// </summary>
    private void OnHealRequest(GameEventType.Unit.HealRequest evt)
    {
        // 处理治疗逻辑
        float healAmount = evt.Amount;
        _log.Info($"收到治疗请求: {healAmount}");

        // ✅ 通过事件发送结果,而非直接调用other Component方法
        // 这只是示例,实际根据需求选择合适的事件类型
    }
}
