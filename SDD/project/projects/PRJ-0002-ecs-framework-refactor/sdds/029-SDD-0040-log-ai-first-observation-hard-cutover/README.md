# SDD-0040 Log AI-first Observation Hard Cutover

## Index Card

- **Status**: blocked
- **Created**: 2026-06-09
- **Updated**: 2026-06-10
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/tools/logger
- **Tags**: tools, log, observation, ai-first, hard-cutover

## What This SDD Is About

本 SDD 执行 Log AI-first Observation hard cutover：把当前 `Src/ECS/Tools/Logger/Log.cs` 的 legacy 文本输出升级为结构化观测入口，并同批收口 Validation artifact、Godot scene runner 分析、`logctl analyze/query`、owner `Log.md` 和测试 PASS/FAIL 事实源。

当前 T1.1~T1.12 已完成。用户指出原 10/10 完成状态与 `design/Tool/10.Log` 不一致后，已补齐 `Config/Log` profile/rules/overrides、runtime profile metadata、budget suppressed summary、`logctl profile show` 和 `suggest` 的 `profilePatch` dry-run 输出。SDD 保持 blocked，仅因为当前没有可验证本框架工作树的承载游戏 runner，Godot scene smoke 未运行且未伪造通过。

2026-06-10 补充：项目级 current 设计入口已更新到 `design/Tool/10.Log/README.md`、`source-request.md` 和 `07-当前样本日志问题与整理方案.md`。本 SDD 下 `design/` 子目录保留 2026-06-09 执行快照，若与项目级 current Log 设计冲突，以项目级 `design/Tool/10.Log/` 为准。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — SDD-0040 的范围、取舍、DeepThink 确认包和 DesignCritic 结论
3. `execution-prompt.md` — 新会话执行提示词
4. `tasks.md` — 当前任务拆分
5. `progress.md` — 最近结论和恢复点
6. `bdd.md` — 行为场景或不适用说明
7. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SDD-0040 实现任务已按补齐后的 T1.1~T1.12 完成；2026-06-10 样本复查新增 analyzer digest / flow 边界 / owner 字段契约 follow-up，详见项目级 `design/Tool/10.Log/07-当前样本日志问题与整理方案.md`。
- **Next Action**: 非 Godot 门禁已通过；恢复或提供能验证当前框架工作树的承载游戏 runner 后，再运行 Godot scene smoke、`logctl analyze/query` 和 gate report；后续 analyzer follow-up 按项目级 current Log 设计执行。
- **Open Blockers**: Godot scene smoke blocked: 当前没有可验证本框架工作树的承载游戏 runner。Games/BrotatoLike 不是 git 仓，且缺少 Tools/run-godot-scene.sh 与 SlimeAI；Games/BrotatoLikeOld 虽有 runner，但 wrapper 指向缺失的 /home/slime/Code/SlimeAI/.codex/... 和 /home/slime/Code/SlimeAI/SlimeAI/GameOS/SlimeAI.GameOS.csproj，且 SlimeAI submodule commit 与当前框架工作树不一致。已通过非 Godot 门禁，未伪造场景验证通过。
