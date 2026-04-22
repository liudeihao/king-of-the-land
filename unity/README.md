# Unity 工程目录

**已创建（请与 `ProjectSettings/ProjectVersion.txt` 对稿）**：

- 编辑器 **6000.3.13f1**
- Hub 模板 **Universal 2D**（URP，包 `com.unity.render-pipelines.universal`）

本目录为 Unity 项目根（`Assets/`、`Packages/`、`ProjectSettings/`）。

- 实现与引擎约定见 `[docs/实现/技术选型.md](../docs/实现/技术选型.md)`、`[docs/实现/原型构建步骤.md](../docs/实现/原型构建步骤.md)`。
- 勿将 `docs/` 或规格 Markdown 放入 `Assets/`，与仓库根 `docs/` 保持分离。

## 原型（核心循环 + L1 试装）

在 **SampleScene** 中按 **Play**：由 `LandKing.Prototype.PrototypeEntry` 自动生成 `PrototypeGameRoot`，运行 20×20 地图、10 只猿、分阶段 Tick 与旱灾/降雨。

- **编年史持久化**：`WorldSaveV1.Chronicle` 与 `SimParams.ChronicleMaxEntries`（0=按 64、钳 8..256）控制环形条数；读档时右侧由存档重放。展示由 `WorldEventFormatting` 单点维护。
- **MVP/里程碑四向**：西、东两岸各一群；旱灾时**河道**随总水位发灰；`SimParams.EastShoreNarrativeTick`（0=不播）可改东岸叙事时刻；[Tab] 在存活者间环选；HUD 有暂停提示与「正在追踪」。
- **里程碑五（起步）**：每只猿有 0–1 **压力**（`Stress`），旱/饿升、饱腹易降；成年**社交步进**概率随压力略降。`SimParams.StressDroughtScale` / `StressHungerScale` / `StressRelaxPerTick` / `SocialStressInhibit` 可调；存档含 `Stress`。
- **L1 数据 Mod**：`Assets/StreamingAssets/Mods/<文件夹>/` 下至少需 `**mod.json` 与/或 `sim_params.json`**. `mod.json` 可含 `id` / `version` / `kind`（`core` 在拓扑同层优先）/ `dependencies`（`id` + `version` 范围，如 `>=1.0.0`）/ `conflicts`（同批不能共存）。`sim_params.json` 仍为 `patches` 表，按**依赖解析后的顺序**叠到 `SimParams`；任一步失败则**整批**回退默认并打 Log + HUD 首条错误。样例 `**000_landking_core`**（仅元数据、无补丁）+ `**001_slower_crisis**`（依赖 `landking.core`、改旱灾参数）。删除 `Mods` 下内容可测纯默认。
- **存档 v1**（`WorldSaveV1` + `SimRng`）：保存到 `Application.persistentDataPath` 下的 `landking_save_v1.json`，含参数快照、地图、全部猿、水位、PRNG 状态，以及**本局 L1 包文件夹/展示名顺序**与可选 **Chronicle**；**F5** 存、**F9** 读。若读档时 StreamingAssets 里 Mod 组合与存盘时不同，会在事件栏与 Log 中**提示**（世界仍以存档内数据为准）。**V** 开关「镜头跟随当前选中」。
- 更细的玩法说明见 `docs/实现/原型构建步骤.md`。