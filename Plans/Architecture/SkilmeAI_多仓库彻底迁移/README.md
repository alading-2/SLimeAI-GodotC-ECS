# SlimeAI 多仓库彻底迁移计划

> 日期：2026-05-04
> 状态：执行中
> 目标：把当前 `brotato-my` 从长期主项目降级为迁移输入仓库，建立 `SlimeAI` 顶层工作区、独立 AI 框架仓库、Godot 引擎源码目录和独立游戏仓库。

## 1. 核心决策

本次迁移不做旧结构兼容。当前仓库只作为迁移输入、资产清单和迁移计划来源，后续长期建设发生在新的多仓库结构中。

已确定决策：

- 顶层工作区命名为 `SlimeAI`。
- 第一阶段只做 Godot，不做跨引擎抽象；Unity / 自研引擎只预留目录和文档边界。
- `SlimeAI` AI 框架、Godot 引擎源码、每个游戏都使用独立版本边界；当前引擎源码权威路径是 `/home/slime/Code/SlimeAI/Engine/godot-4.6.2-stable`。
- Godot 引擎构建入口是 `/home/slime/Code/SlimeAI/Engine/Tools/build-linux-editor-mono.sh`；当前机器缺少 `SCons`，尚未打出 CLI 可执行文件。
- 游戏运行时通过 `SlimeAI.GameOS` NuGet / DLL / 本地包引用框架，不复制框架源码。
- 游戏 AI 工作流通过本地 `.codex/skills` 和 `DocsAI/ExternalFrameworkMap.md` 查找框架版本、框架源码路径和契约文档。
- 框架深层 Skill 保留在 `SlimeAI` 框架仓库；游戏仓库只放游戏开发入口和框架引用入口。
- 框架升级通过显式版本发布和游戏项目显式升级完成，不自动影响所有游戏。

## 2. 目标工作区

```text
/home/slime/Code/SlimeAI/
  SlimeAI/                         # repo: AI 框架主仓库
    GameOS/
    DataOS/
    Agent/
    Packages/
    Docs/
    DocsAI/
    Plans/
    Tools/

  Engine/                           # Godot 源码、fork、trace 补丁
    godot-4.6.2-stable/
    godot-ai-trace-fork/

  Games/
    BrotatoLike/                    # repo: 第一个正式游戏项目
    NextGameA/
    NextGameB/

  Workspace/                        # 可选 repo: 工作区说明、clone 脚本、版本锁
```

`/home/slime/Code/SlimeAI/` 本身可以只是本地工作区，不必作为 git 仓库。真正的版本边界由内部仓库承担。

## 3. SlimeAI 主仓库职责

```text
SlimeAI/
  GameOS/
    Runtime/                       # Entity / Event / Data / Relationship / Schedule / Pool / Timer / Resource
    Capabilities/                  # Movement / Collision / Damage / Ability / Feature / AI / Projectile / UIHud
    Validation/                    # build / scene test / capability test / regression gate
    Observation/                   # logs / dumps / traces / reports
    GodotBridge/                   # Godot Node / SceneTree / Resource / Physics bridge

  DataOS/
    Schema/
    Migrations/
    Generators/
    Snapshots/
    Analytics/

  Agent/
    SkillsSource/                  # 通用 Skill 源头和模板
    Protocols/
    Templates/
    DocsAI/

  Packages/
    LocalNuGet/
    Build/
    ReleaseNotes/

  Tools/
  Docs/
  DocsAI/
  Plans/
```

第一阶段 `GameOS` 可以继续使用 Godot 类型和 Godot C# 生命周期，不为了未来引擎预留而抽象掉真实边界。

## 4. 游戏仓库职责

以 `Games/BrotatoLike` 为例：

```text
Games/BrotatoLike/
  AGENTS.md
  project.godot
  BrotatoLike.csproj
  Data/
  Assets/
  Scenes/
  Src/
    Game/
  DocsAI/
    INDEX.md
    ExternalFrameworkMap.md
    GameProjectState.md
  .codex/
    skills/
      project-index/
      game-development/
      gameos-reference/
      data-authoring/
      godot-scene-test/
```

游戏项目可以保留当前项目中杂七杂八但仍有价值的游戏内容。框架不兼容旧目录，但游戏资产可以先迁移再清理。

## 5. 框架引用策略

阶段划分：

```text
阶段 1：源码项目引用
  游戏 csproj 引用本地 SlimeAI.GameOS.csproj，方便迁移期 Debug 和 AI 读源码。

阶段 2：本地 NuGet 包
  SlimeAI 发布本地 package，游戏锁定明确版本，升级时跑迁移和回归。

阶段 3：稳定 Runtime DLL / NuGet
  Runtime 稳定后以包形式消费；Capabilities 可按稳定度拆成核心包、源码扩展包或游戏本地扩展。
```

DLL / NuGet 只解决编译依赖，不解决 AI 理解问题。每个发布包必须同时生成：

```text
SlimeAI.GameOS.dll
SlimeAI.GameOS.xml
SlimeAI.GameOS.Contracts.md
SlimeAI.GameOS.ApiIndex.md
SlimeAI.GameOS.DebugGuide.md
SlimeAI.GameOS.Migration.md
```

