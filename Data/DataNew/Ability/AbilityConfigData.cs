namespace Slime.ConfigNew.Abilities;

/// <summary>
/// 技能配置（纯 POCO，不继承 Resource）
/// </summary>
public class AbilityConfigData
{
    // ====== 实例 ======

    /// <summary>猛击</summary>
    public static readonly AbilityConfigData Slam = new()
    {
        Name = "猛击",
        FeatureGroupId = "技能.主动",
        FeatureHandlerId = "技能.主动.猛击",
        Description = "在角色周围随机位置猛击地面，对范围内敌人造成物理伤害",
        AbilityIconPath = "res://icon.svg",
        AbilityTriggerMode = AbilityTriggerMode.Manual,
        AbilityCostType = AbilityCostType.Mana,
        AbilityCooldown = 1.0f,
        AbilityCastRange = 500f,
        AbilityEffectRadius = 300f,
        EffectScenePath = "res://assets/Effect/020/AnimatedSprite2D/020.tscn",
        AbilityDamage = 30f
    };

    /// <summary>位置目标</summary>
    public static readonly AbilityConfigData TargetPointSkill = new()
    {
        Name = "位置目标",
        FeatureGroupId = "技能.主动",
        Description = "选择一个位置进行范围攻击",
        AbilityIconPath = "res://icon.svg",
        AbilityTriggerMode = AbilityTriggerMode.Manual,
        AbilityCostType = AbilityCostType.Mana,
        AbilityCooldown = 1.0f,
        AbilityTargetSelection = AbilityTargetSelection.Point,
        AbilityCastRange = 400f,
        AbilityEffectRadius = 200f,
        AbilityDamage = 10f
    };

    /// <summary>环绕技能</summary>
    public static readonly AbilityConfigData OrbitSkill = new()
    {
        Name = "环绕技能",
        FeatureGroupId = "技能.被动",
        FeatureHandlerId = "技能.被动.环绕技能",
        Description = "生成多个投射物环绕玩家旋转，碰触敌人造成伤害（验证 Orbit 模式）",
        AbilityIconPath = "res://icon.svg",
        AbilityTriggerMode = AbilityTriggerMode.Manual,
        AbilityCooldown = 1.0f,
        ProjectileScenePath = "res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn",
        AbilityDamage = 20f
    };

    /// <summary>正弦波射击</summary>
    public static readonly AbilityConfigData SineWaveShot = new()
    {
        Name = "正弦波射击",
        FeatureGroupId = "技能.投射物",
        FeatureHandlerId = "技能.投射物.正弦波射击",
        Description = "发射正弦波形弹道向敌人射击（验证 SineWave 模式）",
        AbilityIconPath = "res://icon.svg",
        AbilityTriggerMode = AbilityTriggerMode.Manual,
        AbilityCooldown = 1.0f,
        AbilityCastRange = 600f,
        ProjectileScenePath = "res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn",
        AbilityDamage = 25f
    };

    /// <summary>定点抛炸弹</summary>
    public static readonly AbilityConfigData ParabolaShot = new()
    {
        Name = "定点抛炸弹",
        FeatureGroupId = "技能.投射物",
        FeatureHandlerId = "技能.投射物.定点抛炸弹",
        Description = "每隔一段时间向施法者周围随机落点抛出一枚炸弹，落地时造成范围伤害（固定终点 Parabola 模式）",
        AbilityIconPath = "res://icon.svg",
        AbilityTriggerMode = AbilityTriggerMode.Periodic,
        AbilityCooldown = 1.0f,
        AbilityCastRange = 700f,
        AbilityEffectRadius = 250f,
        EffectScenePath = "res://assets/Effect/004龙卷风/AnimatedSprite2D/004龙卷风.tscn",
        ProjectileScenePath = "res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn",
        AbilityDamage = 9f
    };

