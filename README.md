# HKBN.ProAssetInspector

ArcGIS Pro Add-in for map-based asset inspection and validation.

> **bug-fix 分支**：本分支含面试/练习用预埋缺陷。面试官或同事请参阅 [docs/BUGFIX_EXERCISE_GUIDE.md](docs/BUGFIX_EXERCISE_GUIDE.md)（勿发给候选人）。正确实现请以 `master` 为准。 Reads selected features from the active map, applies business rules, and supports zoom, selection, and CSV export.

## Features

- **HKBN Tools** ribbon tab with **Asset Inspector** button
- **HKBN Asset Inspector** dock pane
- Map status and feature layer list (including layers inside **GroupLayer**)
- Layer metadata (geometry type, feature class name, field counts, key field detection)
- Load multi-layer map selections into an asset grid
- Loose field name mapping (AssetID, ASSET_ID, Type, etc.)
- Asset validation (errors, warnings, info)
- Zoom to and select features on the map
- Export validation results to UTF-8 BOM CSV (Excel-friendly)

## Prerequisites

- ArcGIS Pro 3.3+ (SDK references installed under `C:\Program Files\ArcGIS\Pro`)
- Visual Studio 2022 with MSBuild (for full add-in packaging)
- .NET 8 Windows x64

## Demo data setup

Run the arcpy script **before** testing with the sample project:

```bat
cd /d D:\Workspace\HKBN.ProAssetInspector\Projects\ProAssetInspector\scripts
"C:\Program Files\ArcGIS\Pro\bin\Python\envs\arcgispro-py3\python.exe" CreateProAssetInspectorFeatureClasses.py --recreate
```

This creates seven feature classes in `Projects/ProAssetInspector/ProAssetInspector.gdb` and configures `ProAssetInspector.aprx` with group layers:

| Group / layer | Feature classes |
|---------------|-----------------|
| Distribution Assets | Asset_Pole, Asset_Cable, Asset_Transformer |
| Transmission Assets | Asset_PowerLine, Asset_Switch |
| Top-level | Asset_Building, Asset_Misc |

## DAML note (DockPane)

Per [Esri DockpaneSimple sample](https://github.com/Esri/arcgis-pro-sdk-community-samples/blob/master/Framework/DockpaneSimple/Config.daml) and [DockPaneManager.Find](https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic10118.html), `<dockPanes>` must be declared **inside** `<insertModule>`, not at the root of `<ArcGIS>`. The `dockPane` `id` must match the string passed to `DockPaneManager.Find()` (for example `HKBN_ProAssetInspector_AssetInspectorDockPane`).

## Build and run the Add-in

1. Open `HKBN.ProAssetInspector.sln` in Visual Studio 2022.
2. Build the solution (F6). Use Visual Studio MSBuild if `dotnet build` fails on SDK packaging targets.
3. Press F5 to start ArcGIS Pro (see `Properties\launchSettings.json`).
4. Enable the add-in under **Settings → Add-In Manager** if needed.
5. Open `Projects\ProAssetInspector\ProAssetInspector.aprx`.
6. On the **HKBN Tools** tab, click **Asset Inspector**.

## Test steps

1. Open the dock pane with no map → status shows **No active map view.** (no crash).
2. Open `ProAssetInspector.aprx` → **Refresh Layers** → seven feature layers listed.
3. Select features across multiple layers → **Load Selected Assets** → grid populated.
4. **Validate Assets** → issues for missing AssetID, invalid status, voltage rules, field warnings.
5. Select a row → **Zoom To Selected Asset** / **Select In Map**.
6. **Export Validation Report** → open CSV in Excel; Chinese/UTF-8 displays correctly.

## Field mapping

| Property | Candidate field names |
|----------|------------------------|
| AssetID | AssetID, ASSETID, ASSET_ID, asset_id, ID, OBJECTID |
| AssetType | AssetType, ASSETTYPE, ASSET_TYPE, asset_type, Type, TYPE |
| Status | Status, STATUS, status, AssetStatus, ASSET_STATUS |
| VoltageLevel | VoltageLevel, VOLTAGELEVEL, VOLTAGE_LEVEL, voltage_level, Voltage, VOLTAGE |
| Owner | Owner, OWNER, owner, Maintainer, MAINTAINER |

Matching is case-insensitive; the first existing field name in the list is used.

## ArcGIS Pro SDK threading

- Access maps, layers, selection sets, feature classes, row cursors, and geometry inside `QueuedTask.Run`.
- Return plain `List` / DTO objects from `QueuedTask.Run`.
- Update `ObservableCollection` and other WPF-bound state on the UI thread after the task completes.
- Do not update bound collections from inside `QueuedTask.Run`.

## Project structure

```
HKBN.ProAssetInspector/
├── Commands/
├── DockPanes/
├── Models/
├── Services/
├── Utilities/
├── Projects/ProAssetInspector/
│   ├── ProAssetInspector.gdb
│   ├── ProAssetInspector.aprx
│   └── scripts/CreateProAssetInspectorFeatureClasses.py
├── Config.daml
└── README.md
```

## Known limitations

- No connection to HKBN production databases; works on current map feature layers only.
- Layer lookup uses layer name only (first match wins).
- Validation rules and allowed status values are hard-coded in `AssetValidationService`.
- arcpy script requires a local ArcGIS Pro license.

## Future extensions

- HKBN production field mapping
- Utility Network integration
- JSON-driven validation rules
- Excel export
- D&C Nexus Core unified asset model
