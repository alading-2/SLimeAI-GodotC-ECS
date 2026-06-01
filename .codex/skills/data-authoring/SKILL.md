---
name: data-authoring
description: 修改 SlimeAI DataOS schema、migration、snapshot generator、validator、DataKey authoring 映射或游戏 seed 数据时使用。
---

# DataOS / Authoring 入口

## 必读入口

- `DocsAI/DataOS/Overview.md`
- `DocsAI/README.md`
- `DocsAI/ECS/Runtime/Data/Data系统说明.md`
- `DocsAI/ECS/Capabilities/Ability/README.md`
- `DocsAI/ECS/Capabilities/Feature/README.md`
- `DocsAI/ECS/Capabilities/Unit/README.md`
- `/home/slime/Code/SlimeAI/Games/BrotatoLike/DocsAI/GameProjectState.md`

## 源码位置

- `DataOS/Schema/`
- `DataOS/Migrations/`
- `DataOS/Generators/`
- `DataOS/Validation/`
- 旧 ECS PRJ-0002 DataOS: `/home/slime/Code/SlimeAI/SlimeAI/Data/DataOS/`
- `Src/ECS/Runtime/Data/`
- `Data/DataKey/`
- `Src/ECS/Capabilities/*/Events/`
- `Src/ECS/Capabilities/*/`
- `/home/slime/Code/SlimeAI/Games/BrotatoLike/DataOS/Authoring/BrotatoLike.seed.sql`

## 规则

- Authoring 数据写 SQLite seed / migration，运行时消费 generated snapshot。
- DataOS authoring 首选清晰业务表，不把万能 `data_field` / EAV 行作为 Unit、Ability、Feature、System、Spawn 等业务内容的主入口。
- 新业务内容应写入 `unit_player`、`unit_enemy`、`unit_targeting_indicator`、`ability`、`ability_effect`、`ability_projectile`、`ability_movement_*`、`feature_definition`、`feature_modifier`、`system_config`、`system_preset`、`spawn_config` 等业务表，再由 generator 投影到 Runtime snapshot fields。
- `data_record / data_field` 只保留兼容和 projection 输出语义；除迁移兼容或测试 fixture 外，不新增手写业务 `data_field` rows。
- Runtime DataKey 用 `DataKey<T>` 暴露为 typed handle，并通过 `FrameworkDataKeys.RegisterAll()` 或 profile `DataCatalog` 进入 active catalog；不要新增 `DataMeta`、`DataRegistry` 或裸字符串业务访问。
- `data_key_descriptor` 显式保存 descriptor-first 字段定义：owner、runtime type、storage/write/range/modifier/migration policy、compute、dependencies、allowed values 和 presentation；`runtime_snapshot.json.descriptors` 必须与 `RuntimeDataDescriptorDto` 对齐。
- `dataos_runtime_field_stream` / records 只能引用已存在 descriptor，field type 必须与 descriptor `value_type` 一致；disabled capability 必须在生成阶段裁剪 descriptors 和 records。
- `resource_entry` 只做 ResourceCatalog lookup、legacy 分类或无单一业务 owner 的资源索引；内容归属路径优先写在业务表列中。
- 新字段必须同步 generator / validator / contracts / Runtime tests；涉及 Godot 或游戏侧胶水时补独立 Godot 验证场景，主场景 smoke 只作补充。
- 游戏专有 seed 留在游戏仓库，不写入框架通用 DataOS seed。
- 触及游戏 seed / snapshot / bootstrap 时，框架 build/tests 之外必须运行对应游戏仓 build；触及旧输入仓迁移对照时追加 `Resources/Else/brotato-my` build。
- 触及单位组合 profile 所需数据时，BrotatoLike `unit.player/*` / `unit.enemy/*` seed 必须显式覆盖 `Collision.Layer`、`Collision.Mask`、`Collision.Radius`、`Damage.ContactDamage`、`Damage.ContactDamageInterval`、`Attack.Interval`、`Attack.WindUpTime`、`Attack.RecoveryTime` 和 `AI.IsEnabled`，并重新生成 `DataOS/Snapshots/runtime_snapshot.json`。
- 游戏专属单位数值、资源路径和 profile 选择留在游戏 seed / 游戏代码；不得写入框架默认 DataOS seed。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
# 如果承载游戏提供 runner，再执行游戏 DataOS snapshot 与 Godot smoke。
```
