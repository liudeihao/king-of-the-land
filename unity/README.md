# Unity 工程目录

**已创建（请与 `ProjectSettings/ProjectVersion.txt` 对稿）**：

- 编辑器 **6000.3.13f1**
- Hub 模板 **Universal 2D**（URP，包 `com.unity.render-pipelines.universal`）

本目录为 Unity 项目根（`Assets/`、`Packages/`、`ProjectSettings/`）。

- 实现与引擎约定见 `[docs/实现/技术选型.md](../docs/实现/技术选型.md)`、`[docs/实现/原型构建步骤.md](../docs/实现/原型构建步骤.md)`。
- 勿将 `docs/` 或规格 Markdown 放入 `Assets/`，与仓库根 `docs/` 保持分离。

## 原型（核心循环 + L1 试装）

在 **SampleScene** 中按 **Play**：由 `LandKing.Prototype.PrototypeEntry` 自动生成 `PrototypeGameRoot`，运行 20×20 地图、10 只猿、分阶段 Tick 与旱灾/降雨。

- **L1 数据 Mod**：`Assets/StreamingAssets/Mods/<文件夹>/mod.json`（展示名）+ `sim_params.json`（`patches` 键值，见 `L1ParamPatchFile` 支持的字段）。子文件夹名排序依次合并到 `SimParams`。自带示例 `001_slower_crisis`：更晚的旱情起点与更慢水位下降。删除该文件夹即恢复纯默认表。
- 更细的玩法说明见 `docs/实现/原型构建步骤.md`。