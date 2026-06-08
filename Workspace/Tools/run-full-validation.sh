#!/usr/bin/env bash
# SlimeAI 端到端验证 wrapper
# 一键跑：build → tests → scene list → main smoke → analyzer
# 任一步失败即停，输出失败步骤名
set -euo pipefail

START_TIME=$(date +%s)
FAILED_STEP=""

run_step() {
    local name="$1"
    shift
    echo "==> [$name] starting..."
    local step_start=$(date +%s)
    if "$@"; then
        local step_end=$(date +%s)
        echo "==> [$name] PASS ($((step_end - step_start))s)"
    else
        local step_end=$(date +%s)
        echo "==> [$name] FAIL ($((step_end - step_start))s)"
        FAILED_STEP="$name"
        return 1
    fi
}

# 1. 框架 build（TODO: 框架/游戏仓分离后创建 Workspace/Tools/run-build.sh）
# run_step "framework-build" bash -c "cd /home/slime/Code/SlimeAI/SlimeAI && Tools/run-build.sh"
run_step "framework-build" bash -c "cd /home/slime/Code/SlimeAI/SlimeAI && dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly"

# 2. 框架 tests（TODO: 框架/游戏仓分离后创建 Workspace/Tools/run-tests.sh）
# run_step "framework-tests" bash -c "cd /home/slime/Code/SlimeAI/SlimeAI && Tools/run-tests.sh"

# 3. 游戏 build
run_step "game-build" bash -c "cd /home/slime/Code/SlimeAI/Games/BrotatoLike && Tools/run-build.sh"

# 4. 场景列表
run_step "scene-list" bash -c "cd /home/slime/Code/SlimeAI/Games/BrotatoLike && Tools/run-godot-scene.sh list"

# 5. Main smoke
run_step "main-smoke" bash -c "cd /home/slime/Code/SlimeAI/Games/BrotatoLike && Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs"

# 6. Analyzer
run_step "analyzer" bash -c "cd /home/slime/Code/SlimeAI/Games/BrotatoLike && Tools/analyze-godot-scene-logs.sh"

END_TIME=$(date +%s)
echo ""
echo "=========================================="
if [ -z "$FAILED_STEP" ]; then
    echo "ALL PASS ($((END_TIME - START_TIME))s total)"
else
    echo "FAILED at step: $FAILED_STEP ($((END_TIME - START_TIME))s total)"
    exit 1
fi
