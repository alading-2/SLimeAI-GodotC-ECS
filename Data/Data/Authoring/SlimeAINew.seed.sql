PRAGMA foreign_keys = ON;

INSERT OR REPLACE INTO capability_manifest(capability_id, enabled, dependencies)
VALUES
    ('Base', 1, ''),
    ('Attribute', 1, ''),
    ('Unit', 1, ''),
    ('AI', 1, ''),
    ('Ability', 1, ''),
    ('Feature', 1, ''),
    ('System', 1, ''),
    ('Resource', 1, ''),
    ('Movement', 1, ''),
    ('Test', 1, '');

INSERT OR REPLACE INTO unit_player(
    id, name, entity_type, death_type, visual_scene_path, health_bar_height,
    pickup_range, exp_reward, detection_range, collision_team, collision_layer, collision_mask,
    collision_radius, max_hp, base_hp_regen, life_steal, base_attack, base_attack_speed,
    attack_range, crit_rate, crit_damage, penetration, base_defense, damage_reduction,
    move_speed, dodge_chance, description
) VALUES
    ('player.deluyi', '德鲁伊', 'Unit', 'Hero', 'res://assets/Unit/Player/deluyi/AnimatedSprite2D/deluyi.tscn', 120,
     300, 0, 0, 1, 2, 24,
     0, 100, 1, 0, 10, 100,
     150, 5, 100, 0, 5, 0,
     200, 0, '');

INSERT OR REPLACE INTO unit_enemy(
    id, name, entity_type, death_type, visual_scene_path, health_bar_height,
    pickup_range, exp_reward, detection_range, collision_team, collision_layer, collision_mask,
    collision_radius, max_hp, base_hp_regen, life_steal, base_attack, base_attack_speed,
    attack_range, crit_rate, crit_damage, penetration, base_defense, damage_reduction,
    move_speed, dodge_chance,
    spawn_is_enabled, spawn_position_strategy, spawn_min_wave, spawn_max_wave, spawn_interval,
    spawn_max_count_per_wave, spawn_single_count, spawn_single_variance, spawn_start_delay, spawn_weight,
    description
) VALUES
    ('enemy.yuren', '鱼人', 'Unit', 'Normal', 'res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn', 0,
     0, 2, 500, 2, 4, 8,
     0, 150, 0, 0, 6, 100,
     200, 0, 100, 0, 1, 0,
     150, 0,
     1, 'Rectangle', 1, -1, 2.0,
     -1, 3, 1, 0, 10,
     ''),
    ('enemy.chailangren', '豺狼人', 'Unit', 'Normal', 'res://assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn', 155,
     0, 5, 500, 2, 4, 8,
     0, 100, 0, 0, 5, 100,
     200, 0, 100, 0, 3, 0,
     150, 0,
     1, 'Circle', 1, -1, 3.0,
     -1, 2, 0, 0, 10,
     '');

INSERT OR REPLACE INTO unit_targeting_indicator(
    id, name, entity_type, visual_scene_path, is_show_health_bar,
    collision_team, collision_layer, collision_mask, collision_radius, max_hp, is_invulnerable,
    move_speed, description
) VALUES
    ('unit.targeting_indicator', 'TargetingIndicator', 'TargetingIndicator', '', 0,
     0, 0, 0, 0, 1000000, 1,
     400, '');

