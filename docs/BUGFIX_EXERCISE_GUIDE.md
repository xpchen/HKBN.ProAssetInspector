# HKBN.ProAssetInspector — bug-fix 分支练习说明（面试官 / 同事用）

> **分支**：`bug-fix`（已预埋缺陷）  
> **对照基准**：`master`（完整正确版本）  
> **用途**：交给候选人修复，或由同事按本文验收修复结果。  
> **请勿**将本文档直接发给候选人。

---

## 使用方式

1. 候选人从 `bug-fix` 分支拉取代码，在 Visual Studio 中编译并调试 Add-in。
2. 打开 `Projects/ProAssetInspector/ProAssetInspector.aprx`，启用 Add-in，使用 **HKBN Tools → Asset Inspector**。
3. 按下方测试用例复现问题 → 修改代码 → 再跑测试用例验收。
4. 面试官可用 `git diff master..bug-fix` 查看预埋改动范围（共 6 处主题）。

---

## 缺陷清单总表

| 编号 | 缺陷简述 | 预期现象（修复前） | 修复后预期 | 主要涉及文件 |
|------|----------|-------------------|------------|--------------|
| **L2** | 电力资产关键词不完整 | `Cable` / `Transformer` 缺电压不报警 | 二者缺 `VoltageLevel` 时出现 `MISSING_VOLTAGE_LEVEL`（Warning） | `Models/FieldMapping.cs` |
| **L5** | CSV 未转义换行符 | 导出含换行的 `Message` 时 Excel 列错位 | 含 `\r`/`\n` 的字段被双引号包裹，列对齐正确 | `Utilities/CsvWriter.cs` |
| **L7** | 刷新图层不保留选中项 | **Refresh Layers** 后下拉框总回到第一项 | 若原图层仍存在，刷新后仍选中该 `LayerName` | `DockPanes/AssetInspectorDockPaneViewModel.cs` |
| **L8** | 字段名子串匹配 | 误匹配含 `ID` 子串的字段（如 `OBJECTID` 抢在 `AssetID` 前） | 仅精确匹配候选名（大小写不敏感） | `Utilities/FieldValueReader.cs` |
| **M1** | UI 集合在 `QueuedTask` 内更新 | **Load Selected Assets** 可能崩溃、表格不刷新或状态异常 | 仅在 `QueuedTask` 内读 GIS 数据返回 DTO，UI 线程再更新 `ObservableCollection` | `DockPanes/AssetInspectorDockPaneViewModel.cs` |
| **M2** | 未枚举 GroupLayer 内图层 | 下拉框缺少组内图层（如 `Asset_Pole`） | 与 `master` 一致，列出全部 `FeatureLayer`（含组内） | `Services/MapLayerService.cs` |

---

## 分项说明与测试用例

### L2 — 电力资产电压校验关键词缺失

**缺陷说明**  
`FieldMapping.PowerAssetKeywords` 中缺少 `Cable`、`Transformer`。`AssetValidationService` 对这两类资产不会触发 `MISSING_VOLTAGE_LEVEL` 规则。

**修复前预期**  
1. 打开 `ProAssetInspector.aprx`，加载选择集：在 **Asset_Cable** 选 `CBL-003`（无电压），在 **Asset_Transformer** 选 `XFMR-002`（无电压）。  
2. **Load Selected Assets** → **Validate Assets**。  
3. 结果中**没有**针对上述两条的 `MISSING_VOLTAGE_LEVEL`（Warning）。  
4. 对比：选 **Asset_Pole** 的 `POLE-004`（Pole、无电压）应仍出现 `MISSING_VOLTAGE_LEVEL`。

**修复后预期**  
- `Cable`、`Transformer` 且 `VoltageLevel` 为空时，均产生：  
  - `Severity` = Warning  
  - `IssueCode` = `MISSING_VOLTAGE_LEVEL`  
  - `Message` = `VoltageLevel is required for power assets.`

**测试用例**

| 步骤 | 操作 | 期望（修复后） |
|------|------|----------------|
| L2-1 | 仅选 `Asset_Cable` / `CBL-003` → Load → Validate | 1 条 `MISSING_VOLTAGE_LEVEL` |
| L2-2 | 仅选 `Asset_Transformer` / `XFMR-002` → Load → Validate | 1 条 `MISSING_VOLTAGE_LEVEL` |
| L2-3 | 选 `Asset_Pole` / `POLE-004` → Load → Validate | 仍有 `MISSING_VOLTAGE_LEVEL`（回归） |
| L2-4 | 选 `Asset_Building` / `BLD-001`（非电力）→ Validate | 无电压相关 Warning |

