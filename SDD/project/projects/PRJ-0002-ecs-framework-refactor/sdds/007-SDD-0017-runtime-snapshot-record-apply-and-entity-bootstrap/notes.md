# Notes

## References

- `design/2.Data系统优化/01-代码实现说明.md` §4、§5、§10。
- `design/2.Data系统优化/03-完全重构范围与TDD测试计划.md` §4.6。
- `design/2.Data系统优化/03-完全重构范围与TDD测试计划.md` §7 建议的 Data System Full Rewrite 拆分。

## Dependencies

- **Project**: PRJ-0002 ECS Framework Optimization
- **Data Rewrite Series**: SDD-0012 → SDD-0019
- **Historical Baseline**: SDD-0011 只作为已完成的 C#-first 编译修复历史，不再代表最新 Data 完整重构目标。

## Open Questions

- ApplyRecord 默认聚合错误还是遇错 fail fast，由调用方策略参数决定。
