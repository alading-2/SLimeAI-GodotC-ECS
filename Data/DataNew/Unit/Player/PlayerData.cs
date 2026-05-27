using System.Collections.Generic;
using slime.data;
namespace slime.data.Units
{
    /// <summary>
    /// 玩家配置（纯 POCO）
    /// </summary>
    public class PlayerData : UnitData
    {
        /// <summary>全部数据。</summary>
        public static IReadOnlyList<PlayerData> All => DataTable.GetAll<PlayerData>();

        /// <summary>按 Name 获取数据，找不到返回 null 并记录日志。</summary>
        public static PlayerData? Get(string name) => DataTable.GetByName<PlayerData>(name);

        // ====== 玩家专有 ======

        /// <summary>
        /// 基础法力值
        /// </summary>
        public float BaseMana { get; set; } = (float)DataKey.BaseMana.DefaultValue!;

        /// <summary>
        /// 基础法力回复 (每秒)
        /// </summary>
        public float BaseManaRegen { get; set; } = (float)DataKey.BaseManaRegen.DefaultValue!;

        /// <summary>
        /// 拾取范围
        /// </summary>
        public float PickupRange { get; set; } = (float)DataKey.PickupRange.DefaultValue!;

        /// <summary>
        /// 基础技能伤害
        /// </summary>
        [DataKey(nameof(DataKey.AbilityDamageBonus))]
        public float BaseSkillDamage { get; set; } = (float)DataKey.AbilityDamageBonus.DefaultValue!;

        /// <summary>
        /// 冷却缩减 (%)
        /// </summary>
        public float CooldownReduction { get; set; } = (float)DataKey.CooldownReduction.DefaultValue!;

        // ====== 命名快捷属性 ======

        /// <summary>德鲁伊</summary>
        public static PlayerData Deluyi => DataTable.GetRequiredByName<PlayerData>("德鲁伊");
    }
}
