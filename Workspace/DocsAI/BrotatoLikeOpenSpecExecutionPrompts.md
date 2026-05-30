# BrotatoLike OpenSpec 执行提示词

> 更新日期：2026-05-21  
> 目的：新开对话时，直接复制其中一个提示词，让 AI 按对应 OpenSpec change 执行。  
> 当前计划提交：`0f08624 OpenSpec: plan remaining BrotatoLike migration work`  
> 游戏基线提交：`9fa6ab4 Fix respawn HUD and health bar migration gaps`

## 使用方式

每次新对话只执行一个 change。先复制“通用前置提示词”，再追加一个具体 change 提示词。不要一次让新对话执行全部 10 个 change，否则验证范围和 git 边界会混乱。

推荐顺序：

1. `stabilize-brotatolike-release-batch`
2. `validate-brotatolike-dash-main-skill`
3. `restore-brotatolike-chain-lightning-line-vfx`
4. `expand-brotatolike-skill-loadout`
5. `validate-brotatolike-projectile-and-passive-skills`
6. `design-brotatolike-levelup-choice-loop`
7. `design-brotatolike-shop-item-loop`
8. `complete-brotatolike-wave-run-flow`
9. `brotatolike-character-selection`
10. `brotatolike-manual-device-qa`

## 通用前置提示词

```text
你现在在 /home/slime/Code/SlimeAI 工作区执行 BrotatoLike OpenSpec change。

必须遵守：
- 默认中文回答；命令、代码、错误保留原文。
- 先读 /home/slime/Code/SlimeAI/AGENTS.md。
- 先读 Workspace/SystemAgent/README.md、Workspace/SystemAgent/INDEX.md。
- 先读 Games/BrotatoLike/DocsAI/INDEX.md、GameProjectState.md、MigrationLedger.md、UnfinishedMigrationHandoff.md、ValidationCatalog.md。
- 先进入对应 git 边界运行 git status --short；不要混入已有无关改动。
- 不要直接改 Games/BrotatoLike/SlimeAI/ submodule 里的框架代码；框架改动要去 /home/slime/Code/SlimeAI/SlimeAI。
- 修改前先读相关文件；修改后总结改动和验证结果。
- 能验证就跑 build/scene/OpenSpec validate；不能验证必须说明原因。
- Godot scene gate 必须检查 index.json、result.json 和 artifact；expectedInputs、expectedObservations、passCriteria、failCriteria、artifactPath 必须非空。
- 每完成 tasks.md 中一项就更新对应 checkbox。
- 完成后按影响边界分别提交；push 需要我明确确认。

执行流程：
1. cd /home/slime/Code/SlimeAI
2. 读取对应 openspec/changes/<change>/proposal.md、design.md、specs/**/spec.md、tasks.md。
3. 运行 openspec instructions apply --change "<change>" --json 并读取返回的 contextFiles。
4. 按 tasks.md 从上到下执行，不要跳过验证。
5. 更新相关 DocsAI 和 OpenSpec tasks。
6. 运行 openspec validate <change> --strict。
7. 如果 change 完成，按 OpenSpec archive 流程归档并提交；如果未完成，明确剩余任务和阻塞。
```

## 1. Release Batch 稳定化

```text
请执行 OpenSpec change：stabilize-brotatolike-release-batch

目标：
- 重跑 BrotatoLike release-batch，生成新的 gate-report.json。
- 复验历史 blocker：PlayableUX 和 Progression。
- 如果仍失败，修复实际代码或 validation oracle，不要只改文档。
- 更新 GameProjectState、ValidationCatalog、UnfinishedMigrationHandoff 中的最新验证路径和剩余风险。

重点读取：
- openspec/changes/stabilize-brotatolike-release-batch/*
- Games/BrotatoLike/DocsAI/ValidationManifest.json
- Games/BrotatoLike/DocsAI/ValidationCatalog.md
- Games/BrotatoLike/Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidationScene.cs
- Games/BrotatoLike/Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidationScene.cs
- Games/BrotatoLike/Src/Game/Progression/BrotatoLikeProgressionService.cs

必须验证：
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run-all --manifest DocsAI/ValidationManifest.json --release-batch --continue-on-fail --log-dir .ai-temp/scene-tests/runs --errors-only
Tools/analyze-godot-scene-logs.sh --run-dir <new-run-dir> --manifest DocsAI/ValidationManifest.json --gate-report <new-run-dir>/gate-report.json

完成条件：
- 新 gate-report.json 路径写入 DocsAI。
- 所有失败场景都有明确修复或明确保留 blocker。
- openspec validate stabilize-brotatolike-release-batch --strict 通过。
```

