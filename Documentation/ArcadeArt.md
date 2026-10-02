# 鲜艳海底美术版本

新增场景：`Assets/PBR/Scenes/FishAttentionArcade.unity`。在 Unity 中双击场景，点击 Play；先看到鱼的待机展示，再点击 START 开始原有实验流程。建议 Game 视图选择 16:9。

## 美术变化

- 原创绘制的蓝绿海水、珊瑚礁远景、沙地、水面和斜射光束，替代纯色背景。
- 新增立体枝状珊瑚、盘状珊瑚、紫色海绵、海藻、海星和气泡装饰。中央礁石仍为真实碰撞遮挡物。
- 鱼保留现有三种动画网格，新增奶油色/金色/粉色条纹、鳍部配色、轮廓高光和立体眼睛。
- A 为橙色/奶油色鱼，B 为蓝色/金色鱼，干扰项为紫色/粉色鱼。新版 HUD 单独标明颜色，旧版仍显示原有图例。
- 当前默认固定 10 条鱼，待机和正式 block 使用同一批对象；开始、结束、计分均不删除鱼。

这是参考街机捕鱼游戏明快配色制作的第一版美术升级，不是《捕鱼达人》原版素材，也尚未达到其完整商业美术资源量。当前是原创绘制远景结合立体目标与礁石的 2.5D 场景；不是完整可自由行走的海底建模。背景内不画鱼，避免把不可交互的绘制鱼误当成目标。

原 `FishAttentionExperiment.unity` 和 `Generated` 素材仍保留。新版专用材质、预制体和配置在 `GeneratedArcade`；编辑器菜单 `PBR > Build colorful reef scene` 重建新版。重建会覆盖新版生成资源，手工调整前请另存副本。

背景、气泡和光束目前是静态视觉装饰，鱼有实际游泳动画。更换了礁石外形和干扰鱼配色，正式研究前需重新校准目标可辨识度和遮挡时长。三墙投影的背景接缝与离轴投影仍需现场设计，本轮只针对单屏 16:9 预览。

## 原创背景来源

文件：`Assets/PBR/Art/ReefBackdrop.png`。使用内置 image_gen 工具生成，未使用 CLI/API 密钥，也未提取参考游戏的贴图。

最终生成提示词：

> Create an original production-ready background texture for a vibrant underwater arcade fishing game. Wide 16:9 landscape, high resolution 2560x1440 if supported. Viewpoint underwater looking horizontally into a tropical reef lagoon, slightly down across a sandy clearing, immersive first-person aquarium view, NOT top-down. Luminous saturated turquoise water transitioning to deep cobalt distance, beautiful diagonal shafts of sunlight entering from the surface at top left, soft underwater light caustics. Rich painterly high-end casual mobile game 3D illustration, polished rounded forms, layered depth, brilliant but tasteful coral pink, purple, orange, teal and lime reef vegetation clustered along left and right outer edges and lower corners. Fine sand, layered distant rounded limestone coral shelves, distant blue silhouettes, sparse subtle bubbles. The center 60 percent is open blue water and unobstructed sandy swimming space reserved for separately rendered gameplay fish and real foreground occluders. Background only: NO fish, NO animals, NO interface, NO text, NO coins, NO treasure, NO weapons, NO people, NO logo, NO frame. Crisp detailed illustration, cheerful saturated colors, strong depth. This will be a Unity in-game distant backdrop, not a screenshot or mockup.

提示词中的分辨率是生成请求，以 PNG 实际尺寸为准。

## 验证

Unity 6000.6.3f1 Windows 构建成功，79 项检查通过。新版 28 秒自动运行通过正确捕获、错误责任捕获、干扰鱼拒绝、实体礁石阻挡、出现/遮挡/重现、停留恢复、临时移交捕获及到期检查。证据在 `Documentation/ArtValidation`。

`ArtValidation/art-preview.png` 是上一次美术验证的 Unity 实际渲染画面。旧的 `BuildsArcade/PBRReef.exe` 保留为美术版本；当前固定鱼群版本使用 `BuildsPersistent/PBRReef.exe`。新版场景排在 Build Settings 的首位。

## 固定鱼群与行为

配置位于 `Assets/PBR/GeneratedArcade/PilotConfig.asset`，`persistentSchool` 已启用。默认 `fishCount = 10`，修改后重新进入 Play 生效。运行时不补鱼、不定时生成，不因为离开路径或捕获删除鱼。

- Swimming：在有限水域内随机选择游动目的地，默认速度 1.5 单位/秒。
- Accelerating：逐渐加速到 4 单位/秒，再随机切换状态。
- Hiding：连续游到中央实体礁石后方；验证鱼体周边被遮挡且到达目的地后，停留 2–3.5 秒，再游出。整个过程保持网格和碰撞体启用。
- Stationary：位置静止 3–7 秒，保留轻微摆尾。

每条鱼独立随机选择状态和时长，不会整群同步切换。藏匿靠实际遮挡，数量显示包含石后鱼。鱼在岩石后的时候，捕获射线仍被岩石阻挡。活动区当前按新版礁石布局设置，更换礁石或投影布局后需要重验藏匿点。

捕获改为命中计分，鱼继续存在；同一条鱼有 3 秒计分冷却。日志包含 `fish_present`、`fish_behavior`、`hide_arrived`（behavior detail）、可见性变化与命中事件；samples 增加运动状态、速度与隐藏停留字段。开始实验仅重置实验计时及权限，不重置鱼的位置或运动状态。

固定鱼群允许重复命中，因此汇总脚本不再给此模式计算旧的“消失目标捕获率”或“从生成到捕获反应时”，这两个字段返回 null。原场景仍使用原有生成/路径/离场模式。
