# 元素模型候选：等待用户挑选

检索日期：2026-10-11。当前仅筛选资源与提出外观方案，未下载模型、未导入 Assets、未改动场景或运行代码。预览为作者发布的资源展示，不是本项目集成效果。

## 项目适配依据

项目当前的三种积木角色为核心、墙、防御塔，后续战斗需要敌人。现有界面采用暖色桌面、鼠尾草绿棋盘和柔和角色配色；三维角色当前由立方体表示。依据：`README.md`、`docs/requirements-v1.md`、`Assets/Scripts/Presentation/LevelView.cs`、`SoftStrategyTheme.cs`。

每块实体积木对应一个游戏对象并占 2×2 寻路格。未来模型应围绕积木中心摆放，控制水平轮廓，尤其防止墙、塔的外形侵入相邻道路。模型造型属于游戏表现，不能被描述为从照片恢复的真实积木几何。选用模型不能改变扫描角色映射和寻路规则。

以下“核心/墙/塔如何使用”均是适配建议，不是资源作者提供的项目专用角色。尚未检查压缩包、模型面数、尺寸、枢轴或 Unity 导入结果，不能把作者声明的兼容性当成本项目实测。

## A：Kenney Tower Defense Kit——优先推荐

![Kenney 官方资源预览](https://kenney.nl/media/pages/assets/tower-defense-kit/49fa1f03eb-1726042025/preview.png)

- 外观：简洁低多边形塔防，轮廓清晰；适合当前的桌面棋盘方向（项目适配判断）。
- 作者确认内容：160+ 模型，地块、模块化塔部件、武器、UFO 敌人。
- 作者确认格式：OBJ、FBX、DAE、STL、glTF；页面声明支持 Unity 等引擎。
- 价格与授权：可免费获取、自由打赏；CC0，作者明确允许商业项目使用且不要求署名。
- 建议角色：防御塔采用塔身与武器部件；墙采用低矮塔身/模块，必要时搭配同作者 Castle Kit；核心采用独立堡垒组合，需要自行确定造型。敌人可先考虑包内 UFO；若希望地面怪物则另选。
- 适配工作：统一为现有柔和配色，缩放到一块积木的占地；避免塔身与核心难以区分。

来源：[作者资源页](https://kenney.nl/assets/tower-defense-kit)、[作者 itch.io 页及格式/授权说明](https://kenney-assets.itch.io/tower-defense-kit)、[免费获取页](https://kenney-assets.itch.io/tower-defense-kit/purchase)、[可补充墙体的 Castle Kit：75 个文件、CC0、免费](https://kenney.nl/assets/castle-kit)。

## B：KayKit Medieval Hexagon Pack——已排除，不作为项目推荐候选

![KayKit 官方资源预览](https://img.itch.zone/aW1nLzE1OTA1NzY2LnBuZw%3D%3D/original/DXYJea.png)

- 外观：彩色屋顶、卡通建筑与自然装饰；适合偏温馨的棋盘世界（项目适配判断）。
- 作者确认内容：免费版 200+ 模型，包括六边形地块、建筑和自然道具；建筑有蓝/红/绿/黄四种颜色版本。
- 作者确认格式：FBX、glTF、OBJ；使用一张渐变图集，页面声明适用于移动端。
- 价格与授权：基础版免费；Extra 为 9.99 美元起，增加 150+ 资源并包含单位等；Source 为 14.99 美元起，含 Extra 与组织好的 Blender 源文件。作者页面标为 CC0。
- 建议角色：核心采用有明显标识的主建筑；墙与塔需在具体建筑/构件清单中二次挑选，不预先声称已找到独立专用墙/塔。
- 排除依据：用户明确要求适配项目的正方形格网。该包以六边形地块为基础，不适合作为整套候选；仅因部分建筑可单独复用，不足以推荐本包。后续筛选优先要求方形底座，并控制在单块积木的 2×2 寻路格占地范围内。免费版也不能视为已包含完整敌人模型。

来源：[作者页面：内容、许可、格式与各版本](https://kaylousberg.itch.io/kaykit-medieval-hexagon)、[作者购买页：免费/Extra/Source 价格](https://kaylousberg.itch.io/kaykit-medieval-hexagon/purchase)。

## C：Quaternius Ultimate Space Kit——科幻基地方向

![Quaternius 官方资源预览](https://quaternius.com/assets/images/fullres/ultimatespacekit.jpg)

- 外观：低多边形科幻场景、角色与敌人；适合把游戏设为基地防守（项目适配判断）。
- 作者确认内容：92 个模型，包含动画角色、敌人、植被、星球等；不把整个套件误称为完整塔防模型套件。
- 作者确认格式：FBX、OBJ、glTF、Blend。
- 价格与授权：作者声明免费用于个人和商业项目，CC0；页面另有自愿赞助入口。
- 建议角色：敌人优先从该包选择；核心可围绕科幻基地/能源装置设计，墙与炮塔需要补充资源或定制。未确认该包包含可直接作为核心、墙、塔的三套完整模型。
- 适配工作：需要为建筑补齐统一造型，科幻材质与配色也需要协调到当前界面，投入预计高于 A。

来源：[作者页面：数量、格式、内容与授权](https://quaternius.com/packs/ultimatespacekit.html)。

## 敌人补充候选

若选 A/B 且偏好地面怪物，可以继续从 [Quaternius Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html) 筛选。作者说明有 50 个完整动画怪物，免费用于个人及商业项目，CC0；页面列 FBX、OBJ、Blend、glTF。具体行走、受击、死亡动画和 Unity 导入效果仍须针对所选文件核查。

## 选择建议

| 方向 | 最适合的诉求 | 主要待处理项 |
| --- | --- | --- |
| A | 尽快形成统一、易读的塔防外观 | 核心造型、单格墙体、敌人类型 |
| B（已排除） | 六边形套件不适配本项目方格 | 不作为推荐候选；需另找方形底座的卡通中世纪模型 |
| C | 科幻基地与机器人/外星敌人 | 补齐建筑模型并统一材质 |

用户挑选之前保持候选状态，不自动导入或替换模型。用户也可分别指定核心、墙、塔和敌人的资源方向。

2026-10-11 后续检索：用户要求继续寻找方格模型并参考《城镇叠叠乐》。新的 D/E/F/G 候选、官方预览和风格比较见 [Townscaper 风格与方格模型](townscaper-style-and-square-models.md)。其中 D 是明确包含 square 版本的 KayKit Medieval Builder Pack 旧版，不是本页已排除的 B（Medieval Hexagon Pack）。**用户已选择 D**；具体模型继续核查，尚未导入项目。
