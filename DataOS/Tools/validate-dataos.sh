#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 1 ]; then
    echo "usage: $0 <authoring.db>" >&2
    exit 2
fi

db_path="$1"
if [ ! -f "$db_path" ]; then
    echo "DataOS db not found: $db_path" >&2
    exit 1
fi

errors=0

run_check() {
    local check_id="$1"
    local sql="$2"
    local rows
    rows="$(sqlite3 -batch -noheader "$db_path" "$sql")"
    if [ -n "$rows" ]; then
        echo "ERROR [$check_id]"
        echo "$rows"
        errors=$((errors + 1))
    fi
}

run_check "foreign_key" "PRAGMA foreign_key_check;"

run_check "duplicate_name" "
SELECT 'unit_player:' || name || ':' || group_concat(id) FROM unit_player GROUP BY name HAVING COUNT(*) > 1
UNION ALL SELECT 'unit_enemy:' || name || ':' || group_concat(id) FROM unit_enemy GROUP BY name HAVING COUNT(*) > 1
UNION ALL SELECT 'unit_targeting_indicator:' || name || ':' || group_concat(id) FROM unit_targeting_indicator GROUP BY name HAVING COUNT(*) > 1
UNION ALL SELECT 'ability:' || name || ':' || group_concat(id) FROM ability GROUP BY name HAVING COUNT(*) > 1
UNION ALL SELECT 'system_config:' || system_id || ':' || group_concat(id) FROM system_config GROUP BY system_id HAVING COUNT(*) > 1
UNION ALL SELECT 'system_preset:' || preset_name || ':' || group_concat(id) FROM system_preset GROUP BY preset_name HAVING COUNT(*) > 1;"

run_check "required_res_path" "
SELECT 'unit_player.visual_scene_path:' || id || ':' || COALESCE(visual_scene_path, '') FROM unit_player WHERE visual_scene_path NOT LIKE 'res://%'
UNION ALL SELECT 'unit_enemy.visual_scene_path:' || id || ':' || COALESCE(visual_scene_path, '') FROM unit_enemy WHERE visual_scene_path NOT LIKE 'res://%'
UNION ALL SELECT 'ability.icon_path:' || id || ':' || COALESCE(icon_path, '') FROM ability WHERE trim(COALESCE(icon_path, '')) <> '' AND icon_path NOT LIKE 'res://%'
UNION ALL SELECT 'ability.effect_scene_path:' || id || ':' || COALESCE(effect_scene_path, '') FROM ability WHERE trim(COALESCE(effect_scene_path, '')) <> '' AND effect_scene_path NOT LIKE 'res://%'
UNION ALL SELECT 'ability.projectile_scene_path:' || id || ':' || COALESCE(projectile_scene_path, '') FROM ability WHERE trim(COALESCE(projectile_scene_path, '')) <> '' AND projectile_scene_path NOT LIKE 'res://%'
UNION ALL SELECT 'resource_entry.resource_path:' || resource_key || ':' || COALESCE(resource_path, '') FROM resource_entry WHERE legacy_status = 'active' AND resource_path NOT LIKE 'res://%';"

run_check "bool_values" "
SELECT 'unit_player.is_show_health_bar:' || id FROM unit_player WHERE is_show_health_bar IS NOT NULL AND is_show_health_bar NOT IN (0, 1)
UNION ALL SELECT 'unit_enemy.is_show_health_bar:' || id FROM unit_enemy WHERE is_show_health_bar IS NOT NULL AND is_show_health_bar NOT IN (0, 1)
UNION ALL SELECT 'unit_enemy.spawn_is_enabled:' || id FROM unit_enemy WHERE spawn_is_enabled IS NOT NULL AND spawn_is_enabled NOT IN (0, 1)
UNION ALL SELECT 'unit_targeting_indicator.is_show_health_bar:' || id FROM unit_targeting_indicator WHERE is_show_health_bar IS NOT NULL AND is_show_health_bar NOT IN (0, 1)
UNION ALL SELECT 'unit_targeting_indicator.is_invulnerable:' || id FROM unit_targeting_indicator WHERE is_invulnerable IS NOT NULL AND is_invulnerable NOT IN (0, 1)
UNION ALL SELECT 'ability.uses_charges:' || id FROM ability WHERE uses_charges IS NOT NULL AND uses_charges NOT IN (0, 1)
UNION ALL SELECT 'system_config.required:' || id FROM system_config WHERE required IS NOT NULL AND required NOT IN (0, 1)
UNION ALL SELECT 'system_config.auto_load:' || id FROM system_config WHERE auto_load IS NOT NULL AND auto_load NOT IN (0, 1)
UNION ALL SELECT 'system_config.start_enabled:' || id FROM system_config WHERE start_enabled IS NOT NULL AND start_enabled NOT IN (0, 1)
UNION ALL SELECT 'system_preset.is_active:' || id FROM system_preset WHERE is_active IS NOT NULL AND is_active NOT IN (0, 1);"

run_check "ability_handler" "
SELECT id || ':' || name FROM ability WHERE trim(COALESCE(feature_handler_id, '')) = '';"

run_check "active_preset" "
SELECT 'active_count=' || COUNT(*) FROM system_preset WHERE is_active = 1 HAVING COUNT(*) <> 1;"

run_check "target_point_skill_removed" "
SELECT id || ':' || name FROM ability WHERE name = '位置目标' OR id = 'ability.target_point_skill';"

if [ "$errors" -ne 0 ]; then
    echo "DataOS validation failed: $errors check(s)" >&2
    exit 1
fi

echo "DataOS validation passed: $db_path"
