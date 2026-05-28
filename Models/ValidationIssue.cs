namespace HKBN.ProAssetInspector.Models
{
    public class ValidationIssue
    {
        public string Severity { get; set; } = string.Empty;
        public string IssueCode { get; set; } = string.Empty;
        public string LayerName { get; set; } = string.Empty;
        public long ObjectID { get; set; }
        public string AssetID { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
