using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using HKBN.ProAssetInspector.Models;
using HKBN.ProAssetInspector.Utilities;

namespace HKBN.ProAssetInspector.Services
{
    public class AssetSelectionService : IAssetSelectionService
    {
        public Task<IReadOnlyList<AssetRecord>> LoadSelectedAssetsAsync()
        {
            var map = MapView.Active?.Map;
            if (map == null)
                return Task.FromResult((IReadOnlyList<AssetRecord>)Array.Empty<AssetRecord>());

            var selection = map.GetSelection();
            if (selection == null || selection.Count == 0)
                return Task.FromResult((IReadOnlyList<AssetRecord>)Array.Empty<AssetRecord>());

            var records = new List<AssetRecord>();
            foreach (var entry in selection.ToDictionary())
            {
                if (entry.Key is not FeatureLayer featureLayer)
                    continue;

                var objectIds = entry.Value;
                if (objectIds == null || objectIds.Count == 0)
                    continue;

                records.AddRange(ReadFeatures(featureLayer, objectIds));
            }

            return Task.FromResult((IReadOnlyList<AssetRecord>)records);
        }

        private static IEnumerable<AssetRecord> ReadFeatures(FeatureLayer featureLayer, ICollection<long> objectIds)
        {
            var featureClass = featureLayer.GetFeatureClass();
            if (featureClass == null)
                yield break;

            var definition = featureClass.GetDefinition();
            var query = new QueryFilter { ObjectIDs = objectIds.ToList() };

            using var cursor = featureClass.Search(query, false);
            while (cursor.MoveNext())
            {
                using var row = cursor.Current;
                yield return BuildRecord(featureLayer, featureClass, definition, row);
            }
        }

        private static AssetRecord BuildRecord(
            FeatureLayer featureLayer,
            FeatureClass featureClass,
            FeatureClassDefinition definition,
            Row row)
        {
            var warnings = new List<string>();

            FieldValueReader.TryGetString(row, definition, FieldMapping.AssetIdCandidates, out var assetId, out var w1);
            if (w1 != null) warnings.Add(w1);

            FieldValueReader.TryGetString(row, definition, FieldMapping.AssetTypeCandidates, out var assetType, out var w2);
            if (w2 != null) warnings.Add(w2);

            FieldValueReader.TryGetString(row, definition, FieldMapping.StatusCandidates, out var status, out var w3);
            if (w3 != null) warnings.Add(w3);

            FieldValueReader.TryGetDouble(row, definition, FieldMapping.VoltageLevelCandidates, out var voltage, out var w4);
            if (w4 != null) warnings.Add(w4);

            FieldValueReader.TryGetString(row, definition, FieldMapping.OwnerCandidates, out var owner, out var w5);
            if (w5 != null) warnings.Add(w5);

            Geometry shape = null;
            try
            {
                shape = row[definition.GetShapeField()] as Geometry;
            }
            catch
            {
                // ignore shape read errors
            }

            return new AssetRecord
            {
                ObjectID = row.GetObjectID(),
                LayerName = featureLayer.Name,
                FeatureClassName = featureClass.GetName(),
                AssetID = assetId ?? string.Empty,
                AssetType = assetType ?? string.Empty,
                Status = status ?? string.Empty,
                VoltageLevel = voltage,
                Owner = owner ?? string.Empty,
                GeometryType = shape?.GeometryType.ToString() ?? featureLayer.ShapeType.ToString(),
                ShapeExtent = GeometryExtentHelper.ToDto(shape?.Extent),
                FieldReadWarnings = warnings
            };
        }
    }
}
