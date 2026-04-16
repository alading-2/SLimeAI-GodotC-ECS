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
            Test_EntitySelectionAbility_AllowsHandlerManagedExecution();
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
        ability.Events.On<GameEventType.Ability.TryTriggerEventData>(
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
            new GameEventType.Ability.TryTriggerEventData(context) //触发上下文
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
