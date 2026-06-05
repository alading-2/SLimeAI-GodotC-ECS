# Spec / Code Alignment Check

> Baseline 由 `Workspace/SystemAgent/` 与当前 SDD 管理。执行任务前必须区分 SDD/历史基线、代码事实和 future backlog。

读取时机：实现 SDD task、引用历史基线、修改 DocsAI 事实源、或发现 artifact 与代码描述冲突时读取。

## 检查顺序

1. 查看当前 SDD：

```bash
python3 Workspace/SDD/sdd.py list
```

2. 读目标 SDD：

```bash
python3 Workspace/SDD/sdd.py show <sdd-id>
```

3. 读取 SDD `README.md`、`design/`、`tasks.md`、`progress.md`、`bdd.md`（如存在），不要猜 artifact 路径。
4. 对每个关键 symbol 做代码搜索：

```bash
rg -n "<SymbolName>" SlimeAI Games/BrotatoLike Resources/Else/brotato-my openspec -S
```

5. 对照 SDD、历史 `openspec/specs/<capability>/spec.md`、当前 SDD progress、`DocsAI/ECS/` 文档、tests。

## 判断

- 历史 spec 已 archive 且代码存在：可作为当前事实引用。
- 历史 spec 已 archive 但代码不存在：标为 alignment gap；先修代码或更新 artifact，不能当已完成事实。
- active SDD 描述但未完成：只能引用 SDD 路径，不能写进长期 skill baseline。
- ProjectState 明确列为 future backlog：只能写“未落地 / future backlog”，不能写成已完成。

## P4 教训

P4 当前事实：

- 已落地：`SchedulePhase`、`RuntimeCommandBuffer`、8 种 typed command kind、`EnterGuard(reason)`、`RuntimeWorld.Commands / Schedule`、`CommandPlaybackReport`。
- 未落地：`IRuntimeSystem -> IRuntimeProcess` rename、`SystemConfig + SystemDescriptor + SystemRuntimeInfo` 合并。

引用 P4 时必须按上面区分；不能因为早期 design 文字出现过 rename 就写成已完成。

## 停手条件

- artifact 与 `ProjectState.md` 冲突。
- spec 使用的术语在代码 grep 不存在，且不是明确的 future backlog。
- tasks 要求的验证无法证明实际代码行为。

先修 artifact，再继续实现。
