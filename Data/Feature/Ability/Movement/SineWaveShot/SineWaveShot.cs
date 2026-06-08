using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 正弦波弹技能执行器 - 验证 SineWave 运动模式
/// 优先朝最近敌方单位发射正弦波投射物，找不到敌人时退回随机方向演示弹道，沿途可碰撞多个敌人造成伤害
/// </summary>
internal class SineWaveShotExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(SineWaveShotExecutor));
    private const float DefaultFallbackRange = 600f;
    private const float FallbackMaxDistance = 1800f;

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new SineWaveShotExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.SineWaveShot;

    /// <summary>
    /// 执行正弦波弹技能：优先锁定敌方单位决定出射方向，再生成 1 枚正弦波投射物并挂接碰撞伤害。
    /// </summary>
    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;

        var damage = ability.Data.Get<float>(GeneratedDataKey.FinalAbilityDamage); // 最终技能伤害

        float castRange = ability.Data.Get<float>(GeneratedDataKey.AbilityCastRange);
        float effectiveRange = castRange > 0f ? castRange : DefaultFallbackRange; // 实际索敌范围
        var lockedTarget = FindNearestEnemy(caster, casterNode, effectiveRange); // 本次锁定目标，失败时允许随机兜底
        Vector2 directionPoint = lockedTarget?.Node.GlobalPosition ?? GetRandomDirectionPoint(casterNode); // 有目标时朝敌人，无目标时随机方向
        Vector2 rawDirection = directionPoint - casterNode.GlobalPosition; // 原始朝向向量
        Vector2 dir = rawDirection.LengthSquared() >= 0.001f
            ? rawDirection.Normalized()
            : Vector2.Right;
        Vector2 spawnPos = AbilityTool.ResolveSpawnPosition(
            casterNode.GlobalPosition, // 原始生成点
            dir, // 初始朝向
            20f // 出生前推距离
        );
        var projectileScenePath = ability.Data.Get<string>(GeneratedDataKey.ProjectileScene); // 投射物场景路径

        var projectile = ProjectileTool.Spawn(
            caster, // 投射物归属者
            spawnPos, // 生成位置
            projectileScenePath, // 投射物视觉路径
            "SineWaveShotProjectile" // 投射物名称
        );
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        bool hasAppliedDamage = false; // 防止碰撞命中后完成回调对同一锁定目标重复结算
        float maxDistance = ResolveMaxDistance(
            spawnPos,
            lockedTarget?.Node.GlobalPosition ?? directionPoint,
            lockedTarget.HasValue);

        projectile.Events.Emit(new GameEventType.Unit.MovementStarted(
                MoveMode.SineWave, // 移动模式：正弦波
                new MovementParams
                {
                    Mode = MoveMode.SineWave, // 移动模式
                    Angle = Mathf.RadToDeg(dir.Angle()), // 初始朝向角度（度）
                    ActionSpeed = 350f, // 前进速度
                    WaveAmplitude = lockedTarget.HasValue ? 24f : 60f, // 锁敌时降低横摆，避免从目标旁边擦过
                    WaveFrequency = 2f, // 波浪频率
                    MaxDistance = maxDistance, // 锁敌只负责定向，不截短穿透飞行距离
                    DestroyOnComplete = true, // 正常飞完后销毁
                    CollisionParams = new MovementCollisionParams
                    {
                        TeamFilter = TeamFilter.Enemy, // 阵营过滤：只接受敌方
                        EntityTypeFilter = EntityType.Unit, // 实体类型过滤：只接受单位
                        StopAfterCollisionCount = -1, // 只通知不停止
                        // DestroyOnStop = true, // 停止时同步销毁投射物
                        OnCollision = collisionCtx =>
                        {
                            if (ApplyCollisionDamage(collisionCtx, caster, casterNode, damage))
                            {
                                hasAppliedDamage = true;
                            }
                        } // 命中回调
                    },
                    OnStop = stopContext =>
                    {
                        if (hasAppliedDamage || lockedTarget == null) return;
                        if (stopContext.Reason != MovementStopReason.Completed) return;
                        hasAppliedDamage = ApplyDirectDamage(lockedTarget.Value.Entity, caster, casterNode, damage);
                    }, // 如果弹道未触发碰撞，完成时对本次锁定目标兜底结算一次
                    RotateToVelocity = true, // 朝速度方向旋转视觉
                }
            )
        );

        _log.Info($"正弦波弹: 采样点={directionPoint}, 方向={dir}, 角度={Mathf.RadToDeg(dir.Angle()):F1}°");
        return new AbilityExecutedResult { TargetsHit = 1 }; // 异步投射物：这里只表示成功生成 1 枚，不代表伤害命中上限
    }

    /// <summary>
    /// 在施法者周围圆环内随机采样一点，用作正弦波弹的初始朝向。
    /// </summary>
    private static Vector2 GetRandomDirectionPoint(
        Node2D casterNode) // 施法者节点
    {
        using var result = TargetQueryEngine.QueryPositions(new TargetSelectorQuery
        {
            Geometry = GeometryType.Ring, // 查询形状
            Origin = casterNode.GlobalPosition, // 查询中心
            InnerRange = 120f, // 随机方向最小半径
            Range = 260f, // 随机方向最大半径
            MaxTargets = 1 // 最大目标数
        });

        if (result.Items.Count > 0)
        {
            return result.Items[0];
        }

        return casterNode.GlobalPosition + Vector2.Right * 160f;
    }

    private static float ResolveMaxDistance(
        Vector2 spawnPosition, // 投射物生成点
        Vector2 directionPoint, // 锁定目标点或随机方向点
        bool hasLockedTarget) // 是否有锁定敌人
    {
        if (!hasLockedTarget)
        {
            return FallbackMaxDistance;
        }

        // 锁敌只决定朝向；最大距离保持为长弹道，避免最近敌人把无限碰撞命中截短成单目标。
        return Mathf.Max(FallbackMaxDistance, spawnPosition.DistanceTo(directionPoint));
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
            MaxTargets = 1 // 只需要一个方向目标
        });

        _log.Debug(
            $"锁敌查询: Range={effectiveRange}, {FormatDiagnostics(result.Diagnostics)}, Target={FormatTarget(result.Items.Count > 0 ? result.Items[0] : null)}");

        return result.Items.Count > 0 && result.Items[0] is Node2D targetNode
            ? (result.Items[0], targetNode)
            : null;
    }

    /// <summary>
    /// 正弦波弹每次有效碰撞时立即结算伤害；运动系统负责停下和销毁投射物。
    /// </summary>
    private static bool ApplyCollisionDamage(
        MovementCollisionContext collisionCtx, // 碰撞上下文
        IEntity caster, // 技能施法者
        Node2D casterNode, // 施法者节点
        float damage) // 最终伤害值
    {
        if (collisionCtx.TargetEntity == null) return false; // 没有实体目标时不结算伤害
        var targetEntity = collisionCtx.TargetEntity; // 命中的实体目标
        return ApplyDirectDamage(targetEntity, caster, casterNode, damage);
    }

    private static bool ApplyDirectDamage(
        IEntity targetEntity, // 目标实体
        IEntity caster, // 技能施法者
        Node2D casterNode, // 施法者节点
        float damage) // 最终伤害值
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

    private static bool CanApplyDamage(
        IEntity targetEntity, // 目标实体
        Node2D casterNode, // 技能施法者节点
        out string reason) // 拒绝原因
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
    // 正弦波技能只负责发射和每次有效碰撞伤害，停止与回收统一交给 Movement 系统处理。
}
