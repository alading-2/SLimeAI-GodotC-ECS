# brotato-my 归档说明

> 日期：2026-05-06
> 状态：已归档，不再作为长期主项目
> 新工作区：`/home/slime/Code/SkilmeAI/`

---

## 仓库定位

本仓库已从长期主项目降级为**迁移输入仓库**。所有新开发、架构演进和 Capability 扩展都在 `/home/slime/Code/SkilmeAI/` 工作区进行。

---

## 已迁移内容清单

### 1. 代码（Src/ECS/）→ 已删除

| 旧路径 | 新位置 | 状态 |
|--------|--------|------|
| `Src/ECS/Base/Entity/Core` | `SkilmeAI/GameOS/Runtime/Entity` | 已迁移 |
| `Src/ECS/Base/Component/IComponent.cs` | `SkilmeAI/GameOS/Runtime/Component` | 已迁移 |
| `Src/ECS/Base/Data` | `SkilmeAI/GameOS/Runtime/Data` | 已迁移 |
| `Src/ECS/Base/Event` | `SkilmeAI/GameOS/Runtime/Event` | 已迁移 |
| `Src/ECS/Base/Entity/Relationship` | `SkilmeAI/GameOS/Runtime/Relationship` | 已迁移 |
| `Src/ECS/Tools/ObjectPool` | `SkilmeAI/GameOS/Runtime/Pool` | 已迁移 |
| `Src/ECS/Tools/TimerManager` | `SkilmeAI/GameOS/Runtime/Timer` | 已迁移 |
| `Src/ECS/Base/System/Movement` | `SkilmeAI/GameOS/Capabilities/Movement` | 已迁移 |
| `Src/ECS/Base/Component/Collision` | `SkilmeAI/GameOS/Capabilities/Collision` | 已迁移 |
| `Src/ECS/Base/System/DamageSystem` | `SkilmeAI/GameOS/Capabilities/Damage` | 已迁移 |
| `Src/ECS/Base/System/FeatureSystem` | `SkilmeAI/GameOS/Capabilities/Feature` | 已迁移 |
| `Src/ECS/Base/System/AbilitySystem` | `SkilmeAI/GameOS/Capabilities/Ability` | 已迁移 |
| `Src/ECS/Base/System/AISystem` | `SkilmeAI/GameOS/Capabilities/AI` | 已迁移 |
| `Src/ECS/Base/Entity/Projectile` | `SkilmeAI/GameOS/Capabilities/Projectile` | 已迁移 |
| `Src/ECS/Base/System/AttackSystem` | `SkilmeAI/GameOS/Capabilities/Attack` | 已迁移 |
| `Src/ECS/Base/System/EffectSystem` | `SkilmeAI/GameOS/Capabilities/Effect` | 已迁移 |
| `Src/ECS/Base/Component/Unit` | `SkilmeAI/GameOS/Capabilities/Unit` | 已迁移 |
| `Src/ECS/UI` | `Games/BrotatoLike/Src/Game/` + 新 UI 体系 | 已迁移 |
| `Src/ECS/Test` | `SkilmeAI/GameOS/Validation` + `Games/BrotatoLike/Tools/` | 已迁移 |
| `Src/ECS/Tools` | `SkilmeAI/GameOS/Runtime` + `GodotBridge` | 已迁移 |

**验证**：新仓库 `Tools/run-build.sh` + `Tools/run-godot-smoke.sh` PASS。

### 2. 数据（Data/）→ 已删除

| 旧路径 | 新位置 | 状态 |
|--------|--------|------|
| `Data/DataNew/*.cs` | `Games/BrotatoLike/DataOS/Authoring/BrotatoLike.seed.sql` | 已迁移 |
| `Data/DataKey/*.cs` | `SkilmeAI/GameOS/Capabilities/*/DataKeys.cs` | 已迁移 |
| `Data/EventType/*.cs` | `SkilmeAI/GameOS/Runtime/Event` + Capability 分域 | 已迁移 |
| `Data/ResourceManagement/*.cs` | `SkilmeAI/GameOS/Runtime/Resource` + DataOS | 已迁移 |
| `Data/Config/*.cs` | `Games/BrotatoLike/DataOS/Authoring/` system.config | 已迁移 |
| `Data/Data/*.tres` | DataOS seed 中对应的 data_field / data_record | 已迁移 |

**验证**：`Games/BrotatoLike/Tools/run-dataos-snapshot.sh` 生成 snapshot 成功，smoke 读取验证通过。

### 3. 游戏资产（assets/）→ 已复制

| 旧路径 | 新位置 | 状态 |
|--------|--------|------|
| `assets/` | `Games/BrotatoLike/assets/` | 已复制，路径保持 `res://assets/...` |

### 4. 文档和 Skill

| 旧路径 | 新位置 | 状态 |
|--------|--------|------|
| `DocsAI/Protocols` | `SkilmeAI/DocsAI/Protocols` | 已迁移 |
| `DocsAI/Modules` | Capability `Contract.md` + `SkilmeAI/DocsAI` | 已迁移 |
| `.claude/skills` | `SkilmeAI/.codex/skills` + `Agent/SkillsSource` | 已重写为入口型 |
| `.codex/skills` | `SkilmeAI/.codex/skills` + `Games/BrotatoLike/.codex/skills` | 已重写为入口型 |
| `Plans/Architecture/` | `SkilmeAI/Plans/` + `SkilmeAI/DocsAI/MigrationFromBrotatoMy.md` | 已迁移总结 |

---

## 保留内容

以下目录作为迁移审计痕迹和历史参考保留：

- `Plans/Architecture/` — 迁移计划原始文档
- `Docs/`、`DocsAI/` — 旧文档参考
- `assets/` — 游戏资产原始副本（新仓库 `Games/BrotatoLike/assets/` 是主副本）
- `.claude/`、`.codex/` — 旧 Skill 参考
- `AGENTS.md`、`CLAUDE.md` — 仓库配置历史
- 本文件 `ARCHIVE.md`

---

## 如需恢复

旧代码和数据的完整历史保留在 git 历史中。如需查阅已删除的 `Src/ECS/` 或 `Data/` 内容：

```bash
cd /home/slime/Code/Godot/Games/MyGames/brotato-my
git show HEAD:Src/ECS/...
git show HEAD:Data/...
```

---

## 新仓库入口

```bash
cd /home/slime/Code/SkilmeAI/SkilmeAI      # 框架仓库
cd /home/slime/Code/SkilmeAI/Games/BrotatoLike  # 游戏仓库
```

迁移总结文档：`/home/slime/Code/SkilmeAI/SkilmeAI/DocsAI/MigrationFromBrotatoMy.md`
