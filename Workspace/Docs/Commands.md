# 常用命令参考

所有命令在 `SlimeAI/` 目录下执行（除非另有说明）。

## DataOS 数据生成

数据流向：`seed SQL -> build-authoring-db.sh -> authoring.db -> generate-runtime-snapshot.sh -> runtime_snapshot.json -> generate-data-key-handles.py -> DataKey_Generated.cs`

### 完整生成（推荐）

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
bash Data/DataOS/Tools/generate-all.sh
```

从 seed SQL 重建 authoring DB，生成 runtime_snapshot.json 和 DataKey_Generated.cs，并校验一致性。

### 分步执行

```bash
# 1. 重建 authoring DB（从 seed SQL）
bash Data/DataOS/Tools/build-authoring-db.sh

# 2. 生成 runtime_snapshot.json（从 authoring DB）
bash Data/DataOS/Tools/generate-runtime-snapshot.sh \
  Data/DataOS/Authoring/slimeainew.authoring.db \
  Data/DataOS/Snapshots/runtime_snapshot.json

# 3. 生成 DataKey_Generated.cs（从 runtime_snapshot.json）
python3 Data/DataOS/Tools/generate-data-key-handles.py \
  Data/DataOS/Snapshots/runtime_snapshot.json \
  Data/DataKey/Generated/DataKey_Generated.cs

# 4. 验证一致性
bash Data/DataOS/Tools/validate-dataos.sh \
  Data/DataOS/Authoring/slimeainew.authoring.db
```

### 仅验证

```bash
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
```

## 框架构建与测试

```bash
cd /home/slime/Code/SlimeAI/SlimeAI

# 构建
Tools/run-build.sh

# 测试
Tools/run-tests.sh
```

## SDD / AI 配置验证

```bash
cd /home/slime/Code/SlimeAI/SlimeAI

# SDD 验证
python3 Workspace/SDD/sdd.py validate --all

# AI 配置同步
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh

# Skill lint
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

## Godot 场景测试

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike

# 运行场景测试
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs

# 分析日志
Tools/analyze-godot-scene-logs.sh
```

## Git 子模块更新

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
git submodule update --remote SlimeAI
git add SlimeAI && git commit
```