## 2. Dash 玩家技能主路径验收

```text
请执行 OpenSpec change：validate-brotatolike-dash-main-skill

目标：
- 验证玩家通过正式技能栏选择 dash，并用 UseSkill 触发。
- artifact 记录 selected skill id、释放前后玩家位置、Dash 距离、冷却、UI 状态。
- 验证 Dash 与暂停、死亡/复活、输入 adapter 重新绑定不冲突。

重点读取：
- openspec/changes/validate-brotatolike-dash-main-skill/*
- Games/BrotatoLike/Src/Game/BrotatoLikeGameRuntime.cs
- Games/BrotatoLike/Src/Game/GodotActiveSkillInputComponent.cs
- Games/BrotatoLike/Src/Game/BrotatoLikeAbilityHandlers.cs
- Games/BrotatoLike/Src/Game/BrotatoLikePlayableSliceAcceptance.cs
- Games/BrotatoLike/Src/Validation/Game/GameLifecycle/BrotatoLikeGameplayLifecycleValidationScene.cs
- Games/BrotatoLike/DataOS/Authoring/BrotatoLike.seed.sql

必须验证：
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/GameLifecycle/BrotatoLikeGameplayLifecycleValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh

完成条件：
- Dash 不是只靠 handler direct call，而是通过玩家 input action 路径触发。
- scene artifact 五字段非空。
- MigrationLedger 和 UnfinishedMigrationHandoff 更新 Dash 状态。
```

## 3. Chain Lightning 连线 VFX

```text
请执行 OpenSpec change：restore-brotatolike-chain-lightning-line-vfx

目标：
- 让 Chain Lightning 每段弹跳生成真正连接 source/target 的 Line2D。
- 不要把 BrotatoLike 专属 LightningLineEffect 硬编码进框架通用 spawner。
- artifact 记录 scene path、source/target entity id、start/end position、Line2D points、duration、cleanup。

重点读取：
- openspec/changes/restore-brotatolike-chain-lightning-line-vfx/*
- Games/BrotatoLike/Src/Game/BrotatoLikeAbilityHandlers.cs
- Games/BrotatoLike/Src/Game/VFX/LightningLineEffect.cs
- Games/BrotatoLike/Scenes/VFX/LightningLineEffect.tscn
- Games/BrotatoLike/DataOS/Authoring/BrotatoLike.seed.sql
- Games/BrotatoLike/SlimeAI/GameOS/GodotBridge/GodotProjectileEffectSpawner.cs 只读参考

必须验证：
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh

完成条件：
- LightningLineEffect.SetLine(from, to) 有真实调用路径或等价绑定路径。
- 多段弹跳 VFX 和清理有结构化证据。
- DocsAI 不再把链电 VFX 标为“仅路径存在”。
```

## 4. 技能 Loadout 扩展

```text
请执行 OpenSpec change：expand-brotatolike-skill-loadout

目标：
- 把默认 loadout 和可获得技能池分开。
- 设计玩家如何获得默认四槽以外技能。
- 支持 deterministic validation loadout，给后续逐技能验收使用。
- UI artifact 区分 visible active slots、total owned abilities、selected ability。

重点读取：
- openspec/changes/expand-brotatolike-skill-loadout/*
- Games/BrotatoLike/DataOS/Authoring/BrotatoLike.seed.sql
- Games/BrotatoLike/Src/Game/BrotatoLikeGameRuntime.cs
- Games/BrotatoLike/Src/Game/GodotActiveSkillInputComponent.cs
- Games/BrotatoLike/Src/Game/UI/ActiveSkillBarUI.cs
- Games/BrotatoLike/Src/Game/UI/ActiveSkillSlotUI.cs
- Games/BrotatoLike/Scenes/UI/ActiveSkillBarUI.tscn
- Games/BrotatoLike/Scenes/UI/ActiveSkillSlotUI.tscn

必须验证：
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh

完成条件：
- SpawnPlayer 不再散落硬编码所有 loadout 规则。
- 非默认技能有明确 skill pool 或 validation loadout 来源。
- 文档明确哪些技能“可获得”、哪些只是 handler/data 存在。
```

