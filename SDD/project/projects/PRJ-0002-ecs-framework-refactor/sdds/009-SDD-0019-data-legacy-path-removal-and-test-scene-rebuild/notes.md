# Notes

## References

- `design/Runtime/2.Data系统优化/03-完全重构范围与TDD测试计划.md` §2、§3、§5、§6、§8。
- `design/Runtime/2.Data系统优化/README.md` §10.5、§11、§12。
- `design/Runtime/2.Data系统优化/03-完全重构范围与TDD测试计划.md` §7 建议的 Data System Full Rewrite 拆分。

## Dependencies

- **Project**: PRJ-0002 ECS Framework Optimization
- **Data Rewrite Series**: SDD-0012 → SDD-0019
- **Historical Baseline**: SDD-0011 只作为已完成的 C#-first 编译修复历史，不再代表最新 Data 完整重构目标。

## Open Questions

- 旧 Data/Data 中若存在非 Data 专属资源，执行时逐项判定 owner，不在规划阶段假设删除。
