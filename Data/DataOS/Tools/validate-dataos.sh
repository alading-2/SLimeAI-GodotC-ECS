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

descriptor_rows="$(python3 - "$db_path" <<'PY'
import json
import sqlite3
import sys

db_path = sys.argv[1]
conn = sqlite3.connect(db_path)
conn.row_factory = sqlite3.Row
errors = []

value_types = {'string', 'string_array', 'int', 'float', 'double', 'bool', 'vector2', 'enum', 'modifier_list', 'object_ref'}
storage_policies = {'persisted', 'runtime_state', 'runtime_only', 'computed', 'authoring_blob'}
write_policies = {'read_write', 'loader_only', 'system_only', 'computed_readonly', 'debug_only'}
range_policies = {'none', 'validate', 'clamp_runtime', 'reject_runtime'}
modifier_policies = {'none', 'numeric', 'debug_only'}
migration_policies = {'default', 'never', 'always', 'profile_only'}
numeric_types = {'int', 'float', 'double'}

resolver_ids = {row['compute_id'] for row in conn.execute('SELECT compute_id FROM data_compute_resolver_manifest')}
descriptor_keys = {row['stable_key'] for row in conn.execute('SELECT stable_key FROM data_key_descriptor')}

def emit(row, field, code, detail):
    stable_key = row['stable_key'] if row['stable_key'] else '<empty>'
    errors.append(f'data_key_descriptor|{stable_key}|{field}|{code}|{detail}')

def parse_json(row, field, expected):
    raw = row[field]
    try:
        value = json.loads(raw)
    except json.JSONDecodeError as ex:
        emit(row, field, 'invalid_json', str(ex))
        return None
    if expected == 'array' and not isinstance(value, list):
        emit(row, field, 'json_not_array', raw)
        return None
    if expected == 'object' and not isinstance(value, dict):
        emit(row, field, 'json_not_object', raw)
        return None
    return value

def validate_default(row, value_type):
    text = row['default_value_text']
    try:
        if value_type == 'int':
            int(text)
        elif value_type in {'float', 'double'}:
            float(text)
        elif value_type == 'bool' and text.lower() not in {'true', 'false'}:
            raise ValueError(text)
    except ValueError:
        emit(row, 'default_value_text', 'invalid_default', f'{value_type}:{text}')

def parse_optional_float(row, field):
    raw = row[field]
    if raw is None or str(raw).strip() == '':
        return None
    try:
        return float(raw)
    except ValueError:
        emit(row, field, 'invalid_number', str(raw))
        return None

for row in conn.execute('SELECT * FROM data_key_descriptor ORDER BY stable_key'):
    stable_key = row['stable_key']
    if not stable_key or not stable_key.strip():
        emit(row, 'stable_key', 'empty_key', '')

    value_type = row['value_type']
    if value_type not in value_types:
        emit(row, 'value_type', 'invalid_value_type', value_type)
    else:
        validate_default(row, value_type)

    for field, allowed in (
        ('storage_policy', storage_policies),
        ('write_policy', write_policies),
        ('range_policy', range_policies),
        ('modifier_policy', modifier_policies),
        ('migration_policy', migration_policies),
    ):
        if row[field] not in allowed:
            emit(row, field, 'invalid_policy', row[field])

    min_value = parse_optional_float(row, 'min_value')
    max_value = parse_optional_float(row, 'max_value')
    if min_value is not None and max_value is not None and min_value > max_value:
        emit(row, 'min_value', 'range_min_gt_max', f'{min_value}>{max_value}')

    dependencies = parse_json(row, 'dependencies_json', 'array')
    compute_params = parse_json(row, 'compute_params_json', 'object')
    allowed_values = parse_json(row, 'allowed_values_json', 'array')

    if dependencies is not None:
        for dependency in dependencies:
            if not isinstance(dependency, str) or not dependency.strip():
                emit(row, 'dependencies_json', 'invalid_dependency', str(dependency))
            elif dependency not in descriptor_keys:
                emit(row, 'dependencies_json', 'unknown_dependency', dependency)

    if compute_params is not None:
        for key, value in compute_params.items():
            if not isinstance(key, str) or not isinstance(value, (str, int, float, bool)):
                emit(row, 'compute_params_json', 'invalid_compute_param', str(key))

    if allowed_values is not None:
        for value in allowed_values:
            if isinstance(value, str):
                continue
            if not isinstance(value, dict) or not str(value.get('value', '')).strip():
                emit(row, 'allowed_values_json', 'invalid_allowed_value', str(value))

    if row['storage_policy'] == 'computed' and not row['compute_id'].strip():
        emit(row, 'compute_id', 'missing_compute_id', '')
    if row['compute_id'].strip() and row['compute_id'] not in resolver_ids:
        emit(row, 'compute_id', 'missing_resolver', row['compute_id'])
    if row['modifier_policy'] == 'numeric' and value_type not in numeric_types:
        emit(row, 'modifier_policy', 'modifier_requires_numeric_type', value_type)

print('\n'.join(errors))
PY
)"
if [ -n "$descriptor_rows" ]; then
    echo "ERROR [descriptor]"
    echo "$descriptor_rows"
    errors=$((errors + 1))
fi

record_rows="$(python3 - "$db_path" <<'PY'
import sqlite3
import sys

db_path = sys.argv[1]
conn = sqlite3.connect(db_path)
conn.row_factory = sqlite3.Row
errors = []
descriptor_types = {
    row['stable_key']: row['value_type']
    for row in conn.execute('SELECT stable_key, value_type FROM data_key_descriptor')
}

def emit(row, field, code, detail):
    row_id = f"{row['table_id']}:{row['record_id']}:{row['field_key']}"
    errors.append(f"dataos_runtime_field_stream|{row_id}|{field}|{code}|{detail}")

for row in conn.execute('SELECT table_id, record_id, field_key, value_type, value_text FROM dataos_runtime_field_stream ORDER BY table_id, record_id, field_key'):
    descriptor_type = descriptor_types.get(row['field_key'])
    if descriptor_type is None:
        emit(row, 'field_key', 'unknown_key', row['field_key'])
        continue
    if row['value_type'] != descriptor_type:
        emit(row, 'value_type', 'type_mismatch', f"{row['value_type']}!={descriptor_type}")

print('\n'.join(errors))
PY
)"
if [ -n "$record_rows" ]; then
    echo "ERROR [record_descriptor]"
    echo "$record_rows"
    errors=$((errors + 1))
fi

if [ "$errors" -ne 0 ]; then
    echo "DataOS validation failed: $errors check(s)" >&2
    exit 1
fi

echo "DataOS validation passed: $db_path"