## 5. 投射物和被动技能逐技能验收

```text
请执行 OpenSpec change：validate-brotatolike-projectile-and-passive-skills

目标：
- 逐技能验证 sine_wave_shot、boomerang_throw、bezier_shot、parabola_shot、arc_shot。
- 逐技能验证 orbit_skill、circle_damage、aura_shield。
- 每个技能 artifact 记录 trigger result、scene path、movement mode、命中、生命周期、cleanup。
- 使用 deterministic validation loadout，不依赖随机升级或商店。

重点读取：
- openspec/changes/validate-brotatolike-projectile-and-passive-skills/*
- Games/BrotatoLike/DataOS/Authoring/BrotatoLike.seed.sql
- Games/BrotatoLike/Src/Game/BrotatoLikeAbilityHandlers.cs
- Games/BrotatoLike/Src/Game/GodotActiveSkillInputComponent.cs
- Games/BrotatoLike/DocsAI/MigrationLedger.md
- Resources/Else/brotato-my/Data/Data/Ability/**/*

建议新增或扩展：
- Games/BrotatoLike/Src/Validation/Game/Skills/*
- Games/BrotatoLike/DocsAI/ValidationCatalog.md
- Games/BrotatoLike/DocsAI/ValidationManifest.json（若纳入 release-batch）

必须验证：
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run <new-or-existing-skill-validation-scene> --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh

完成条件：
- 每个技能都有按 ability id 分组的 pass/fail evidence。
- DataOS-only、handler-only、playable-complete 状态在 MigrationLedger 中分清。
- openspec validate validate-brotatolike-projectile-and-passive-skills --strict 通过。
```

## 6. 升级三选一和经验条

```text
请执行 OpenSpec change：design-brotatolike-levelup-choice-loop

目标：
- 实现升级 choice flow：经验到阈值后打开 scene-backed 三选一或固定数量选择。
- 选择后通过 Runtime Data / Feature / Ability / Progression service 应用 gameplay effect。
- 添加正式经验条或 scene-backed experience UI evidence。
- 验证选择期间 pause/half-pause 策略、选择应用、UI 清理。

重点读取：
- openspec/changes/design-brotatolike-levelup-choice-loop/*
- Games/BrotatoLike/Src/Game/Progression/BrotatoLikeProgressionService.cs
- Games/BrotatoLike/Src/Game/UI/BrotatoLikeHud.cs
- Games/BrotatoLike/Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidationScene.cs
- Games/BrotatoLike/Scenes/UI/*
- Resources/Else/brotato-my 里 Level/Upgrade/Choice/Feature 相关旧输入

必须验证：
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh

完成条件：
- Level-up 不再只是 code-created Label。
- artifact 记录 choices shown、selected choice、before/after state。
- DocsAI 更新升级选择和经验 UI 状态。
```

## 7. 商店和道具

```text
请执行 OpenSpec change：design-brotatolike-shop-item-loop

目标：
- 设计并实现最小 item authoring、currency、shop offer、purchase flow。
- 创建 scene-backed shop panel 和 item card。
- 验证 affordable purchase、unaffordable rejection、currency change、item effect、UI cleanup。
- 不做完整 meta progression，不迁所有旧道具。

重点读取：
- openspec/changes/design-brotatolike-shop-item-loop/*
- Games/BrotatoLike/Src/Game/Progression/*
- Games/BrotatoLike/DataOS/Authoring/BrotatoLike.seed.sql
- Games/BrotatoLike/Scenes/UI/*
- Resources/Else/brotato-my 中 Shop/Item/Currency/Rarity/Price/Effect 相关旧输入

建议新增：
- Games/BrotatoLike/Src/Game/Shop/*
- Games/BrotatoLike/Src/Game/Items/*
- Games/BrotatoLike/Scenes/UI/Shop*.tscn
- Games/BrotatoLike/Src/Validation/Game/Shop/*

必须验证：
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run <new-shop-item-validation-scene> --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh

完成条件：
- item 定义、shop offers、purchase result 都有结构化 artifact。
- 购买效果通过 Runtime Data / Feature / Progression 可观察，不是 UI 假状态。
- 文档明确商店/道具第一版范围和未迁旧道具。
```

