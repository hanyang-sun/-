# 小焰核心投影

2026-11-03 检索 GitHub：Unity sprite animation、sprite flipbook、Unity-Technologies 2D animation samples。目标是在现有单格投影面片播放已确认的小焰受击稿，并保留消隐和红色涟漪。

- [2d-animation-samples](https://github.com/Unity-Technologies/2d-animation-samples) 的 Documentation/2DAnimation.md 使用骨骼、蒙皮和 Sprite Skin。适合复杂可变形角色，本功能只需要短时二维关键姿态，额外包和骨架不匹配现有投影面片。
- [2d-animation-v2-samples](https://github.com/Unity-Technologies/2d-animation-v2-samples) 的 README 说明其统一蒙皮工作流针对 Unity 2018.3 预览包，版本与本项目不同。

两者的许可证页面未成功取得，只做方案比较，未采用外部资产或代码。用项目原创 PNG 烘焙离线帧，运行时按时间选帧，沿用已有材质、投影门控及扣血事件。验证常态、扣血动画、恢复及采样消隐。
