# 连续投影下保持识别率

Status: resolved

投影开启且相机会同时拍到积木和投影光。识别率要保持，投影不能为了拍照而频闪。方案必须能用于之后的真实相机和投影机。

## 验收

- 播放中不插入消隐帧。消隐比例大于 0 的采集被拒绝。
- 曝光未锁定，或快门期间投影内容变了，该帧被拒绝。
- 用全黑、全白和各颜色的标定照片可以拟合光度参数，并在带投影的合成照片上分对空格和四种颜色。
- 投影把通道打满时，该格为未知，不会变成另一种积木颜色。
- 旧的色度分类在强色投影下会把空桌面认成有颜色；新识别器仍判为空。
- 仿真接通投影后，识别不再跨帧隐藏投影面片。

## Answer

采用锁定曝光下的乘性光度模型，见 `docs/flicker-free-projection-recognition.md` 与 `docs/research/flicker-free-projection-recognition-github.md`。纯 C# 检查在 `Tests/ProjectedLightChecks.cs`。
