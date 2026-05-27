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
profile="${DATAOS_PROFILE:-slimeainew}"
catalog_id="${DATAOS_CATALOG_ID:-$profile}"

if [ ! -f "$db_path" ]; then
    echo "DataOS db not found: $db_path" >&2
    exit 1
fi

"$repo_root/DataOS/Tools/validate-dataos.sh" "$db_path"
mkdir -p "$(dirname "$output_path")"

sqlite3 "$db_path" > "$output_path" <<SQL
PRAGMA foreign_keys = ON;

WITH
enabled_capabilities AS (
    SELECT capability_id
    FROM capability_manifest
    WHERE enabled = 1
    ORDER BY capability_id
),
field_rows AS (
    SELECT 'PlayerData' AS legacy_table, 'unit.player' AS table_id, id AS record_id, name AS display_name,
           'Name' AS field_key, 'string' AS value_type, name AS value_text, 'unit_player' AS source_table, id AS source_row_id, 'name' AS source_column
    FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'Team', 'enum', 'Player', 'unit_player', id, 'team' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'EntityType', 'enum', entity_type, 'unit_player', id, 'entity_type' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'DeathType', 'enum', death_type, 'unit_player', id, 'death_type' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'VisualScenePath', 'string', visual_scene_path, 'unit_player', id, 'visual_scene_path' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'HealthBarHeight', 'float', CAST(health_bar_height AS TEXT), 'unit_player', id, 'health_bar_height' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'BaseHp', 'float', CAST(max_hp AS TEXT), 'unit_player', id, 'max_hp' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'BaseHpRegen', 'float', CAST(base_hp_regen AS TEXT), 'unit_player', id, 'base_hp_regen' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'LifeSteal', 'float', CAST(life_steal AS TEXT), 'unit_player', id, 'life_steal' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'BaseAttack', 'float', CAST(base_attack AS TEXT), 'unit_player', id, 'base_attack' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'BaseAttackSpeed', 'float', CAST(base_attack_speed AS TEXT), 'unit_player', id, 'base_attack_speed' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'AttackRange', 'float', CAST(attack_range AS TEXT), 'unit_player', id, 'attack_range' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'CritRate', 'float', CAST(crit_rate AS TEXT), 'unit_player', id, 'crit_rate' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'CritDamage', 'float', CAST(crit_damage AS TEXT), 'unit_player', id, 'crit_damage' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'Penetration', 'float', CAST(penetration AS TEXT), 'unit_player', id, 'penetration' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'BaseDefense', 'float', CAST(base_defense AS TEXT), 'unit_player', id, 'base_defense' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'DamageReduction', 'float', CAST(damage_reduction AS TEXT), 'unit_player', id, 'damage_reduction' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'MoveSpeed', 'float', CAST(move_speed AS TEXT), 'unit_player', id, 'move_speed' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'DodgeChance', 'float', CAST(dodge_chance AS TEXT), 'unit_player', id, 'dodge_chance' FROM unit_player
    UNION ALL SELECT 'PlayerData', 'unit.player', id, name, 'PickupRange', 'float', CAST(pickup_range AS TEXT), 'unit_player', id, 'pickup_range' FROM unit_player

    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'Name', 'string', name, 'unit_enemy', id, 'name' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'Team', 'enum', 'Enemy', 'unit_enemy', id, 'team' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'EntityType', 'enum', entity_type, 'unit_enemy', id, 'entity_type' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'DeathType', 'enum', death_type, 'unit_enemy', id, 'death_type' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'VisualScenePath', 'string', visual_scene_path, 'unit_enemy', id, 'visual_scene_path' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'HealthBarHeight', 'float', CAST(health_bar_height AS TEXT), 'unit_enemy', id, 'health_bar_height' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'BaseHp', 'float', CAST(max_hp AS TEXT), 'unit_enemy', id, 'max_hp' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'BaseHpRegen', 'float', CAST(base_hp_regen AS TEXT), 'unit_enemy', id, 'base_hp_regen' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'LifeSteal', 'float', CAST(life_steal AS TEXT), 'unit_enemy', id, 'life_steal' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'BaseAttack', 'float', CAST(base_attack AS TEXT), 'unit_enemy', id, 'base_attack' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'BaseAttackSpeed', 'float', CAST(base_attack_speed AS TEXT), 'unit_enemy', id, 'base_attack_speed' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'AttackRange', 'float', CAST(attack_range AS TEXT), 'unit_enemy', id, 'attack_range' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'CritRate', 'float', CAST(crit_rate AS TEXT), 'unit_enemy', id, 'crit_rate' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'CritDamage', 'float', CAST(crit_damage AS TEXT), 'unit_enemy', id, 'crit_damage' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'Penetration', 'float', CAST(penetration AS TEXT), 'unit_enemy', id, 'penetration' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'BaseDefense', 'float', CAST(base_defense AS TEXT), 'unit_enemy', id, 'base_defense' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'DamageReduction', 'float', CAST(damage_reduction AS TEXT), 'unit_enemy', id, 'damage_reduction' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'MoveSpeed', 'float', CAST(move_speed AS TEXT), 'unit_enemy', id, 'move_speed' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'DodgeChance', 'float', CAST(dodge_chance AS TEXT), 'unit_enemy', id, 'dodge_chance' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'ExpReward', 'int', CAST(exp_reward AS TEXT), 'unit_enemy', id, 'exp_reward' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'DetectionRange', 'float', CAST(detection_range AS TEXT), 'unit_enemy', id, 'detection_range' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'IsEnableSpawnRule', 'bool', CASE spawn_is_enabled WHEN 1 THEN 'true' ELSE 'false' END, 'unit_enemy', id, 'spawn_is_enabled' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'SpawnStrategy', 'enum', spawn_position_strategy, 'unit_enemy', id, 'spawn_position_strategy' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'SpawnMinWave', 'int', CAST(spawn_min_wave AS TEXT), 'unit_enemy', id, 'spawn_min_wave' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'SpawnMaxWave', 'int', CAST(spawn_max_wave AS TEXT), 'unit_enemy', id, 'spawn_max_wave' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'SpawnInterval', 'float', CAST(spawn_interval AS TEXT), 'unit_enemy', id, 'spawn_interval' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'SpawnMaxCountPerWave', 'int', CAST(spawn_max_count_per_wave AS TEXT), 'unit_enemy', id, 'spawn_max_count_per_wave' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'SingleSpawnCount', 'int', CAST(spawn_single_count AS TEXT), 'unit_enemy', id, 'spawn_single_count' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'SingleSpawnVariance', 'int', CAST(spawn_single_variance AS TEXT), 'unit_enemy', id, 'spawn_single_variance' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'SpawnStartDelay', 'float', CAST(spawn_start_delay AS TEXT), 'unit_enemy', id, 'spawn_start_delay' FROM unit_enemy
    UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name, 'SpawnWeight', 'int', CAST(spawn_weight AS TEXT), 'unit_enemy', id, 'spawn_weight' FROM unit_enemy

    UNION ALL SELECT 'TargetingIndicatorData', 'unit.targeting_indicator', id, name, 'Name', 'string', name, 'unit_targeting_indicator', id, 'name' FROM unit_targeting_indicator
    UNION ALL SELECT 'TargetingIndicatorData', 'unit.targeting_indicator', id, name, 'Team', 'enum', 'Neutral', 'unit_targeting_indicator', id, 'team' FROM unit_targeting_indicator
    UNION ALL SELECT 'TargetingIndicatorData', 'unit.targeting_indicator', id, name, 'EntityType', 'enum', entity_type, 'unit_targeting_indicator', id, 'entity_type' FROM unit_targeting_indicator
    UNION ALL SELECT 'TargetingIndicatorData', 'unit.targeting_indicator', id, name, 'VisualScenePath', 'string', visual_scene_path, 'unit_targeting_indicator', id, 'visual_scene_path' FROM unit_targeting_indicator
    UNION ALL SELECT 'TargetingIndicatorData', 'unit.targeting_indicator', id, name, 'IsShowHealthBar', 'bool', CASE is_show_health_bar WHEN 1 THEN 'true' ELSE 'false' END, 'unit_targeting_indicator', id, 'is_show_health_bar' FROM unit_targeting_indicator
    UNION ALL SELECT 'TargetingIndicatorData', 'unit.targeting_indicator', id, name, 'BaseHp', 'float', CAST(max_hp AS TEXT), 'unit_targeting_indicator', id, 'max_hp' FROM unit_targeting_indicator
    UNION ALL SELECT 'TargetingIndicatorData', 'unit.targeting_indicator', id, name, 'IsInvulnerable', 'bool', CASE is_invulnerable WHEN 1 THEN 'true' ELSE 'false' END, 'unit_targeting_indicator', id, 'is_invulnerable' FROM unit_targeting_indicator
    UNION ALL SELECT 'TargetingIndicatorData', 'unit.targeting_indicator', id, name, 'MoveSpeed', 'float', CAST(move_speed AS TEXT), 'unit_targeting_indicator', id, 'move_speed' FROM unit_targeting_indicator
    UNION ALL SELECT 'TargetingIndicatorData', 'unit.targeting_indicator', id, name, 'BaseAttackSpeed', 'float', '0', 'unit_targeting_indicator', id, 'base_attack_speed' FROM unit_targeting_indicator

    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'Name', 'string', name, 'ability', id, 'name' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityFeatureGroup', 'string', feature_group_id, 'ability', id, 'feature_group_id' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'FeatureHandlerId', 'string', feature_handler_id, 'ability', id, 'feature_handler_id' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'Description', 'string', description, 'ability', id, 'description' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityIcon', 'string', icon_path, 'ability', id, 'icon_path' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'EntityType', 'enum', entity_type, 'ability', id, 'entity_type' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityType', 'enum', ability_type, 'ability', id, 'ability_type' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityTriggerMode', 'enum', trigger_mode, 'ability', id, 'trigger_mode' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityCostType', 'enum', cost_type, 'ability', id, 'cost_type' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityCostAmount', 'float', CAST(cost_amount AS TEXT), 'ability', id, 'cost_amount' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityCooldown', 'float', CAST(cooldown AS TEXT), 'ability', id, 'cooldown' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityDamage', 'float', CAST(damage AS TEXT), 'ability', id, 'damage' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'IsAbilityUsesCharges', 'bool', CASE uses_charges WHEN 1 THEN 'true' ELSE 'false' END, 'ability', id, 'uses_charges' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityMaxCharges', 'int', CAST(max_charges AS TEXT), 'ability', id, 'max_charges' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityChargeTime', 'float', CAST(charge_time AS TEXT), 'ability', id, 'charge_time' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityTargetSelection', 'enum', target_selection, 'ability', id, 'target_selection' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityCastRange', 'float', CAST(cast_range AS TEXT), 'ability', id, 'cast_range' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'AbilityEffectRadius', 'float', CAST(effect_radius AS TEXT), 'ability', id, 'effect_radius' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'EffectScene', 'string', COALESCE(effect_scene_path, ''), 'ability', id, 'effect_scene_path' FROM ability
    UNION ALL SELECT CASE WHEN chain_count IS NULL THEN 'AbilityData' ELSE 'ChainAbilityData' END, 'ability', id, name, 'ProjectileScene', 'string', COALESCE(projectile_scene_path, ''), 'ability', id, 'projectile_scene_path' FROM ability
    UNION ALL SELECT 'ChainAbilityData', 'ability', id, name, 'AbilityChainCount', 'int', CAST(chain_count AS TEXT), 'ability', id, 'chain_count' FROM ability WHERE chain_count IS NOT NULL
    UNION ALL SELECT 'ChainAbilityData', 'ability', id, name, 'AbilityChainRange', 'float', CAST(chain_range AS TEXT), 'ability', id, 'chain_range' FROM ability WHERE chain_range IS NOT NULL
    UNION ALL SELECT 'ChainAbilityData', 'ability', id, name, 'AbilityChainDelay', 'float', CAST(chain_delay AS TEXT), 'ability', id, 'chain_delay' FROM ability WHERE chain_delay IS NOT NULL
    UNION ALL SELECT 'ChainAbilityData', 'ability', id, name, 'AbilityChainDamageDecay', 'float', CAST(chain_damage_decay AS TEXT), 'ability', id, 'chain_damage_decay' FROM ability WHERE chain_damage_decay IS NOT NULL

    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'SystemId', 'string', system_id, 'system_config', id, 'system_id' FROM system_config
    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'MountGroup', 'string', mount_group, 'system_config', id, 'mount_group' FROM system_config
    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'Tags', 'string', tags, 'system_config', id, 'tags' FROM system_config
    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'Required', 'bool', CASE required WHEN 1 THEN 'true' ELSE 'false' END, 'system_config', id, 'required' FROM system_config
    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'AutoLoad', 'bool', CASE auto_load WHEN 1 THEN 'true' ELSE 'false' END, 'system_config', id, 'auto_load' FROM system_config
    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'StartEnabled', 'bool', CASE start_enabled WHEN 1 THEN 'true' ELSE 'false' END, 'system_config', id, 'start_enabled' FROM system_config
    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'Priority', 'int', CAST(priority AS TEXT), 'system_config', id, 'priority' FROM system_config
    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'AllowedFlowStates', 'string', allowed_flow_states, 'system_config', id, 'allowed_flow_states' FROM system_config
    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'RequiredOverlays', 'string', required_overlays, 'system_config', id, 'required_overlays' FROM system_config
    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'BlockedOverlays', 'string', blocked_overlays, 'system_config', id, 'blocked_overlays' FROM system_config
    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'AllowedSimulationStates', 'string', allowed_simulation_states, 'system_config', id, 'allowed_simulation_states' FROM system_config
    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'Dependencies', 'string_array', dependencies, 'system_config', id, 'dependencies' FROM system_config
    UNION ALL SELECT 'SystemData', 'system.config', id, system_id, 'Description', 'string', description, 'system_config', id, 'description' FROM system_config

    UNION ALL SELECT 'SystemPresetData', 'system.preset', id, preset_name, 'PresetName', 'string', preset_name, 'system_preset', id, 'preset_name' FROM system_preset
    UNION ALL SELECT 'SystemPresetData', 'system.preset', id, preset_name, 'IsActive', 'bool', CASE is_active WHEN 1 THEN 'true' ELSE 'false' END, 'system_preset', id, 'is_active' FROM system_preset
    UNION ALL SELECT 'SystemPresetData', 'system.preset', id, preset_name, 'EnabledTags', 'string', enabled_tags, 'system_preset', id, 'enabled_tags' FROM system_preset
    UNION ALL SELECT 'SystemPresetData', 'system.preset', id, preset_name, 'EnabledSystemIds', 'string_array', enabled_system_ids, 'system_preset', id, 'enabled_system_ids' FROM system_preset
    UNION ALL SELECT 'SystemPresetData', 'system.preset', id, preset_name, 'DisabledSystemIds', 'string_array', disabled_system_ids, 'system_preset', id, 'disabled_system_ids' FROM system_preset
    UNION ALL SELECT 'SystemPresetData', 'system.preset', id, preset_name, 'Description', 'string', description, 'system_preset', id, 'description' FROM system_preset
),
descriptor_source AS (
    SELECT
        field_key,
        CASE
            WHEN field_key IN ('Name', 'Description', 'Id', 'SourceEntityId', 'OriginEntityId') THEN 'Base'
            WHEN field_key IN ('Team', 'EntityType', 'DeathType', 'VisualScenePath', 'Level', 'UnitRank', 'IsShowHealthBar', 'IsEnableSpawnRule',
                'SpawnStrategy', 'SpawnMinWave', 'SpawnMaxWave', 'SpawnInterval', 'SpawnMaxCountPerWave', 'SingleSpawnCount',
                'SingleSpawnVariance', 'SpawnStartDelay', 'SpawnWeight', 'HealthBarHeight', 'ExpReward', 'IsDead', 'IsInvulnerable') THEN 'Unit'
            WHEN field_key IN ('DetectionRange') THEN 'AI'
            WHEN field_key IN ('BaseHp', 'BaseHpRegen', 'LifeSteal', 'BaseAttack', 'BaseAttackSpeed', 'AttackRange', 'CritRate',
                'CritDamage', 'Penetration', 'BaseDefense', 'DamageReduction', 'MoveSpeed', 'DodgeChance', 'PickupRange') THEN 'Attribute'
            WHEN field_key IN ('AbilityFeatureGroup', 'AbilityIcon', 'AbilityType', 'AbilityLevel', 'AbilityMaxLevel', 'AbilityDamage',
                'AbilityDamageBonus', 'CooldownReduction', 'AbilityCooldown', 'IsAbilityUsesCharges', 'AbilityMaxCharges',
                'AbilityChargeTime', 'AbilityCostType', 'AbilityCostAmount', 'AbilityTriggerMode', 'AbilityTargetSelection',
                'AbilityCastRange', 'AbilityEffectRadius', 'AbilityChainCount', 'AbilityChainRange', 'AbilityChainDelay',
                'AbilityChainDamageDecay', 'EffectScene', 'ProjectileScene') THEN 'Ability'
            WHEN field_key IN ('FeatureHandlerId') THEN 'Feature'
            WHEN field_key IN ('SystemId', 'MountGroup', 'Tags', 'Required', 'AutoLoad', 'StartEnabled', 'Priority',
                'AllowedFlowStates', 'RequiredOverlays', 'BlockedOverlays', 'AllowedSimulationStates', 'Dependencies',
                'PresetName', 'IsActive', 'EnabledTags', 'EnabledSystemIds', 'DisabledSystemIds') THEN 'System'
            ELSE 'shared'
        END AS owner_capability,
        value_type AS descriptor_type,
        CASE field_key
            WHEN 'Name' THEN ''
            WHEN 'Team' THEN 'Neutral'
            WHEN 'EntityType' THEN 'None'
            WHEN 'DeathType' THEN 'Normal'
            WHEN 'VisualScenePath' THEN ''
            WHEN 'HealthBarHeight' THEN '100'
            WHEN 'BaseHp' THEN '10'
            WHEN 'BaseHpRegen' THEN '0'
            WHEN 'LifeSteal' THEN '0'
            WHEN 'BaseAttack' THEN '0'
            WHEN 'BaseAttackSpeed' THEN '100'
            WHEN 'AttackRange' THEN '0'
            WHEN 'CritRate' THEN '0'
            WHEN 'CritDamage' THEN '100'
            WHEN 'Penetration' THEN '0'
            WHEN 'BaseDefense' THEN '0'
            WHEN 'DamageReduction' THEN '0'
            WHEN 'MoveSpeed' THEN '100'
            WHEN 'DodgeChance' THEN '0'
            WHEN 'PickupRange' THEN '300'
            WHEN 'ExpReward' THEN '1'
            WHEN 'DetectionRange' THEN '500'
            WHEN 'IsEnableSpawnRule' THEN 'true'
            WHEN 'SpawnStrategy' THEN 'Rectangle'
            WHEN 'SpawnMinWave' THEN '0'
            WHEN 'SpawnMaxWave' THEN '-1'
            WHEN 'SpawnInterval' THEN '1'
            WHEN 'SpawnMaxCountPerWave' THEN '-1'
            WHEN 'SingleSpawnCount' THEN '1'
            WHEN 'SingleSpawnVariance' THEN '0'
            WHEN 'SpawnStartDelay' THEN '0'
            WHEN 'SpawnWeight' THEN '10'
            WHEN 'IsShowHealthBar' THEN 'true'
            WHEN 'IsInvulnerable' THEN 'false'
            WHEN 'AbilityFeatureGroup' THEN ''
            WHEN 'FeatureHandlerId' THEN ''
            WHEN 'Description' THEN ''
            WHEN 'AbilityIcon' THEN ''
            WHEN 'AbilityType' THEN 'Passive'
            WHEN 'AbilityLevel' THEN '1'
            WHEN 'AbilityMaxLevel' THEN '10'
            WHEN 'AbilityTriggerMode' THEN 'None'
            WHEN 'AbilityCostType' THEN 'None'
            WHEN 'AbilityCostAmount' THEN '0'
            WHEN 'AbilityCooldown' THEN '0'
            WHEN 'AbilityDamage' THEN '0'
            WHEN 'IsAbilityUsesCharges' THEN 'false'
            WHEN 'AbilityMaxCharges' THEN '0'
            WHEN 'AbilityChargeTime' THEN '0'
            WHEN 'AbilityTargetSelection' THEN 'None'
            WHEN 'AbilityCastRange' THEN '0'
            WHEN 'AbilityEffectRadius' THEN '0'
            WHEN 'EffectScene' THEN ''
            WHEN 'ProjectileScene' THEN ''
            WHEN 'AbilityChainCount' THEN '3'
            WHEN 'AbilityChainRange' THEN '300'
            WHEN 'AbilityChainDelay' THEN '0.2'
            WHEN 'AbilityChainDamageDecay' THEN '100'
            WHEN 'SystemId' THEN ''
            WHEN 'MountGroup' THEN 'Else'
            WHEN 'Tags' THEN ''
            WHEN 'Required' THEN 'false'
            WHEN 'AutoLoad' THEN 'true'
            WHEN 'StartEnabled' THEN 'true'
            WHEN 'Priority' THEN '0'
            WHEN 'AllowedFlowStates' THEN ''
            WHEN 'RequiredOverlays' THEN ''
            WHEN 'BlockedOverlays' THEN ''
            WHEN 'AllowedSimulationStates' THEN ''
            WHEN 'Dependencies' THEN ''
            WHEN 'PresetName' THEN 'DefaultPreset'
            WHEN 'IsActive' THEN 'false'
            WHEN 'EnabledTags' THEN ''
            WHEN 'EnabledSystemIds' THEN ''
            WHEN 'DisabledSystemIds' THEN ''
            ELSE ''
        END AS default_value_text,
        field_key AS display_name
    FROM (
        SELECT field_key, value_type
        FROM field_rows
        GROUP BY field_key, value_type
    )
),
active_fields AS (
    SELECT f.*
    FROM field_rows f
    JOIN descriptor_source d ON d.field_key = f.field_key
    LEFT JOIN capability_manifest c ON c.capability_id = d.owner_capability
    WHERE COALESCE(c.enabled, 1) = 1
),
record_docs AS (
    SELECT
        r.legacy_table,
        r.table_id,
        r.record_id,
        json_object(
            'table', r.table_id,
            'legacyTable', r.legacy_table,
            'id', r.record_id,
            'name', COALESCE(NULLIF(MAX(r.display_name), ''), r.record_id),
            'fields', COALESCE((
                SELECT json_group_object(f.field_key, json(
                    CASE
                        WHEN f.value_type = 'int' THEN json_object('type', f.value_type, 'value', CAST(f.value_text AS INTEGER))
                        WHEN f.value_type = 'float' THEN json_object('type', f.value_type, 'value', CAST(f.value_text AS REAL))
                        WHEN f.value_type = 'bool' THEN json_object('type', f.value_type, 'value', json(CASE lower(f.value_text) WHEN 'true' THEN 'true' ELSE 'false' END))
                        ELSE json_object('type', f.value_type, 'value', COALESCE(f.value_text, ''))
                    END
                ))
                FROM active_fields f
                WHERE f.table_id = r.table_id
                  AND f.record_id = r.record_id
                ORDER BY f.field_key
            ), json('{}')),
            'legacyData', COALESCE((
                SELECT json_group_object(
                    CASE f.field_key
                        WHEN 'AbilityFeatureGroup' THEN 'FeatureGroupId'
                        WHEN 'AbilityIcon' THEN 'AbilityIconPath'
                        WHEN 'EffectScene' THEN 'EffectScenePath'
                        WHEN 'ProjectileScene' THEN 'ProjectileScenePath'
                        WHEN 'AbilityChainCount' THEN 'ChainCount'
                        WHEN 'AbilityChainRange' THEN 'ChainRange'
                        WHEN 'AbilityChainDelay' THEN 'ChainDelay'
                        WHEN 'AbilityChainDamageDecay' THEN 'ChainDamageDecay'
                        ELSE f.field_key
                    END,
                    json(
                        CASE
                            WHEN f.value_type = 'int' THEN CAST(f.value_text AS INTEGER)
                            WHEN f.value_type = 'float' THEN CAST(f.value_text AS REAL)
                            WHEN f.value_type = 'bool' THEN json(CASE lower(f.value_text) WHEN 'true' THEN 'true' ELSE 'false' END)
                            ELSE json_quote(COALESCE(f.value_text, ''))
                        END
                    )
                )
                FROM active_fields f
                WHERE f.table_id = r.table_id
                  AND f.record_id = r.record_id
                ORDER BY f.field_key
            ), json('{}'))
        ) AS doc
    FROM active_fields r
    GROUP BY r.legacy_table, r.table_id, r.record_id
    ORDER BY r.table_id, r.record_id
),
descriptor_docs AS (
    SELECT json_object(
        'key', d.field_key,
        'stableKey', d.field_key,
        'ownerCapability', d.owner_capability,
        'ownerSkill', lower(d.owner_capability) || '-system',
        'type', d.descriptor_type,
        'valueType', d.descriptor_type,
        'defaultValue', CASE
            WHEN d.descriptor_type = 'int' THEN CAST(d.default_value_text AS INTEGER)
            WHEN d.descriptor_type = 'float' THEN CAST(d.default_value_text AS REAL)
            WHEN d.descriptor_type = 'bool' THEN json(CASE lower(d.default_value_text) WHEN 'true' THEN 'true' ELSE 'false' END)
            ELSE d.default_value_text
        END,
        'displayName', d.display_name,
        'description', '',
        'iconPath', '',
        'category', d.owner_capability,
        'minValue', NULL,
        'maxValue', NULL,
        'options', json('[]'),
        'isPercentage', json('false'),
        'supportsModifiers', json(CASE
            WHEN d.field_key IN ('BaseHp', 'BaseHpRegen', 'LifeSteal', 'BaseAttack', 'BaseAttackSpeed', 'AttackRange',
                'CritRate', 'CritDamage', 'Penetration', 'BaseDefense', 'DamageReduction', 'MoveSpeed', 'DodgeChance',
                'PickupRange', 'AbilityDamage', 'AbilityDamageBonus', 'CooldownReduction', 'AbilityCooldown',
                'AbilityChainCount', 'AbilityChainRange', 'AbilityChainDamageDecay') THEN 'true'
            ELSE 'false'
        END),
        'isComputed', json('false')
    ) AS doc
    FROM descriptor_source d
    ORDER BY d.field_key
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
    WHERE legacy_status NOT IN ('intentionally-dropped', 'missing')
    ORDER BY category, resource_key
),
counts AS (
    SELECT
        (SELECT COUNT(*) FROM descriptor_docs) AS descriptor_count,
        (SELECT COUNT(*) FROM record_docs) AS record_count,
        (SELECT COUNT(*) FROM resource_docs) AS resource_count
)
SELECT json_object(
    'schemaVersion', 2,
    'generatedAtUtc', '$generated_at_utc',
    'manifest', json_object(
        'schemaVersion', 2,
        'generatedAtUtc', '$generated_at_utc',
        'profile', '$profile',
        'catalogId', '$catalog_id',
        'source', 'DataOS/Authoring/slimeainew.authoring.db',
        'enabledCapabilities', COALESCE((SELECT json_group_array(capability_id) FROM enabled_capabilities), json('[]')),
        'descriptorCount', (SELECT descriptor_count FROM counts),
        'recordCount', (SELECT record_count FROM counts),
        'resourceCount', (SELECT resource_count FROM counts),
        'validation', json_object('warningCount', 0, 'errorCount', 0)
    ),
    'descriptors', COALESCE((SELECT json_group_array(json(doc)) FROM descriptor_docs), json('[]')),
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
