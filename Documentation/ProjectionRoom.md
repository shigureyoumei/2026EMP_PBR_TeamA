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

这些是公开资料中的尺寸，不是当前场地实测值。本版本用四个平面近似投影墙，尚未复现实际曲面/圆角、投影机布局、畸变校正、边缘融合、立体显示或地面投影。投影上下边界暂按整墙设定，现场确认后应修改。四台 Unity 相机是四个逻辑墙面视图，不等于现场四台投影机。

## 已实现行为

四面相机使用墙面边界和观察点计算离轴透视。矩形房间各墙的视角不同，不再固定每面 90 度；相邻墙面共享同一条边界射线。观察点改变后，相机矩阵自动更新。

`0` 四格预览、`1–4` 单面、`5` 连续长条。桌面预览按墙面实际宽高比保留黑边；长条中前后墙比左右墙更宽。四显示输出仍通过 `-pbr-four-displays` 启用，显示器映射和现场拼接系统尚未验证。

物理房间坐标与现有海底场景坐标分开：`ProjectionRoom.ToVirtual()` 将物理观察点映射到海底观察点。调整房间尺寸不会自动把鱼群或海底模型拉伸。

`ProjectionRoom.TryWallHit(Ray, out wall, out point, out uv)` 接受物理房间坐标系内的位置和方向，返回最近的有效墙面交点及 UV；没有交点时返回 false。wall 顺序为前、右、后、左。后续头部和手持设备应先标定到这个坐标系，再调用它；本次尚未连接传感器或实现渔网动画。当前鼠标捕鱼仍沿用现有虚拟场景输入。

## 验证与启动

最新渔网互动运行文件：`BuildsLow/PBRLow.exe`，初始总计 50 条鱼，捕获后数量减少，见 `NetCapture.md`。自然海底版本记录见 `NaturalReef.md`。旧版投影室单独运行文件为 `BuildsRoom/PBRRoom.exe`。自动检查记录位于 `Documentation/ProjectionRoomValidation/`。几何检查覆盖三种房间尺寸、中心及偏心观察点、墙面角点与射线交点；运行检查覆盖四墙接缝、固定鱼群和正常定时 block。

编辑器验证入口：`PBR.Editor.ProjectionRoomSetup.InstallBatch`（仅供独立验证工程批处理）。玩家验证参数：`-pbr-room-smoke`。此批处理入口会保存当前 360 场景并构建玩家，日常修改参数不需要运行它。
