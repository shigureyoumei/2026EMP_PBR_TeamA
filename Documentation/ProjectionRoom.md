# Large Space 投影室设置

打开 `Assets/PBR/Scenes/FishAttention360.unity`。停止 Play 后，在顶部菜单选择 **PBR > Select projection room settings**，或在 Hierarchy 选择 **Projection Room**。在 Inspector 修改参数，按 Ctrl+S 保存场景。运行期间修改仅用于试调，不会在停止 Play 后保留。

| Inspector 参数 | 初始值 | 含义 |
|---|---:|---|
| 房间宽度 Width | 25 m | 左右方向，X 轴 |
| 房间长度 Depth | 15 m | 前后方向，Z 轴 |
| 房间高度 Height | 7.7 m | 地面到顶部 |
| 观察眼高 Eye Height | 1.6 m | 受试者眼睛离地高度；这是可修改的人体预设，不是场地数据 |
| 海底虚拟视点高度 Y | 0.55 m | 海底场景内相机的 Y 坐标，约高于当前中心海床 0.8 m；独立于受试者实际眼高 |
| 站位偏移 Tester Offset | X=0, Z=0 | 相对地面中心的水平位置 |
| 投影下沿距地高度 | 0 m | 有效墙面投影的下边界 |
| 投影上沿距顶距离 | 0 m | 顶部未使用高度 |
| 显示房间与观察点辅助线 | 开启 | Scene 视图中的四墙边界、地面、顶部和观察点 |

房间原点位于地面中心；+Z 为前墙，+X 为右墙。单位为米。请使用 Inspector 参数，不要移动或缩放 Projection Room 对象。Scene 视图打开 Gizmos 后可见彩色墙框和黄色观察点；辅助框不会出现在实验画面，也不会遮挡鱼。

需要更贴近海底时，在同一 Inspector 中降低“海底虚拟视点高度 Y”。当前从 Y=3 降到 Y=0.55，中心海床约为 Y=-0.25。四台相机、鼠标瞄准原点和鱼群遮挡观察点会自动同步。不要通过调整真实“观察眼高”来改变游戏视角，也不要单独移动某一台相机；真实眼高继续用于投影室标定。

## 尺寸来源和适用范围

