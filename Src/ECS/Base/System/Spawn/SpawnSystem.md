# SpawnSystem (敌人生成与波次管理系统)

`SpawnSystem` 是游戏的核心控制模块，负责管理敌人生成、波次流程以及游戏生命周期逻辑。该系统已升级为**程序化生成 (Procedural Generation)** 架构，通过规则配置自动组装每一波的怪物刷新逻辑。

## 核心设计理念

- **规则驱动 (Rule-Based)**: 不再手动配置每一波的敌人,而是定义"规则"。
  - 例如:"史莱姆在第 1-5 波出现,间隔 3 秒"。
  - 系统会根据当前波次自动筛选并激活符合条件的规则。
- **DataNew 配置**: 敌人生成规则来自 `EnemyData.All`，系统节奏常量来自 `SpawnSystemConfig`。
- **TimerManager 驱动**: 使用项目统一的 `TimerManager` 管理所有计时器,实现对象池复用,零 GC 压力。
- **生成管线化 (Pipeline)**:
  - **What (生成什么)**: 由 `EnemyData` 中启用的 Spawn Rule 字段决定。
  - **Where (在哪里生成)**: 委托给 `SpawnPositionCalculator` 处理（支持屏幕外、随机等策略）。
  - **How (如何生成)**: 系统在初始化时自动为所有配置的敌人创建对象池,并强制使用 `ObjectPoolManager` 进行复用。

## 数据结构

### SpawnSystemConfig

全局游戏节奏配置。

- `WaveDuration`: 每一波的标准持续时间（秒）。
- `MaxWaves`: 最大波次数量。

### EnemyData Spawn Rule

定义特定敌人的出现逻辑。

- `IsEnableSpawnRule`: 是否启用生成规则。
- `SpawnMinWave` / `SpawnMaxWave`: 出现的波次范围（闭区间，-1 表示无限）。
- `SpawnInterval`: 生成间隔（秒）。
- `SingleSpawnCount` / `SingleSpawnVariance`: 单次生成数量与波动值。
- `SpawnStartDelay`: 波次开始后的首次生成延迟。

## 核心逻辑流

1. **系统初始化 (`_Ready`)**:

   - 系统进入运行态后，按 `EnemyData.All` 筛选当前波次激活的生成规则。
   - 敌人实体通过 `EntityManager.Spawn(... UsingObjectPool = true, PoolName = EnemyPool)` 生成。
   - 对象池由 `ObjectPoolInit` 预热，`SpawnSystem` 不直接创建对象池。

2. **启动波次 (`StartWave`)**:

   - 使用 `TimerManager.Delay()` 创建波次主计时器 (`_waveTimer`)。
   - 筛选当前波次 (`CurrentWaveIndex`) 激活的所有规则。
   - 初始化运行时状态 (`RuleRuntimeState`),重置累积时间。
   - 使用 `TimerManager.Loop()` 创建核心轮询计时器 (`_checkTimer`)。

3. **生成的驱动 (TimerManager Architecture)**:

   - 采用 **TimerManager 统一管理** 架构,所有计时器由对象池复用,零 GC 压力。
   - `_checkTimer` 每 0.2 秒触发一次 `OnCheckTimerTimeout` 回调。
   - 在回调中遍历所有激活的规则,累加 `delta` 时间。
   - 当 `AccumulatedTime >= SpawnInterval` 时,触发生成逻辑并扣除时间(支持"追赶"机制以应对卡顿)。

4. **动态适应**:

   - 如果某一波没有任何规则匹配(空波次),系统会发出警告,但游戏流程不会中断。
   - 支持无限波次模式。

## 外部命令

外部调用不要直接使用 `SpawnSystem.Instance` 执行业务，应通过 `SystemManager.Execute` 进入系统，由 `SystemData` 统一判断当前是否允许执行：

- `StartWaveRequest`: 开启指定波次。
- `SpawnBatchRequest`: 手动批量生成（用于测试或特殊事件）。
- `KillAllEnemiesRequest`: 清理敌人池中的活跃敌人。

```csharp
SystemManager.Instance.Execute<SpawnSystem, SpawnBatchRequest, SpawnBatchResult>(
    new SpawnBatchRequest(3, EnemyData.Yuren, SpawnPositionStrategy.Rectangle)
);
```
