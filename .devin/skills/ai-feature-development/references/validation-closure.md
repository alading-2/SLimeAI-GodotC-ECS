# Validation Closure Matrix

AI 开发结束时不能只看代码是否写完，必须按改动影响面补齐 build / tests / scene / docs 证据。验证命令失败时不要跳过，先修复；确实受环境限制不能跑时，最终汇报写明命令、失败点和剩余风险。

## 固定门禁

### 1. 框架仓门禁

触及 `SlimeAI/` 的 Runtime、Capability、DataOS、GodotBridge、测试或工具时必须运行：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
```

DataOS schema、migration、seed、snapshot generator、validator 或 Runtime loader 相关改动追加：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
```

### 2. 承载游戏门禁

触及 GodotBridge、真实 Godot Node 生命周期、Physics、Input、Resource、UI、动画、游戏侧 adapter / handler / seed / scene，或新增 Godot 验证场景时，必须先显式构建承载游戏，再跑 Godot：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
Tools/run-godot-scene.sh run res://Src/Validation/<Area>/<Layer>/<Scene>.tscn --build --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

显式 `dotnet build` 不能被 Godot runner 内部 build 替代：显式 build 用来提前暴露 `.csproj`、DataOS snapshot、submodule mirror 和编译引用问题；`--build` 用来保证 Godot headless 运行前仍处于已编译状态。若当前游戏仓没有 `Tools/run-godot-scene.sh`，最终汇报必须说明该场景门禁因 runner 缺失未运行。

### 3. 旧输入仓门禁

触及 `Resources/Else/brotato-my`，或从旧输入仓迁移 / 对照旧脚本、场景、资源、Data、UI、Ability、ECS 逻辑时，必须验证旧输入仓当前仍可构建，避免把旧仓破坏后再迁入错误状态：

```bash
cd /home/slime/Code/SlimeAI/Resources/Else/brotato-my
dotnet build Brotato_my.sln
```

如果需要运行旧仓 Godot 场景，先运行上面的 `dotnet build Brotato_my.sln`，再使用旧仓 `DocsAI/Tests/` 记录的 runner 命令；不要把旧仓测试结果当成新框架功能完成证据。

## 功能类型追加要求

- 纯 Runtime / Capability 逻辑：补 `Tests/SlimeAI.GameOS.Tests/` 最小行为断言。
- DataOS 新字段：覆盖 schema / migration / seed 或 snapshot generator / validator / Runtime loader 断言，并同步 descriptor mirror。
- GodotBridge 或游戏侧胶水：补独立 Godot 验证场景；`run-main-smoke` / `Scenes/Main.tscn` 只能作为回归补充。
- 框架级 Godot 场景：场景写在框架仓 `Src/Validation/...`，脚本写在 `Src/Validation/...`；当前阶段跑 Godot 前同步到 `Games/BrotatoLike/SlimeAI/` 工作树。
- 游戏专属功能：验证证据必须来自对应游戏仓 build / scene artifact；不能只用框架 Runtime tests 代替。

## 文档和路由闭环

- Runtime / Capability API：同步 `DocsAI/GameOS/Contracts.md`、`DocsAI/GameOS/ApiIndex.md`、对应 Capability `Contract.md` / `Debug.md` / `Tests.md` / `CapabilityIndex.md`。
- Runtime Data / DataOS：同步 `DocsAI/DataOS/Overview.md`、`DocsAI/DataOS/SnapshotManifest.md`、schema / validator 说明和相关 `*DataKeys.cs` owner 文档。
- Godot 场景验证：同步 `DocsAI/Tests/GodotSceneTesting.md`、`DocsAI/Tests/GodotSceneValidation.md`、游戏侧 `DocsAI/GameProjectState.md` 和场景 README。
- 当前状态：同步 `DocsAI/ProjectState.md`；如果接入游戏切片，同步游戏侧状态和迁移台账。
- AI 路由：owner skill / rule / command 只改统一源 `.ai-config/`，再运行 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`；hook/subagent 直接改 `.claude/.codex` 项目配置。