## 8. 多波 Run Flow

```text
请执行 OpenSpec change：complete-brotatolike-wave-run-flow

目标：
- 让 run flow 至少从第 1 波完整进入第 2 波。
- 定义 wave state machine：preparing/running/completed/reward-or-shop/next-wave。
- 验证 pause、death/respawn、cleanup 和 wave transition。
- 记录 runtime entity/node counts，防止敌人、pickup、projectile、effect 泄漏。

重点读取：
- openspec/changes/complete-brotatolike-wave-run-flow/*
- Games/BrotatoLike/Src/Game/BrotatoLikeEnemySpawnSystem.cs
- Games/BrotatoLike/Src/Game/BrotatoLikeGameRuntime.cs
- Games/BrotatoLike/Src/Game/Progression/BrotatoLikeProgressionService.cs
- Games/BrotatoLike/DataOS/Authoring/BrotatoLike.seed.sql
- Games/BrotatoLike/Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidationScene.cs

建议新增：
- Games/BrotatoLike/Src/Validation/Game/RunFlow/*

必须验证：
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run <new-run-flow-validation-scene> --timeout 15 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh

完成条件：
- 第 1 波开始、完成、奖励/波间阶段、第 2 波开始都有 evidence。
- pause/death/respawn 不破坏 wave flow。
- DocsAI 更新完整多波状态。
```

## 9. 角色选择

```text
请执行 OpenSpec change：brotatolike-character-selection

目标：
- 建立至少两个可选角色的 authoring。
- 创建 scene-backed character select UI。
- 让 runtime 根据 selected character id 生成玩家，而不是固定 deluyi。
- 验证两个角色的视觉/属性/起始技能差异，以及 HUD、输入、血条、技能栏绑定。

重点读取：
- openspec/changes/brotatolike-character-selection/*
- Games/BrotatoLike/assets/Unit/Player/**/*
- Games/BrotatoLike/DataOS/Authoring/BrotatoLike.seed.sql
- Games/BrotatoLike/Src/Game/BrotatoLikeGameRuntime.cs
- Games/BrotatoLike/Src/Game/BrotatoLikeUnitProfiles.cs
- Games/BrotatoLike/Src/Game/UI/BrotatoLikeHud.cs

建议新增：
- Games/BrotatoLike/Scenes/UI/CharacterSelect*.tscn
- Games/BrotatoLike/Src/Validation/Game/CharacterSelection/*

必须验证：
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run <new-character-selection-validation-scene> --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh

完成条件：
- 至少两个角色有 meaningful difference。
- Main 默认 fallback 仍可启动。
- 文档明确角色选择不等于 meta unlock。
```

## 10. 真实设备 QA

```text
请执行 OpenSpec change：brotatolike-manual-device-qa

目标：
- 建立手动设备 QA 文档和记录模板。
- 区分真实键鼠/真实手柄证据和 Godot runner Input.ActionPress 证据。
- 覆盖移动、技能切换、技能释放、point target confirm/cancel、pause/resume、death/respawn、UI focus。
- 如果没有设备，标记 not-tested，不要写 pass。

重点读取：
- openspec/changes/brotatolike-manual-device-qa/*
- Games/BrotatoLike/project.godot
- Games/BrotatoLike/Src/Game/Bridge/BrotatoLikePlayerInputComponent.cs
- Games/BrotatoLike/Src/Game/GodotActiveSkillInputComponent.cs
- Games/BrotatoLike/Src/Game/BrotatoLikeTargetingController.cs
- Games/BrotatoLike/Scenes/UI/*

建议新增：
- Games/BrotatoLike/DocsAI/ManualDeviceQA.md

必须验证：
cd /home/slime/Code/SlimeAI
openspec validate brotatolike-manual-device-qa --strict

如有设备可用，再人工执行 checklist，并记录：
- date
- commit
- OS
- Godot run mode
- device model
- input mapping notes
- pass/fail/not-tested
- failure reproduction steps

完成条件：
- QA 文档能直接指导人工测试。
- 自动 runner evidence 不再被写成真实设备 pass。
- UnfinishedMigrationHandoff 指向 QA 文档。
```