INSERT OR REPLACE INTO ability(
    id, name, feature_group_id, feature_handler_id, description, icon_path,
    entity_type, ability_type, trigger_mode, cost_type, cost_amount, cooldown, damage,
    uses_charges, max_charges, charge_time, target_selection, cast_range, effect_radius,
    effect_scene_path, projectile_scene_path, chain_count, chain_range, chain_delay, chain_damage_decay
) VALUES
    ('ability.slam', '猛击', '技能.主动', '技能.主动.猛击', '在角色周围随机位置猛击地面，对范围内敌人造成物理伤害', 'res://icon.svg',
     'Ability', 'Active', 'Manual', 'Mana', 0, 1, 30,
     0, 0, 0, 'None', 500.0, 300.0,
     'res://assets/Effect/020/AnimatedSprite2D/020.tscn', '', NULL, NULL, NULL, NULL),
    ('ability.orbit_skill', '环绕技能', '技能.被动', '技能.被动.环绕技能', '生成多个投射物环绕玩家旋转，碰触敌人造成伤害（验证 Orbit 模式）', 'res://icon.svg',
     'Ability', 'Passive', 'Manual', 'None', 0, 1, 20,
     0, 0, 0, 'None', 0, 0,
     '', 'res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn', NULL, NULL, NULL, NULL),
    ('ability.sine_wave_shot', '正弦波射击', '技能.投射物', '技能.投射物.正弦波射击', '发射正弦波形弹道向敌人射击（验证 SineWave 模式）', 'res://icon.svg',
     'Ability', 'Active', 'Manual', 'None', 0, 1, 25,
     0, 0, 0, 'None', 600.0, 0,
     '', 'res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn', NULL, NULL, NULL, NULL),
    ('ability.parabola_shot', '定点抛炸弹', '技能.投射物', '技能.投射物.定点抛炸弹', '每隔一段时间向施法者周围随机落点抛出一枚炸弹，落地时造成范围伤害（固定终点 Parabola 模式）', 'res://icon.svg',
     'Ability', 'Active', 'Periodic', 'None', 0, 1, 9,
     0, 0, 0, 'None', 700.0, 250.0,
     'res://assets/Effect/004龙卷风/AnimatedSprite2D/004龙卷风.tscn', 'res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn', NULL, NULL, NULL, NULL),
    ('ability.boomerang_throw', '回旋镖投掷', '技能.投射物', '技能.投射物.回旋镖投掷', '投掷回旋镖，飞出后自动返回，来回命中敌人（验证 Boomerang 模式）', 'res://icon.svg',
     'Ability', 'Active', 'Manual', 'None', 0, 1, 22,
     0, 0, 0, 'None', 800.0, 0,
     '', 'res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn', NULL, NULL, NULL, NULL),
    ('ability.arc_shot', '圆弧射击', '技能.投射物', '技能.投射物.圆弧射击', '发射沿圆弧轨迹飞行的投射物（验证 CircularArc 模式）', 'res://icon.svg',
     'Ability', 'Active', 'Manual', 'None', 0, 1, 26,
     0, 0, 0, 'Entity', 700.0, 0,
     '', 'res://assets/Projectile/Projectile/Polygon2D/BoomerangChevron.tscn', NULL, NULL, NULL, NULL),
    ('ability.bezier_shot', '贝塞尔射击', '技能.投射物', '技能.投射物.贝塞尔射击', '发射沿二次贝塞尔曲线飞行的弓形弹（验证 BezierCurve 模式）', 'res://icon.svg',
     'Ability', 'Active', 'Manual', 'None', 0, 1, 30,
     0, 0, 0, 'None', 600.0, 0,
     '', 'res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn', NULL, NULL, NULL, NULL),
    ('ability.dash', '冲刺', '技能.位移', '技能.位移.冲刺', '高速冲向目标方向，瞬间位移躲避危险', 'res://icon.svg',
     'Ability', 'Active', 'Manual', 'None', 0, 1, 0,
     0, 0, 0, 'None', 300.0, 300.0,
     'res://assets/Effect/lrsc3/AnimatedSprite2D/lrsc3.tscn', '', NULL, NULL, NULL, NULL),
    ('ability.circle_damage', '圆环伤害', '技能.被动', '技能.被动.圆环伤害', '周身燃起烈焰光环，每秒对周围敌人造成魔法伤害', 'res://icon.svg',
     'Ability', 'Passive', 'Permanent', 'None', 0, 1, 10,
     0, 0, 0, 'None', 0, 500.0,
     'res://assets/Effect/003/AnimatedSprite2D/003.tscn', '', NULL, NULL, NULL, NULL),
    ('ability.chain_lightning', '闪电链', '技能.主动', '技能.主动.连锁闪电', '释放链式闪电，在多个敌人间弹跳造成魔法伤害，每次弹跳衰减', 'res://icon.svg',
     'Ability', 'Active', 'Manual', 'Mana', 0, 1, 50,
     0, 0, 0, 'Entity', 600.0, 0,
     '', 'res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn', 3, 300.0, 0.2, 100.0);

