using System;
using System.Runtime.CompilerServices;
using Godot;
using Slime.Test;

/// <summary>
/// AbilitySystem 流水线测试。
/// </summary>
public partial class AbilitySystemPipelineTest : Node
{
    private static readonly Log _log = new(nameof(AbilitySystemPipelineTest));

    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        _log.Info("开始 AbilitySystem 流水线测试");

        try
        {
            Test_AbilityFeatureHandler_DoesNotExposePrepareCast();
            Test_EntitySelectionAbility_AllowsHandlerManagedExecution();
            Test_AbilityToolHandler_CanExecuteThroughPipeline();
            Test_Dash_UsesLastMoveDirectionWhenVelocityIsZero();
            Test_ParabolaShot_UsesRandomPointInCircleInsteadOfEnemyPosition();
            Test_BoomerangThrow_UsesRandomPointInRingInsteadOfEnemyPosition();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"AbilitySystem 流水线测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    /// <summary>
    /// 回归测试：
    /// Ability 子域 Handler 只保留 FeatureSystem 生命周期桥接，不再暴露施法前置准备钩子。
    /// </summary>
    private void Test_AbilityFeatureHandler_DoesNotExposePrepareCast()
    {
        var prepareCast = typeof(AbilityFeatureHandler).GetMethod(
            "PrepareCast", //旧的消耗前准备钩子名称
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
        );

        AssertEqual(
            "AbilityFeatureHandler 不应再暴露 PrepareCast",
            null, //期望没有该方法
            prepareCast //实际反射结果
        );
    }

    /// <summary>
    /// 回归测试：
    /// 即使 AbilityTargetSelection=Entity，只要 Handler 可以自行处理，系统也不应在执行前强制失败。
    /// </summary>
    private void Test_EntitySelectionAbility_AllowsHandlerManagedExecution()
    {
        var owner = new TestEntity
        {
            Name = "PipelineOwner"
        };
        AddChild(owner);

        var ability = new AbilityEntity
        {
            Name = "PipelineAbility"
        };
        AddChild(ability);

        var ownerId = owner.GetInstanceId().ToString();
        owner.Data.Set(DataKey.Id, ownerId);
        owner.Data.Set(DataKey.Name, owner.Name);
        owner.Data.Set(DataKey.CurrentMana, 50f);

        var abilityId = ability.GetInstanceId().ToString();
        ability.Data.Set(DataKey.Id, abilityId);
        ability.Data.Set(DataKey.Name, "HandlerManagedEntityAbility");
        ability.Data.Set(DataKey.FeatureEnabled, true);
        ability.Data.Set(DataKey.FeatureIsActive, false);
        ability.Data.Set(DataKey.FeatureHandlerId, AbilitySystemPipelineTestHandler.HandlerId);
        ability.Data.Set(DataKey.AbilityType, (int)AbilityType.Active);
        ability.Data.Set(DataKey.AbilityTriggerMode, (int)AbilityTriggerMode.Manual);
        ability.Data.Set(DataKey.AbilityTargetSelection, (int)AbilityTargetSelection.Entity);
        ability.Data.Set(DataKey.AbilityCostType, (int)AbilityCostType.Mana);
        ability.Data.Set(DataKey.AbilityCostAmount, 10f);
        ability.Data.Set(DataKey.IsAbilityUsesCharges, false);

        EntityManager.Register(owner);
        EntityManager.Register(ability);
        EntityRelationshipManager.AddRelationship(
            ownerId, //父实体ID
            abilityId, //子实体ID
            EntityRelationshipType.ENTITY_TO_ABILITY //关系类型
        );

        var costComponent = new CostComponent();
        EntityManager.AddComponent(ability, costComponent);
        ability.Events.On<GameEventType.Ability.TryTrigger>(
            GameEventType.Ability.TryTrigger,
            AbilitySystem.HandleTryTrigger
        );

        AbilitySystemPipelineTestHandler.ExecuteCount = 0;

        var context = new CastContext
        {
            Ability = ability,
            Caster = owner,
            ResponseContext = new EventContext()
        };

        ability.Events.Emit(
            GameEventType.Ability.TryTrigger,
            new GameEventType.Ability.TryTrigger(context) //触发上下文
        );

        var result = context.ResponseContext?.HasResult == true
            ? context.ResponseContext.GetResult<TriggerResult>()
            : TriggerResult.Failed;

        AssertEqual(
            "Entity 目标技能应允许 Handler 自行处理而不是被系统提前拦截",
            TriggerResult.Success, //期望结果
            result //实际结果
        );
        AssertEqual(
            "成功执行后应扣除法力",
            40f, //期望法力
            owner.Data.Get<float>(DataKey.CurrentMana) //实际法力
        );
        AssertEqual(
            "Handler 应被真正执行一次",
            1, //期望执行次数
            AbilitySystemPipelineTestHandler.ExecuteCount //实际执行次数
        );

        EntityManager.Destroy(ability);
        EntityManager.Destroy(owner);
    }

    /// <summary>
    /// 回归测试：
    /// Ability 子域通用工具迁出基类后，具体 Handler 仍应能通过 AbilityTool 访问施法上下文。
    /// </summary>
    private void Test_AbilityToolHandler_CanExecuteThroughPipeline()
    {
        var owner = new AbilityToolPipelineTestCaster
        {
            Name = "AbilityToolOwner"
        };
        AddChild(owner);

        var ability = new AbilityEntity
        {
            Name = "AbilityToolAbility"
        };
        AddChild(ability);

        var ownerId = owner.GetInstanceId().ToString();
        owner.Data.Set(DataKey.Id, ownerId);
        owner.Data.Set(DataKey.Name, owner.Name);
        owner.Data.Set(DataKey.CurrentMana, 30f);
        owner.Data.Set(DataKey.AbilityDamageBonus, 150f);

        var abilityId = ability.GetInstanceId().ToString();
        ability.Data.Set(DataKey.Id, abilityId);
        ability.Data.Set(DataKey.Name, "AbilityToolBridgeAbility");
        ability.Data.Set(DataKey.FeatureEnabled, true);
        ability.Data.Set(DataKey.FeatureIsActive, false);
        ability.Data.Set(DataKey.FeatureHandlerId, AbilityToolPipelineTestHandler.HandlerId);
        ability.Data.Set(DataKey.AbilityType, (int)AbilityType.Active);
        ability.Data.Set(DataKey.AbilityTriggerMode, (int)AbilityTriggerMode.Manual);
        ability.Data.Set(DataKey.AbilityTargetSelection, (int)AbilityTargetSelection.None);
        ability.Data.Set(DataKey.AbilityCostType, (int)AbilityCostType.Mana);
        ability.Data.Set(DataKey.AbilityCostAmount, 5f);
        ability.Data.Set(DataKey.IsAbilityUsesCharges, false);
        ability.Data.Set(DataKey.AbilityDamage, 20f);

        EntityManager.Register(owner);
        EntityManager.Register(ability);
        EntityRelationshipManager.AddRelationship(
            ownerId, //父实体ID
            abilityId, //子实体ID
            EntityRelationshipType.ENTITY_TO_ABILITY //关系类型
        );

        var costComponent = new CostComponent();
        EntityManager.AddComponent(ability, costComponent);
        ability.Events.On<GameEventType.Ability.TryTrigger>(
            GameEventType.Ability.TryTrigger,
            AbilitySystem.HandleTryTrigger
        );

        AbilityToolPipelineTestHandler.ExecuteCount = 0;
        AbilityToolPipelineTestHandler.finalDamage = 0f;
        AbilityToolPipelineTestHandler.LastCasterName = string.Empty;

        var context = new CastContext
        {
            Ability = ability,
            Caster = owner,
            ResponseContext = new EventContext()
        };

        ability.Events.Emit(
            GameEventType.Ability.TryTrigger,
            new GameEventType.Ability.TryTrigger(context) //触发上下文
        );

        var result = context.ResponseContext?.HasResult == true
            ? context.ResponseContext.GetResult<TriggerResult>()
            : TriggerResult.Failed;

        AssertEqual(
            "AbilityTool Handler 应能正常通过流水线执行",
            TriggerResult.Success, //期望结果
            result //实际结果
        );
        AssertEqual(
            "AbilityTool 应能正确计算技能伤害倍率",
            30f, //期望伤害
            AbilityToolPipelineTestHandler.finalDamage //实际伤害
        );
        AssertEqual(
            "AbilityTool 应能读取施法者节点",
            owner.Name.ToString(), //期望施法者名
            AbilityToolPipelineTestHandler.LastCasterName //实际施法者名
        );
        AssertEqual(
            "AbilityTool Handler 应被执行一次",
            1, //期望执行次数
            AbilityToolPipelineTestHandler.ExecuteCount //实际执行次数
        );

        EntityManager.Destroy(ability);
        EntityManager.Destroy(owner);
    }

    /// <summary>
    /// 回归测试：
    /// Dash 在当前速度为 0 时，应优先使用“最后一次主动移动方向缓存”，而不是默认回退到向右。
    /// </summary>
    private void Test_Dash_UsesLastMoveDirectionWhenVelocityIsZero()
    {
        var caster = new AbilityPipelineTargetPointTestEntity
        {
            Name = "DashCaster",
            GlobalPosition = Vector2.Zero
        };
        AddChild(caster);

        caster.Data.Set(DataKey.Id, caster.GetInstanceId().ToString());
        caster.Data.Set(DataKey.Velocity, Vector2.Zero);
        caster.Data.Set(DataKey.LastMoveDirection, Vector2.Left);

        EntityManager.Register(caster);

        try
        {
            var executorType = typeof(AbilitySystemPipelineTest).Assembly.GetType("DashExecutor");
            var method = executorType?.GetMethod(
                "ResolveDashDirection",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
            );
            var result = method?.Invoke(null, new object[] { caster, caster });

            AssertEqual(
                "Dash 应能通过反射找到方向解析逻辑",
                true,
                result is Vector2
            );

            if (result is not Vector2 dashDirection) return;

            AssertEqual(
                "Dash 在当前速度为 0 时应优先使用最后一次主动移动方向缓存",
                true,
                dashDirection.IsEqualApprox(Vector2.Left)
            );
        }
        finally
        {
            EntityManager.Destroy(caster);
        }
    }

    /// <summary>
    /// 回归测试：
    /// ParabolaShot 应改为以施法者为圆心，在施法范围圆内随机选择落点，而不是直接取敌方单位当前位置。
    /// </summary>
    private void Test_ParabolaShot_UsesRandomPointInCircleInsteadOfEnemyPosition()
    {
        var caster = new AbilityPipelineTargetPointTestEntity
        {
            Name = "ParabolaCaster",
            GlobalPosition = Vector2.Zero
        };
        AddChild(caster);

        var enemy = new AbilityPipelineTargetPointTestEntity
        {
            Name = "ParabolaEnemy",
            GlobalPosition = new Vector2(80f, 0f)
        };
        AddChild(enemy);

        caster.Data.Set(DataKey.Id, caster.GetInstanceId().ToString());
        caster.Data.Set(DataKey.Team, Team.Player);
        caster.Data.Set(DataKey.EntityType, EntityType.Unit);

        enemy.Data.Set(DataKey.Id, enemy.GetInstanceId().ToString());
        enemy.Data.Set(DataKey.Team, Team.Enemy);
        enemy.Data.Set(DataKey.EntityType, EntityType.Unit);

        EntityManager.Register(caster);
        EntityManager.Register(enemy);

        try
        {
            var executorType = typeof(AbilitySystemPipelineTest).Assembly.GetType("ParabolaBombardmentExecutor");
            var method = executorType?.GetMethod(
                "GetBombardTargetPoint",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
            );
            var result = method?.Invoke(null, new object[] { caster, caster, 120f });

            AssertEqual(
                "ParabolaShot 应能通过反射找到取落点逻辑",
                true, //期望存在该方法
                result is Vector2 //实际是否成功拿到结果
            );

            if (result is not Vector2 targetPoint) return;

            AssertEqual(
                "ParabolaShot 随机落点必须位于施法范围圆内",
                true, //期望在圆内
                GeometryCalculator.IsPointInCircle(targetPoint, caster.GlobalPosition, 120f) //实际判定
            );
            AssertEqual(
                "ParabolaShot 不应再直接使用敌方单位当前位置作为落点",
                true, //期望不是敌人位置
                !targetPoint.IsEqualApprox(enemy.GlobalPosition) //实际是否仍锁定敌人位置
            );
        }
        finally
        {
            EntityManager.Destroy(enemy);
            EntityManager.Destroy(caster);
        }
    }

    /// <summary>
    /// 回归测试：
    /// BoomerangThrow 应改为以施法者为圆心，在施法范围圆环内随机选择去程目标点，而不是直接取敌方单位当前位置。
    /// </summary>
    private void Test_BoomerangThrow_UsesRandomPointInRingInsteadOfEnemyPosition()
    {
        var caster = new AbilityPipelineTargetPointTestEntity
        {
            Name = "BoomerangCaster",
            GlobalPosition = Vector2.Zero
        };
        AddChild(caster);

        var enemy = new AbilityPipelineTargetPointTestEntity
        {
            Name = "BoomerangEnemy",
            GlobalPosition = new Vector2(80f, 0f)
        };
        AddChild(enemy);

        caster.Data.Set(DataKey.Id, caster.GetInstanceId().ToString());
        caster.Data.Set(DataKey.Team, Team.Player);
        caster.Data.Set(DataKey.EntityType, EntityType.Unit);

        enemy.Data.Set(DataKey.Id, enemy.GetInstanceId().ToString());
        enemy.Data.Set(DataKey.Team, Team.Enemy);
        enemy.Data.Set(DataKey.EntityType, EntityType.Unit);

        EntityManager.Register(caster);
        EntityManager.Register(enemy);

        try
        {
            var executorType = typeof(AbilitySystemPipelineTest).Assembly.GetType("BoomerangThrowExecutor");
            var method = executorType?.GetMethod(
                "GetThrowTarget",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
            );
            var result = method?.Invoke(null, new object[] { caster, caster, 200f });

            AssertEqual(
                "BoomerangThrow 应能通过反射找到取目标点逻辑",
                true, //期望存在该方法
                result is Vector2 //实际是否成功拿到结果
            );

            if (result is not Vector2 targetPoint) return;

            float distance = targetPoint.DistanceTo(caster.GlobalPosition); // 与施法者的距离
            AssertEqual(
                "BoomerangThrow 随机目标点必须位于施法范围圆环内",
                true, //期望在圆环内
                distance >= 90f && distance <= 200f //实际判定
            );
            AssertEqual(
                "BoomerangThrow 不应再直接使用敌方单位当前位置作为去程目标点",
                true, //期望不是敌人位置
                !targetPoint.IsEqualApprox(enemy.GlobalPosition) //实际是否仍锁定敌人位置
            );
        }
        finally
        {
            EntityManager.Destroy(enemy);
            EntityManager.Destroy(caster);
        }
    }

    private void AssertEqual<T>(string name, T expected, T actual)
    {
        if (Equals(expected, actual))
        {
            Pass($"{name} | expected={expected} actual={actual}");
            return;
        }

        Fail($"{name} | expected={expected} actual={actual}");
    }

    private void Pass(string message)
    {
        _passedCount++;
        _log.Success($"[PASS] {message}");
    }

    private void Fail(string message)
    {
        _failedCount++;
        _log.Error($"[FAIL] {message}");
    }
}

/// <summary>
/// 用于验证 AbilitySystem 不应强绑通用目标校验的测试 Handler。
/// </summary>
internal sealed class AbilitySystemPipelineTestHandler : AbilityFeatureHandler
{
    public const string HandlerId = "测试.技能.流水线.HandlerManagedEntityAbility";

