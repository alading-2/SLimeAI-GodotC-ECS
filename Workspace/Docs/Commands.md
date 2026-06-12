# 常用命令参考

这个文档只放高频命令，用来替代常用 VSCode Task。默认在框架仓执行：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
```

进入游戏仓的命令会单独标注。

## 高频命令

框架构建：日常改 C# 后先跑这个。

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

DataOS 完整更新：改 seed SQL、descriptor 或数据字段后跑这个；会重建 DB、生成 runtime snapshot、生成 `DataKey_Generated.cs`，最后校验。

```bash
bash Data/DataOS/Tools/generate-all.sh
```

DataOS 仅校验：没改数据生成物，只想检查当前 DB、snapshot、DataKey 是否一致。

```bash
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
```

AI 配置同步：改 `.ai-config/skills` 或 `.ai-config/rules` 后跑这个；会按 `.ai-config/sync-targets.json` 同步所有目标，并自动跑 changed skill lint。

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
```

SDD 全量校验：改 SDD 文档、任务、进度、索引后跑这个。

```bash
python3 Workspace/SDD/sdd.py validate --all
```

全量验证 wrapper：需要一次性跑框架 build、游戏 build、场景列表、主场景 smoke 和日志分析时使用。当前要求承载游戏仓已存在 `Tools/run-build.sh` 与 `Tools/run-godot-scene.sh`。

```bash
bash Workspace/Tools/run-full-validation.sh
```

## DataOS 分步命令

只有在完整更新失败、需要定位是哪一步坏了，或只想更新某个生成物时使用。

数据流向：

```text
seed SQL -> authoring.db -> runtime_snapshot.json -> DataKey_Generated.cs
```

只重建 authoring DB。

```bash
bash Data/DataOS/Tools/build-authoring-db.sh
```

只生成 runtime snapshot。

```bash
bash Data/DataOS/Tools/generate-runtime-snapshot.sh \
  Data/DataOS/Authoring/slimeainew.authoring.db \
  Data/DataOS/Snapshots/runtime_snapshot.json
```

只生成 DataKey handle。

```bash
python3 Data/DataOS/Tools/generate-data-key-handles.py \
  Data/DataOS/Snapshots/runtime_snapshot.json \
  Data/DataKey/Generated/DataKey_Generated.cs
```

固定 snapshot 生成时间：需要稳定 diff 时使用。

```bash
DATAOS_GENERATED_AT_UTC=1970-01-01T00:00:00Z \
bash Data/DataOS/Tools/generate-runtime-snapshot.sh \
  Data/DataOS/Authoring/slimeainew.authoring.db \
  Data/DataOS/Snapshots/runtime_snapshot.json
```

## 可选命令

资源 catalog 更新：新增、删除、移动 `.tscn` / `.tres` 后使用，生成 `Data/ResourceManagement/ResourcePaths.cs`。

```bash
dotnet run --project Workspace/Tools/ResourceGenerator/ResourceGenerator.csproj
```

Skill lint 全量扫描：需要完整检查 `.ai-config/skills` 时使用；普通同步只需要跑 `sync-ai-config.sh`。

```bash
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

Hook smoke：改 `.claude/settings.json`、`.codex/hooks.json` 或 SystemAgent hook 后使用。

```bash
python3 Workspace/SystemAgent/Tools/systemagent-hooks/run-hook-smoke.py
```

Godot 主场景 smoke：只适用于已经包含 scene runner 的游戏仓；当前 `Games/BrotatoLike/` 尚未初始化 runner。

```bash
cd /home/slime/Code/SlimeAI/Games/<GameWithRunner>
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

更多低频命令直接查对应 CLI 的 `--help` 或工具 README。
