# 用户原始请求：弃用 ECS 框架方向

> 日期：2026-06-16  
> 来源：当前 Codex 会话  
> 用途：保留原始问题，正文设计文档只引用本文件，避免长请求淹没问题分析。

## 原文摘要

用户作出重大决策：弃用 ECS 框架。

核心要点：

- 发现 SlimeAI 之前把 ECS 用错了；真正 ECS 的核心是 DOD、数据结构、CPU cache、底层 storage/query/schedule，而不是 OOP 对象驱动。
- Godot 不适合做纯 ECS；Godot Node 已经支持动态添加/移除功能，更适合 OOP / Node composition。
- SlimeAI 的核心需求是高度解耦、功能解耦、系统解耦、快速开发游戏 MVP，而不是优先追求性能。
- AI-first 不需要 ECS，OOP 也可以完成，而且更适合 Godot。
- ECS 的部分概念可保留，例如 component/system 解耦，但 entity 应该改成 Object。
- 数据系统需要重新思考：事件驱动可以保留，Data 不一定继续集中保存所有数据，数据可以考虑放在 Component 里，不需要完全数据/逻辑分离。
- 需要深度搜索资料，在 `DocsAI/思考/框架/ECS框架` 说明真正 ECS 框架是什么。
- 需要在 `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/9.ECS框架优化/4.弃用ECS框架` 生成说明文档，个数不限。
- 需要重新判断 QFramework 是否适合 SlimeAI，尤其是它的数据结构、框架解耦能力和是否适合做大框架。
- `5.Data类型系统重构` 与 `6.架构学习` 两个 Data 文档包可以整合，只需要描述问题即可；Data 系统需要重写，具体方案待定。
- 这是重大重构和方向变化，先定概念方向，后续一步步改，最开始大概率是 Data。

## 用户明确裁决

```text
不用 ECS 这个方向已定。
Data 系统需要重写，完全重构，怎么写待定。
先定概念方向，不急着实现。
```
