namespace HKBN.ProAssetInspector.Models
{
    public class LayerInfo
    {
        public string LayerName { get; set; } = string.Empty;
        public string GeometryType { get; set; } = string.Empty;
        public string FeatureClassName { get; set; } = string.Empty;
        public int FieldCount { get; set; }
        public bool HasAssetIdField { get; set; }
        public bool HasAssetTypeField { get; set; }
        public bool HasStatusField { get; set; }
    }
}
