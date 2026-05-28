using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;

namespace HKBN.ProAssetInspector.Utilities
{
    public static class FieldValueReader
    {
        public static bool TryGetString(
            Row row,
            TableDefinition tableDefinition,
            IEnumerable<string> candidateFieldNames,
            out string value,
            out string warning)
        {
            value = string.Empty;
            warning = null;

            var field = FindField(tableDefinition, candidateFieldNames);
            if (field == null)
            {
                warning = $"No matching field found for candidates: {string.Join(", ", candidateFieldNames)}.";
                return false;
            }

            try
            {
                var raw = row[field.Name];
                if (raw == null || raw is DBNull)
                    return true;

                value = Convert.ToString(raw)?.Trim() ?? string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                warning = $"Cannot read field '{field.Name}' as string: {ex.Message}";
                return false;
            }
        }

        public static bool TryGetDouble(
            Row row,
            TableDefinition tableDefinition,
            IEnumerable<string> candidateFieldNames,
            out double? value,
            out string warning)
        {
            value = null;
            warning = null;

            var field = FindField(tableDefinition, candidateFieldNames);
            if (field == null)
            {
                warning = $"No matching field found for candidates: {string.Join(", ", candidateFieldNames)}.";
                return false;
            }

            try
            {
                var raw = row[field.Name];
                if (raw == null || raw is DBNull)
                    return true;

                value = Convert.ToDouble(raw);
                return true;
            }
            catch (Exception ex)
            {
                warning = $"Cannot read field '{field.Name}' as number: {ex.Message}";
                return false;
            }
        }

        public static bool HasField(TableDefinition tableDefinition, IEnumerable<string> candidateFieldNames) =>
            FindField(tableDefinition, candidateFieldNames) != null;

        private static Field FindField(TableDefinition tableDefinition, IEnumerable<string> candidateFieldNames)
        {
            var fields = tableDefinition.GetFields();
            foreach (var candidate in candidateFieldNames)
            {
                var match = fields.FirstOrDefault(f =>
                    f.Name.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0);
                if (match != null)
                    return match;
            }

            return null;
        }
    }
}
