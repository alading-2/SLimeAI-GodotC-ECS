PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS unit_player (
    id TEXT PRIMARY KEY CHECK (trim(id) <> ''),
    name TEXT NOT NULL CHECK (trim(name) <> ''),
    entity_type TEXT NOT NULL DEFAULT 'Ability',
    death_type TEXT NOT NULL DEFAULT 'Hero',
    visual_scene_path TEXT NOT NULL DEFAULT '',
    health_bar_height REAL NOT NULL DEFAULT 0,
    is_show_health_bar INTEGER CHECK (
        is_show_health_bar IN (0, 1)
        OR is_show_health_bar IS NULL
    ),
    pickup_range REAL NOT NULL DEFAULT 0,
    exp_reward INTEGER NOT NULL DEFAULT 0,
    detection_range REAL NOT NULL DEFAULT 0,
    collision_team INTEGER NOT NULL DEFAULT 0,
    collision_layer INTEGER NOT NULL DEFAULT 0,
    collision_mask INTEGER NOT NULL DEFAULT 0,
    collision_radius REAL NOT NULL DEFAULT 0,
    max_hp REAL NOT NULL DEFAULT 0,
    base_hp_regen REAL NOT NULL DEFAULT 0,
    life_steal REAL NOT NULL DEFAULT 0,
    base_attack REAL NOT NULL DEFAULT 0,
    base_attack_speed REAL NOT NULL DEFAULT 0,
    attack_range REAL NOT NULL DEFAULT 0,
    crit_rate REAL NOT NULL DEFAULT 0,
    crit_damage REAL NOT NULL DEFAULT 100,
    penetration REAL NOT NULL DEFAULT 0,
    base_defense REAL NOT NULL DEFAULT 0,
    damage_reduction REAL NOT NULL DEFAULT 0,
    move_speed REAL NOT NULL DEFAULT 0,
    dodge_chance REAL NOT NULL DEFAULT 0,
    description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS unit_enemy (
    id TEXT PRIMARY KEY CHECK (trim(id) <> ''),
    name TEXT NOT NULL CHECK (trim(name) <> ''),
    entity_type TEXT NOT NULL DEFAULT 'Unit',
    death_type TEXT NOT NULL DEFAULT 'Normal',
    visual_scene_path TEXT NOT NULL DEFAULT '',
    health_bar_height REAL NOT NULL DEFAULT 0,
    is_show_health_bar INTEGER CHECK (
        is_show_health_bar IN (0, 1)
        OR is_show_health_bar IS NULL
    ),
    pickup_range REAL NOT NULL DEFAULT 0,
    exp_reward INTEGER NOT NULL DEFAULT 0,
    detection_range REAL NOT NULL DEFAULT 0,
    collision_team INTEGER NOT NULL DEFAULT 0,
    collision_layer INTEGER NOT NULL DEFAULT 0,
    collision_mask INTEGER NOT NULL DEFAULT 0,
    collision_radius REAL NOT NULL DEFAULT 0,
    max_hp REAL NOT NULL DEFAULT 0,
    base_hp_regen REAL NOT NULL DEFAULT 0,
    life_steal REAL NOT NULL DEFAULT 0,
    base_attack REAL NOT NULL DEFAULT 0,
    base_attack_speed REAL NOT NULL DEFAULT 0,
    attack_range REAL NOT NULL DEFAULT 0,
    crit_rate REAL NOT NULL DEFAULT 0,
    crit_damage REAL NOT NULL DEFAULT 100,
    penetration REAL NOT NULL DEFAULT 0,
    base_defense REAL NOT NULL DEFAULT 0,
    damage_reduction REAL NOT NULL DEFAULT 0,
    move_speed REAL NOT NULL DEFAULT 0,
    dodge_chance REAL NOT NULL DEFAULT 0,
    spawn_is_enabled INTEGER CHECK (
        spawn_is_enabled IN (0, 1)
        OR spawn_is_enabled IS NULL
    ),
    spawn_position_strategy TEXT,
    spawn_min_wave INTEGER,
    spawn_max_wave INTEGER,
    spawn_interval REAL,
    spawn_max_count_per_wave INTEGER,
    spawn_single_count INTEGER,
    spawn_single_variance INTEGER,
    spawn_start_delay REAL,
    spawn_weight INTEGER,
    description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS unit_targeting_indicator (
    id TEXT PRIMARY KEY CHECK (trim(id) <> ''),
    name TEXT NOT NULL CHECK (trim(name) <> ''),
    entity_type TEXT NOT NULL DEFAULT 'TargetingIndicator',
    visual_scene_path TEXT NOT NULL DEFAULT '',
    is_show_health_bar INTEGER CHECK (
        is_show_health_bar IN (0, 1)
        OR is_show_health_bar IS NULL
    ),
    collision_team INTEGER NOT NULL DEFAULT 0,
    collision_layer INTEGER NOT NULL DEFAULT 0,
    collision_mask INTEGER NOT NULL DEFAULT 0,
    collision_radius REAL NOT NULL DEFAULT 0,
    max_hp REAL NOT NULL DEFAULT 0,
    is_invulnerable INTEGER CHECK (
        is_invulnerable IN (0, 1)
        OR is_invulnerable IS NULL
    ),
    move_speed REAL NOT NULL DEFAULT 0,
    description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS ability (
    id TEXT PRIMARY KEY CHECK (trim(id) <> ''),
    name TEXT NOT NULL CHECK (trim(name) <> ''),
    feature_group_id TEXT NOT NULL DEFAULT '',
    feature_handler_id TEXT NOT NULL CHECK (
        trim(feature_handler_id) <> ''
    ),
    description TEXT NOT NULL DEFAULT '',
    icon_path TEXT NOT NULL DEFAULT '',
    entity_type TEXT NOT NULL DEFAULT 'Ability',
    ability_type TEXT NOT NULL DEFAULT 'Active',
    trigger_mode TEXT NOT NULL DEFAULT 'Manual',
    cost_type TEXT NOT NULL DEFAULT 'None',
    cost_amount REAL NOT NULL DEFAULT 0,
    cooldown REAL NOT NULL DEFAULT 0,
    damage REAL NOT NULL DEFAULT 0,
    uses_charges INTEGER CHECK (
        uses_charges IN (0, 1)
        OR uses_charges IS NULL
    ),
    max_charges INTEGER,
    charge_time REAL,
    target_selection TEXT,
    cast_range REAL,
    effect_radius REAL,
    effect_scene_path TEXT,
    projectile_scene_path TEXT,
    chain_count INTEGER,
    chain_range REAL,
    chain_delay REAL,
    chain_damage_decay REAL
);

CREATE TABLE IF NOT EXISTS system_config (
    id TEXT PRIMARY KEY CHECK (trim(id) <> ''),
    system_id TEXT NOT NULL CHECK (trim(system_id) <> ''),
    mount_group TEXT NOT NULL DEFAULT 'Else',
    tags TEXT NOT NULL DEFAULT '',
    required INTEGER CHECK (
        required IN (0, 1)
        OR required IS NULL
    ),
    auto_load INTEGER CHECK (
        auto_load IN (0, 1)
        OR auto_load IS NULL
    ),
    start_enabled INTEGER CHECK (
        start_enabled IN (0, 1)
        OR start_enabled IS NULL
    ),
    priority INTEGER NOT NULL DEFAULT 0,
    allowed_flow_states TEXT NOT NULL DEFAULT '',
    required_overlays TEXT NOT NULL DEFAULT '',
    blocked_overlays TEXT NOT NULL DEFAULT '',
    allowed_simulation_states TEXT NOT NULL DEFAULT '',
    dependencies TEXT NOT NULL DEFAULT '',
    description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS system_preset (
    id TEXT PRIMARY KEY CHECK (trim(id) <> ''),
    preset_name TEXT NOT NULL CHECK (trim(preset_name) <> ''),
    is_active INTEGER CHECK (
        is_active IN (0, 1)
        OR is_active IS NULL
    ),
    enabled_tags TEXT NOT NULL DEFAULT '',
    enabled_system_ids TEXT NOT NULL DEFAULT '',
    disabled_system_ids TEXT NOT NULL DEFAULT '',
    description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS resource_entry (
    category TEXT NOT NULL,
    resource_key TEXT PRIMARY KEY CHECK (trim(resource_key) <> ''),
    resource_path TEXT NOT NULL CHECK (trim(resource_path) <> ''),
    owner_capability TEXT NOT NULL DEFAULT 'shared',
    legacy_status TEXT NOT NULL DEFAULT 'active',
    source_table TEXT NOT NULL DEFAULT '',
    source_row_id TEXT NOT NULL DEFAULT '',
    source_column TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS data_key_descriptor (
    stable_key TEXT PRIMARY KEY CHECK (trim(stable_key) <> ''),
    owner_domain TEXT NOT NULL DEFAULT 'runtime',
    owner_capability TEXT NOT NULL DEFAULT 'shared',
    owner_skill TEXT NOT NULL DEFAULT '',
    value_type TEXT NOT NULL DEFAULT '',
    runtime_type_id TEXT NOT NULL DEFAULT '',
    default_value_text TEXT NOT NULL DEFAULT '',
    storage_policy TEXT NOT NULL DEFAULT 'persisted' CHECK (
        storage_policy IN (
            'persisted',
            'runtime_state',
            'runtime_only',
            'computed',
            'authoring_blob'
        )
    ),
    write_policy TEXT NOT NULL DEFAULT 'read_write' CHECK (
        write_policy IN (
            'read_write',
            'loader_only',
            'system_only',
            'computed_readonly',
            'debug_only'
        )
    ),
    range_policy TEXT NOT NULL DEFAULT 'none' CHECK (
        range_policy IN (
            'none',
            'validate',
            'clamp_runtime',
            'reject_runtime'
        )
    ),
    modifier_policy TEXT NOT NULL DEFAULT 'none' CHECK (
        modifier_policy IN (
            'none',
            'numeric',
            'debug_only'
        )
    ),
    migration_policy TEXT NOT NULL DEFAULT 'default' CHECK (
        migration_policy IN (
            'default',
            'never',
            'always',
            'profile_only'
        )
    ),
    compute_id TEXT NOT NULL DEFAULT '',
    dependencies_json TEXT NOT NULL DEFAULT '[]' CHECK (json_valid(dependencies_json)),
    compute_params_json TEXT NOT NULL DEFAULT '{}' CHECK (
        json_valid(compute_params_json)
    ),
    allowed_values_json TEXT NOT NULL DEFAULT '[]' CHECK (
        json_valid(allowed_values_json)
    ),
    display_name TEXT NOT NULL DEFAULT '',
    description TEXT NOT NULL DEFAULT '',
    ui_group TEXT NOT NULL DEFAULT '',
    reset_group TEXT NOT NULL DEFAULT '',
    unit TEXT NOT NULL DEFAULT '',
    format TEXT NOT NULL DEFAULT '',
    icon_path TEXT NOT NULL DEFAULT '',
    category TEXT NOT NULL DEFAULT '',
    min_value TEXT,
    max_value TEXT,
    options_json TEXT NOT NULL DEFAULT '[]',
    is_percentage INTEGER NOT NULL DEFAULT 0,
    supports_modifiers INTEGER NOT NULL DEFAULT 0,
    is_computed INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS data_compute_resolver_manifest (
    compute_id TEXT PRIMARY KEY CHECK (trim(compute_id) <> ''),
    owner_capability TEXT NOT NULL DEFAULT 'shared',
    description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS capability_manifest (
    capability_id TEXT PRIMARY KEY CHECK (trim(capability_id) <> ''),
    enabled INTEGER NOT NULL DEFAULT 1,
    dependencies TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS data_table (
    table_id TEXT NOT NULL,
    record_id TEXT NOT NULL,
    display_name TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (table_id, record_id)
);

CREATE TABLE IF NOT EXISTS dataos_runtime_field_stream (
    table_id TEXT NOT NULL,
    record_id TEXT NOT NULL,
    display_name TEXT NOT NULL DEFAULT '',
    field_key TEXT NOT NULL,
    value_type TEXT NOT NULL,
    value_text TEXT NOT NULL DEFAULT '',
    source_table TEXT NOT NULL DEFAULT '',
    source_row_id TEXT NOT NULL DEFAULT '',
    source_column TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (table_id, record_id) REFERENCES data_table (table_id, record_id) ON DELETE CASCADE
);