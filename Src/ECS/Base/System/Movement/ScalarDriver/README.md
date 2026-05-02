# ScalarDriver 源码入口

`ScalarDriver` 是 Movement 内部的通用标量驱动器，用于让单个数值随时间推进。完整 Movement 规则见 `DocsAI/Modules/Movement.md`。

## 适用场景

- `OrbitRadius` 随时间扩张、收缩或反弹。
- `WaveAmplitude` 动态变大或衰减。
- `WaveFrequency` 按边界模式往返或冻结。

## 核心文件

- `ScalarMotion.cs`

该文件包含：

- `ScalarDriver`
- `ScalarDriverParams`
- `ScalarDriverState`
- `ScalarDriverStepResult`
- `ScalarBoundaryResponse`
- `ScalarBoundMode`
- `ScalarBoundaryHandler`

## 职责边界

- ScalarDriver 只推进标量值、速度、边界响应和完成状态。
- ScalarDriver 不直接移动实体。
- ScalarDriver 不写 `Data`。
- 具体策略决定该标量如何参与轨迹公式。

## 使用规则

1. `MovementParams` 中保留基础值字段。
2. 可选增加 `ScalarDriverParams?` 字段。
3. 策略 `OnEnter` 创建 `ScalarDriverState`。
4. 策略 `Update` 调用 `ScalarDriver.Step()`。
5. 日志上下文传入 `MoveMode` 和参数名。

## 当前接入点

- `OrbitStrategy`：`OrbitRadiusScalarDriver`
- `SineWaveStrategy`：`WaveAmplitudeScalarDriver`
- `SineWaveStrategy`：`WaveFrequencyScalarDriver`

新增策略若只是驱动一个连续标量，优先复用本目录能力，不要在策略里重复实现边界响应。
