# SkilmeAI 工作区边界说明

> 日期：2026-05-04

## 工作区

```text
/home/slime/Code/SkilmeAI/
  SkilmeAI/        # 框架主仓库
  Engine/          # Godot 引擎源码和未来 trace fork
  Games/
    BrotatoLike/   # 第一个正式游戏仓库
  Workspace/       # 工作区说明、版本锁和 clone 脚本
```

`/home/slime/Code/SkilmeAI/` 本身不要求是 git 仓库。内部 `SkilmeAI` 和 `Games/BrotatoLike` 是独立版本边界。

## 框架仓库边界

框架仓库负责：

- `GameOS` Runtime、Capabilities、Validation、Observation、GodotBridge。
- `DataOS` schema、生成器、snapshot 和数据校验。
- 通用 AI 协议、Skill 源头和模板。
- 包发布、API 文档和迁移说明。

框架仓库不放：

- BrotatoLike 专属资产。
- 单个游戏的数值和关卡内容。
- 未抽象稳定的玩法实验。

## 游戏仓库边界

游戏仓库负责：

- Godot 项目文件、场景、资产和游戏特定代码。
- 游戏本地 `DocsAI/ExternalFrameworkMap.md`。
- 游戏入口型 Skill。
- 锁定使用的 `SkilmeAI.GameOS` 版本或本地项目引用。

游戏仓库默认不直接修改框架源码。确认是框架 bug 时，切到框架仓库修复并发布版本，再升级游戏引用。

## Engine 边界

Godot 引擎目录用于：

- 保存 Godot 源码。
- 承载未来 PhysicsServer2D trace fork。
- 输出底层 trace 补丁和引擎调试报告。

当前已确认本地 Godot 源码在：

```text
/home/slime/Code/SkilmeAI/Engine/godot-4.6.2-stable
```
