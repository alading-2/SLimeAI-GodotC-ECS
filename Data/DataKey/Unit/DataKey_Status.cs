/// <summary>
/// 数据键定义 - 状态快照域。
/// </summary>
public static partial class DataKey
{
    /// <summary>状态聚合后是否允许思考。</summary>
    public static readonly DataKey<bool> StatusCanThink = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(StatusCanThink), DisplayName = "状态允许思考", Description = "状态聚合后是否允许 AI/逻辑继续思考", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = true  });

    /// <summary>状态聚合后是否允许主动移动。</summary>
    public static readonly DataKey<bool> StatusCanMoveInput = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(StatusCanMoveInput), DisplayName = "状态允许主动移动", Description = "状态聚合后是否允许玩家/AI 主动写入移动意图", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = true  });

    /// <summary>状态聚合后是否允许攻击。</summary>
    public static readonly DataKey<bool> StatusCanAttack = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(StatusCanAttack), DisplayName = "状态允许攻击", Description = "状态聚合后是否允许发起普通攻击", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = true  });

    /// <summary>状态聚合后是否允许施法。</summary>
    public static readonly DataKey<bool> StatusCanCast = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(StatusCanCast), DisplayName = "状态允许施法", Description = "状态聚合后是否允许技能进入触发流水线", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = true  });

    /// <summary>状态聚合后的无敌标记。</summary>
    public static readonly DataKey<bool> StatusIsInvulnerable = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(StatusIsInvulnerable), DisplayName = "状态无敌", Description = "状态聚合后的无敌标记", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false  });

    /// <summary>状态聚合后的控制免疫标记。</summary>
    public static readonly DataKey<bool> StatusIsControlImmune = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(StatusIsControlImmune), DisplayName = "状态控制免疫", Description = "状态聚合后的控制免疫标记", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false  });
}
