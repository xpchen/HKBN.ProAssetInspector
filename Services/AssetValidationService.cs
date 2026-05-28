using System;
using System.Collections.Generic;
using System.Linq;
using HKBN.ProAssetInspector.Models;

namespace HKBN.ProAssetInspector.Services
{
    public class AssetValidationService : IAssetValidationService
    {
        public IReadOnlyList<ValidationIssue> Validate(IEnumerable<AssetRecord> records)
        {
            var issues = new List<ValidationIssue>();
            if (records == null)
                return issues;

            foreach (var record in records)
            {
                ValidateRecord(record, issues);
            }

            return issues;
        }

        private static void ValidateRecord(AssetRecord record, ICollection<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(record.AssetID))
            {
                AddIssue(issues, record, "Error", "MISSING_ASSET_ID", "AssetID is missing.");
            }

            if (string.IsNullOrWhiteSpace(record.AssetType))
            {
                AddIssue(issues, record, "Warning", "MISSING_ASSET_TYPE", "AssetType is missing.");
            }

            if (!string.IsNullOrWhiteSpace(record.Status) &&
                !FieldMapping.ValidStatusValues.Any(v => string.Equals(v, record.Status, StringComparison.OrdinalIgnoreCase)))
            {
                AddIssue(issues, record, "Warning", "INVALID_STATUS", "Status is not in allowed values.");
            }

            if (IsPowerAsset(record.AssetType) && !record.VoltageLevel.HasValue)
            {
                AddIssue(issues, record, "Warning", "MISSING_VOLTAGE_LEVEL", "VoltageLevel is required for power assets.");
            }

            if (record.VoltageLevel.HasValue && record.VoltageLevel.Value < 0)
            {
                AddIssue(issues, record, "Error", "INVALID_VOLTAGE_LEVEL", "VoltageLevel cannot be negative.");
            }

            if (record.FieldReadWarnings != null)
            {
                foreach (var warning in record.FieldReadWarnings.Where(w => !string.IsNullOrWhiteSpace(w)))
                {
                    AddIssue(issues, record, "Info", "FIELD_READ_WARNING", warning);
                }
            }
        }

        private static bool IsPowerAsset(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType))
                return false;

            return FieldMapping.PowerAssetKeywords.Any(k =>
                assetType.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void AddIssue(
            ICollection<ValidationIssue> issues,
            AssetRecord record,
            string severity,
            string code,
            string message)
        {
            issues.Add(new ValidationIssue
            {
                Severity = severity,
                IssueCode = code,
                LayerName = record.LayerName,
                ObjectID = record.ObjectID,
                AssetID = record.AssetID,
                AssetType = record.AssetType,
                Message = message
            });
        }
    }
}