    /// <summary>回旋镖投掷</summary>
    public static readonly AbilityConfigData BoomerangThrow = new()
    {
        Name = "回旋镖投掷",
        FeatureGroupId = "技能.投射物",
        FeatureHandlerId = "技能.投射物.回旋镖投掷",
        Description = "投掷回旋镖，飞出后自动返回，来回命中敌人（验证 Boomerang 模式）",
        AbilityIconPath = "res://icon.svg",
        AbilityTriggerMode = AbilityTriggerMode.Manual,
        AbilityCooldown = 1.0f,
        AbilityTargetSelection = AbilityTargetSelection.None,
        AbilityCastRange = 800f,
        ProjectileScenePath = "res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn",
        AbilityDamage = 22f
    };

    /// <summary>圆弧射击</summary>
    public static readonly AbilityConfigData ArcShot = new()
    {
        Name = "圆弧射击",
        FeatureGroupId = "技能.投射物",
        FeatureHandlerId = "技能.投射物.圆弧射击",
        Description = "发射沿圆弧轨迹飞行的投射物（验证 CircularArc 模式）",
        AbilityIconPath = "res://icon.svg",
        AbilityTriggerMode = AbilityTriggerMode.Manual,
        AbilityCooldown = 1.0f,
        AbilityTargetSelection = AbilityTargetSelection.Entity,
        AbilityCastRange = 700f,
        ProjectileScenePath = "res://assets/Projectile/Projectile/Polygon2D/BoomerangChevron.tscn",
        AbilityDamage = 26f
    };

    /// <summary>贝塞尔射击</summary>
    public static readonly AbilityConfigData BezierShot = new()
    {
        Name = "贝塞尔射击",
        FeatureGroupId = "技能.投射物",
        FeatureHandlerId = FeatureId.Ability.Projectile.BezierShot,
        Description = "发射沿二次贝塞尔曲线飞行的弓形弹（验证 BezierCurve 模式）",
        AbilityIconPath = "res://icon.svg",
        AbilityTriggerMode = AbilityTriggerMode.Manual,
        AbilityCooldown = 1.0f,
        AbilityCastRange = 600f,
        ProjectileScenePath = "res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn",
        AbilityDamage = 30f
    };

    /// <summary>冲刺</summary>
    public static readonly AbilityConfigData Dash = new()
    {
        Name = "冲刺",
        FeatureGroupId = "技能.位移",
        FeatureHandlerId = "技能.位移.冲刺",
        Description = "高速冲向目标方向，瞬间位移躲避危险",
        AbilityIconPath = "res://icon.svg",
        AbilityTriggerMode = AbilityTriggerMode.Manual,
        AbilityCooldown = 1.0f,
        AbilityCastRange = 300f,
        AbilityEffectRadius = 300f,
        EffectScenePath = "res://assets/Effect/lrsc3/AnimatedSprite2D/lrsc3.tscn"
    };

    /// <summary>圆环伤害</summary>
    public static readonly AbilityConfigData CircleDamage = new()
    {
        Name = "圆环伤害",
        FeatureGroupId = "技能.被动",
        FeatureHandlerId = "技能.被动.圆环伤害",
        Description = "周身燃起烈焰光环，每秒对周围敌人造成魔法伤害",
        AbilityIconPath = "res://icon.svg",
        AbilityType = AbilityType.Passive,
        AbilityTriggerMode = AbilityTriggerMode.Permanent,
        AbilityCooldown = 1.0f,
        AbilityEffectRadius = 500f,
        EffectScenePath = "res://assets/Effect/003/AnimatedSprite2D/003.tscn",
        AbilityDamage = 10f
    };

    /// <summary>光环护盾</summary>
    public static readonly AbilityConfigData AuraShield = new()
    {
        Name = "光环护盾",
        FeatureGroupId = "技能.被动",
        FeatureHandlerId = "技能.被动.光环护盾",
        Description = "在玩家旁生成跟随护盾，接触敌人造成伤害（验证 AttachToHost 模式）",
        AbilityIconPath = "res://icon.svg",
        AbilityTriggerMode = AbilityTriggerMode.Manual,
        AbilityCooldown = 1.0f,
        AbilityDamage = 15f
    };
    // ====== 基础信息 ======

    /// <summary>
    /// 技能名称
    /// </summary>
    public string? Name { get; set; } = (string)DataKey.Name.DefaultValue!;