    public static int ExecuteCount { get; set; }

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new AbilitySystemPipelineTestHandler());
    }

    public override string FeatureId => HandlerId;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        ExecuteCount++;
        return new AbilityExecutedResult
        {
            TargetsHit = 0
        };
    }
}

/// <summary>
/// 用于验证 Ability 领域工具迁出 AbilityFeatureHandler 后，Handler 直接读取 CastContext 工作。
/// </summary>
internal sealed class AbilityToolPipelineTestHandler : AbilityFeatureHandler
{
    public const string HandlerId = "测试.技能.流水线.AbilityToolBridgeAbility";

    public static int ExecuteCount { get; set; }
    public static float finalDamage { get; set; }
    public static string LastCasterName { get; set; } = string.Empty;

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new AbilityToolPipelineTestHandler());
    }

    public override string FeatureId => HandlerId;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;

        ExecuteCount++;
        finalDamage = ability.Data.Get<float>(DataKey.FinalAbilityDamage);
        LastCasterName = casterNode.Name.ToString();

        return new AbilityExecutedResult
        {
            TargetsHit = 1
        };
    }
}

/// <summary>
/// Node2D 版测试施法者，用于验证技能 Handler 本地节点判断逻辑。
/// </summary>
internal partial class AbilityToolPipelineTestCaster : Node2D, IEntity
{
    public EventBus Events { get; } = new EventBus();
    public Data Data { get; } = new();
}

/// <summary>
/// 抛炸弹落点测试实体。
/// </summary>
internal partial class AbilityPipelineTargetPointTestEntity : Node2D, IEntity
{
    public EventBus Events { get; } = new EventBus();
    public Data Data { get; } = new();
}
