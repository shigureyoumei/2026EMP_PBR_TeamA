# 验证记录

2026-09-30 使用本机 Unity 6000.6.3f1、Windows Standalone、NVIDIA RTX 3060 完成验证。

## 已通过

1. 独立验证副本中编译 C# 并构建 Windows 原型，BuildPipeline 结果 Success。
2. 79 个编辑器断言通过：四条件的角色权限、不可捕获干扰项、可移交/不可移交目标区别、角度环绕、每条路径入口/中点/出口的实体遮挡、三个模型在动画多个时刻的实际蒙皮尺寸与目标中心对齐。
3. 28 秒自动运行通过：两次不同目标的正确捕获（包括队友在临时权限下捕获）、一次 wrong_owner、一次 wrong_distractor、一次 shot_blocked、六次 occlusion_enter、四次 fish_reappeared、两次 handoff_start、一次 handoff_end，以及自然遗漏与结束删失。
4. 独立朝向、目标采样、事件、会话配置四类日志均生成，验证脚本确认临时权限约 4 秒到期。日志中的 frame_gap/sample_late 如实保留；自动离屏截图会占用主线程，不能用这次冒烟运行做性能结论。
5. 实际离屏渲染截图已检查，动画鱼、岩石、水草和海床可见。修复了 FBX 静态包围盒与蒙皮实际尺寸不同、100 倍节点缩放和 BakeMesh 缩放补偿导致的问题。
6. PowerShell 摘要回归检查通过：删失目标不进入 resolved capture rate 分母；179° 到 -179° 计为 2°；无效输入不计入头部运动；反应时间按生成时间计算。

证据：`Validation/editmode-results.txt`、`Validation/final-smoke/`、`Validation/smoke-preview.png`。测试会话的 pair ID 明确为 automated_smoke_not_participant，不是参与者数据。

## 复现

编辑器菜单 `PBR > Validate project` 验证当前打开的实验场景。`PBR > Build or rebuild experiment scene` 重建生成场景，谨慎保留自己后续手改的场景副本。

命令行 `-executeMethod PBR.Editor.PbrProjectBuilder.BuildAndValidateBatch` 可重建、检查和构建（仅对未被其他 Unity 实例打开的验证副本运行）。运行生成的 `PBRPrototype.exe -batchmode -pbr-smoke` 会自动测试、生成截图和日志并退出。普通双击不会进入自动测试。

`Tools/Verify-Smoke.ps1 -SessionDirectory <自动测试日志目录>` 检查事件链；`Tools/Summarize-Session.ps1 -SessionDirectory <日志目录>` 输出 summary.json。

## 验证边界

后台普通屏幕截图为空，因此使用 URP SingleCameraRequest 离屏渲染核查 3D 场景。截图不包含 OnGUI 控制面板；尚未对真实用户鼠标操作做人工可用性测试。

Unity 构建/退出包含引擎自身的内存分配诊断；初始化 URP 时存在未使用的 GaussianDepthOfField shader 剥离警告。本原型未启用景深后处理；离屏场景正常渲染。未据此宣称整个 Unity 版本没有引擎问题。

尚未连接真实 IMU、独立按钮、私有提示设备或 270° 投影现场，因此没有硬件同步、漂移、输入延迟、跨墙画面和真实参与者研究有效性验证。三视图模式是可生成的扩展入口，本轮测试覆盖默认单屏桌面配置。
