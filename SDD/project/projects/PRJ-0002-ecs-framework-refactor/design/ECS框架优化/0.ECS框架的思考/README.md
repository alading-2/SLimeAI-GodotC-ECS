# ECS 框架的思考

> 状态：current design notes
> 更新：2026-06-06
> 范围：SlimeAI ECS 框架概念层、Data 作为底层协议的定位、AI-first 框架取舍

## 定位

本目录保存 ECS 框架优化前的概念层深度思考，不直接承接代码实现。实现方案仍由后续 SDD 或 owner 设计文档处理。

当前重点不是“怎么改某个函数”，而是确认 SlimeAI 的框架模型是否成立：

- Data 是否应该作为框架底层核心。
- Data/Event 做底层协议、Component/System 做功能拼装，是否能支撑 AI-first 大框架目标。
- 与传统 ECS 的纯数据 Component + System 逻辑模型相比，SlimeAI 应采纳什么、不采纳什么。
- `1.拆箱装箱+GC优化/设计/01-Data运行时object去除设计.md` 的方向是否正确，哪里还需要补充、收窄或后续拆 SDD。
- `SDD-0031` 完成后仍存在的 `object?` 哪些是合理边界，哪些是协议债务，哪些是必须优先清理的业务误用。

## 阅读顺序

1. `01-Data作为ECS框架核心的概念复盘与方案批判.md`
2. `../1.拆箱装箱+GC优化/设计/01-Data运行时object去除设计.md`
3. `../1.拆箱装箱+GC优化/设计/00-总览与AI-first裁决.md`

## 非目标

- 不在本目录写代码实现步骤。
- 不替代 `SDD-0031 Data Runtime Generic Slot Hard Cutover`。
- 不把 Bevy、Unity Entities、Flecs、EnTT 的 API 形态复制成 SlimeAI public API。
- 不把性能优化压过 AI-first 的事实源、契约和可组合目标。
