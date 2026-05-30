# Notes

## References

- `design/2.Data系统优化/01-代码实现说明.md` §7。
- `design/2.Data系统优化/02-DataMeta属性审计与Feature计算边界.md` §3、§4.13、§4.14、§7.3。
- `design/2.Data系统优化/03-完全重构范围与TDD测试计划.md` §7 建议的 Data System Full Rewrite 拆分。

## Dependencies

- **Project**: PRJ-0002 ECS Framework Optimization
- **Data Rewrite Series**: SDD-0012 → SDD-0019
- **Historical Baseline**: SDD-0011 只作为已完成的 C#-first 编译修复历史，不再代表最新 Data 完整重构目标。

## Open Questions

- resolver side-effect guard 若无法完全自动检测，需以 contract + reviewer gate 兜底。
