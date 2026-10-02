# PBR 双人协作捕鱼实验

本工程根据《Responsibility-Aware Adaptive Assistance in Large-Area Collaborative Visual Search》申请书，搭建双人责任分配、监视方向估计、方向提示和临时任务移交的 Unity 实验原型。

**当前最新版本：可配置 Large Space 投影室与多样化四面 3D 海底。** 打开 `Assets/PBR/Scenes/FishAttention360.unity` 或运行 `BuildsRoom/PBRRoom.exe`。五类遮挡物包括礁石、洞穴、天然拱洞、密集水草和扇珊瑚；鱼速已提高，背景细节可选择低、中、高三档。仍为固定 10 条鱼，保留前后与高低游动、真实藏匿和平滑转向。`0` 四面预览，`1–4` 单面，`5` 连续长条。配置位于 `Assets/PBR/Generated360/PilotConfig.asset`，详见 `Documentation/Reef360.md`。房间初始尺寸为公开资料中的 25 × 15 × 7.7 米；停止 Play 后，通过 `PBR > Select projection room settings` 修改尺寸、眼高与站位并保存。详见 `Documentation/ProjectionRoom.md`。下文保留先前版本的说明。

新增鲜艳海底美术版本：打开 `Assets/PBR/Scenes/FishAttentionArcade.unity`。新版使用原创珊瑚海底背景、立体珊瑚装饰和彩色条纹鱼，详见 `Documentation/ArcadeArt.md`。原场景仍保留。

当前新版默认固定 10 条鱼，随机游动、加速、游到实体礁石后藏匿、静止；捕获计分后鱼保留，3 秒后可再次计分。待机、开始和结束 block 都保持同一批鱼。配置使用 `Assets/PBR/GeneratedArcade/PilotConfig.asset`；直接运行版本为 `BuildsPersistent/PBRReef.exe`。以下原路径/生成参数仅适用于原场景。

## 打开与运行

从 GitHub 克隆后，在 Unity Hub 中打开仓库根目录，等待 Unity 重新生成缓存。`Builds*` 中的本地可执行文件、备份和独立验证工程不随仓库上传；可直接在编辑器中运行场景。

1. Unity Hub 打开本仓库根目录（当前为 `D:\projects\2026EMP_PBR_TeamA`，包含 `Assets`、`Packages`、`ProjectSettings`）。实际编辑器版本是 **6000.6.3f1（Unity 6.6）**，URP 17.6.0；没有降级到此前聊天建议的 6.3 LTS。
2. 等待 Package Manager 导入 **Splines 2.9.1**，打开 `Assets/PBR/Scenes/FishAttentionExperiment.unity`。
3. 点击 Unity 的 Play，输入匿名 pair ID，选择实验条件，点击 START。默认每个 block 90 秒。
4. 橙色鱼属于 A，青色鱼属于 B，灰色鱼是干扰项；背上白点表示允许临时移交的目标。A/B 的非白点目标始终保留原责任。
5. 鼠标控制选中参与者的瞄准方向；Tab 切换 A/B；鼠标左键捕获选中参与者的目标；F 为 A 按钮，J 为 B 按钮。方向键控制另一人的方向。Esc 中止 block。

这是同机键鼠调试方式，不能代替正式双人输入。IMU 的 yaw 是头部指向代理量，不是眼动或注视测量。

## 已实现的原型

- Quaternius 动画鱼 FBX、游泳动画循环、Kenney 实体岩石、水草、海床和雾效。
- Unity 官方 Splines 路径；鱼经过礁石后方，在路径中点停留两秒再出现。
- 两个参与者独立的朝向、按钮状态和捕获权限；射线首先撞到岩石时阻止捕获。
- 固定责任、共享责任、责任人提示、自适应移交四种条件。
- 不变的原责任人、独立的临时接收者和到期时间；单个目标最多移交一次。
- 匿名会话元数据、事件日志、连续朝向日志、目标位置与监视覆盖样本。
- 输入适配器接口、270° 三视图预览开关、可编辑参数、场景生成与验证菜单。

## 调节参数

在 Inspector 选择 `Assets/PBR/Generated/PilotConfig.asset`：

