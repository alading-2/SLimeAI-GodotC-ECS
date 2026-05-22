#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 2 ]; then
    echo "usage: $0 <authoring.db> <output.json>" >&2
    exit 2
fi

db_path="$1"
output_path="$2"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
generated_at_utc="${DATAOS_GENERATED_AT_UTC:-$(date -u +%Y-%m-%dT%H:%M:%SZ)}"

if [ ! -f "$db_path" ]; then
    echo "DataOS db not found: $db_path" >&2
    exit 1
fi

"$repo_root/DataOS/Tools/validate-dataos.sh" "$db_path"
mkdir -p "$(dirname "$output_path")"

sqlite3 "$db_path" > "$output_path" <<SQL
PRAGMA foreign_keys = ON;

WITH
record_docs AS (
    SELECT json_object(
        'table', 'PlayerData',
        'id', id,
        'name', name,
        'data', json_object(
            'Name', name,
            'Team', 'Player',
            'EntityType', entity_type,
            'DeathType', death_type,
            'VisualScenePath', visual_scene_path,
            'HealthBarHeight', health_bar_height,
            'BaseHp', max_hp,
            'BaseHpRegen', base_hp_regen,
            'LifeSteal', life_steal,
            'BaseAttack', base_attack,
            'BaseAttackSpeed', base_attack_speed,
            'AttackRange', attack_range,
            'CritRate', crit_rate,
            'CritDamage', crit_damage,
            'Penetration', penetration,
            'BaseDefense', base_defense,
            'DamageReduction', damage_reduction,
            'MoveSpeed', move_speed,
            'DodgeChance', dodge_chance,
            'PickupRange', pickup_range
        )
    ) AS doc
    FROM unit_player
    UNION ALL
    SELECT json_object(
        'table', 'EnemyData',
        'id', id,
        'name', name,
        'data', json_object(
            'Name', name,
            'Team', 'Enemy',
            'EntityType', entity_type,
            'DeathType', death_type,
            'VisualScenePath', visual_scene_path,
            'HealthBarHeight', health_bar_height,
            'BaseHp', max_hp,
            'BaseHpRegen', base_hp_regen,
            'LifeSteal', life_steal,
            'BaseAttack', base_attack,
            'BaseAttackSpeed', base_attack_speed,
            'AttackRange', attack_range,
            'CritRate', crit_rate,
            'CritDamage', crit_damage,
            'Penetration', penetration,
            'BaseDefense', base_defense,
            'DamageReduction', damage_reduction,
            'MoveSpeed', move_speed,
            'DodgeChance', dodge_chance,
            'ExpReward', exp_reward,
            'DetectionRange', detection_range,
            'IsEnableSpawnRule', spawn_is_enabled,
            'SpawnStrategy', spawn_position_strategy,
            'SpawnMinWave', spawn_min_wave,
            'SpawnMaxWave', spawn_max_wave,
            'SpawnInterval', spawn_interval,
            'SpawnMaxCountPerWave', spawn_max_count_per_wave,
            'SingleSpawnCount', spawn_single_count,
            'SingleSpawnVariance', spawn_single_variance,
            'SpawnStartDelay', spawn_start_delay,
            'SpawnWeight', spawn_weight
        )
    ) AS doc
    FROM unit_enemy
    UNION ALL
    SELECT json_object(
        'table', 'TargetingIndicatorData',
        'id', id,
        'name', name,
        'data', json_object(
            'Name', name,
            'Team', 'Neutral',
            'EntityType', entity_type,
            'VisualScenePath', visual_scene_path,
            'IsShowHealthBar', is_show_health_bar,
            'BaseHp', max_hp,
            'IsInvulnerable', is_invulnerable,
            'MoveSpeed', move_speed,
            'BaseAttackSpeed', 0
        )
    ) AS doc
    FROM unit_targeting_indicator
    UNION ALL
    SELECT json_object(
        'table', CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END,
        'id', id,
        'name', name,
        'data', json_object(
            'Name', name,
            'FeatureGroupId', feature_group_id,
            'FeatureHandlerId', feature_handler_id,
            'Description', description,
            'AbilityIconPath', icon_path,
            'EntityType', entity_type,
            'AbilityType', ability_type,
            'AbilityTriggerMode', trigger_mode,
            'AbilityCostType', cost_type,
            'AbilityCostAmount', cost_amount,
            'AbilityCooldown', cooldown,
            'AbilityDamage', damage,
            'IsAbilityUsesCharges', uses_charges,
            'AbilityMaxCharges', max_charges,
            'AbilityChargeTime', charge_time,
            'AbilityTargetSelection', target_selection,
            'AbilityCastRange', cast_range,
            'AbilityEffectRadius', effect_radius,
            'EffectScenePath', effect_scene_path,
            'ProjectileScenePath', projectile_scene_path,
            'ChainCount', chain_count,
            'ChainRange', chain_range,
            'ChainDelay', chain_delay,
            'ChainDamageDecay', chain_damage_decay,
            'LineEffectScenePath', ''
        )
    ) AS doc
    FROM ability
    UNION ALL
    SELECT json_object(
        'table', 'SystemData',
        'id', id,
        'name', system_id,
        'data', json_object(
            'SystemId', system_id,
            'MountGroup', mount_group,
            'Tags', tags,
            'Required', required,
            'AutoLoad', auto_load,
            'StartEnabled', start_enabled,
            'Priority', priority,
            'AllowedFlowStates', allowed_flow_states,
            'RequiredOverlays', required_overlays,
            'BlockedOverlays', blocked_overlays,
            'AllowedSimulationStates', allowed_simulation_states,
            'Dependencies', dependencies,
            'Description', description
        )
    ) AS doc
    FROM system_config
    UNION ALL
    SELECT json_object(
        'table', 'SystemPresetData',
        'id', id,
        'name', preset_name,
        'data', json_object(
            'PresetName', preset_name,
            'IsActive', is_active,
            'EnabledTags', enabled_tags,
            'EnabledSystemIds', enabled_system_ids,
            'DisabledSystemIds', disabled_system_ids,
            'Description', description
        )
    ) AS doc
    FROM system_preset
),
resource_docs AS (
    SELECT json_object(
        'category', category,
        'key', resource_key,
        'path', resource_path,
        'ownerCapability', owner_capability,
        'legacyStatus', legacy_status,
        'sourceTable', source_table,
        'sourceRowId', source_row_id,
        'sourceColumn', source_column
    ) AS doc
    FROM resource_entry
    WHERE legacy_status <> 'intentionally-dropped'
    ORDER BY category, resource_key
)
SELECT json_object(
    'schemaVersion', 1,
    'generatedAtUtc', '$generated_at_utc',
    'source', 'DataOS/Authoring/slimeainew.authoring.db',
    'records', COALESCE((SELECT json_group_array(json(doc)) FROM record_docs), json('[]')),
    'resources', COALESCE((SELECT json_group_array(json(doc)) FROM resource_docs), json('[]'))
);
SQL

if command -v jq >/dev/null 2>&1; then
    tmp_path="${output_path}.tmp"
    jq . "$output_path" > "$tmp_path"
    mv "$tmp_path" "$output_path"
fi

echo "generated $output_path"
