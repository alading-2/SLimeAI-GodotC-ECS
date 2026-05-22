using System.Collections.Generic;
using slime.data;
namespace slime.data.Units
{
    /// <summary>
    /// 敌人配置（纯 POCO）
    /// </summary>
    public class EnemyData : UnitData
    {
        /// <summary>全部数据。</summary>
        public static IReadOnlyList<EnemyData> All => DataTable.GetAll<EnemyData>();

        /// <summary>按 Name 获取数据，找不到返回 null 并记录日志。</summary>
        public static EnemyData? Get(string name) => DataTable.GetByName<EnemyData>(name);

        // ====== 敌人专有 ======

        /// <summary>
        /// 击杀经验值奖励
        /// </summary>
        public int ExpReward { get; set; } = (int)DataKey.ExpReward.DefaultValue!;

        // ====== AI 配置 ======

        /// <summary>
        /// AI 检测范围
        /// </summary>
        public float DetectionRange { get; set; } = (float)DataKey.DetectionRange.DefaultValue!;

        // ====== Spawn Rule ======

        /// <summary>
        /// 是否启用生成规则
        /// </summary>
        public bool IsEnableSpawnRule { get; set; } = (bool)DataKey.IsEnableSpawnRule.DefaultValue!;

        /// <summary>
        /// 生成位置策略 (Rectangle/Circle)
        /// </summary>
        public SpawnPositionStrategy SpawnStrategy { get; set; } = (SpawnPositionStrategy)DataKey.SpawnStrategy.DefaultValue!;

        /// <summary>
        /// 起始波次 (从第几波开始生成)
        /// </summary>
        public int SpawnMinWave { get; set; } = (int)DataKey.SpawnMinWave.DefaultValue!;

        /// <summary>
        /// 截止波次 (-1表示无限制)
        /// </summary>
        public int SpawnMaxWave { get; set; } = (int)DataKey.SpawnMaxWave.DefaultValue!;

        /// <summary>
        /// 生成间隔 (秒)
        /// </summary>
        public float SpawnInterval { get; set; } = (float)DataKey.SpawnInterval.DefaultValue!;

        /// <summary>
        /// 单波次最大生成数量 (-1表示无限制)
        /// </summary>
        public int SpawnMaxCountPerWave { get; set; } = (int)DataKey.SpawnMaxCountPerWave.DefaultValue!;

        /// <summary>
        /// 单次生成数量
        /// </summary>
        public int SingleSpawnCount { get; set; } = (int)DataKey.SingleSpawnCount.DefaultValue!;

        /// <summary>
        /// 生成数量波动值 (最终数量 = Count ± Variance)
        /// </summary>
        public int SingleSpawnVariance { get; set; } = (int)DataKey.SingleSpawnVariance.DefaultValue!;

        /// <summary>
        /// 波次开始后的首次生成延迟 (秒)
        /// </summary>
        public float SpawnStartDelay { get; set; } = (float)DataKey.SpawnStartDelay.DefaultValue!;

        /// <summary>
        /// 生成权重 (用于随机生成池)
        /// </summary>
        public int SpawnWeight { get; set; } = (int)DataKey.SpawnWeight.DefaultValue!;

        // ====== 命名快捷属性 ======

        /// <summary>鱼人</summary>
        public static EnemyData Yuren => DataTable.GetRequiredByName<EnemyData>("鱼人");

        /// <summary>豺狼人</summary>
        public static EnemyData Chailangren => DataTable.GetRequiredByName<EnemyData>("豺狼人");
    }
}
