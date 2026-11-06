# 连续投影光度识别

Status: resolved

Type: task

播放时投影常亮。用已知投影指令从照片还原反射率，再分类。不把跨帧消隐当作实拍方案。

## Answer

`PhotometricProfile`、`ProjectedLightRecognizer` 和 `PhotometricComposer` 在 Vision 程序集，不依赖 Unity。仿真通过 `CaptureAlbedoForMixing` 合成照片，并在返回前把投影画回预览。投影接通后识别面板走这条路径。