**修复要点**  
恢复 `PowerAssetKeywords` 为完整列表：`Pole`, `Line`, `Cable`, `Transformer`, `Switch`, `Power`（与 `master` 一致）。

---

### L5 — CSV 换行符未转义

**缺陷说明**  
`CsvWriter.Escape` 仅在含逗号或双引号时加引号，未处理 `\r`、`\n`。

**修复前预期**  
1. 制造至少一条 `Message` 含换行的校验结果（或临时在验证逻辑里写死一条多行 Message）。  
2. **Export Validation Report**，用 Excel 打开 CSV。  
3. 该行被拆成多行，列错位。

**修复后预期**  
- 含换行的字段整段落在双引号内；Excel 单行显示，列数与表头一致（7 列）。  
- 仍保持 UTF-8 BOM。

**测试用例**

| 步骤 | 操作 | 期望（修复后） |
|------|------|----------------|
| L5-1 | Validate 得到多条 Issues → Export | 文件可打开，表头 7 列 |
| L5-2 | 在 Excel 中检查含换行 `Message` 的行 | 单行、列对齐 |
| L5-3 | 检查 `Severity,IssueCode,...` 表头 | 中文/英文均不乱码（BOM 仍有效） |
| L5-4 | 字段含逗号、双引号 | 仍正确转义（回归） |

**修复要点**  
```csharp
var needsQuotes = value.Contains(',') || value.Contains('"')
    || value.Contains('\r') || value.Contains('\n');
```

---

### L7 — Refresh Layers 不保留当前图层

**缺陷说明**  
`RefreshLayersAsync` 未保存 `previousLayer`，刷新后总是 `SelectedLayer = Layers.FirstOrDefault()`。

**修复前预期**  
1. 图层下拉选 **Asset_Cable**（或任意非第一项）。  
2. 点击 **Refresh Layers**。  
3. 下拉框跳回列表第一项（如 **Asset_Building**），图层详情随之改变。

**修复后预期**  
- 刷新前选中 `Asset_Cable`，刷新后仍选中 **Asset_Cable**（若该层仍在地图中）。  
- 若该层已不存在，则回退到第一项。

**测试用例**

| 步骤 | 操作 | 期望（修复后） |
|------|------|----------------|
| L7-1 | 选 **Asset_Transformer** → Refresh Layers | 仍为 **Asset_Transformer** |
| L7-2 | 展开并确认组内图层在列表中（配合 M2 修复后） | 可选 **Asset_Pole** 等 |
| L7-3 | 无地图时 Refresh | 提示 `No active map view.`，不崩溃 |

**修复要点**  
刷新前 `var previousLayer = SelectedLayer?.LayerName;`，填充列表后按名称恢复选中。

---

### L8 — 字段名子串匹配（非精确匹配）

**缺陷说明**  
`FieldValueReader.FindField` 使用 `IndexOf` 子串匹配。候选名 `ID` 会匹配 `OBJECTID`，在候选顺序中若先匹配到 `ID`，可能读错字段。

**修复前预期**  
1. 在 **Asset_Pole** 等有标准 `AssetID` 的图层上，Load 选中要素。  
2. 部分要素的 `AssetID` 显示为数字型 OID，或出现奇怪字段读取 Warning。  
3. 与 `master` 对比：同一选择集下 `AssetID` 应对应业务字段 `AssetID` 列（如 `POLE-001`）。

**修复后预期**  
- 按候选列表**精确**匹配字段名（大小写不敏感）。  
- `Asset_Pole` 的 `POLE-002`（空 AssetID）仍报 `MISSING_ASSET_ID`，不误用 `OBJECTID` 充填（除非业务上仅配置了 OBJECTID 候选且无 AssetID 字段）。

**测试用例**

| 步骤 | 操作 | 期望（修复后） |
|------|------|----------------|
| L8-1 | 选 **Asset_Pole** / `POLE-001` → Load | `AssetID` = `POLE-001` |
| L8-2 | 选 **Asset_Pole** / `POLE-002`（空 AssetID）→ Validate | `MISSING_ASSET_ID`（Error） |
| L8-3 | 选 **Asset_Cable** / `CBL-001` → Load | `AssetID` = `CBL-001`（读 `ASSET_ID` 别名） |
| L8-4 | 选 **Asset_Misc** → Load | 有 `FIELD_READ_WARNING`（缺标准字段），不崩溃 |

**修复要点**  
恢复字典精确查找 + 按 `candidateFieldNames` 顺序优先，与 `master` 中 `FindField` 实现一致。

---

### M1 — 在 QueuedTask 内更新 ObservableCollection（线程违规）

