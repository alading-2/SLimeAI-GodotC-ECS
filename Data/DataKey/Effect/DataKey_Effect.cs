using Godot;

/// <summary>
/// 数据键定义 - 特效系统
/// </summary>
public static partial class DataKey
{
    // === 特效播放 ===

    /// <summary>播放速率倍率 (float, 默认1.0)</summary>
    public static readonly DataKey<float> EffectPlayRate = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(EffectPlayRate), DisplayName = "特效播放速率", Category = DataCategory_Ability.Effect, Type = typeof(float), DefaultValue = 1f, MinValue = 0f });

    /// <summary>特效缩放 (Vector2, 默认1,1)</summary>
    public static readonly DataKey<Vector2> EffectScale = DataRegistry.Register<Vector2>(
        new DataMeta { Key = nameof(EffectScale), DisplayName = "特效缩放", Category = DataCategory_Ability.Effect, Type = typeof(Vector2), DefaultValue = Vector2.One });

    /// <summary>特效偏移 (Vector2, 默认0,0)</summary>
    public static readonly DataKey<Vector2> EffectOffset = DataRegistry.Register<Vector2>(
        new DataMeta { Key = nameof(EffectOffset), DisplayName = "特效偏移", Category = DataCategory_Ability.Effect, Type = typeof(Vector2), DefaultValue = Vector2.Zero });

    // === 特效附着 ===

    /// <summary>是否附着到宿主 (bool)</summary>
    public static readonly DataKey<bool> EffectIsAttached = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(EffectIsAttached), DisplayName = "特效是否附着", Category = DataCategory_Ability.Effect, Type = typeof(bool), DefaultValue = false });

    // === 特效动画 ===

    /// <summary>指定播放的动画名 (string, 空=播放默认)</summary>
    public static readonly DataKey<string> EffectAnimationName = DataRegistry.Register<string>(
        new DataMeta { Key = nameof(EffectAnimationName), DisplayName = "特效动画名", Category = DataCategory_Ability.Effect, Type = typeof(string), DefaultValue = string.Empty });

    /// <summary>是否循环播放 (bool, 默认false)</summary>
    public static readonly DataKey<bool> EffectIsLooping = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(EffectIsLooping), DisplayName = "特效是否循环", Category = DataCategory_Ability.Effect, Type = typeof(bool), DefaultValue = false });
}
