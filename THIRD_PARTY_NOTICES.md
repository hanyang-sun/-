# Third-party notices

## Roy-T.AStar 最小堆

- 来源：[roy-t/AStar](https://github.com/roy-t/AStar)，提交 `3d57efaa83c8cd217e4973fd4738c57e2f694bd2`。
- 原文件：[Roy-T.AStar/Collections/MinHeap.cs](https://github.com/roy-t/AStar/blob/3d57efaa83c8cd217e4973fd4738c57e2f694bd2/Roy-T.AStar/Collections/MinHeap.cs)。
- 本地文件：`Assets/ThirdParty/RoyTAStar/MinHeap.cs`，直接下载原文件，未修改实现。
- 授权：MIT，Copyright (c) 2017 R. A. Triesscheijn。完整声明保存在 `Assets/ThirdParty/RoyTAStar/LICENSE.txt`。
- 原文件声明参考 [robin-thomas/min-heap](https://github.com/robin-thomas/min-heap)。上游 MIT 声明（Copyright (c) 2014 Robin Thomas）保存在 `Assets/ThirdParty/RoyTAStar/UPSTREAM-LICENSE.txt`，读取版本为 `a1a8d7137f3afdf2b5ebf93b9d4059c4d1dd96e8`。
- 采用范围：`GridPathfinder` 的待搜索候选队列，仅调用 `Insert`、`Extract`、`Count`。四方向、单位代价、多核心与等距选择规则由本项目适配。

思路参考而未采用源码或资产的项目，及各候选的比较，见 `docs/research/existing-milestone-github.md`。

## js-aruco 标记编码与解码

- 来源：[jcmellado/js-aruco](https://github.com/jcmellado/js-aruco)，提交 `2203d4b5efb601c39054a341bf04702409699383`。
- 原文件：[src/aruco.js](https://github.com/jcmellado/js-aruco/blob/2203d4b5efb601c39054a341bf04702409699383/src/aruco.js)。
- 本地文件：`Assets/ThirdParty/JsAruco/ArucoCode.cs`。
- 授权：原文件头为 MIT，Copyright (c) 2011 Juan Mellado。代码保留完整 MIT 声明，仓库完整许可证及附带声明保存在 `Assets/ThirdParty/JsAruco/LICENSE.txt`。
- 改编范围：`getMarker` 的黑边校验、`hammingDistance` 的合法行码、`mat2id`、`rotate`，改为 C# 二值数组接口；拒绝多种有效旋转的歧义，并添加格纸生成所需的逆向编码接口。
- 不采用仓库中的 cv.js、POSIT、AForge 或 StackBlur 实现。格纸定位、坐标求解、颜色与关卡衔接为项目适配层；未直接引入 OpenCV 或 glfx.js 代码。
- 检索比较、授权范围、兼容性判断与验证见 `docs/research/photo-recognition-github.md`。

## Colourful 色彩转换子集

- 来源：[tompazourek/Colourful](https://github.com/tompazourek/Colourful/tree/9c9a2c80a28b4108fc00f2b87ebc18bf38cea66c)，提交 `9c9a2c80a28b4108fc00f2b87ebc18bf38cea66c`。
- 本地文件：`Assets/ThirdParty/Colourful/NormalizedLab.cs`。MIT，Copyright (c) 2023 Tomáš Pažourek；完整授权保存在同目录 `LICENSE.txt`。
- 改编 `sRGBCompanding.ConvertToLinear`、`XYZToLabConverter.Convert` 的数值子集，固定 sRGB/D65 矩阵及白点，省略上游转换器、色域和矩阵构建框架。
- RGB 最大通道归一化、弱亮度权重距离、暗部过滤及中心/外围采样属于本项目适配；该距离不是标准 Delta E。
- colour-science/colour 与 OpenCV 仅作研究参考，不引入其源码或依赖。候选比较和固定源码链接见 `docs/research/color-recognition-github.md`。
