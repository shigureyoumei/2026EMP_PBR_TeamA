# 素材和依赖来源

核对日期：2026-09-30。

2026-11-01 更新：当前四面自然海底使用新增的程序化鱼形、礁石、水草与珊瑚网格，并引入 Poly Haven 的 CC0 扫描岩石/沙地纹理。Powered by [Poly Haven](https://polyhaven.com/)。完整来源、作者、限制与文件位置见 [NaturalReef.md](NaturalReef.md)。下表为早期原型的历史素材记录。

| 内容 | 来源 | 许可/状态 |
| --- | --- | --- |
| 鱼模型 Fish1/Fish2/Fish3 与游泳动画 | https://opengameart.org/content/animated-fish （作者 quaternius） | CC0；作者 Animated Fish 2018 素材，已导入 |
| 原计划 Cute Fish 2020 | https://quaternius.com/packs/cutefish.html | CC0；官方下载的 Google Drive 返回 Quota exceeded，未导入，不能冒称已采用 |
| 岩石 rock_largeA / rock_largeB / rock_tallA | https://kenney.nl/assets/nature-kit | CC0；下载包 License.txt 标注 Nature Kit 2.1 |
| 路径组件 | https://docs.unity3d.com/Packages/com.unity.splines@2.9/manual/index.html | Unity Splines 2.9.1；从官方 UPM 注册表解析，遵循包内 LICENSE.md |
| 水草、海床、场景、实验脚本 | 本工程生成 | 自行实现，无第三方 Aquarium/MRTK/Boids 代码 |

两个 CC0 包各自的原始 License.txt 保存在 Assets/ThirdParty 下。FBX 原文件保留；实验使用的 URP 材质、缩放和动画预制体位于 Assets/PBR/Generated。

模型外观按 A/B/干扰角色重设材质为橙、青、灰，便于调试；这会改变原素材配色。所有生成脚本都在本地工程内。没有下载或执行第三方项目脚本。

Unity 版本以已有 ProjectSettings/ProjectVersion.txt 为准：6000.6.3f1。原聊天建议的 6.3 LTS 为另一版本，本次未静默降级。
