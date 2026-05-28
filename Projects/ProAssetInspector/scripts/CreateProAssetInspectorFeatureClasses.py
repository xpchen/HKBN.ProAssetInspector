# -*- coding: utf-8 -*-
"""
Create demo feature classes in ProAssetInspector.gdb and configure ProAssetInspector.aprx.
Run from ArcGIS Pro Python: python CreateProAssetInspectorFeatureClasses.py [--recreate]
"""
import argparse
import os
import sys

import arcpy

# Base coordinates (Hong Kong 1980 Grid, WKID 2326)
ORIGIN_X = 836000.0
ORIGIN_Y = 816000.0

STANDARD_FIELDS = [
    ("AssetID", "TEXT", 50),
    ("AssetType", "TEXT", 50),
    ("Status", "TEXT", 50),
    ("VoltageLevel", "DOUBLE", None),
    ("Owner", "TEXT", 100),
]

CABLE_FIELDS = [
    ("ASSET_ID", "TEXT", 50),
    ("ASSET_TYPE", "TEXT", 50),
    ("ASSET_STATUS", "TEXT", 50),
    ("VOLTAGE_LEVEL", "DOUBLE", None),
    ("OWNER", "TEXT", 100),
]

MISC_FIELDS = [
    ("Type", "TEXT", 50),
    ("status", "TEXT", 50),
]

FEATURE_CLASSES = [
    {
        "name": "Asset_Pole",
        "geometry": "POINT",
        "fields": STANDARD_FIELDS,
        "samples": [
            # Expected: OK
            {"xy": (0, 0), "attrs": ("POLE-001", "Pole", "Active", 11.0, "HKBN")},
            # Expected: MISSING_ASSET_ID
            {"xy": (50, 0), "attrs": (None, "Pole", "Active", 11.0, "HKBN")},
            # Expected: INVALID_STATUS
            {"xy": (100, 0), "attrs": ("POLE-003", "Pole", "Broken", 11.0, "HKBN")},
            # Expected: MISSING_VOLTAGE_LEVEL
            {"xy": (150, 0), "attrs": ("POLE-004", "Pole", "Active", None, "HKBN")},
            # Expected: MISSING_ASSET_TYPE
            {"xy": (200, 0), "attrs": ("POLE-005", None, "Planned", 11.0, "HKBN")},
            {"xy": (250, 0), "attrs": ("POLE-006", "Pole", "Retired", 11.0, "Contractor")},
        ],
    },
    {
        "name": "Asset_PowerLine",
        "geometry": "POLYLINE",
        "fields": STANDARD_FIELDS,
        "samples": [
            {"coords": [(0, -100), (200, -100)], "attrs": ("LINE-001", "Power Line", "Active", 132.0, "HKBN")},
            {"coords": [(0, -150), (200, -150)], "attrs": ("LINE-002", "Line", "Under Construction", 33.0, "HKBN")},
            # Expected: INVALID_VOLTAGE_LEVEL
            {"coords": [(0, -200), (200, -200)], "attrs": ("LINE-003", "Cable", "Active", -1.0, "HKBN")},
            {"coords": [(0, -250), (200, -250)], "attrs": ("LINE-004", "Power Line", "Inactive", None, "HKBN")},
        ],
    },
    {
        "name": "Asset_Cable",
        "geometry": "POLYLINE",
        "fields": CABLE_FIELDS,
        "samples": [
            {"coords": [(300, 0), (500, 0)], "attrs": ("CBL-001", "Cable", "Active", 11.0, "HKBN")},
            {"coords": [(300, -50), (500, -50)], "attrs": ("CBL-002", "Cable", "Planned", 22.0, "Maintainer")},
            {"coords": [(300, -100), (500, -100)], "attrs": ("CBL-003", "Cable", "Retired", None, "HKBN")},
        ],
    },
    {
        "name": "Asset_Transformer",
        "geometry": "POINT",
        "fields": STANDARD_FIELDS,
        "samples": [
            {"xy": (0, 100), "attrs": ("XFMR-001", "Transformer", "Active", 11.0, "HKBN")},
            {"xy": (50, 100), "attrs": ("XFMR-002", "Transformer", "Active", None, "HKBN")},
            {"xy": (100, 100), "attrs": ("XFMR-003", "Transformer", "Planned", 22.0, "HKBN")},
        ],
    },
    {
        "name": "Asset_Switch",
        "geometry": "POINT",
        "fields": STANDARD_FIELDS,
        "samples": [
            {"xy": (0, 200), "attrs": ("SW-001", "Switch", "Active", 11.0, "HKBN")},
            {"xy": (50, 200), "attrs": ("SW-002", "Switch Gear", "Inactive", 11.0, "HKBN")},
        ],
    },
    {
        "name": "Asset_Building",
        "geometry": "POLYGON",
        "fields": STANDARD_FIELDS,
        "samples": [
            {
                "ring": [(600, 0), (700, 0), (700, 100), (600, 100), (600, 0)],
                "attrs": ("BLD-001", "Building", "Active", None, "HKBN"),
            },
            {
                "ring": [(600, -100), (700, -100), (700, -50), (600, -50), (600, -100)],
                "attrs": ("BLD-002", "Office", "Planned", None, "Owner Co"),
            },
        ],
    },
    {
        "name": "Asset_Misc",
        "geometry": "POINT",
        "fields": MISC_FIELDS,
        "samples": [
            {"xy": (400, 200), "attrs": ("MiscA", "Active")},
            {"xy": (450, 200), "attrs": ("MiscB", "Broken")},
            {"xy": (500, 200), "attrs": (None, "Active")},
        ],
    },
]

