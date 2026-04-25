using slime.data.Units;

/// <summary>
/// 批量生成敌人命令。
/// </summary>
/// <param name="Count">生成数量。</param>
/// <param name="EnemyData">敌人配置。</param>
/// <param name="Strategy">生成位置策略。</param>
public readonly record struct SpawnBatchRequest(int Count, EnemyData EnemyData, SpawnPositionStrategy Strategy);

/// <summary>
/// 批量生成敌人结果。
/// </summary>
/// <param name="SpawnedCount">请求生成数量。</param>
public readonly record struct SpawnBatchResult(int SpawnedCount);

/// <summary>
/// 开始波次命令。
/// </summary>
/// <param name="WaveIndex">波次索引。</param>
public readonly record struct StartWaveRequest(int WaveIndex);

/// <summary>
/// 开始波次结果。
/// </summary>
/// <param name="Started">是否发起波次。</param>
public readonly record struct StartWaveResult(bool Started);

/// <summary>
/// 清理敌人命令。
/// </summary>
public readonly record struct KillAllEnemiesRequest;

/// <summary>
/// 清理敌人结果。
/// </summary>
/// <param name="Cleared">是否发起清理。</param>
public readonly record struct KillAllEnemiesResult(bool Cleared);
