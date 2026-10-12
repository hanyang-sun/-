# 正方形格网与 Townscaper 风格：模型补充候选

日期：2026-10-11。范围仅为 Kenney Fantasy Town Kit 与 Quaternius Medieval Village Pack 的作者第一方页面；共三次浏览调用，没有下载模型，没有修改 Assets 或代码。

| 候选 | 第一方已确认信息 | 方格适配与 Townscaper 风格判断 |
| --- | --- | --- |
| [Kenney Fantasy Town Kit](https://kenney.nl/assets/fantasy-town-kit) | 3D；160 个文件；有变体；标签为中世纪、墙、城镇、建筑；CC0；免费，可自愿捐赠；版本 2.0 为完全重制。页面未列出逐模型清单或格式清单。 | **优先让用户查看这一包**：题材覆盖城镇建筑和墙，比科幻包更贴近用户的城镇方向。这是用途判断，不代表已核实 Townscaper 的具体视觉相似度。官网没有正方形占地、统一模块尺寸或邻接拼接规则的规格，不能保证即插即用到方格。 |
| [Quaternius Medieval Village Pack](https://quaternius.com/packs/medievalvillage.html) | 44 个模型；中世纪建筑与道具；FBX、OBJ、Blend；CC0；作者明确免费用于个人与商业项目。 | 适合比较另一组村镇建筑，可考虑将显著建筑作为核心或防御塔的外观。官网描述为建筑和道具，没有明确称它们为可叠层建筑模块，亦未保证正方形占地。具体颜色、轮廓与 Townscaper 的差别需查看作者宣传图后判断。 |

格式核查补充：Kenney 的[通用导入指南](https://kenney.nl/knowledge-base/game-assets-3d/importing-3d-models-into-game-engines)说明其 glTF 以 GLB 分发，Unity 推荐 FBX，也支持 OBJ；该指南不能证明 Fantasy Town Kit 当前压缩包逐项包含哪些格式。发布页本轮未给格式清单，因此保持“未核查包内格式”。

选型应区分三件事：城镇风格的外观、四边形轮廓、真正可贴合正方形格网的尺寸。前两项不能证明第三项。两包作者页面均未提供模型实际 X/Z 尺寸、枢轴位置与占格约定；目前只有方格适配候选，没有经过测量的直接兼容模型。

如用户选定外观，再测量被选模型的水平包围盒与屋檐外伸，设置统一正方形基座/缩放和中心锚点；墙体需检查接缝与四向旋转。用户希望参考 Townscaper 的外观，不等于授权实现其自动屋顶、层叠、邻接造型或水岸生成行为；作者页面也未承诺这些行为。

预览状态：两包的作者页面链接已核查，可用于浏览原始预览。本轮未取得可确认的宣传图片直链，故不猜测图片 URL；Quaternius 页面文本中的 Image 链接实际指向导航 SVG，不应误用为模型预览。