| 参数 | 默认值 | 含义 |
| --- | --- | --- |
| durationSeconds | 90 | block 时长 |
| spawnInterval | 3 | 生成间隔，秒 |
| travelSeconds | 16 | 不含停留的路径时间 |
| hidePauseSeconds | 2 | 路径中点停留时间 |
| monitoringHalfAngle | 25 | 头部监视扇区半角，度 |
| cueDelay | 1 | 责任人未覆盖目标的持续时间阈值 |
| handoffDelay | 2 | 移交使用的覆盖缺口阈值 |
| handoffSeconds | 4 | 临时权限时长 |
| workloadThreshold | 2 | 可见且在责任人监视扇区内的原责任目标数量阈值 |
| sampleHz | 20 | 目标采样频率，受帧率限制，记录实际时间 |

参数均是**待预实验校准的起点**，不是申请书已确定的实验参数。固定种子保证条件间的路径选择序列相同；真实时钟和帧调度会造成实际生成时刻差异，日志保留迟到事件。路径进度按时间参数推进，不保证沿曲线的恒定世界速度。

`PBR > Build or rebuild experiment scene` 会重新生成本项目专用的场景、材质和预制体；手动调整前请复制场景，以免重建覆盖调整。`PilotConfig.asset` 中的实验数值会保留。原有 SampleScene 保留。

## 日志

输出目录：`Application.persistentDataPath/PBRLogs/<UTC时间_唯一ID>`。Windows 此工程通常为 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\PBR\PBRLogs`。运行时对象的 `LastLogFolder` 可取得实际目录。

- `session.json`：匿名 pair ID、block、条件、配置快照、Unity 版本、UTC 起点。
- `events.csv`：生成、出现、进入遮挡、再次出现、路径停留/恢复、按钮、捕获/误捕、提示、移交开始/结束、目标遗漏、block 结束。
- `orientation.csv`：独立于鱼数量的连续双人朝向、输入来源和有效性。
- `samples.csv`：每条活跃鱼的位置、角度、原责任、双人扇区覆盖、中心视线可见性、临时接收者、工作负荷和缺口计时。

所有相对时间使用 Unity 的实时单调时钟，记录实际帧号；CSV 数字使用点号小数。每秒刷新文件，正常结束关闭文件。存储失败会停止 block。`frame_gap`、`sample_late` 和 `spawn_late` 不能在分析时当成正常连续数据。中止或时间到时仍存活的鱼标为 `target_censored`，不伪装成遗漏。

使用 PowerShell 运行 `& .\Tools\Summarize-Session.ps1 -SessionDirectory '<日志目录>'` 生成描述性摘要，无需安装 Python。它不会运行混合效应模型，也不会把调试日志当作研究结果。

## 硬件和投影

`ParticipantInput.SubmitOrientation(player, yawDegrees, pitchDegrees)` 和 `SubmitButton(player)` 是主线程适配入口，player 为 0/1。`CalibrateForward(player)` 将当前外部 yaw 置为正前方；超过 0.5 秒没有外部更新的输入视为失效，禁止捕获，也不据此推断责任人失察。读取串口/UDP 的线程须通过队列把样本交给主线程。本次未接入未知协议的 IMU 或按钮。

启用 `panoramicPreview` 后重建场景会生成三台相机和三组礁石/路线。这是逻辑布局预览。三幅画面只有在各视口为正方形时才各有约 90° 水平视野；现场三墙尺寸、观察点、输出方式、投影畸变和边缘融合未标定。正式投影应使用测得的墙面几何建立离轴投影矩阵，而不是把普通宽屏分成三栏就视作完成标定。

当前共用一个观察点，A/B 提示均显示在公共调试 HUD。申请书要求的“仅给责任人提示”**尚不能在共享屏幕上成立**，正式试验需要独立耳机、控制器反馈或私有显示通道。正式双人空间位置差异也需纳入校准。

## 研究和素材记录

详见 `Documentation/ResearchDesign.md`、`Documentation/AssetSources.md` 和 `Documentation/Validation.md`。申请书作为背景资料，不把表格中的填写说明当作本次开发命令。未修改原 Word 文件，也未替申请人提交表格。
