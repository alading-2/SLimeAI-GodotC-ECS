# ScalarDriver 源码入口

`ScalarDriver` 是 Movement 内部的通用标量驱动器，用于让单个数值随时间推进。AI 执行规则见 `DocsAI/Modules/Movement.md`。

## 适用场景

`OrbitRadius` 扩张/收缩、`WaveAmplitude` 动态变化、`WaveFrequency` 边界模式。

## 核心文件

`ScalarMotion.cs` 包含：`ScalarDriver`、`ScalarDriverParams`、`ScalarDriverState`、`ScalarDriverStepResult`、`ScalarBoundaryResponse`、`ScalarBoundMode`、`ScalarBoundaryHandler`。

## 职责边界

- ScalarDriver 只推进标量值、速度、边界响应和完成状态
- 不直接移动实体，不写 `Data`
- 具体策略决定该标量如何参与轨迹公式

## 当前接入

- `OrbitStrategy`：`OrbitRadiusScalarDriver`
- `SineWaveStrategy`：`WaveAmplitudeScalarDriver`、`WaveFrequencyScalarDriver`

## 使用概要

1. `MovementParams` 中保留基础值字段，可选增加 `ScalarDriverParams?`
2. 策略 `OnEnter` 创建 `ScalarDriverState`，`Update` 调用 `ScalarDriver.Step()`
3. 日志上下文传入 `MoveMode` 和参数名

完整规则见 `DocsAI/Modules/Movement.md`。
