# Townscaper 风格参考与新方格模型候选

核查日期：2026-10-11。用户要求寻找其他模型，并参考《城镇叠叠乐》（Townscaper），保留本项目正方形格网。**用户已选择 D：KayKit Medieval Builder Pack 1.0（Legacy）的正方形版本。**接下来核查具体模型与占地，暂不导入 Assets 或修改运行逻辑。官方预览及待检模型仅放在独立的 visualizations 目录。

## 1. Townscaper 的可借鉴特征

官方介绍明确说明：玩家选择颜色并在不规则格网上放置房屋块，系统依照组合生成房屋、拱门、楼梯、桥与庭院。因此，“使用同样的美术语言”与“采用同样的网格/生成规则”应分别评估；本项目继续使用正方形格位。

来源：[Raw Fury 官方介绍](https://rawfury.com/games/townscaper/)、[官方 Steam 商店](https://store.steampowered.com/app/1291340/Townscaper/)、[作者浏览器试玩](https://oskarstalberg.com/Townscaper/)。

![Townscaper 官方截图](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1291340/ss_989ff1808d9416c7b22da3e16ee54874697b1cea.1920x1080.jpg?t=1790002949)

以下为对这张官方截图的视觉观察，而非开发者对渲染实现的技术说明：

- **配色：**红、浅黄、青绿、乳白等墙面色组成城镇，暖橙色屋顶将不同建筑联系起来；蓝绿色水面和背景衬托建筑。
- **造型：**简单而略有变化的建筑体块、小窗、尖顶与坡屋顶、窄栏杆、石基座。轮廓清晰，屋檐和墙面衔接有细节。
- **表面：**墙面有细砖缝和轻微色差，屋顶有瓦片分线；不是所有表面都只有无细节的纯色。
- **明暗：**整体柔和，但屋檐、拱门和建筑接缝的暗部有层次。风格接近微缩城镇与插画。
- **整体感：**建筑共享窗门比例、屋顶颜色和基座语言，彼此连接形成一致的街区。

## 2. 推荐用于本项目的造型方案（待选，不实施）

| 元素 | 建议形态 | 方格与游戏可读性要求 |
| --- | --- | --- |
| 核心 | 青绿色迷你钟楼/主楼，暖橙色坡屋顶，明显的旗帜或徽记 | 放入一块积木占地；比墙更高、轮廓与塔可区分 |
| 墙 | 暖灰/乳白矮石墙，统一方形基座；可准备独立块、直连和转角外观 | 外观连接不合并积木记录、不改变占格；每块保持独立对象身份 |
| 防御塔 | 赭黄色瞭望塔，顶部小弩机或炮口 | 屋檐和武器摆动尽量不覆盖道路，保持攻击朝向可读 |
| 地面 | 正方形浅色石板或现有柔和棋盘 | 保留 10×10 实体格与 20×20 寻路格映射；路径和出生点清楚可辨 |
| 敌人 | 小比例、简化轮廓的地面角色 | 另行挑选动画资源；建筑包不能被当成完整敌人包 |

这是参考后的原创适配建议，不是 Townscaper 的模型清单，也不把该游戏的原始模型视为可复用资源。

## 3. 新候选 D：KayKit Medieval Builder Pack（Legacy，方格版）

**本轮对方格支持最明确的候选。与前次排除的 Medieval Hexagon Pack 不同。**

- 作者明确列出三种摆放风格：hexagonal、square、free placement。
- 200+ 中世纪场景资源，10+ 建筑、防御墙、道路及连接块、水面和装饰。
- 免费/自愿付费，CC0，作者允许个人和商业使用且不要求署名。
- 作者列出 FBX、OBJ、DAE、glTF。
- 作者页面顶部明确注明旧版不再更新；页面下方 FAQ 仍有旧的更新承诺，应以顶部 Legacy 说明为准。
- 官方图库可直接看到正方形地块、直墙和转角。方格支持已有作者文字与图片证据，但具体建筑尺寸、枢轴、屋檐边界和 Unity 材质仍未实测。
- 与 Townscaper 的关系（视觉判断）：橙色屋顶、小建筑和微缩比例可参考；原配色偏鲜亮、墙体偏军事城堡，需要软化配色并简化细节。

来源：[作者发布页](https://kaylousberg.itch.io/kaykit-medieval-builder-pack)。

![官方资源总览，左侧为方形地块，下方为建筑及墙](https://img.itch.zone/aW1hZ2UvMTE0ODY1MS82NzE1NTEyLnBuZw==/original/t%2F2WaG.png)

![官方三种布局示例，左侧为方格摆放](https://img.itch.zone/aW1hZ2UvMTE0ODY1MS82NzE1NTIzLnBuZw==/original/BRUymT.png)

## 4. 新候选 E：Kenney Fantasy Town Kit

- 官网列 160 个文件、3D、颜色变体、城镇/墙/建筑题材，2.0 已重制；免费、CC0。
- 已检查官方总览和拼装示例：可见矩形墙板、门窗、屋顶及四边形地面构件，可以组合建筑；不将所有建筑直接称为标准正方形。
- 可考虑用于核心建筑和方形塔身；每种角色需要先拼装成符合单块积木占地的候选外观。
- 与 Townscaper 的关系（视觉判断）：简单坡屋顶和小窗、低细节模块便于修改；当前木梁较明显、屋顶有红/绿两组，需改为彩色墙面与统一暖橙屋顶，减轻木梁的视觉比重。
- 比直接套用大房屋更容易控制占地，但需要拼装与材质调整。官网未列此包完整格式清单，未检查压缩包，暂不承诺特定文件格式。

来源：[作者官网](https://kenney.nl/assets/fantasy-town-kit)。

![Kenney 官方拼装示例](https://kenney.nl/media/pages/assets/fantasy-town-kit/c13eafebab-1754222363/sample.png)

## 5. 新候选 F：Kenney Building Kit（模块构件，低优先级）

- 作者介绍有墙、地板、门窗等；80+ 模型，OBJ/FBX/glTF；免费/自愿付费、CC0；页面声明支持 Unity 等引擎。
- 官方预览有方形地板和矩形墙体，可作为制作方格建筑的模块素材。
- 原示例为灰色平顶建筑，与 Townscaper 的橙色坡屋顶和彩色砖墙差异较大；需要补屋顶与材质。适合希望自行定制核心和塔的情况，不作为最接近 Townscaper 的现成外观推荐。
- 具体尺寸、连接规则和 Unity 导入未验证。

来源：[作者发布页](https://kenney-assets.itch.io/building-kit)、[作者官网](https://kenney.nl/assets/building-kit)。

![Kenney Building Kit 官方拼装示例](https://kenney.nl/media/pages/assets/building-kit/617be3e07b-1718801879/sample.png)

## 6. 新候选 G：Quaternius Medieval Village Pack（备选）

- 44 个模型，中世纪建筑及道具；FBX、OBJ、Blend；作者说明免费用于个人和商业项目，CC0。
- 官方预览有房屋与高塔，能提供核心/塔的造型素材；没有确认适合本项目的完整独立防御墙组。
- 与 Townscaper 的关系（视觉判断）：坡屋顶与微缩建筑方向相近，但木梁、屋瓦、石块细节更多，原配色更浓重。要接近目标风格需要减少细节并统一配色。
- 部分建筑是长方形或带外伸台阶/附属建筑，不能仅缩放就保证单格适配；需要针对具体模型筛选，优先级低于 D/E。

来源：[作者官网](https://quaternius.com/packs/medievalvillage.html)。

![Quaternius 官方建筑预览](https://quaternius.com/assets/images/fullres/medievalvillage.jpg)

## 7. 本轮建议

用户已选择 D；E/F/G 保留为研究备选，不继续作为本次方案主资源。D 的方格支持和防御墙依据更明确。配色与表现继续参考 Townscaper，但没有将宣传兼容性当作本项目实测；选定资源包不表示已经导入或确认具体模型尺寸。

具体核心、墙、塔的模型包文件、占地、方向、材质和许可随附文件需继续核查；项目场景保持现状。

已完成首次包内检查，真实文件名、OBJ 占地尺寸和许可记录见 [D 的选定与模型核查](kaykit-d-selection.md)。已确认 castle、watchtower、直墙、转角和正方形地块；尚未导入 Unity。