## 6. Skill 策略

Skill 分三层：

```text
1. Skill Source
   SlimeAI/Agent/SkillsSource/
   通用 Skill 源头，不一定直接被 Codex / Claude 自动加载。

2. Framework Active Skills
   SlimeAI/.codex/skills/
   打开 SlimeAI 框架仓库时使用。

3. Game Active Skills
   Games/<GameName>/.codex/skills/
   打开具体游戏仓库时使用。
```

游戏仓库只激活入口型 Skill。默认不允许游戏任务直接修改框架源码；如果定位到框架 bug，应切换到 `SlimeAI` 仓库修复、发布新版本，再升级游戏。

## 7. 里程碑

### M1：迁移蓝图冻结

输出：

- 本计划。
- 多仓库 AI 工作流协议。
- 当前仓库资产分流清单。
- 新工作区目录和仓库边界说明。
- 执行状态：见 `Progress.md`。
- 资产分流：见 `AssetRouting.md`。
- 工作区边界：见 `WorkspaceLayout.md`。

验收：

```bash
rg -n "SlimeAI|多仓库|ExternalFrameworkMap|NuGet" Plans DocsAI Docs
```

### M2：新工作区骨架

输出：

- `/home/slime/Code/SlimeAI/` 顶层工作区。
- `SlimeAI` 主仓库骨架。
- `Engine` 目录和 Godot 源码位置确认。
- `Games/BrotatoLike` 新游戏仓库骨架。
- 每个仓库的 `AGENTS.md / DocsAI/INDEX.md / ProjectState`。

验收：

```bash
find /home/slime/Code/SlimeAI -maxdepth 3 -type d | sort
git -C /home/slime/Code/SlimeAI/SlimeAI status --short
git -C /home/slime/Code/SlimeAI/Games/BrotatoLike status --short
```

### M3：GameOS 最小可构建包

输出：

- `SlimeAI.GameOS` Godot C# 项目。
- Runtime 最小内核迁移：Entity / Event / Data / Relationship / Schedule / Resource / Pool / Timer。
- `SlimeAI.GameOS.Contracts.md` 和 `ApiIndex.md`。
- 本地 NuGet 或项目引用样例。

验收：

```bash
dotnet build
dotnet pack
```

### M4：BrotatoLike 游戏仓库接入

输出：

- `BrotatoLike` Godot 项目可打开、可构建。
- 游戏项目引用 `SlimeAI.GameOS`。
- 最小启动场景、Runtime smoke probe、GodotBridge 探针。
- `DocsAI/ExternalFrameworkMap.md`。
- 游戏入口 Skill。

验收：

```bash
dotnet build
godot --headless --build-solutions --path .
```

### M5：GodotBridge 迁移

输出：

- Node Entity 生命周期桥。
- Component 自动注册 / 注销桥。
- Entity-Component 关系写入 `RelationshipType.EntityToComponent`。
- Godot `_Process` 驱动 `TimerManager.Instance.Tick`。
- BrotatoLike 编译接入 GodotBridge 探针。

验收：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI && Tools/run-build.sh && Tools/run-tests.sh
cd /home/slime/Code/SlimeAI/Games/BrotatoLike && Tools/run-build.sh
```

后续扩展：

- Node 对象池泊车 / 脱树。
- 碰撞隔离和 PhysicsServer2D trace 关联。
- Godot headless 场景断言。

### M6：Capabilities 迁移

输出：

- Movement / Collision / Damage / Ability / Feature / AI 按 Capability 包迁入 `SlimeAI.GameOS`。
- 每个 Capability 包含 manifest、Contract、Data schema、测试和 Debug 文档。
- BrotatoLike 只保留游戏特定技能、数值、资源和规则。

验收：

```bash
Tools/run-capability-test.sh Movement
Tools/run-capability-test.sh Collision
Tools/run-capability-test.sh Damage
dotnet build
```

### M7：DataOS 接管数据

输出：

- SQLite authoring schema。
- 迁移 System / Unit / Ability / Feature 数据。
- 生成 runtime snapshot。
- 游戏数据和通用数据分库或分 schema 管理。

验收：

```bash
Tools/validate-data.sh
Tools/generate-data-snapshot.sh
dotnet build
```

### M7：旧仓库归档

输出：

- 当前 `brotato-my` 只保留迁移记录或直接归档。
- 新仓库完成最小回归。
- 旧路径不再作为 AI 开发入口。

验收：

```bash
rg -n "brotato-my|Src/ECS/Base|Data/DataNew" /home/slime/Code/SlimeAI/SlimeAI /home/slime/Code/SlimeAI/Games/BrotatoLike
```

## 8. 当前仓库定位

从本计划生效后，当前仓库定位改为：

```text
迁移输入仓库
  - 提供旧代码、旧数据、旧场景、旧文档和经验
  - 提供迁移计划与资产分流说明
  - 不再新增长期架构能力
  - 不保留旧兼容层
```

新增框架能力、游戏长期功能和 DataOS 实现应迁入新仓库后再继续。
