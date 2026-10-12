# 已选定 D：KayKit Medieval Builder Pack 1.0

日期：2026-10-11。用户明确回复“选择 d”。选定资源为 **KayKit Medieval Builder Pack 1.0（Legacy）的正方形版本**；配色与表现继续参考 Townscaper。原始“暂不立即使用”的要求保留：本轮下载至独立检查目录并核对模型，没有导入 Unity Assets，没有替换场景或修改运行代码。

## 来源和检查记录

- [作者 itch.io 发布页](https://kaylousberg.itch.io/kaykit-medieval-builder-pack)：确认版本、square 支持、免费 CC0，且 Legacy 已停止更新。
- [作者在 OpenGameArt 的发布页](https://opengameart.org/content/kaykit-medieval-builder-pack-10)、[作者账号](https://opengameart.org/users/kay-lousberg)：取得该作者发布的 1.0 压缩包。
- [下载文件](https://opengameart.org/sites/default/files/kaykit_medieval_builder_pack_1.0.zip)。
- 独立存放：`C:/Users/孙含睿/.codex/visualizations/2026/10/11/01a12931-f752-7023-8b0d-8ef3226c4b4a/kaykit-d-staging/kaykit_medieval_builder_pack_1.0.zip`。
- SHA256：`588D931B62A33D71BB114078F0A7C02309A97A25C51A47083CF077BD97EDAA3E`。
- 已读取包内 `License.txt`：作者 Kay Lousberg，CC0，允许个人、教育和商业项目，署名非强制。
- 包内检查发现 226 FBX、226 GLB、226 DAE、229 OBJ、229 MTL、14 PNG。这些是文件计数，不当作去重后的模型数量。网页写 glTF，实际压缩包提供其 GLB 二进制形式。

## 具体角色搭配

以下是所选包内真实存在的文件，作为第一轮适配建议。Unity 尚未导入；FBX 尺寸与材质还需在引擎内验证。表内尺寸来自对应 OBJ 的顶点包围盒，顺序为 X×Y×Z，单位是原始模型坐标，不代表毫米或 Unity 导入后的最终尺寸。

| 用途 | 包内 FBX | 对应 OBJ 尺寸 X×Y×Z | 适配判断 |
| --- | --- | --- | --- |
| 核心 | `Models/objects/fbx/castle.fbx` | 1.8048×2.5328×1.8523 | 水平范围小于 2×2 地块，轮廓较高，适合作核心城堡 |
| 防御塔 | `Models/objects/fbx/watchtower.fbx` | 0.9707×1.9785×1.7131 | 水平范围小于 2×2 地块；需调整摆放中心并核对朝向。模型存在不意味着已有塔防攻击动画 |
| 墙（直段） | `Models/objects/fbx/wall_straight.fbx` | 2.0000×1.1524×0.5343 | 长度等于地块边长，墙体较薄；独立积木的逻辑占地仍是完整 2×2 寻路格 |
| 墙（转角素材） | `Models/objects/fbx/wall_corner.fbx` | 1.3778×1.1524×1.3778 | 包围盒中心偏离原点，需要专门定位；作为转角外观素材保留 |
| 正方形地块 | `Models/tiles/square/fbx/square_forest.fbx` | 2.0000×1.0000×2.0000 | 实际 X/Z 等长，支持方格判断；高度较厚，未来需要安排底板高度 |

同名 OBJ 均存在于 `Models/objects/obj/` 或 `Models/tiles/square/obj/`，并有对应 MTL。

原始 OBJ 包围盒：

- castle：min=(-0.902419,-0.025685,-0.949929)，max=(0.902419,2.507078,0.902419)。
- watchtower：min=(-0.476962,-0.025685,-0.879982)，max=(0.493725,1.952801,0.833112)。
- wall_straight：min=(-1,0,-0.267157)，max=(1,1.152435,0.267157)。
- wall_corner：min=(-0.999999,0,-0.999999)，max=(0.377816,1.152435,0.377815)。
- square_forest：min=(-1,0,-1)，max=(1,1,1)。

由此可以确认选中对象具备放入 2×2 水平范围的初步条件，但不能宣称完整的碰撞、材质、旋转、屋檐和动态武器适配已经完成。转角需要正确的偏移，直墙的外观与逻辑占地应明确区分。

## 待应用的视觉方向

核心以柔和青绿色标识，墙采用暖灰/乳白，塔采用赭黄；保留暖橙屋顶和柔和阴影，参考 Townscaper 的微缩城镇观感。上述配色只影响游戏模型表现，实体照片颜色仍由用户标定。

一块积木继续对应一个关卡对象并占 2×2 寻路格，模型薄墙的空白部分不自动变为通路。地块目录明确采用 `tiles/square`。敌人另行选型；本次选定建筑包没有连带选定敌人模型。

本轮状态：**资源 D 已选定，具体模型与包内许可已核查，仍待 Unity 导入验证和用户后续的应用指令。**
