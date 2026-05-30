    # Notes

    ## References

    - `../../design/03-Hook与Gate重写方案.md`
- `../../design/SystemAgent问题清单.md`
- `../../design/06-实施路线图.md`

    ## Scope Notes

    - 本 SDD 只生成任务计划，不在创建阶段修改实现、hook runtime、`.ai-config` 或 subagent 配置。
    - 具体实施时必须先读取当前 SDD Latest Resume、tasks 和 bdd，再读取共享设计引用。

    ## Open Questions

    - T1.1 执行时是否需要扩大 Git boundary 或引入 worktree，应在 progress 中记录。
    - 如果实施中发现任务应拆分为更小 SDD，应先更新 roadmap 再继续。