- [筑波大学 TGSW 2020 Proceedings](https://tgsw.tsukuba.ac.jp/TGSW2020/update/wp-content/uploads/2020/07/TGSW2020_21Nov_Proceedings.pdf)：LargeSpace 为 15 × 25 × 7.7 米全周显示空间。
- [日本 VR 学会 2021 技术报告，2C1-7](https://conference.vrsj.org/ac2021/program/doc/2C1-7.pdf)：明确写出宽 25 m、深 15 m、高 7.7 m。
- [EMP 官方设施介绍](https://www.emp.tsukuba.ac.jp/environment/research)：全周墙面与地面立体投影。页面中的 **18 × 9 × 7.4 m 是钢索运动平台的可动范围**，不是房间尺寸。
- [筑波大学学位论文 DA09220，1.1.3.3](https://tsukuba.repo.nii.ac.jp/record/50869/files/DA09220.pdf)：描述投影表面的曲率。

这些是公开资料中的尺寸。默认四墙模式用四个平面近似围墙，投影上下边界暂按整墙设定。使用场地样例的曲面网格和投影机参数时，启动下文的 Large Space 模式。

## 已实现行为

四面相机使用墙面边界和观察点计算离轴透视。矩形房间各墙的视角不同，不再固定每面 90 度；相邻墙面共享同一条边界射线。观察点改变后，相机矩阵自动更新。

`0` 四格预览、`1–4` 单面、`5` 连续长条。桌面预览按墙面实际宽高比保留黑边；长条中前后墙比左右墙更宽。四显示输出仍通过 `-pbr-four-displays` 启用，显示器映射和现场拼接系统尚未验证。

物理房间坐标与现有海底场景坐标分开：`ProjectionRoom.ToVirtual()` 将物理观察点映射到海底观察点。调整房间尺寸不会自动把鱼群或海底模型拉伸。

`ProjectionRoom.TryWallHit(Ray, out wall, out point, out uv)` 接受物理房间坐标系内的位置和方向，返回最近的有效墙面交点及 UV；没有交点时返回 false。wall 顺序为前、右、后、左。后续头部和手持设备应先标定到这个坐标系，再调用它；本次尚未连接传感器或实现渔网动画。当前鼠标捕鱼仍沿用现有虚拟场景输入。

## 验证与启动

最新渔网互动运行文件：`BuildsLow/PBRLow.exe`，初始总计 50 条鱼，捕获后数量减少，见 `NetCapture.md`。自然海底版本记录见 `NaturalReef.md`。旧版投影室单独运行文件为 `BuildsRoom/PBRRoom.exe`。自动检查记录位于 `Documentation/ProjectionRoomValidation/`。几何检查覆盖三种房间尺寸、中心及偏心观察点、墙面角点与射线交点；运行检查覆盖四墙接缝、固定鱼群和正常定时 block。

编辑器验证入口：`PBR.Editor.ProjectionRoomSetup.InstallBatch`（仅供独立验证工程批处理）。玩家验证参数：`-pbr-room-smoke`。此批处理入口会保存当前 360 场景并构建玩家，日常修改参数不需要运行它。

## Large Space 样例输出

用 Unity 6.6 打开 PBR，选择 **PBR > Build Large Space player**，生成 `BuildsLargeSpace/PBRLargeSpace.exe`。部署时复制完整的构建目录，并在 EMP 电脑上双击 `Start EMP.cmd`。

在构建目录运行以下命令，按现场画面接入方式选择一个模式。

| PowerShell 命令 | 输出 |
|---|---|
| `./PBRLargeSpace.exe -pbr-large-space preview` | 单屏两排，同时检查 A、B 两组六路画面 |
| `./PBRLargeSpace.exe -pbr-large-space a` | 将样例前六路铺满主显示器 |
| `./PBRLargeSpace.exe -pbr-large-space b` | 将样例后六路铺满主显示器 |
| `./PBRLargeSpace.exe -pbr-large-space dual` | 同一进程将 A、B 两组送到显示器 0、1。检测到一个显示器时进入预览 |
| `./PBRLargeSpace.exe -pbr-large-space emp` | 按现场映射输出六屏，每屏四格。两个眼位使用同一个单目画面。识别不到六个显示输出时进入预览 |

每组按样例视口顺序横排。正式输出时使用现场要求的桌面分辨率和显示器顺序。正式模式每路按样例遮罩的 1400 × 1050 像素渲染，预览使用一半分辨率，可用 `-pbr-large-space-channel-width 1280` 按相同比例调整。固定物理视点取自样例 `scene.unity` 的 ObserverLS，为 `(0, 1.7, -0.1)` 米。可用 `-pbr-large-space-eye "0,1.6,0"` 临时覆盖。虚拟海底视点仍由场景里的 Projection Room 设置。

**Enter** 开始实验，**Esc** 结束，**R** 在暂停时重置鱼群，**H** 显示或隐藏操作面板。鼠标从校正后的画面反算瞄准射线，左键发网，**Tab** 切换玩家。运行期间两位玩家的准星会一起经过投影校正。

`Assets/PBR/Resources/PBRLargeSpace` 含样例的屏幕模型、融合遮罩和从 `DistortedDrawerLS.prefab` 转出的 12 路参数。Unity 6 URP 根据固定观察点生成单目墙面与地面画面，并在对应投影通道应用遮罩。现场应以当前屏幕几何、标定参数和通道映射为准。旧样例的双眼立体驱动、OptiTrack 头部追踪和跨电脑帧同步需另外接入。两台电脑分别运行 A、B 会各自模拟一份鱼群，互动展示应采用同一进程的双屏输出，或由现场系统分配同一画面源。
