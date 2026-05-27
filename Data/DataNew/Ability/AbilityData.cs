using System.Collections.Generic;
using slime.data;

namespace slime.data.Abilities;

/// <summary>
/// 技能配置（纯 POCO，不继承 Resource）
/// </summary>
public class AbilityData
{
    /// <summary>全部数据。</summary>
    public static IReadOnlyList<AbilityData> All => DataTable.GetAll<AbilityData>();

    /// <summary>按 Name 获取数据，找不到返回 null 并记录日志。</summary>
    public static AbilityData? Get(string name) => DataTable.GetByName<AbilityData>(name);

    // ====== 命名快捷属性 ======

    /// <summary>猛击</summary>
    public static AbilityData Slam => DataTable.GetRequiredByName<AbilityData>("猛击");

    /// <summary>环绕技能</summary>
    public static AbilityData OrbitSkill => DataTable.GetRequiredByName<AbilityData>("环绕技能");

    /// <summary>正弦波射击</summary>
    public static AbilityData SineWaveShot => DataTable.GetRequiredByName<AbilityData>("正弦波射击");

    /// <summary>定点抛炸弹</summary>
    public static AbilityData ParabolaShot => DataTable.GetRequiredByName<AbilityData>("定点抛炸弹");

    /// <summary>回旋镖投掷</summary>
    public static AbilityData BoomerangThrow => DataTable.GetRequiredByName<AbilityData>("回旋镖投掷");

    /// <summary>圆弧射击</summary>
    public static AbilityData ArcShot => DataTable.GetRequiredByName<AbilityData>("圆弧射击");

    /// <summary>贝塞尔射击</summary>
    public static AbilityData BezierShot => DataTable.GetRequiredByName<AbilityData>("贝塞尔射击");

    /// <summary>冲刺</summary>
    public static AbilityData Dash => DataTable.GetRequiredByName<AbilityData>("冲刺");

    /// <summary>圆环伤害</summary>
    public static AbilityData CircleDamage => DataTable.GetRequiredByName<AbilityData>("圆环伤害");

    // ====== 基础信息 ======

    /// <summary>
    /// 技能名称
    /// </summary>
    public string? Name { get; set; } = (string)DataKey.Name.DefaultValue!;

    /// <summary>
    /// 技能分组 ID
    /// </summary>
    [DataKey(nameof(DataKey.AbilityFeatureGroup))]
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
    [DataKey(nameof(DataKey.AbilityIcon))]
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
    [DataKey(nameof(DataKey.EffectScene))]
    public string EffectScenePath { get; set; } = "";

    /// <summary>
    /// 技能投射物视觉场景路径 (res:// 路径字符串)
    /// </summary>
    [DataKey(nameof(DataKey.ProjectileScene))]
    public string ProjectileScenePath { get; set; } = "";

    // ====== 伤害效果 ======

    /// <summary>
    /// 技能伤害数值
    /// </summary>
    public float AbilityDamage { get; set; } = (float)DataKey.AbilityDamage.DefaultValue!;
}
