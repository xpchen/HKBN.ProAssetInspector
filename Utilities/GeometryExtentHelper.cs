using ArcGIS.Core.Geometry;
using HKBN.ProAssetInspector.Models;

namespace HKBN.ProAssetInspector.Utilities
{
    public static class GeometryExtentHelper
    {
        private const double DefaultPointBufferMeters = 50.0;

        public static ExtentDto ToDto(Envelope envelope)
        {
            if (envelope == null || envelope.IsEmpty)
                return null;

            return new ExtentDto
            {
                XMin = envelope.XMin,
                YMin = envelope.YMin,
                XMax = envelope.XMax,
                YMax = envelope.YMax,
                Wkid = envelope.SpatialReference?.Wkid ?? 0
            };
        }

        public static Envelope ExpandForZoom(Geometry geometry, double bufferMeters = DefaultPointBufferMeters)
        {
            if (geometry == null)
                return null;

            var extent = geometry.Extent;
            if (extent == null || extent.IsEmpty)
                return extent;

            if (geometry.GeometryType == GeometryType.Point ||
                (extent.Width < 0.001 && extent.Height < 0.001))
            {
                return GeometryEngine.Instance.Buffer(geometry, bufferMeters)?.Extent ?? extent;
            }

            return extent;
        }
    }
}
