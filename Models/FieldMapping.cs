namespace HKBN.ProAssetInspector.Models
{
    public static class FieldMapping
    {
        public static readonly string[] AssetIdCandidates =
        {
            "AssetID", "ASSETID", "ASSET_ID", "asset_id", "ID", "OBJECTID"
        };

        public static readonly string[] AssetTypeCandidates =
        {
            "AssetType", "ASSETTYPE", "ASSET_TYPE", "asset_type", "Type", "TYPE"
        };

        public static readonly string[] StatusCandidates =
        {
            "Status", "STATUS", "status", "AssetStatus", "ASSET_STATUS"
        };

        public static readonly string[] VoltageLevelCandidates =
        {
            "VoltageLevel", "VOLTAGELEVEL", "VOLTAGE_LEVEL", "voltage_level", "Voltage", "VOLTAGE"
        };

        public static readonly string[] OwnerCandidates =
        {
            "Owner", "OWNER", "owner", "Maintainer", "MAINTAINER"
        };

        public static readonly string[] ValidStatusValues =
        {
            "Active", "Retired", "Planned", "Inactive", "Under Construction"
        };

        public static readonly string[] PowerAssetKeywords =
        {
            "Pole", "Line", "Cable", "Transformer", "Switch", "Power"
        };
    }
}
