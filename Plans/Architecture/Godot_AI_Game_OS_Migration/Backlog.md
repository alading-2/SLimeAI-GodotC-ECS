# Godot AI Game OS Migration Backlog

## P0

- 迁移 Runtime 最小内核清单：Entity / Event / Resource / Pool / Timer。
- 为 Runtime Data 增加行为测试。
- 建立 `SkilmeAI.GameOS.Contracts.md` 和 `ApiIndex.md` 生成规则，而不是手工长期维护。
- 迁移 BrotatoLike 最小启动场景和 `Src/Main`。

## P1

- 建立 Capability 模板：`capability.json`、`Contract.md`、`Debug.md`、`Tests/`。
- 以 Movement 作为第一个 Capability 迁移样板。
- 建立 `Tools/run-capability-test.sh` 包装命令。
- 建立 Validation / Observation 最小日志目录规范。

## P2

- 建立 DataOS SQLite `core.sql`。
- 迁移 `Data/DataNew/System` 为数据库样板。
- 编写数据 snapshot 生成器。
- 重写 `.codex/skills` 为新仓库入口型 Skill。
