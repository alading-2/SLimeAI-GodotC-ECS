using System;
using System.IO;

/// <summary>
/// descriptor-first Data runtime 启动器。
/// </summary>
public sealed class DataRuntimeBootstrap
{
    private static readonly Lazy<DataRuntimeBootstrap> DefaultLazy = new(CreateDefault);
    private readonly DataComputeRegistry _computeRegistry;
    private RuntimeDataSnapshotLoader? _loader;

    /// <summary>
    /// 使用默认 framework resolver 创建启动器。
    /// </summary>
    public DataRuntimeBootstrap()
        : this(CreateDefaultComputeRegistry())
    {
    }

    /// <summary>
    /// 使用指定 resolver 注册表创建启动器。
    /// </summary>
    /// <param name="computeRegistry">computed resolver 注册表。</param>
    public DataRuntimeBootstrap(DataComputeRegistry computeRegistry)
    {
        ArgumentNullException.ThrowIfNull(computeRegistry);
        _computeRegistry = computeRegistry; // computed resolver 注册表
    }

    /// <summary>
    /// 当前 Data catalog。
    /// </summary>
    public DataDefinitionCatalog Catalog { get; private set; } = null!;

    /// <summary>
    /// 当前 runtime snapshot。
    /// </summary>
    public RuntimeDataSnapshot Snapshot { get; private set; } = null!;

    /// <summary>
    /// 当前 snapshot loader。
    /// </summary>
    public RuntimeDataSnapshotLoader Loader => _loader ?? throw new InvalidOperationException("DataRuntimeBootstrap 尚未 Initialize。");

    /// <summary>
    /// 仓库 runtime snapshot 默认启动器。
    /// </summary>
    public static DataRuntimeBootstrap Default => DefaultLazy.Value;

    /// <summary>
    /// 初始化 snapshot 与 catalog。
    /// </summary>
    /// <param name="snapshotJson">runtime_snapshot.json 内容。</param>
    public void Initialize(string snapshotJson)
    {
        _loader = new RuntimeDataSnapshotLoader(_computeRegistry);
        Snapshot = _loader.LoadFromJson(snapshotJson);
        Catalog = _loader.BuildCatalog(Snapshot.Descriptors);
    }

    /// <summary>
    /// 按 table/id 查找 record。
    /// </summary>
    /// <param name="table">record table。</param>
    /// <param name="id">record id。</param>
    public RuntimeDataRecordDto FindRecord(string table, string id)
    {
        EnsureInitialized();
        return Snapshot.FindRecord(table, id);
    }

    /// <summary>
    /// 按 table/name 查找 record。
    /// </summary>
    /// <param name="table">record table。</param>
    /// <param name="name">record 展示名。</param>
    public RuntimeDataRecordDto FindRecordByName(string table, string name)
    {
        EnsureInitialized();
        return Snapshot.FindRecordByName(table, name);
    }

    /// <summary>
    /// 创建并应用 record 的 Data 容器。
    /// </summary>
    /// <param name="record">snapshot record。</param>
    public Data CreateData(RuntimeDataRecordDto record)
    {
        EnsureInitialized();
        var data = new Data(Catalog);
        var report = Loader.ApplyRecord(data, Catalog, record);
        if (report.HasErrors)
        {
            throw new InvalidOperationException(report.ToSummary());
        }

        return data;
    }

    /// <summary>
    /// 将现有 Data 容器绑定当前 catalog 并应用 record。
    /// </summary>
    /// <param name="data">目标 Data 容器。</param>
    /// <param name="record">snapshot record。</param>
    public DataApplyReport ApplyToData(Data data, RuntimeDataRecordDto record)
    {
        ArgumentNullException.ThrowIfNull(data);
        EnsureInitialized();
        data.BindRuntimeCatalog(Catalog);
        return Loader.ApplyRecord(data, Catalog, record);
    }

    private void EnsureInitialized()
    {
        if (_loader == null || Catalog == null || Snapshot == null)
        {
            throw new InvalidOperationException("DataRuntimeBootstrap 尚未 Initialize。");
        }
    }

    /// <summary>
    /// 注册框架内置的 6 个 computed resolver：
    /// AttributeBonus（属性加成）、Percent（百分比）、AttackInterval（攻击间隔）、
    /// Regen（恢复）、EffectiveHp（有效生命）、Dps（每秒伤害）。
    /// </summary>
    private static DataComputeRegistry CreateDefaultComputeRegistry()
    {
        var registry = new DataComputeRegistry();
        registry.Register(new AttributeBonusComputeResolver());
        registry.Register(new PercentComputeResolver());
        registry.Register(new AttackIntervalComputeResolver());
        registry.Register(new RegenComputeResolver());
        registry.Register(new EffectiveHpComputeResolver());
        registry.Register(new DpsComputeResolver());
        return registry;
    }

    private static DataRuntimeBootstrap CreateDefault()
    {
        var bootstrap = new DataRuntimeBootstrap();
        bootstrap.Initialize(File.ReadAllText(ResolveRuntimeSnapshotPath()));
        return bootstrap;
    }

    /// <summary>
    /// 从 AppContext.BaseDirectory 向上遍历查找 runtime_snapshot.json，找不到则回退到 Environment.CurrentDirectory。
    /// </summary>
    private static string ResolveRuntimeSnapshotPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "Data", "DataOS", "Snapshots", "runtime_snapshot.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        var cwdCandidate = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "Data", "DataOS", "Snapshots", "runtime_snapshot.json"));
        if (File.Exists(cwdCandidate))
        {
            return cwdCandidate;
        }

        throw new FileNotFoundException("runtime_snapshot.json not found from application base directory or current directory.");
    }
}
