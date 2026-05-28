using System.Collections.Generic;

namespace HKBN.ProAssetInspector.Models
{
    public class AssetRecord
    {
        public long ObjectID { get; set; }
        public string LayerName { get; set; } = string.Empty;
        public string FeatureClassName { get; set; } = string.Empty;
        public string AssetID { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double? VoltageLevel { get; set; }
        public string Owner { get; set; } = string.Empty;
        public string GeometryType { get; set; } = string.Empty;
        public ExtentDto ShapeExtent { get; set; }
        public List<string> FieldReadWarnings { get; set; } = new();
    }
}
