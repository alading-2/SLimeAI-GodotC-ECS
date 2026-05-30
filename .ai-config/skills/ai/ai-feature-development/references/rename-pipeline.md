# Rename Pipeline

> Baseline 由 `Workspace/SystemAgent/` 与当前 SDD 管理。public symbol migration 或删旧入口必须先有 SDD。

读取时机：改名、删除旧 public API、移动目录、替换协议名、把 static facade 收束到 RuntimeWorld、清理 legacy 符号时读取。

## 9 步流水线

1. 全量搜索旧符号：

```bash
rg -n "OldSymbol|OldNamespace|OldFileName" SlimeAI Games/BrotatoLike Resources/Else/brotato-my openspec SlimeAI/DocsAI -S
```

2. 写旧 -> 新映射：列出改名、删除、保留兼容、迁移到游戏侧四类。
3. 先新增 typed 替代符号或新 facade，确保新路径能独立验证。
4. 同步 SDD / DocsAI artifact，明确哪些符号本轮不改。
5. 一次性删除旧符号和旧测试入口；不要边写边删让搜索结果漂移。
6. 跑 build，用编译失败逐个修引用。
7. 改 tests，补旧符号不存在的 grep 证据。
8. 改 DocsAI / Contract / ApiIndex / ProjectState / owner skill。
9. 如果承载游戏或 submodule 指针受影响，按跨仓流程同步；禁止直接改 `Games/*/SlimeAI/` 镜像内容。

## 分类

- 完整 rename：旧 public API 删除，新 API 接管，例如 P1 `RelationshipManager` 被 `LifecycleTree` 替代。
- facade 收束：旧 static API 保留但转发到新 owner，例如 P2b `EntityManager` 转发到 `RuntimeWorld.Default.Entities`；新代码优先 world-scoped API。
- 下沉到游戏：framework 删除游戏语义，游戏仓新增 owner，例如 P3 输入事件和 `BrotatoLikePlayerInputComponent`。
- future backlog：设计上想改但本轮不做，例如 P4 未执行 `IRuntimeSystem -> IRuntimeProcess` rename，也未合并 `SystemConfig + SystemDescriptor + SystemRuntimeInfo`。

## 停手信号

- 搜索结果横跨 framework、game、old input repo，但 tasks 没列多仓验证。
- 旧符号被 reflection、scene path、DataOS seed 或 generated snapshot 使用。
- spec 写成已完成，但代码 grep 不存在。
- rename 会触发超过当前 change 边界的语义重写。

出现以上情况先更新 SDD artifact 或拆分任务，不要继续顺手改。
