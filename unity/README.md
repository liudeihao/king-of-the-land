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
- **里程碑五（起步）**：**压力/勇气/好奇**；好奇略提高游荡**挪步**频率（`CuriosityWanderLively`）；**婴/幼/少**有概率在游荡后朝**在世亲代**再挪一步（`ParentImitateBaseChance`）。**同族印象**、**果记**、**雌体压力与受孕**见前文。上列均在 `SimParams`，L1 可按字段名改。
- **L1 数据 Mod**：`Assets/StreamingAssets/Mods/<文件夹>/` 下至少需 `**mod.json` 与/或 `sim_params.json`**. `mod.json` 可含 `id` / `version` / `kind`（`core` 在拓扑同层优先）/ `dependencies` / `conflicts`。**`sim_params.json` 的 `patches[].key` 对 `SimParams` 的公开实例字段名**（如 `DroughtStartTick`、`PeerMemoryWanderBias`，大小写不敏感），`value` 为数字字符串；**不再用手写白名单**，凡 `SimParams` 里 **int / float / double** 字段均可被数据 Mod 改。依依赖序叠到默认 `SimParams` 再建局；任一步整包解析失败则回退默认。样例 `**000_landking_core`** + `**001_slower_crisis**`。删 `Mods` 可测纯默认。
- **存档 v1**（`WorldSaveV1` + `SimRng`）：保存到 `Application.persistentDataPath` 下的 `landking_save_v1.json`，含参数快照、地图、全部猿、水位、PRNG 状态，以及**本局 L1 包文件夹/展示名顺序**与可选 **Chronicle**；**F5** 存、**F9** 读。若读档时 StreamingAssets 里 Mod 组合与存盘时不同，会在事件栏与 Log 中**提示**（世界仍以存档内数据为准）。**V** 开关「镜头跟随当前选中」。
- 更细的玩法说明见 `docs/实现/原型构建步骤.md`。