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
            Test_FeatureContext_UsesTypedAbilityExecutionBoundary();
            Test_Dash_UsesLastMoveDirectionWhenVelocityIsZero();
            Test_ParabolaShot_UsesRandomPointInCircleInsteadOfEnemyPosition();
            Test_BoomerangThrow_PrefersEnemyDirectionAndFallsBackToRandomRing();
            Test_SineWaveShot_PrefersEnemyDirectionTarget();
            Test_BoomerangThrow_LockedTargetExtendsThroughEnemy();
            Test_SineWaveShot_LockedTargetKeepsPiercingDistance();
            Test_CostComponent_UsesTypedResourceKeysForAllCostTypes();
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
        owner.Data.Set(GeneratedDataKey.Id, ownerId);
        owner.Data.Set(GeneratedDataKey.Name, owner.Name.ToString());
        owner.Data.Set(GeneratedDataKey.CurrentMana, 50f);

        var abilityId = ability.GetInstanceId().ToString();
        ability.Data.Set(GeneratedDataKey.Id, abilityId);
        ability.Data.Set(GeneratedDataKey.Name, "HandlerManagedEntityAbility");
        ability.Data.Set(GeneratedDataKey.FeatureEnabled, true);
        ability.Data.Set(GeneratedDataKey.FeatureIsActive, false);
        ability.Data.Set(GeneratedDataKey.FeatureHandlerId, AbilitySystemPipelineTestHandler.HandlerId);
        ability.Data.Set(GeneratedDataKey.AbilityType, AbilityType.Active);
        ability.Data.Set(GeneratedDataKey.AbilityTriggerMode, AbilityTriggerMode.Manual);
        ability.Data.Set(GeneratedDataKey.AbilityTargetSelection, AbilityTargetSelection.Entity);
        ability.Data.Set(GeneratedDataKey.AbilityCostType, AbilityCostType.Mana);
        ability.Data.Set(GeneratedDataKey.AbilityCostAmount, 10f);
        ability.Data.Set(GeneratedDataKey.IsAbilityUsesCharges, false);

        EntityManager.Register(owner);
        EntityManager.Register(ability);
        AbilityInventoryService.Runtime.Attach(owner, ability);

        var costComponent = new CostComponent();
        EntityManager.AddComponent(ability, costComponent);
        ability.Events.On<GameEventType.Ability.TryTrigger>(
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
            owner.Data.Get<float>(GeneratedDataKey.CurrentMana) //实际法力
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
        owner.Data.Set(GeneratedDataKey.Id, ownerId);
        owner.Data.Set(GeneratedDataKey.Name, owner.Name.ToString());
        owner.Data.Set(GeneratedDataKey.CurrentMana, 30f);

        var abilityId = ability.GetInstanceId().ToString();
        ability.Data.Set(GeneratedDataKey.Id, abilityId);
        ability.Data.Set(GeneratedDataKey.Name, "AbilityToolBridgeAbility");
        ability.Data.Set(GeneratedDataKey.FeatureEnabled, true);
        ability.Data.Set(GeneratedDataKey.FeatureIsActive, false);
        ability.Data.Set(GeneratedDataKey.FeatureHandlerId, AbilityToolPipelineTestHandler.HandlerId);
        ability.Data.Set(GeneratedDataKey.AbilityType, AbilityType.Active);
        ability.Data.Set(GeneratedDataKey.AbilityTriggerMode, AbilityTriggerMode.Manual);
        ability.Data.Set(GeneratedDataKey.AbilityTargetSelection, AbilityTargetSelection.None);
        ability.Data.Set(GeneratedDataKey.AbilityCostType, AbilityCostType.Mana);
        ability.Data.Set(GeneratedDataKey.AbilityCostAmount, 5f);
        ability.Data.Set(GeneratedDataKey.IsAbilityUsesCharges, false);
        ability.Data.Set(GeneratedDataKey.AbilityDamage, 20f);
        ability.Data.Set(GeneratedDataKey.AbilityDamageBonus, 50f);

        EntityManager.Register(owner);
        EntityManager.Register(ability);
        AbilityInventoryService.Runtime.Attach(owner, ability);

        var costComponent = new CostComponent();
        EntityManager.AddComponent(ability, costComponent);
        ability.Events.On<GameEventType.Ability.TryTrigger>(
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
    /// Ability 接入 FeatureSystem 时，执行输入和输出必须走 typed helper，
    /// 不再依赖 raw object 返回值或调用方手动 cast。
    /// </summary>
    private void Test_FeatureContext_UsesTypedAbilityExecutionBoundary()
    {
        var context = new CastContext();
        var featureContext = new FeatureContext();

        featureContext.SetActivationPayload(context);

        AssertEqual(
            "FeatureContext 应能 typed 读取 Ability CastContext",
            true,
            featureContext.TryGetActivation<CastContext>(out var typedContext)
        );
        AssertEqual(
            "FeatureContext typed activation 应保持原始引用",
            context,
            typedContext
        );

        var result = new AbilityExecutedResult
        {
            TargetsHit = 3
        };
        featureContext.SetExecutionResult(result);

        AssertEqual(
            "FeatureContext 应能 typed 读取 AbilityExecutedResult",
            true,
            featureContext.TryGetExecutionResult<AbilityExecutedResult>(out var typedResult)
        );
        AssertEqual(
            "FeatureContext typed result 应保持原始引用",
            result,
            typedResult
        );
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

        caster.Data.Set(GeneratedDataKey.Id, caster.GetInstanceId().ToString());
        caster.Data.Set(GeneratedDataKey.Velocity, Vector2.Zero);
        caster.Data.Set(GeneratedDataKey.LastMoveDirection, Vector2.Left);

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

        caster.Data.Set(GeneratedDataKey.Id, caster.GetInstanceId().ToString());
        caster.Data.Set(GeneratedDataKey.Team, Team.Player);
        caster.Data.Set(GeneratedDataKey.EntityType, EntityType.Unit);

        enemy.Data.Set(GeneratedDataKey.Id, enemy.GetInstanceId().ToString());
        enemy.Data.Set(GeneratedDataKey.Team, Team.Enemy);
        enemy.Data.Set(GeneratedDataKey.EntityType, EntityType.Unit);

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
                Geometry2D.IsPointInCircle(targetPoint, caster.GlobalPosition, 120f) //实际判定
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
    /// BoomerangThrow 是伤害技能，有敌人时必须优先使用最近敌人的方向作为去程方向；
    /// 只有找不到敌方单位时，才退回随机圆环点来保留 Boomerang 运动模式演示语义。
    /// </summary>
    private void Test_BoomerangThrow_PrefersEnemyDirectionAndFallsBackToRandomRing()
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

        caster.Data.Set(GeneratedDataKey.Id, caster.GetInstanceId().ToString());
        caster.Data.Set(GeneratedDataKey.Team, Team.Player);
        caster.Data.Set(GeneratedDataKey.EntityType, EntityType.Unit);

        enemy.Data.Set(GeneratedDataKey.Id, enemy.GetInstanceId().ToString());
        enemy.Data.Set(GeneratedDataKey.Team, Team.Enemy);
        enemy.Data.Set(GeneratedDataKey.EntityType, EntityType.Unit);

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

            AssertEqual(
                "BoomerangThrow 有敌人时应优先沿最近敌人方向投掷",
                true, //期望锁敌定向
                targetPoint.Normalized().IsEqualApprox(enemy.GlobalPosition.Normalized()) //实际是否沿敌人方向
            );
            AssertEqual(
                "BoomerangThrow 锁敌时不应把最近敌人位置作为唯一终点",
                true, //期望延伸到更远端
                targetPoint.DistanceTo(caster.GlobalPosition) > enemy.GlobalPosition.DistanceTo(caster.GlobalPosition) //实际是否截短
            );

            enemy.Data.Set(GeneratedDataKey.Team, Team.Player); // 变成友方，验证无敌方目标时走随机兜底

            result = method?.Invoke(null, new object[] { caster, caster, 200f });
            if (result is not Vector2 fallbackPoint) return;

            AssertEqual(
                "BoomerangThrow 无敌人时随机目标点必须位于施法范围圆环内",
                true, //期望在圆环内
                fallbackPoint.DistanceTo(caster.GlobalPosition) >= 90f
                    && fallbackPoint.DistanceTo(caster.GlobalPosition) <= 200f //实际判定
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
    /// SineWaveShot 是伤害技能，有敌人时必须解析最近敌方单位作为方向目标，避免随机方向导致释放成功但无伤害。
    /// </summary>
    private void Test_SineWaveShot_PrefersEnemyDirectionTarget()
    {
        var caster = new AbilityPipelineTargetPointTestEntity
        {
            Name = "SineWaveCaster",
            GlobalPosition = Vector2.Zero
        };
        AddChild(caster);

        var enemy = new AbilityPipelineTargetPointTestEntity
        {
            Name = "SineWaveEnemy",
            GlobalPosition = new Vector2(120f, 0f)
        };
        AddChild(enemy);

        caster.Data.Set(GeneratedDataKey.Id, caster.GetInstanceId().ToString());
        caster.Data.Set(GeneratedDataKey.Team, Team.Player);
        caster.Data.Set(GeneratedDataKey.EntityType, EntityType.Unit);

        enemy.Data.Set(GeneratedDataKey.Id, enemy.GetInstanceId().ToString());
        enemy.Data.Set(GeneratedDataKey.Team, Team.Enemy);
        enemy.Data.Set(GeneratedDataKey.EntityType, EntityType.Unit);

        EntityManager.Register(caster);
        EntityManager.Register(enemy);

        try
        {
            var executorType = typeof(AbilitySystemPipelineTest).Assembly.GetType("SineWaveShotExecutor");
            var method = executorType?.GetMethod(
                "FindNearestEnemy",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
            );
            var result = method?.Invoke(null, new object[] { caster, caster, 200f });
            var targetNode = ExtractTupleNode(result);

            AssertEqual(
                "SineWaveShot 应能通过反射找到最近敌方目标",
                true, //期望存在敌方目标
                targetNode != null //实际是否找到
            );

            AssertEqual(
                "SineWaveShot 有敌人时应优先使用最近敌人作为方向目标",
                enemy.GetInstanceId(), //期望敌人节点
                targetNode?.GetInstanceId() ?? 0 //实际锁定节点
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
    /// BoomerangThrow 的锁敌只用于决定投掷方向，不能把最近敌人位置当作唯一终点；
    /// 否则回旋镖会过早返程，沿途无限碰撞命中语义被实际截短成近似单目标。
    /// </summary>
    private void Test_BoomerangThrow_LockedTargetExtendsThroughEnemy()
    {
        var casterPosition = Vector2.Zero;
        var lockedEnemyPosition = new Vector2(80f, 0f);

        var executorType = typeof(AbilitySystemPipelineTest).Assembly.GetType("BoomerangThrowExecutor");
        var method = executorType?.GetMethod(
            "ResolveThrowTargetPoint",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
        );
        var result = method?.Invoke(null, new object[] { casterPosition, lockedEnemyPosition, 200f });

        AssertEqual(
            "BoomerangThrow 应能通过反射找到远端去程点解析逻辑",
            true, //期望存在该方法
            result is Vector2 //实际是否成功拿到结果
        );

        if (result is not Vector2 throwTarget) return;

        AssertEqual(
            "BoomerangThrow 锁敌时去程点应穿过敌人延伸到施法范围远端",
            true, //期望远端点超过最近敌人
            throwTarget.DistanceTo(casterPosition) > lockedEnemyPosition.DistanceTo(casterPosition) //实际是否被截断
        );
        AssertEqual(
            "BoomerangThrow 锁敌时去程点仍应落在施法范围内",
            true, //期望不超过范围
            throwTarget.DistanceTo(casterPosition) <= 200f //实际是否超出范围
        );
    }

    /// <summary>
    /// 回归测试：
    /// SineWaveShot 锁敌只负责朝向，不能把 MaxDistance 收缩到最近敌人距离；
    /// 否则第一目标附近完成运动，后续目标没有机会被 MovementCollision 命中。
    /// </summary>
    private void Test_SineWaveShot_LockedTargetKeepsPiercingDistance()
    {
        var spawnPosition = new Vector2(20f, 0f);
        var lockedEnemyPosition = new Vector2(120f, 0f);

        var executorType = typeof(AbilitySystemPipelineTest).Assembly.GetType("SineWaveShotExecutor");
        var method = executorType?.GetMethod(
            "ResolveMaxDistance",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
        );
        var result = method?.Invoke(null, new object[] { spawnPosition, lockedEnemyPosition, true });

        AssertEqual(
            "SineWaveShot 应能通过反射找到最大飞行距离解析逻辑",
            true, //期望存在该方法
            result is float //实际是否成功拿到结果
        );

        if (result is not float maxDistance) return;

        AssertEqual(
            "SineWaveShot 锁敌时仍应保持远距离穿透飞行",
            true, //期望不是最近敌人距离
            maxDistance > spawnPosition.DistanceTo(lockedEnemyPosition) //实际是否被截断
        );
    }

    private static Node2D? ExtractTupleNode(object? tupleResult)
    {
        if (tupleResult == null) return null;

        // 反射拿 ValueTuple<IEntity, Node2D> 的第二项，避免测试公开 handler 私有 API。
        return tupleResult.GetType().GetField("Item2")?.GetValue(tupleResult) as Node2D;
    }

    /// <summary>
    /// 回归测试：
    /// CostComponent 应通过 generated DataKey 处理所有资源消耗，避免回到裸字符串资源键。
    /// </summary>
    private void Test_CostComponent_UsesTypedResourceKeysForAllCostTypes()
    {
        AssertCostConsumption(AbilityCostType.Mana, GeneratedDataKey.CurrentMana);
        AssertCostConsumption(AbilityCostType.Health, GeneratedDataKey.CurrentHp);
        AssertCostConsumption(AbilityCostType.Energy, GeneratedDataKey.CurrentEnergy);
        AssertCostConsumption(AbilityCostType.Ammo, GeneratedDataKey.CurrentAmmo);
    }

    private void AssertCostConsumption(AbilityCostType costType, DataKey<float> resourceKey)
    {
        var owner = new TestEntity
        {
            Name = $"CostOwner_{costType}"
        };
        AddChild(owner);

        var ability = new AbilityEntity
        {
            Name = $"CostAbility_{costType}"
        };
        AddChild(ability);

        owner.Data.Set(GeneratedDataKey.Id, owner.GetInstanceId().ToString());
        owner.Data.Set(GeneratedDataKey.Name, owner.Name.ToString());
        owner.Data.Set(resourceKey, 50f);

        ability.Data.Set(GeneratedDataKey.Id, ability.GetInstanceId().ToString());
        ability.Data.Set(GeneratedDataKey.Name, ability.Name.ToString());
        ability.Data.Set(GeneratedDataKey.AbilityCostType, costType);
        ability.Data.Set(GeneratedDataKey.AbilityCostAmount, 12f);

        EntityManager.Register(owner);
        EntityManager.Register(ability);
        AbilityInventoryService.Runtime.Attach(owner, ability);

        var component = new CostComponent();
        EntityManager.AddComponent(ability, component);

        var checkContext = new EventContext();
        ability.Events.Emit(new GameEventType.Ability.CheckCanUse(checkContext));
        AssertEqual($"{costType} typed cost check should pass", true, checkContext.Success);

        var consumeContext = new EventContext();
        ability.Events.Emit(new GameEventType.Ability.ConsumeCost(consumeContext));
        AssertEqual($"{costType} typed cost consume should pass", true, consumeContext.Success);
        AssertEqual($"{costType} typed resource should be deducted", 38f, owner.Data.Get(resourceKey));

        EntityManager.Destroy(ability);
        EntityManager.Destroy(owner);
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
        finalDamage = ability.Data.Get<float>(GeneratedDataKey.FinalAbilityDamage);
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
