using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using HKBN.ProAssetInspector.Models;
using HKBN.ProAssetInspector.Utilities;

namespace HKBN.ProAssetInspector.Services
{
    public class MapNavigationService : IMapNavigationService
    {
        public async Task<OperationResult> ZoomToIssueAsync(ValidationIssue issue) =>
            await NavigateAsync(issue?.LayerName, issue?.ObjectID ?? 0, zoom: true, select: false);

        public async Task<OperationResult> SelectIssueAsync(ValidationIssue issue) =>
            await NavigateAsync(issue?.LayerName, issue?.ObjectID ?? 0, zoom: false, select: true);

        public async Task<OperationResult> ZoomToAssetAsync(AssetRecord record) =>
            await NavigateAsync(record?.LayerName, record?.ObjectID ?? 0, zoom: true, select: false);

        public async Task<OperationResult> SelectAssetAsync(AssetRecord record) =>
            await NavigateAsync(record?.LayerName, record?.ObjectID ?? 0, zoom: false, select: true);

        private static async Task<OperationResult> NavigateAsync(string layerName, long objectId, bool zoom, bool select)
        {
            var mapView = MapView.Active;
            if (mapView?.Map == null)
                return OperationResult.Fail("No active map view.");

            if (string.IsNullOrWhiteSpace(layerName) || objectId <= 0)
                return OperationResult.Fail("Invalid layer or feature identifier.");

            var layer = MapLayerService.FindFeatureLayerByName(layerName);
            if (layer == null)
                return OperationResult.Fail($"Layer '{layerName}' was not found on the map.");

            var featureClass = layer.GetFeatureClass();
            if (featureClass == null)
                return OperationResult.Fail("Feature class is not available.");

            using var cursor = featureClass.Search(new QueryFilter { ObjectIDs = new List<long> { objectId } }, false);
            if (!cursor.MoveNext())
                return OperationResult.Fail($"Feature with ObjectID {objectId} was not found.");

            using var row = cursor.Current;
            Geometry shape = null;
            if (row is Feature feature)
                shape = feature.GetShape();
            else
            {
                var shapeField = featureClass.GetDefinition().GetShapeField();
                shape = row[shapeField] as Geometry;
            }

            if (shape == null)
                return OperationResult.Fail("Feature geometry is not available.");

            var map = mapView.Map;
            if (select)
            {
                map.ClearSelection();
                var selection = SelectionSet.FromDictionary(new Dictionary<MapMember, List<long>>
                {
                    { layer, new List<long> { objectId } }
                });
                map.SetSelection(selection);
            }

            if (zoom)
            {
                var extent = GeometryExtentHelper.ExpandForZoom(shape);
                if (extent == null || extent.IsEmpty)
                    return OperationResult.Fail("Cannot determine feature extent.");

                await mapView.ZoomToAsync(extent);
            }

            return OperationResult.Ok();
        }
    }
}
