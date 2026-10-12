Status: ready-for-agent
Completion: complete

# 修复 Windows 导入按钮无选图窗口

范围及验收见 [规格](../spec.md)，实现前记录见 [GitHub 比较](../../../docs/research/windows-photo-picker-github.md)。

## 结果

旧 v1.3.0 的真实程序断言失败，修复后的 v1.3.1 同一断言通过。PNG/JPG/JPEG、中文及空格文件名、首次取消、已有分析结果再次取消均已在实际 Player 验证；返回地图后仍是原有 9 块关卡，运行日志无异常。

完整包 `Builds/BrickDefense-v1.3.1-Windows-x64.zip`：42,817,334 字节；SHA-256 `c5546c09bf9f0028ea82bbc11af1d3fc15b0fd5d454ef0734c13fd525b51f8aa`。解压后 159 个文件逐一通过清单哈希校验。测试程序已关闭。

详细记录见 [验收](../verification/README.md)。未提交或推送 GitHub。
