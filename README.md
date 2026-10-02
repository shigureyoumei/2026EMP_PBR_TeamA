# PBR — Collaborative Fish-Catching Experiment

[English](#english) | [中文](#中文) | [日本語](#日本語)

## English

### Overview

This Unity research prototype is based on the proposal **Responsibility-Aware Adaptive Assistance in Large-Area Collaborative Visual Search**. It explores responsibility allocation, head-direction-based monitoring estimates, directional cues, and temporary task handoffs between two participants.

The current version provides a continuous four-wall 3D reef and a configurable projection room. The school contains ten persistent fish by default. Fish swim, accelerate, hide behind physical cover, and remain stationary, with gradual turns and movement at different depths and heights. Capturing a fish awards a score without removing it; it becomes eligible for scoring again after three seconds. The same fish remain before, during, and after an experimental block.

The reef contains rocks, caves, natural arches, dense aquatic plants, and fan corals. Background detail has three levels. Fish move through one surrounding 3D environment and cross neighboring wall views continuously. Cover blocks capture rays as well as visibility.

### Open and run

1. Clone this repository and open its root folder in Unity Hub. The root contains `Assets`, `Packages`, and `ProjectSettings`; the current local path is `D:\projects\2026EMP_PBR_TeamA`.
2. Use **Unity 6000.6.3f1 (Unity 6.6)**. The project uses **URP 17.6.0** and **Splines 2.9.1**. Allow Unity to restore packages and rebuild its cache.
3. Open `Assets/PBR/Scenes/FishAttention360.unity`, the experiment scene included in this repository.
4. Press Play, enter an anonymous pair ID, choose an experimental condition, and select START. A block lasts 90 seconds by default.
5. Orange fish belong to participant A, cyan fish to B, and gray fish are distractors. A white marker identifies a target eligible for temporary handoff; other A/B targets retain their original responsibility.

Local executables such as `BuildsRoom/PBRRoom.exe`, Unity caches, backups, and the separate validation project are excluded from Git. A fresh clone can run directly in the Unity Editor.

| Control | Action |
| --- | --- |
| Mouse / left click | Aim / capture for the selected participant |
| Tab | Switch between A and B |
| F / J | Trigger A's / B's capture button |
| Arrow keys | Adjust the other participant's direction |
| Esc | Abort the current block |
| 0 | Four-wall desktop preview |
| 1–4 | Front, right, back, or left wall |
| 5 | Continuous strip: front → right → back → left |

These are shared-computer debugging controls, not the final two-person hardware interface. Head direction is a proxy for monitoring direction; it is not eye tracking or a measurement of gaze.

### Experiment and fish settings

Select `Assets/PBR/Generated360/PilotConfig.asset` in the Inspector. Stop Play before changing persistent settings; restart Play to apply fish-count and background-detail changes.

| Parameter | Default | Meaning |
| --- | --- | --- |
| `fishCount` | 10 | Persistent school size |
| `swimSpeed` / `burstSpeed` | 2.4 / 6 | Normal / accelerated speed, world units per second |
| `acceleration` | 3 | Acceleration, world units per second squared |
| `captureRecoverySeconds` | 3 | Delay before a captured fish can score again |
| `backgroundDetail` | 2 | 0 = low, 1 = medium, 2 = high |
| `durationSeconds` | 90 | Block duration in seconds |
| `monitoringHalfAngle` | 25 | Head-direction monitoring sector half-angle, degrees |
| `cueDelay` | 1 | Coverage-gap duration before an owner cue, seconds |
| `handoffDelay` | 2 | Coverage-gap threshold for handoff, seconds |
| `handoffSeconds` | 4 | Temporary capture permission duration, seconds |
| `workloadThreshold` | 2 | Threshold for visible original targets within the owner's monitoring sector |
| `sampleHz` | 20 | Requested target-sampling rate; actual timestamps are recorded |

The four conditions are fixed responsibility, shared responsibility, owner cues, and adaptive handoff. Original ownership remains unchanged; a temporary recipient and expiry are tracked separately. Each target may be handed off at most once.

These defaults are starting points for pilot calibration, not finalized research parameters. Keep background detail and motion settings consistent across experimental comparisons. Turning and arrival braking mean fish do not always move at their maximum speed.

### Projection room and hardware

Stop Play and choose **PBR > Select projection room settings**, or select **Projection Room** in the Hierarchy. Edit the Inspector and save the scene with Ctrl+S.

| Setting | Initial value |
| --- | --- |
| Width (X) / depth (Z) / height | 25 / 15 / 7.7 m |
| Observer eye height | 1.6 m; adjust for the participant |
| Tester offset (X / Z) | 0 / 0 m, the floor center |
| Projection bottom / top margin | 0 / 0 m; confirm active screen boundaries on site |

The dimensions are a published LargeSpace preset, not an on-site measurement. Four cameras compute off-axis projections from the wall boundaries and observer position. Desktop previews preserve wall proportions. Scene-view guides show room boundaries and the observer without obscuring the reef. The optional `-pbr-four-displays` player argument maps front/right/back/left to display indices 0–3; fewer than four displays fall back to desktop preview.

The current model approximates the room with four planes. Actual curved surfaces, projector mapping, warping, edge blending, stereo/floor projection, and physical output remain to be calibrated. Four logical wall cameras do not imply four physical projectors. See [Projection room instructions and sources](Documentation/ProjectionRoom.md).

`ProjectionRoom.TryWallHit(Ray, out wall, out point, out uv)` accepts a ray in physical room coordinates and returns the nearest valid wall intersection. The origin is the floor center and units are meters. This prepares for position-plus-direction input from head and handheld sensors; sensor hardware and the net-launch animation are not yet integrated.

Existing main-thread input adapters are `ParticipantInput.SubmitOrientation(player, yawDegrees, pitchDegrees)` and `SubmitButton(player)`, with player 0/1. `CalibrateForward(player)` makes the current external yaw the forward reference. External input becomes invalid after 0.5 seconds without an update; capture is disabled, and missing input is not interpreted as inattention. Serial/UDP worker threads must queue samples for the main thread.

Both participants currently share one viewpoint and a public debugging HUD. Private owner-only cues require headphones, controller feedback, or another private channel. Different participant positions must also be included in formal calibration.

### Logs and analysis

Sessions are written to `Application.persistentDataPath/PBRLogs/<UTC_timestamp_uniqueID>`. On Windows, the usual location is `%USERPROFILE%\AppData\LocalLow\DefaultCompany\PBR\PBRLogs`; the runtime `LastLogFolder` value gives the actual path.

| File | Contents |
| --- | --- |
| `session.json` | Anonymous pair ID, block, condition, configuration snapshot, Unity version, and UTC start |
| `events.csv` | Appearance, occlusion, reappearance, motion, buttons, capture errors, cues, handoffs, missed/censored targets, and block end |
| `orientation.csv` | Continuous directions for both participants, input source, and validity, independent of fish count |
| `samples.csv` | Active fish positions and angles, ownership, monitoring coverage, center-ray visibility, handoff recipient, workload, and gap timing |

Relative times use Unity's real-time monotonic clock and include actual frame numbers. CSV numbers use decimal points. Files flush every second and close on normal completion; storage failure stops the block. Treat `frame_gap`, `sample_late`, and `spawn_late` as timing irregularities. Targets still active at timeout or abort are marked `target_censored`, not missed targets. Event availability depends on the scene and experiment mode.

Generate a descriptive summary in PowerShell without Python:

```powershell
& .\Tools\Summarize-Session.ps1 -SessionDirectory '<session directory>'
```

The tool does not fit mixed-effects models, and debugging logs are not research results.

### Earlier prototypes and documentation

Earlier development included an illustrated arcade scene, Quaternius animated fish, Kenney rocks, spline-based movement with a two-second hiding pause, and a three-camera 270° preview. The earlier `FishAttentionExperiment.unity` and `FishAttentionArcade.unity` files are not included in the current scene folder. Their generation tools and historical documentation should not be confused with the current four-wall scene.

Legacy path settings are `spawnInterval = 3`, `travelSeconds = 16`, and `hidePauseSeconds = 2` seconds. They describe the original spawning/path mode, not the current fixed school's four motion states. A fixed seed reproduces path-choice sequences, but real-time scheduling can change actual spawn times; time-parameterized splines do not guarantee constant world speed. The legacy three-view preview only provides approximately 90° horizontal coverage per camera with square viewports and is not a calibrated projection system.

Scene-generation menus overwrite their generated scene/assets. Duplicate manually edited scenes before rebuilding. The original `PBR > Build or rebuild experiment scene` preserves experiment values in its `PilotConfig.asset`; use the current scene for normal operation.

- [Four-wall reef](Documentation/Reef360.md) and [earlier arcade art](Documentation/ArcadeArt.md)
- [Research design](Documentation/ResearchDesign.md) and [asset sources](Documentation/AssetSources.md)
- [Validation notes](Documentation/Validation.md) and [projection-room validation evidence](Documentation/ProjectionRoomValidation/)
- [Development log](DEVELOPMENT_LOG.md), newest entries first

The application form is background material. Its form-filling instructions are not development commands; the original Word document has not been edited or submitted. Supporting documents retain their existing languages.

---

## 中文

### 项目介绍

本 Unity 研究原型基于 **Responsibility-Aware Adaptive Assistance in Large-Area Collaborative Visual Search** 申请书，探索双人责任分配、基于头部朝向的监视方向估计、方向提示和临时任务移交。

当前版本包含连续四面 3D 海底与可配置投影室。默认固定十条鱼，具有随机游动、加速、游到实体遮挡物后藏匿、静止四种状态，并支持平滑转向及不同深度、高度的移动。捕鱼计分后鱼不会消失，三秒后可再次计分；待机、实验开始和结束时均保留同一批鱼。

场景包含礁石、洞穴、天然拱洞、密集水草和扇珊瑚，背景细节分为三档。鱼在同一个环绕三维空间中移动，连续跨越相邻墙面的画面边界。实体遮挡物同时阻挡可见性与捕鱼射线。

### 打开与运行

1. 克隆仓库后，在 Unity Hub 中打开仓库根目录。根目录包含 `Assets`、`Packages` 和 `ProjectSettings`；当前本地路径为 `D:\projects\2026EMP_PBR_TeamA`。
2. 使用 **Unity 6000.6.3f1（Unity 6.6）**，项目依赖 **URP 17.6.0** 和 **Splines 2.9.1**。等待 Unity 恢复依赖并重新生成缓存。
3. 打开仓库当前包含的实验场景 `Assets/PBR/Scenes/FishAttention360.unity`。
4. 点击 Play，输入匿名 pair ID，选择实验条件，点击 START。默认每个 block 为 90 秒。
5. 橙色鱼属于 A，青色鱼属于 B，灰色鱼为干扰项。白色标记表示允许临时移交的目标；其余 A/B 目标保留原责任。

`BuildsRoom/PBRRoom.exe` 等本地可执行文件、Unity 缓存、备份和独立验证工程不随 Git 上传。新克隆的工程可直接在 Unity 编辑器中运行。

| 操作 | 功能 |
| --- | --- |
| 鼠标 / 左键 | 控制选中参与者瞄准 / 捕鱼 |
| Tab | 切换 A/B |
| F / J | 触发 A/B 的捕鱼按钮 |
| 方向键 | 调整另一名参与者的方向 |
| Esc | 中止当前 block |
| 0 | 四面桌面预览 |
| 1–4 | 前、右、后、左单面预览 |
| 5 | 连续长条：前 → 右 → 后 → 左 |

以上是同机键鼠调试操作，并非正式双人硬件接口。头部朝向只是监视方向的代理量，不是眼动或注视测量。

### 实验与鱼群参数

在 Inspector 中选择 `Assets/PBR/Generated360/PilotConfig.asset`。停止 Play 后修改需保留的设置；鱼群数量和背景细节修改后重新进入 Play 生效。

| 参数 | 默认值 | 含义 |
| --- | --- | --- |
| `fishCount` | 10 | 固定鱼群数量 |
| `swimSpeed` / `burstSpeed` | 2.4 / 6 | 普通 / 加速速度，世界单位/秒 |
| `acceleration` | 3 | 加速度，世界单位/秒² |
| `captureRecoverySeconds` | 3 | 捕鱼后再次计分的等待秒数 |
| `backgroundDetail` | 2 | 0 低、1 中、2 高 |
| `durationSeconds` | 90 | block 时长，秒 |
| `monitoringHalfAngle` | 25 | 头部监视扇区半角，度 |
| `cueDelay` | 1 | 提示责任人前的覆盖缺口持续秒数 |
| `handoffDelay` | 2 | 触发移交的覆盖缺口阈值，秒 |
| `handoffSeconds` | 4 | 临时捕获权限时长，秒 |
| `workloadThreshold` | 2 | 可见且在责任人监视扇区内的原责任目标数量阈值 |
| `sampleHz` | 20 | 请求的目标采样频率，记录实际时间 |

实验条件包括固定责任、共享责任、责任人提示、自适应移交。原责任人不变，临时接收者和到期时间独立记录；单个目标最多移交一次。

上述默认值是待预实验校准的起点，不是已确定的正式实验参数。条件对比中应固定背景细节与运动设置。鱼在转弯和到达目的地前会减速，不会始终以最大速度移动。

### 投影室与硬件

停止 Play 后，选择 **PBR > Select projection room settings**，或在 Hierarchy 中选择 **Projection Room**。在 Inspector 修改参数，再按 Ctrl+S 保存场景。

| 设置 | 初始值 |
| --- | --- |
| 宽度 X / 深度 Z / 高度 | 25 / 15 / 7.7 米 |
| 观察眼高 | 1.6 米，按受试者调整 |
| 站位偏移 X / Z | 0 / 0 米，即地面中心 |
| 投影下沿距地 / 上沿距顶 | 0 / 0 米，现场确认有效投影边界 |

尺寸来自 LargeSpace 公开资料，尚未现场测量。四台相机根据墙面边界和观察点计算离轴透视，桌面预览保留墙面宽高比例。Scene 辅助线显示房间边界与观察点，不遮挡海底。运行程序时可选用 `-pbr-four-displays` 参数，将前、右、后、左映射到显示器索引 0–3；不足四个显示器时退回桌面预览。

目前使用四平面近似。实际曲面、投影机映射、畸变校正、边缘融合、立体/地面投影和物理输出仍需现场标定。四台逻辑墙面相机不代表四台实体投影机。详见[投影室操作与尺寸来源](Documentation/ProjectionRoom.md)。

`ProjectionRoom.TryWallHit(Ray, out wall, out point, out uv)` 接受物理房间坐标系内的射线，返回最近的有效墙面交点。原点为地面中心，单位为米。这为后续头部与手持设备的“位置＋方向”输入提供基础；尚未接入传感器硬件或实现发射渔网动画。

现有主线程输入接口为 `ParticipantInput.SubmitOrientation(player, yawDegrees, pitchDegrees)` 和 `SubmitButton(player)`，player 为 0/1。`CalibrateForward(player)` 将当前外部 yaw 设为正前方参考。超过 0.5 秒无外部更新时输入失效，禁止捕鱼，也不会将缺失输入视为责任人失察。串口/UDP 工作线程须通过队列向主线程提交样本。

当前两人共用一个观察点和公共调试 HUD。“仅提示责任人”需要独立耳机、控制器反馈或其他私有通道；正式试验也需将两名参与者的位置差异纳入标定。

### 日志与分析

输出目录为 `Application.persistentDataPath/PBRLogs/<UTC时间_唯一ID>`。Windows 通常为 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\PBR\PBRLogs`；运行时 `LastLogFolder` 提供实际路径。

| 文件 | 内容 |
| --- | --- |
| `session.json` | 匿名 pair ID、block、条件、配置快照、Unity 版本与 UTC 起点 |
| `events.csv` | 出现、遮挡、再次出现、运动、按钮、捕获错误、提示、移交、遗漏/删失目标及 block 结束 |
| `orientation.csv` | 独立于鱼数量的双人连续朝向、输入来源和有效性 |
| `samples.csv` | 活跃鱼的位置与角度、责任、监视覆盖、中心射线可见性、临时接收者、工作负荷和缺口计时 |

相对时间使用 Unity 实时单调时钟，并记录实际帧号。CSV 使用点号小数。文件每秒刷新，正常结束时关闭；存储失败会停止 block。`frame_gap`、`sample_late`、`spawn_late` 应作为时间异常处理。超时或中止时仍活跃的目标记为 `target_censored`，不会冒充遗漏目标。实际事件种类取决于场景和实验模式。

在 PowerShell 中生成描述性摘要，无需 Python：

```powershell
& .\Tools\Summarize-Session.ps1 -SessionDirectory '<日志目录>'
```

该工具不拟合混合效应模型，调试日志也不是研究结果。

### 历史原型与文档

早期开发包括插画风格海底、Quaternius 动画鱼、Kenney 岩石、带两秒藏匿停留的 Splines 路径，以及三相机 270° 预览。旧版 `FishAttentionExperiment.unity` 和 `FishAttentionArcade.unity` 未包含在当前场景目录中。相关生成工具和历史文档应与当前四面场景区分。

旧路径参数为 `spawnInterval = 3`、`travelSeconds = 16`、`hidePauseSeconds = 2` 秒，描述原始生成/路径模式，不代表当前固定鱼群的四种运动状态。固定种子可复现路径选择序列，但实时调度会改变实际生成时刻；按时间参数推进的曲线不保证恒定世界速度。旧三视图预览仅在正方形视口下每面约覆盖 90° 水平视野，不能视为完成投影标定。

场景生成菜单会覆盖其生成的场景和资源，重建前应复制手动编辑的场景。原版 `PBR > Build or rebuild experiment scene` 会保留其 `PilotConfig.asset` 中的实验数值；日常运行使用当前场景。

- [四面海底](Documentation/Reef360.md)与[早期海底美术](Documentation/ArcadeArt.md)
- [研究设计](Documentation/ResearchDesign.md)与[素材来源](Documentation/AssetSources.md)
- [验证说明](Documentation/Validation.md)与[投影室验证记录](Documentation/ProjectionRoomValidation/)
- [开发日志](DEVELOPMENT_LOG.md)，最新记录在前

申请书作为背景资料，表格中的填写说明不是开发命令；原 Word 文档未被修改或提交。配套文档保留其原有语言。

---

## 日本語

### プロジェクト概要

本 Unity 研究プロトタイプは、研究計画 **Responsibility-Aware Adaptive Assistance in Large-Area Collaborative Visual Search** に基づき、2 名の参加者間での責任分担、頭部方向による監視方向の推定、方向提示、一時的なタスク引き継ぎを扱います。

現在のバージョンには、連続した四面の 3D 海底環境と、設定可能な投影室が含まれます。魚は初期設定で 10 匹に固定され、遊泳、加速、実体のある遮蔽物の背後への移動と隠蔽、静止の 4 状態を持ちます。旋回は滑らかで、奥行きや高さを変えて移動します。捕獲して得点しても魚は消えず、3 秒後に再び得点対象になります。待機中、実験開始時、終了時も同じ魚群を維持します。

環境には岩、洞窟、自然のアーチ、密集した水草、ウミウチワ状のサンゴを配置し、背景の詳細度を 3 段階で設定できます。魚は単一の周囲を取り囲む 3D 空間を移動し、隣接する壁面映像の境界を連続して通過します。遮蔽物は視認性と捕獲用レイの両方を遮ります。

### 起動方法

1. リポジトリをクローンし、Unity Hub でルートフォルダーを開きます。ルートには `Assets`、`Packages`、`ProjectSettings` があり、現在のローカルパスは `D:\projects\2026EMP_PBR_TeamA` です。
2. **Unity 6000.6.3f1（Unity 6.6）** を使用します。依存パッケージは **URP 17.6.0** と **Splines 2.9.1** です。パッケージの復元とキャッシュの再生成を待ちます。
3. このリポジトリに含まれる実験シーン `Assets/PBR/Scenes/FishAttention360.unity` を開きます。
4. Play を押し、匿名の pair ID と実験条件を設定して START を選択します。1 ブロックの初期設定は 90 秒です。
5. オレンジ色の魚は A、水色の魚は B の担当で、灰色の魚は妨害刺激です。白いマーカーは一時的な引き継ぎが可能な対象を示し、それ以外の A/B 対象は元の担当を維持します。

`BuildsRoom/PBRRoom.exe` などのローカル実行ファイル、Unity キャッシュ、バックアップ、独立した検証用プロジェクトは Git に含まれません。クローン後は Unity Editor から直接実行できます。

| 操作 | 機能 |
| --- | --- |
| マウス / 左クリック | 選択中の参加者の照準 / 捕獲 |
| Tab | A/B の切り替え |
| F / J | A/B の捕獲ボタン |
| 矢印キー | もう一方の参加者の方向調整 |
| Esc | 現在のブロックを中止 |
| 0 | 四面のデスクトッププレビュー |
| 1–4 | 前、右、後、左の単面表示 |
| 5 | 連続した横長表示：前 → 右 → 後 → 左 |

これらは 1 台の PC でのデバッグ操作であり、正式な 2 名用のハードウェア入力ではありません。頭部方向は監視方向の代理指標であり、眼球運動や注視を測定するものではありません。

### 実験と魚群の設定

Inspector で `Assets/PBR/Generated360/PilotConfig.asset` を選択します。保存する設定は Play を停止してから変更してください。魚の数と背景詳細度の変更は Play を再開すると反映されます。

| パラメーター | 初期値 | 意味 |
| --- | --- | --- |
| `fishCount` | 10 | 固定する魚の数 |
| `swimSpeed` / `burstSpeed` | 2.4 / 6 | 通常時 / 加速時の速度、ワールド単位/秒 |
| `acceleration` | 3 | 加速度、ワールド単位/秒² |
| `captureRecoverySeconds` | 3 | 捕獲後、再び得点可能になるまでの秒数 |
| `backgroundDetail` | 2 | 0 = 低、1 = 中、2 = 高 |
| `durationSeconds` | 90 | ブロックの長さ、秒 |
| `monitoringHalfAngle` | 25 | 頭部方向に基づく監視領域の半角、度 |
| `cueDelay` | 1 | 担当者への提示までの未カバー継続時間、秒 |
| `handoffDelay` | 2 | 引き継ぎを開始する未カバー時間の閾値、秒 |
| `handoffSeconds` | 4 | 一時的な捕獲権限の有効時間、秒 |
| `workloadThreshold` | 2 | 担当者の監視領域内で視認可能な元の担当対象数の閾値 |
| `sampleHz` | 20 | 要求する対象サンプリング周波数。実際の時刻も記録 |

実験条件は、固定責任、共有責任、担当者への提示、適応的な引き継ぎの 4 種類です。元の担当者は変更せず、一時的な引き継ぎ先と有効期限を別々に記録します。各対象の引き継ぎは最大 1 回です。

初期値は予備実験で調整するための出発点であり、正式な実験条件として確定した値ではありません。条件比較では背景詳細度と運動設定を統一してください。魚は旋回時や目的地への到着前に減速するため、常に最高速度で移動するわけではありません。

### 投影室とハードウェア

Play を停止し、**PBR > Select projection room settings** を選択するか、Hierarchy の **Projection Room** を選択します。Inspector で変更し、Ctrl+S でシーンを保存します。

| 設定 | 初期値 |
| --- | --- |
| 幅 X / 奥行き Z / 高さ | 25 / 15 / 7.7 m |
| 観察者の眼高 | 1.6 m。参加者に合わせて調整 |
| 立ち位置のオフセット X / Z | 0 / 0 m、床面の中心 |
| 投影下端の床からの高さ / 上端の天井からの余白 | 0 / 0 m。有効投影範囲は現地で確認 |

寸法は LargeSpace の公開資料に基づく初期設定であり、現地で実測した値ではありません。4 台のカメラが壁面境界と観察位置からオフアクシス投影を計算し、デスクトップ表示では壁面の縦横比を維持します。Scene ビューの補助線は海底を遮らずに室内境界と観察点を示します。実行ファイルの `-pbr-four-displays` オプションは、前・右・後・左をディスプレイ番号 0–3 に割り当てます。4 台未満の場合はデスクトップ表示に戻ります。

現時点では 4 平面による近似です。実際の曲面、プロジェクターの割り当て、歪み補正、エッジブレンディング、立体・床面投影、実機出力は現地での調整が必要です。4 台の論理的な壁面カメラは、実際のプロジェクター 4 台を意味しません。[投影室の操作方法と寸法の出典](Documentation/ProjectionRoom.md)を参照してください。

`ProjectionRoom.TryWallHit(Ray, out wall, out point, out uv)` は物理的な室内座標系のレイを受け取り、最も近い有効な壁面交点を返します。原点は床面中心で、単位はメートルです。頭部・手持ちセンサーの「位置＋方向」入力に向けた基盤ですが、センサーハードウェアと網を発射するアニメーションは未実装です。

既存のメインスレッド用入力インターフェースは `ParticipantInput.SubmitOrientation(player, yawDegrees, pitchDegrees)` と `SubmitButton(player)` で、player は 0/1 です。`CalibrateForward(player)` は現在の外部 yaw を正面の基準にします。外部入力が 0.5 秒以上更新されないと無効となり、捕獲を禁止します。入力欠損を不注意とは解釈しません。シリアル/UDP のワーカースレッドはキューを介してメインスレッドにサンプルを渡す必要があります。

現在は 2 名で単一の視点と公開デバッグ HUD を共有します。担当者だけに提示するには、個別のヘッドホン、コントローラーのフィードバック、その他の専用経路が必要です。正式な実験では、参加者間の位置の違いも校正に含める必要があります。

### ログと分析

出力先は `Application.persistentDataPath/PBRLogs/<UTC時刻_一意ID>` です。Windows では通常 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\PBR\PBRLogs` で、実行時の `LastLogFolder` から実際の場所を取得できます。

| ファイル | 内容 |
| --- | --- |
| `session.json` | 匿名 pair ID、ブロック、条件、設定スナップショット、Unity バージョン、UTC 開始時刻 |
| `events.csv` | 出現、遮蔽、再出現、運動、ボタン、捕獲エラー、提示、引き継ぎ、見逃し・打ち切り対象、ブロック終了 |
| `orientation.csv` | 魚の数に依存しない 2 名の連続した方向、入力元、有効性 |
| `samples.csv` | 活動中の魚の位置・角度、担当、監視範囲、中心レイによる可視性、引き継ぎ先、負荷、未カバー時間 |

相対時刻は Unity の実時間単調クロックを使い、実際のフレーム番号も記録します。CSV の小数点はピリオドです。ファイルは毎秒フラッシュし、正常終了時に閉じます。保存に失敗するとブロックを停止します。`frame_gap`、`sample_late`、`spawn_late` は時刻上の不規則性として扱ってください。時間切れや中止時に活動中の対象は `target_censored` とし、見逃し扱いにはしません。記録されるイベントはシーンと実験モードによって異なります。

Python を使わずに PowerShell で記述的な集計を生成できます。

```powershell
& .\Tools\Summarize-Session.ps1 -SessionDirectory '<ログディレクトリ>'
```

このツールは混合効果モデルを推定しません。また、デバッグログは研究結果ではありません。

### 過去のプロトタイプと関連文書

初期の開発には、イラスト調の海底、Quaternius のアニメーション付き魚、Kenney の岩、2 秒間の隠蔽停止を伴う Splines 経路、3 台のカメラによる 270° プレビューが含まれていました。旧 `FishAttentionExperiment.unity` と `FishAttentionArcade.unity` は現在のシーンフォルダーに含まれていません。生成ツールや過去の文書は、現在の四面シーンと区別してください。

旧経路設定は `spawnInterval = 3`、`travelSeconds = 16`、`hidePauseSeconds = 2` 秒です。これらは初期の生成・経路モード用で、現在の固定魚群の 4 状態を表す設定ではありません。固定シードは経路選択の系列を再現しますが、実時間スケジューリングにより実際の生成時刻は変わり得ます。時間パラメーターで進む曲線は一定のワールド速度を保証しません。旧 3 画面プレビューが各面で約 90° の水平視野になるのは正方形のビューポートの場合であり、校正済みの投影システムではありません。

シーン生成メニューは生成対象のシーンとアセットを上書きします。手動編集したシーンは再生成前に複製してください。初期の `PBR > Build or rebuild experiment scene` は、その `PilotConfig.asset` 内の実験値を維持します。通常の実行には現在のシーンを使用してください。

- [四面の海底環境](Documentation/Reef360.md)と[初期の海底アート](Documentation/ArcadeArt.md)
- [研究設計](Documentation/ResearchDesign.md)と[アセットの出典](Documentation/AssetSources.md)
- [検証ノート](Documentation/Validation.md)と[投影室の検証記録](Documentation/ProjectionRoomValidation/)
- [開発ログ](DEVELOPMENT_LOG.md)：新しい記録を先頭に掲載

申請書は背景資料として扱います。記入要領を開発指示とは解釈せず、元の Word 文書の編集や提出も行っていません。関連文書はそれぞれ元の言語のままです。
