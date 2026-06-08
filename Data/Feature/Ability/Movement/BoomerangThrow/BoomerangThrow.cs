using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 回旋镖技能执行器 - 验证 Boomerang 运动模式
/// 优先用最近敌方单位决定去程方向，找不到敌人时退回随机圆环点，过程中可碰撞多个敌人造成伤害
/// </summary>
internal class BoomerangThrowExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(BoomerangThrowExecutor));
    private const float DefaultFallbackRange = 800f;
    private const float InnerRingRatio = 0.45f;
    private const float FallbackForwardDistance = 320f;

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new BoomerangThrowExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.BoomerangThrow;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;

        var damage = ability.Data.Get<float>(GeneratedDataKey.FinalAbilityDamage); // 最终技能伤害

        float castRange = ability.Data.Get<float>(GeneratedDataKey.AbilityCastRange);
        float effectiveRange = castRange > 0f ? castRange : DefaultFallbackRange; // 实际索敌/选点范围
        var lockedTarget = FindNearestEnemy(caster, casterNode, effectiveRange); // 本次锁定目标，失败时允许随机兜底
        var throwTarget = lockedTarget.HasValue
            ? ResolveThrowTargetPoint(casterNode.GlobalPosition, lockedTarget.Value.Node.GlobalPosition, effectiveRange)
            : GetRandomThrowTarget(casterNode, effectiveRange);
        var projectileScenePath = ability.Data.Get<string>(GeneratedDataKey.ProjectileScene); // 投射物场景路径

        var projectile = ProjectileTool.Spawn(
            caster, // 投射物归属者
            casterNode.GlobalPosition, // 生成位置
            projectileScenePath, // 投射物视觉路径
            "BoomerangThrowProjectile" // 投射物名称
        );
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        bool hasAppliedDamage = false; // 防止碰撞命中后完成回调对同一锁定目标重复结算
        projectile.Events.Emit(
            new GameEventType.Unit.MovementStarted(
                MoveMode.Boomerang,
                new MovementParams
                {
                    Mode = MoveMode.Boomerang,
                    TargetPoint = throwTarget, // 去程目标点：有敌人时沿敌人方向延伸到远端，无敌人时使用随机点
                    TargetNode = casterNode, // 显式指定返程宿主，避免策略退回到不可靠的祖先回溯
                    ActionSpeed = 460f, // 去程基础速度
                    BoomerangArcHeight = 160f, // 更大的轨迹弧高
                    BoomerangPauseTime = 0.05f, // 顶点轻微停顿
                    BoomerangIsClockwise = true, // 去程默认顺时针鼓包
                    BoomerangReturnSpeedMultiplier = 1.35f, // 回程更快，强调收回感
                    DestroyOnComplete = true, // 完成后回收
                    CollisionParams = new MovementCollisionParams
                    {
                        TeamFilter = TeamFilter.Enemy, //阵营过滤
                        EntityTypeFilter = EntityType.Unit, //实体类型过滤
                        StopAfterCollisionCount = -1, //只通知不停止
                        DestroyOnStop = false, //不因碰撞销毁
                        OnCollision = collisionCtx =>
                        {
                            if (ApplyCollisionDamage(collisionCtx, caster, casterNode, damage))
                            {
                                hasAppliedDamage = true;
                            }
                        } //命中回调
                    },
                    OnStop = stopContext =>
                    {
                        if (hasAppliedDamage || lockedTarget == null) return;
                        if (stopContext.Reason != MovementStopReason.Completed) return;
                        hasAppliedDamage = ApplyDirectDamage(lockedTarget.Value.Entity, caster, casterNode, damage);
                    }, // 如果弹道未触发碰撞，完成时对本次锁定目标兜底结算一次
                    RotateToVelocity = false, // root 最终旋转改由朝向组件接管
                    Orientation = new OrientationParams
                    {
                        Mode = OrientationMode.FollowMovementAndSpin, // 跟随轨迹切线并叠加自转
                        AngularSpeed = 540f, // 自转角速度
                        AngularAcceleration = 0f, // 匀速自转
                        TotalAngle = -1f, // 不限制总自转角
                        InitialAngle = 0f, // 初始偏移角
                        IsClockwise = true, // 顺时针自转
                    },
                }
            )
        );

        _log.Info($"回旋镖投掷: 目标={throwTarget}");
        return new AbilityExecutedResult { TargetsHit = 1 }; // 异步投射物：这里只表示成功生成 1 枚，不代表伤害命中上限
    }

    private static Vector2 GetThrowTarget(IEntity caster, Node2D casterNode, float castRange)
    {
        float effectiveRange = castRange > 0f ? castRange : DefaultFallbackRange; // 外半径

        var targetInfo = FindNearestEnemy(caster, casterNode, effectiveRange);
        if (targetInfo.HasValue)
        {
            return ResolveThrowTargetPoint(casterNode.GlobalPosition, targetInfo.Value.Node.GlobalPosition, effectiveRange);
        }

        return GetRandomThrowTarget(casterNode, effectiveRange);
    }

    private static Vector2 ResolveThrowTargetPoint(
        Vector2 casterPosition,
        Vector2 lockedTargetPosition,
        float effectiveRange)
    {
        float range = effectiveRange > 0f ? effectiveRange : DefaultFallbackRange; // 目标方向上的远端距离
        Vector2 direction = lockedTargetPosition - casterPosition;
        if (direction.LengthSquared() < 0.001f)
        {
            return casterPosition + Vector2.Right * range;
        }

        // 锁敌只负责决定投掷方向，不把最近敌人当成唯一终点，避免截短无限碰撞命中。
        return casterPosition + direction.Normalized() * range;
    }

    private static Vector2 GetRandomThrowTarget(Node2D casterNode, float effectiveRange)
    {
        float innerRange = effectiveRange * InnerRingRatio; // 内半径，避免目标点离自己过近
        using var result = TargetQueryEngine.QueryPositions(new TargetSelectorQuery
        {
            Geometry = GeometryType.Ring, //圆环
            Origin = casterNode.GlobalPosition, //查询中心
            InnerRange = innerRange, //内半径
            Range = effectiveRange, //外半径
            MaxTargets = 1 //最大目标数
        });
        if (result.Items.Count > 0)
            return result.Items[0];
        return casterNode.GlobalPosition + Vector2.Right * FallbackForwardDistance;
    }

    private static (IEntity Entity, Node2D Node)? FindNearestEnemy(IEntity caster, Node2D casterNode, float effectiveRange)
    {
        using var result = TargetQueryEngine.QueryEntities(new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle, // 搜索施法范围内敌人
            Origin = casterNode.GlobalPosition, // 以施法者为圆心
            Range = effectiveRange, // 搜索半径
            CenterEntity = caster, // 阵营判断基准
            TeamFilter = TeamFilter.Enemy, // 只选敌方
            TypeFilter = EntityType.Unit, // 只选单位
            Sorting = TargetSorting.Nearest, // 优先最近敌人，降低投射物空放概率
            MaxTargets = 1 // 只需要一个去程目标
        });

        _log.Debug(
            $"锁敌查询: Range={effectiveRange}, {FormatDiagnostics(result.Diagnostics)}, Target={FormatTarget(result.Items.Count > 0 ? result.Items[0] : null)}");

        return result.Items.Count > 0 && result.Items[0] is Node2D targetNode
            ? (result.Items[0], targetNode)
            : null;
    }

    private static bool ApplyCollisionDamage(MovementCollisionContext collisionCtx,
        IEntity caster,
        Node2D casterNode,
        float damage)
    {
        if (collisionCtx.TargetEntity == null) return false;
        var targetEntity = collisionCtx.TargetEntity;
        return ApplyDirectDamage(targetEntity, caster, casterNode, damage);
    }

    private static bool ApplyDirectDamage(
        IEntity targetEntity,
        IEntity caster,
        Node2D casterNode,
        float damage)
    {
        if (!CanApplyDamage(targetEntity, casterNode, out var blockedReason))
        {
            _log.Debug($"伤害跳过: Target={FormatTarget(targetEntity)}, Reason={blockedReason}");
            return false;
        }

        if (!AbilityTool.MatchesTeamFilter(caster, targetEntity, TeamFilter.Enemy))
        {
            _log.Debug($"伤害跳过: Target={FormatTarget(targetEntity)}, Reason=TeamFilter");
            return false;
        }

        string beforeHp = FormatHp(targetEntity);
        var result = AbilityImpactTool.Execute(caster, new AbilityImpactOptions
        {
            Targets = new[] { targetEntity },
            Damage = new DamageApplyOptions
            {
                Damage = damage, // 伤害值
                Type = DamageType.Physical, // 伤害类型
                Tags = DamageTags.Ability | DamageTags.Ranged, // 伤害标签
                Attacker = casterNode // 伤害来源
            }
        });
        string afterHp = FormatHp(targetEntity);
        bool applied = result.TargetsHit > 0;
        _log.Debug(
            $"伤害结算: Target={FormatTarget(targetEntity)}, Damage={damage}, Applied={applied}, HitCount={result.TargetsHit}, Hp={beforeHp}->{afterHp}");
        return applied;
    }

    private static bool CanApplyDamage(IEntity targetEntity, Node2D casterNode, out string reason)
    {
        reason = string.Empty;
        if (!GodotObject.IsInstanceValid(casterNode) || casterNode.IsQueuedForDeletion())
        {
            reason = "CasterInvalid";
            return false;
        }

        if (targetEntity is Node targetNode
            && (!GodotObject.IsInstanceValid(targetNode) || targetNode.IsQueuedForDeletion()))
        {
            reason = "TargetInvalid";
            return false;
        }

        // 完成兜底是延迟回调，目标可能在弹道飞行期间死亡或被销毁。
        bool canApply = !targetEntity.Data.Has(GeneratedDataKey.IsDead)
            || !targetEntity.Data.Get<bool>(GeneratedDataKey.IsDead);
        if (!canApply)
        {
            reason = "TargetDead";
        }

        return canApply;
    }

    private static string FormatDiagnostics(TargetQueryDiagnostics diagnostics)
    {
        return $"Candidates={diagnostics.CandidateCount}, Geometry={diagnostics.GeometryHitCount}, TeamFiltered={diagnostics.FilteredByTeamCount}, TypeFiltered={diagnostics.FilteredByTypeCount}, LifecycleFiltered={diagnostics.FilteredByLifecycleCount}, Returned={diagnostics.ReturnedCount}, Truncated={diagnostics.Truncated}";
    }

    private static string FormatTarget(IEntity? entity)
    {
        return entity is Node node
            ? node.Name.ToString()
            : entity?.Data.Get<string>(GeneratedDataKey.Name) ?? "None";
    }

    private static string FormatHp(IEntity entity)
    {
        return entity.Data.Has(GeneratedDataKey.CurrentHp)
            ? entity.Data.Get<float>(GeneratedDataKey.CurrentHp).ToString("F1")
            : "n/a";
    }
}
