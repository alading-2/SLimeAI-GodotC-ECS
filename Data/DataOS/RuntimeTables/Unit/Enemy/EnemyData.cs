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
        public int ExpReward { get; set; } = 1;

        // ====== AI 配置 ======

        /// <summary>
        /// AI 检测范围
        /// </summary>
        public float DetectionRange { get; set; } = 500f;

        // ====== Spawn Rule ======

        /// <summary>
        /// 是否启用生成规则
        /// </summary>
        public bool IsEnableSpawnRule { get; set; } = true;

        /// <summary>
        /// 生成位置策略 (Rectangle/Circle)
        /// </summary>
        public SpawnPositionStrategy SpawnStrategy { get; set; } = SpawnPositionStrategy.Rectangle;

        /// <summary>
        /// 起始波次 (从第几波开始生成)
        /// </summary>
        public int SpawnMinWave { get; set; } = 0;

        /// <summary>
        /// 截止波次 (-1表示无限制)
        /// </summary>
        public int SpawnMaxWave { get; set; } = -1;

        /// <summary>
        /// 生成间隔 (秒)
        /// </summary>
        public float SpawnInterval { get; set; } = 1f;

        /// <summary>
        /// 单波次最大生成数量 (-1表示无限制)
        /// </summary>
        public int SpawnMaxCountPerWave { get; set; } = -1;

        /// <summary>
        /// 单次生成数量
        /// </summary>
        public int SingleSpawnCount { get; set; } = 1;

        /// <summary>
        /// 生成数量波动值 (最终数量 = Count ± Variance)
        /// </summary>
        public int SingleSpawnVariance { get; set; } = 0;

        /// <summary>
        /// 波次开始后的首次生成延迟 (秒)
        /// </summary>
        public float SpawnStartDelay { get; set; } = 0f;

        /// <summary>
        /// 生成权重 (用于随机生成池)
        /// </summary>
        public int SpawnWeight { get; set; } = 10;

        // ====== 实例 ======

        /// <summary>鱼人</summary>
        public static readonly EnemyData Yuren = new()
        {
            ExpReward = 2,
            SpawnMinWave = 1,
            SpawnInterval = 2.0f,
            SingleSpawnCount = 3,
            SingleSpawnVariance = 1,
            Name = "鱼人",
            Team = Team.Enemy,
            VisualScenePath = "res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn",
            BaseHp = 150f,
            BaseAttack = 6f,
            AttackRange = 200f,
            BaseDefense = 1f,
            MoveSpeed = 150f,
        };

        /// <summary>豺狼人</summary>
        public static readonly EnemyData Chailangren = new()
        {
            ExpReward = 5,
            SpawnStrategy = SpawnPositionStrategy.Circle,
            SpawnMinWave = 1,
            SpawnInterval = 3.0f,
            SingleSpawnCount = 2,
            Name = "豺狼人",
            Team = Team.Enemy,
            VisualScenePath = "res://assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn",
            HealthBarHeight = 155f,
            BaseHp = 100f,
            BaseAttack = 5f,
            BaseDefense = 3f,
            MoveSpeed = 150f,
        };
    }
}
