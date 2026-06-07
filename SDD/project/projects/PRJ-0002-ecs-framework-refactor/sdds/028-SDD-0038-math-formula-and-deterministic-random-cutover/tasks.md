# Tasks

## Progress

- **Status**: done
- **Completed**: 10/10
- **Current**: done

## Task List

- [x] T1.1 Readiness baseline
  - **Scope**: 记录 Math 源码、DocsAI、`MyMath` 调用点、随机源、GeometryCalculator / TargetSelector 边界和 SDD-0036 状态。
  - **Validation**: `git status --short`；Math grep gates；`python3 Workspace/SDD/sdd.py validate SDD-0038`。

- [x] T1.2 Deterministic RNG tests first
  - **Scope**: 为概率 0/100/越界、固定 seed、随机采样复现写 RED tests。
  - **Validation**: tests 先红后绿；不依赖统计波动。

- [x] T1.3 Probability / random helper implementation
  - **Scope**: 新增可注入 RNG / seed 的概率与采样 helper；迁移 `CheckProbability` 等调用点。
  - **Validation**: `GD.Randf()` 不作为测试或 gameplay 默认路径。

- [x] T1.4 Formula owner split
  - **Scope**: 按调用点拆护甲/伤害、冷却/充能、属性倍率等公式 owner；删除旧注释中多版本公式残留。
  - **Validation**: Formula 文档说明输入、输出、单位、范围、来源。

- [x] T1.5 Geometry ownership cleanup
  - **Scope**: 明确 `Geometry2D` 属 Math；`GeometryType` / Query 语义属 TargetSelector；`GeometryCalculator` 删除、internal 化或只留迁移说明。
  - **Validation**: DocsAI 不再教 AI 使用旧门面作为 current API。

- [x] T1.6 Bezier / curve semantic boundary
  - **Scope**: 保留 BezierCurve / WaveMath / Curves 纯算法；标注 BezierTemplateBuilder 玩法模板语义并决定是否迁 owner。
  - **Validation**: Movement/Ability 调用点仍通过明确 helper，不复制曲线公式。

- [x] T1.7 Tests and validation scene update
  - **Scope**: 更新 Math tests / MyMathTest 或重命名测试场景，覆盖概率、边界几何、曲线端点和无效参数。
  - **Validation**: tests 稳定，不靠随机统计阈值侥幸通过。

- [x] T1.8 DocsAI and skill sync
  - **Scope**: 更新 `DocsAI/ECS/Tools/Math/`、相关 Capability owner 文档和 `tools` skill。
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`。

- [x] T1.9 Build and optional Godot scene
  - **Scope**: 跑 build；承载游戏 runner 可用时跑 Math test scene。
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`；Godot runner 可用时运行 Math scene。

- [x] T1.10 Closeout
  - **Scope**: 回填 SDD tasks/progress/bdd、项目 roadmap/progress；记录是否需要后续 formula balance SDD。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0038`；`git diff --check`。