INSERT OR REPLACE INTO system_config(
    id, system_id, mount_group, tags, required, auto_load, start_enabled, priority,
    allowed_flow_states, required_overlays, blocked_overlays, allowed_simulation_states,
    dependencies, description
) VALUES
    ('system.object_pool_init', 'ObjectPoolInit', 'Base', 'Core,Runtime', 1, 1, 1, 0,
     '', '', '', '', '', '对象池初始化系统，负责预热常用对象池'),
    ('system.timer_manager', 'TimerManager', 'Base', 'Core,Runtime', 1, 1, 1, 1,
     '', '', '', '', '', '定时器管理系统，提供全局定时器服务'),
    ('system.project_state_bridge', 'ProjectStateBridge', 'Base', 'Core,Runtime', 1, 1, 1, 2,
     '', '', '', '', '', '项目状态桥接系统，监听全局事件并同步到 ProjectStateService'),
    ('system.entity_manager', 'EntityManager', 'Base', 'Core,Runtime', 1, 1, 1, 5,
     '', '', '', '', '', '实体管理器，负责实体的生成、注册、销毁和组件管理。'),
    ('system.damage_service', 'DamageService', 'Combat', 'Core,Combat,Runtime', 0, 1, 1, 10,
     'SessionPlaying', '', 'Blocking', 'Running', '', '伤害处理服务，负责伤害计算、暴击、闪避等核心战斗逻辑'),
    ('system.damage_statistics', 'DamageStatisticsSystem', 'Combat', 'Core,Combat,Runtime', 0, 1, 1, 11,
     'SessionPlaying', '', 'Blocking', 'Running', 'DamageService', '伤害统计系统，记录和分析战斗数据'),
    ('system.recovery', 'RecoverySystem', 'Combat', 'Core,Combat,Runtime', 0, 1, 1, 12,
     'SessionPlaying', '', 'Blocking', 'Running', '', '恢复系统，处理生命值和护盾恢复逻辑'),
    ('system.spawn', 'SpawnSystem', 'Gameplay', 'Gameplay,Runtime', 0, 1, 1, 13,
     'SessionPlaying', '', 'Blocking', 'Running', '', '生成系统，负责敌人和道具的生成逻辑'),
    ('system.targeting_manager', 'TargetingManagerRuntime', 'Combat', 'Core,Combat,Runtime', 0, 1, 1, 14,
     'SessionPlaying', '', 'Blocking', 'Running', '', '目标选择管理系统，提供目标查询和筛选服务'),
    ('system.pause_menu', 'PauseMenuSystem', 'UI', 'UI,Runtime', 0, 1, 1, 20,
     'SessionPlaying', '', '', 'Any', '', '暂停菜单系统，处理暂停菜单的显示和交互'),
    ('system.ui_manager', 'UIManager', 'UI', 'Core,UI,Runtime', 0, 1, 1, 21,
     '', '', '', '', '', 'UI 管理系统，负责 UI 的创建、显示和销毁'),
    ('system.damage_number_bridge', 'DamageNumberRuntimeBridge', 'UI', 'Combat,UI,Runtime', 0, 1, 1, 22,
     'SessionPlaying', '', 'Blocking', 'Running', '', '伤害数字 UI 桥接系统，监听伤害事件并显示伤害数字'),
    ('system.test', 'TestSystem', 'Test', 'Debug,Test', 0, 0, 1, 100,
     '', '', '', '', '', '测试系统，用于调试和监控系统运行状态'),
    ('system.mouse_selection', 'MouseSelectionSystem', 'Debug', 'Debug,Test', 0, 0, 1, 101,
     '', '', '', '', '', '鼠标选择系统，用于调试时选择和查看实体');

INSERT OR REPLACE INTO system_preset(
    id, preset_name, is_active, enabled_tags, enabled_system_ids, disabled_system_ids, description
) VALUES
    ('system.preset.default', 'Default', 1, 'Core,Gameplay,Combat,UI,Roguelike,Runtime', 'TestSystem,MouseSelectionSystem', '', '默认预设，加载核心、玩法、战斗、UI、运行时系统，并显式加载调试入口系统');

INSERT OR REPLACE INTO resource_entry(category, resource_key, resource_path, owner_capability, legacy_status, source_table, source_row_id, source_column)
VALUES
    ('AssetEffect', 'AssetEffect_003', 'res://assets/Effect/003/AnimatedSprite2D/003.tscn', 'shared', 'active', 'ability', 'ability.circle_damage', 'effect_scene_path'),
    ('AssetEffect', 'AssetEffect_004龙卷风', 'res://assets/Effect/004龙卷风/AnimatedSprite2D/004龙卷风.tscn', 'shared', 'active', 'ability', 'ability.parabola_shot', 'effect_scene_path'),
    ('AssetEffect', 'AssetEffect_020', 'res://assets/Effect/020/AnimatedSprite2D/020.tscn', 'shared', 'active', 'ability', 'ability.slam', 'effect_scene_path'),
    ('AssetEffect', 'AssetEffect_lrsc3', 'res://assets/Effect/lrsc3/AnimatedSprite2D/lrsc3.tscn', 'shared', 'active', 'ability', 'ability.dash', 'effect_scene_path'),
    ('AssetProjectile', 'AssetProjectile_ArrowNeedle', 'res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn', 'shared', 'active', 'ability', 'ability.sine_wave_shot', 'projectile_scene_path'),
    ('AssetProjectile', 'AssetProjectile_BoomerangChevron', 'res://assets/Projectile/Projectile/Polygon2D/BoomerangChevron.tscn', 'shared', 'active', 'ability', 'ability.arc_shot', 'projectile_scene_path'),
    ('AssetProjectile', 'AssetProjectile_BulletDiamond', 'res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn', 'shared', 'active', 'ability', 'ability.orbit_skill', 'projectile_scene_path'),
    ('AssetUnitEnemy', 'AssetUnitEnemy_chailangren', 'res://assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn', 'shared', 'active', 'unit_enemy', 'enemy.chailangren', 'visual_scene_path'),
    ('AssetUnitEnemy', 'AssetUnitEnemy_yuren', 'res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn', 'shared', 'active', 'unit_enemy', 'enemy.yuren', 'visual_scene_path'),
    ('AssetUnitPlayer', 'AssetUnitPlayer_deluyi', 'res://assets/Unit/Player/deluyi/AnimatedSprite2D/deluyi.tscn', 'shared', 'active', 'unit_player', 'player.deluyi', 'visual_scene_path');