    /// <summary>
    /// 技能分组 ID
    /// </summary>
    public string? FeatureGroupId { get; set; } = (string)DataKey.AbilityFeatureGroup.DefaultValue!;

    /// <summary>
    /// Feature执行函数ID
    /// </summary>
    public string? FeatureHandlerId { get; set; } = (string)DataKey.FeatureHandlerId.DefaultValue!;

    /// <summary>
    /// 技能描述
    /// </summary>
    public string? Description { get; set; } = (string)DataKey.Description.DefaultValue!;

    /// <summary>
    /// 技能图标路径
    /// </summary>
    public string AbilityIconPath { get; set; } = "";

    /// <summary>
    /// 当前级别
    /// </summary>
    public int AbilityLevel { get; set; } = (int)DataKey.AbilityLevel.DefaultValue!;

    /// <summary>
    /// 最大级别
    /// </summary>
    public int AbilityMaxLevel { get; set; } = (int)DataKey.AbilityMaxLevel.DefaultValue!;

    // ====== 技能类型 ======

    /// <summary>
    /// 实体类型
    /// </summary>
    public EntityType EntityType { get; set; } = EntityType.Ability;

    /// <summary>
    /// 技能类型
    /// </summary>
    public AbilityType AbilityType { get; set; } = (AbilityType)DataKey.AbilityType.DefaultValue!;

    /// <summary>
    /// 触发模式
    /// </summary>
    public AbilityTriggerMode AbilityTriggerMode { get; set; } = (AbilityTriggerMode)DataKey.AbilityTriggerMode.DefaultValue!;

    // ====== 消耗与冷却 ======

    /// <summary>
    /// 消耗类型
    /// </summary>
    public AbilityCostType AbilityCostType { get; set; } = (AbilityCostType)DataKey.AbilityCostType.DefaultValue!;

    /// <summary>
    /// 消耗数值
    /// </summary>
    public float AbilityCostAmount { get; set; } = (float)DataKey.AbilityCostAmount.DefaultValue!;

    /// <summary>
    /// 冷却时间 (秒)
    /// </summary>
    public float AbilityCooldown { get; set; } = (float)DataKey.AbilityCooldown.DefaultValue!;

    // ====== 充能系统 ======

    /// <summary>
    /// 是否使用充能系统
    /// </summary>
    public bool IsAbilityUsesCharges { get; set; } = (bool)DataKey.IsAbilityUsesCharges.DefaultValue!;

    /// <summary>
    /// 最大充能层数
    /// </summary>
    public int AbilityMaxCharges { get; set; } = (int)DataKey.AbilityMaxCharges.DefaultValue!;

    /// <summary>
    /// 充能时间 (秒)
    /// </summary>
    public float AbilityChargeTime { get; set; } = (float)DataKey.AbilityChargeTime.DefaultValue!;

    // ====== 目标选择 ======

    /// <summary>
    /// 目标选择方式
    /// </summary>
    public AbilityTargetSelection AbilityTargetSelection { get; set; } = (AbilityTargetSelection)DataKey.AbilityTargetSelection.DefaultValue!;

    /// <summary>
    /// 施法距离（由具体 Handler 决定如何解释）
    /// </summary>
    public float AbilityCastRange { get; set; } = (float)DataKey.AbilityCastRange.DefaultValue!;

    /// <summary>
    /// 效果半径（命中范围 / AOE 半径）
    /// </summary>
    public float AbilityEffectRadius { get; set; } = (float)DataKey.AbilityEffectRadius.DefaultValue!;

    // ====== 视觉与表现 ======

    /// <summary>
    /// 技能表现特效场景路径 (res:// 路径字符串)
    /// </summary>
    public string EffectScenePath { get; set; } = "";

    /// <summary>
    /// 技能投射物视觉场景路径 (res:// 路径字符串)
    /// </summary>
    public string ProjectileScenePath { get; set; } = "";

    // ====== 伤害效果 ======

    /// <summary>
    /// 技能伤害数值
    /// </summary>
    public float AbilityDamage { get; set; } = (float)DataKey.AbilityDamage.DefaultValue!;
}
