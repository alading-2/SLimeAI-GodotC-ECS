# Math 概念

> status: current
> sourcePaths: Src/ECS/Tools/Math/
> relatedDocs: DocsAI/ECS/Tools/Math/Usage.md, DocsAI/ECS/Tools/Math/Concepts/通用数学工具重构执行方案.md
> lastReviewed: 2026-06-01

## 1. 一句话定位

数学工具层，包含 MyMath（统计公式）、WaveMath（正弦波）、BezierCurve、Curves（椭圆弧/抛物线/圆弧）、Geometry2D 等。

## 2. 核心概念

### 工具分类

- **MyMath**：游戏数值公式（伤害计算、属性缩放）
- **WaveMath**：正弦波运动参数
- **BezierCurve**：贝塞尔曲线采样
- **Curves**：EllipseArc2D、Parabola2D、CircularArc2D（点采样、切线采样、弧长近似）
- **Geometry2D**：几何计算（距离、角度、相交）

### 设计原则

基础向量/矩阵使用 .NET 和 Godot 内置工具，游戏特定轨迹数学自建。

## 3. 职责边界

| Math 做 | Math 不做 |
| ---- | ---- |
| 数学公式和曲线计算 | 物理引擎调用 |
| 几何计算 | AI 路径规划 |