MAP_LAYER_TREE = [
    ("Distribution Assets", ["Asset_Pole", "Asset_Cable", "Asset_Transformer"]),
    ("Transmission Assets", ["Asset_PowerLine", "Asset_Switch"]),
]
TOP_LEVEL_LAYERS = ["Asset_Building", "Asset_Misc"]


def script_paths():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_dir = os.path.dirname(script_dir)
    gdb = os.path.join(project_dir, "ProAssetInspector.gdb")
    aprx = os.path.join(project_dir, "ProAssetInspector.aprx")
    return project_dir, gdb, aprx


def add_fields(fc_path, field_defs):
    existing = {f.name for f in arcpy.ListFields(fc_path)}
    for name, ftype, length in field_defs:
        if name in existing:
            continue
        if ftype == "TEXT":
            arcpy.management.AddField(fc_path, name, ftype, field_length=length)
        else:
            arcpy.management.AddField(fc_path, name, ftype)


def _point_geom(x, y, sr):
    return arcpy.Point(ORIGIN_X + x, ORIGIN_Y + y)


def _polyline_geom(coords, sr):
    array = arcpy.Array([arcpy.Point(ORIGIN_X + c[0], ORIGIN_Y + c[1]) for c in coords])
    return arcpy.Polyline(array, sr)


def _polygon_geom(ring, sr):
    array = arcpy.Array([arcpy.Point(ORIGIN_X + c[0], ORIGIN_Y + c[1]) for c in ring])
    return arcpy.Polygon(array, sr)


def insert_samples(fc_path, fc_def, sr):
    geom_type = fc_def["geometry"]
    field_names = [f[0] for f in fc_def["fields"]]
    insert_fields = ["SHAPE@"] + field_names

    with arcpy.da.InsertCursor(fc_path, insert_fields) as cursor:
        for sample in fc_def["samples"]:
            attrs = sample["attrs"]
            row_attrs = list(attrs) + [None] * (len(field_names) - len(attrs))

            if geom_type == "POINT":
                shape = _point_geom(sample["xy"][0], sample["xy"][1], sr)
            elif geom_type == "POLYLINE":
                shape = _polyline_geom(sample["coords"], sr)
            else:
                shape = _polygon_geom(sample["ring"], sr)

            cursor.insertRow([shape] + row_attrs)


def create_feature_classes(gdb, spatial_ref, recreate):
    arcpy.env.workspace = gdb
    arcpy.env.overwriteOutput = True

    for fc_def in FEATURE_CLASSES:
        name = fc_def["name"]
        fc_path = os.path.join(gdb, name)

        if arcpy.Exists(fc_path):
            if recreate:
                arcpy.management.Delete(fc_path)
            else:
                arcpy.AddMessage(f"Skipping existing {name} (use --recreate to rebuild)")
                continue

        arcpy.management.CreateFeatureclass(
            gdb, name, fc_def["geometry"], spatial_reference=spatial_ref
        )

        add_fields(fc_path, fc_def["fields"])
        insert_samples(fc_path, fc_def, spatial_ref)
        arcpy.AddMessage(f"Created {name} with {arcpy.management.GetCount(fc_path)[0]} features")


def configure_aprx(aprx_path, gdb):
    aprx = arcpy.mp.ArcGISProject(aprx_path)
    m = aprx.listMaps()[0]

    for lyr in list(m.listLayers()):
        m.removeLayer(lyr)

    def add_fc_layer(fc_name, group_lyr=None):
        fc_path = os.path.join(gdb, fc_name)
        added = m.addDataFromPath(fc_path)
        if added is None:
            return None
        if added.name != fc_name:
            added.name = fc_name
        if group_lyr is not None:
            m.addLayerToGroup(group_lyr, added, "AUTO_ARRANGE")
            for root_lyr in list(m.listLayers()):
                if root_lyr.name == fc_name and root_lyr.isGroupLayer is False:
                    try:
                        m.removeLayer(root_lyr)
                    except Exception:
                        pass
                    break
        return added

    for group_name, fc_names in MAP_LAYER_TREE:
        group_lyr = m.createGroupLayer(group_name)
        for fc_name in fc_names:
            add_fc_layer(fc_name, group_lyr)

    for fc_name in TOP_LEVEL_LAYERS:
        add_fc_layer(fc_name, None)

    aprx.save()
    arcpy.AddMessage(f"Updated map in {aprx_path}")


def main():
    parser = argparse.ArgumentParser(description="Create ProAssetInspector demo data")
    parser.add_argument("--recreate", action="store_true", help="Delete and recreate feature classes")
    args = parser.parse_args()

    _, gdb, aprx = script_paths()
    if not os.path.isdir(gdb):
        arcpy.AddError(f"Geodatabase not found: {gdb}")
        sys.exit(1)
    if not os.path.isfile(aprx):
        arcpy.AddError(f"Project not found: {aprx}")
        sys.exit(1)

    sr = arcpy.SpatialReference(2326)
    create_feature_classes(gdb, sr, args.recreate)
    configure_aprx(aprx, gdb)
    arcpy.AddMessage("Done.")


if __name__ == "__main__":
    main()