**缺陷说明**  
`LoadSelectedAssetsAsync` 在 `QueuedTask.Run` 委托内对 `Assets`、`Issues` 等绑定集合执行 `Clear`/`Add`，违反 Pro SDK 线程约定。

**修复前预期**  
1. 地图上多选若干要素。  
2. 多次点击 **Load Selected Assets**。  
3. 可能出现：未处理异常、表格空白、Pro 不稳定；调试器可见跨线程访问绑定集合。

**修复后预期**  
1. `QueuedTask.Run` 仅调用 `_assetSelectionService.LoadSelectedAssetsAsync()` 并返回 `List<AssetRecord>`。  
2. `await` 结束后在 UI 线程 `Clear`/`Add`。  
3. 连续 Load 10 次仍稳定，表格条数与选择一致。

**测试用例**

| 步骤 | 操作 | 期望（修复后） |
|------|------|----------------|
| M1-1 | 跨 **Asset_Pole** + **Asset_Cable** 多选 → Load | 表格多行，条数 = 选择数 |
| M1-2 | 连续快速点击 Load 5 次 | 无崩溃，最终数据正确 |
| M1-3 | Load 后 Validate | Issues 正常生成 |
| M1-4 | 无选择时 Load | 提示 `No selected features found.` |

**修复要点**  
与 `master` 相同模式：

```csharp
var records = await QueuedTask.Run(() => _assetSelectionService.LoadSelectedAssetsAsync());
Assets.Clear();
// ... UI 线程更新集合
```

---

### M2 — 未递归 GroupLayer 内 FeatureLayer

**缺陷说明**  
`MapLayerService` 使用 `map.Layers`（仅顶层）。嵌套在 **Distribution Assets** / **Transmission Assets** 下的图层不会出现在列表中。

**修复前预期**  
1. 打开 `ProAssetInspector.aprx`（组内图层已勾选可见）。  
2. **Refresh Layers**。  
3. 下拉框仅有顶层 **Asset_Building**、**Asset_Misc** 等，**没有** `Asset_Pole`、`Asset_Cable`、`Asset_PowerLine` 等组内图层。  
4. Contents 面板中能看到组内图层，与 DockPane 列表不一致。

**修复后预期**  
- **Refresh Layers** 列出 7 个业务图层（与 `master` 一致）。  
- `FindFeatureLayerByName` 同样使用 flatten 列表，Zoom/Select 可找到组内图层（若已实现导航功能）。

**测试用例**

| 步骤 | 操作 | 期望（修复后） |
|------|------|----------------|
| M2-1 | Refresh Layers | 列表含 `Asset_Pole`, `Asset_Cable`, `Asset_Transformer`, `Asset_PowerLine`, `Asset_Switch`, `Asset_Building`, `Asset_Misc`（7 个） |
| M2-2 | 选中 **Asset_Pole** | 图层详情显示 Point、字段数 > 0 |
| M2-3 | 选组内要素 Load → Validate | 可正常加载、校验 |
| M2-4 | 与 Contents 中 FeatureLayer 数量对比 | 一致（仅 FeatureLayer，不含组名本身） |

**修复要点**  
`GetFeatureLayersAsync` 与 `FindFeatureLayerByName` 均改为：

```csharp
map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
```

---

## 验收检查表（同事勾选）

| 编号 | 测试通过 | 备注 |
|------|----------|------|
| L2 | ☐ | |
| L5 | ☐ | |
| L7 | ☐ | |
| M1 | ☐ | |
| M2 | ☐ | |
| L8 | ☐ | |

**建议验收顺序**：M2 → L7 → M1 → L8 → L2 → L5（先保证地图与线程，再业务规则与导出）。

---

## 与 master 对比命令

```bat
git fetch origin
git diff origin/master..origin/bug-fix --stat
git diff origin/master..origin/bug-fix -- Models/FieldMapping.cs Utilities/CsvWriter.cs Utilities/FieldValueReader.cs Services/MapLayerService.cs DockPanes/AssetInspectorDockPaneViewModel.cs
```

---

## 环境要求

- ArcGIS Pro 3.3+（与 Add-in `desktopVersion` 一致）
- Visual Studio 2022 + ArcGIS Pro SDK
- 已运行或已包含演示数据：`Projects/ProAssetInspector/scripts/CreateProAssetInspectorFeatureClasses.py`（`--recreate`）

---

## 参考文档

- [ProGuide DockPanes](https://doc.esri.com/en/arcgis-pro/latest/sdk/api-reference/conceptdocs/docs/ProGuide-DockPanes.html)
- [QueuedTask](https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic9828.html) — 项目 README 线程说明
- 仓库 `master` 分支 [README.md](../README.md)
