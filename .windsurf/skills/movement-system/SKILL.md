---
name: movement-system
description: 修改 SlimeAI ECS Movement Capability、MovementDataKeys、MovementSystem、运动策略、运动碰撞或 Godot 位移桥时使用。
---

# Movement Capability 入口

## 必读入口

- `DocsAI/README.md`
- `DocsAI/ECS/Capabilities/Movement/README.md`
- `DocsAI/ECS/Capabilities/Collision/README.md`
- `DocsAI/ECS/Runtime/Data/Data系统说明.md`

## 源码位置

- `Src/ECS/Capabilities/Movement/`
- `Src/ECS/Capabilities/Collision/`
- `Src/ECS/Capabilities/Movement/Tests/`
- `Src/ECS/Tools/Math/`
- `Data/DataKey/Component/Movement/`

## 规则

- 对外角度输入使用度，语义为 `0=右、90=下、180=左、正值顺时针`。
- 数值型“不限制”统一用 `-1`，例如最大距离 / 最大时长。
- 纯 Movement 不依赖 `Godot.Vector2` 或 `Node2D`；Godot 同步写在 bridge。
- 新策略通过 `IMovementStrategy` / `MovementStrategyRegistry` 接入，并补 Runtime 测试和 BrotatoLike smoke。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
# 如果承载游戏提供 runner，再执行 Godot smoke:
# cd /home/slime/Code/SlimeAI/Games/<GameWithRunner>
# Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```
