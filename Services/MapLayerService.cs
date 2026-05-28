using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using HKBN.ProAssetInspector.Models;
using HKBN.ProAssetInspector.Utilities;

namespace HKBN.ProAssetInspector.Services
{
    public class MapLayerService : IMapLayerService
    {
        public Task<(bool HasActiveMap, string MapName)> GetMapStatusAsync()
        {
            var mapView = MapView.Active;
            if (mapView?.Map == null)
                return Task.FromResult((false, string.Empty));

            return Task.FromResult((true, mapView.Map.Name ?? "Map"));
        }

        public Task<IReadOnlyList<LayerInfo>> GetFeatureLayersAsync()
        {
            var map = MapView.Active?.Map;
            if (map == null)
                return Task.FromResult((IReadOnlyList<LayerInfo>)new List<LayerInfo>());

            var layers = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
            var result = layers.Select(ToLayerInfo).OrderBy(l => l.LayerName).ToList();
            return Task.FromResult((IReadOnlyList<LayerInfo>)result);
        }

        public Task<LayerInfo> GetLayerInfoAsync(string layerName)
        {
            var layer = FindFeatureLayerByName(layerName);
            return Task.FromResult(layer == null ? null : ToLayerInfo(layer));
        }

        internal static FeatureLayer FindFeatureLayerByName(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return null;

            var map = MapView.Active?.Map;
            if (map == null)
                return null;

            return map.GetLayersAsFlattenedList()
                .OfType<FeatureLayer>()
                .FirstOrDefault(l => l.Name == layerName);
        }

        private static LayerInfo ToLayerInfo(FeatureLayer layer)
        {
            var table = layer.GetFeatureClass()?.GetDefinition();
            return new LayerInfo
            {
                LayerName = layer.Name,
                GeometryType = layer.ShapeType.ToString(),
                FeatureClassName = layer.GetFeatureClass()?.GetName() ?? string.Empty,
                FieldCount = table?.GetFields()?.Count ?? 0,
                HasAssetIdField = table != null && FieldValueReader.HasField(table, FieldMapping.AssetIdCandidates),
                HasAssetTypeField = table != null && FieldValueReader.HasField(table, FieldMapping.AssetTypeCandidates),
                HasStatusField = table != null && FieldValueReader.HasField(table, FieldMapping.StatusCandidates)
            };
        }
    }
}
