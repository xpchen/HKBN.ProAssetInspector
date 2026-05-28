using System.Collections.Generic;
using System.Linq;
using HKBN.ProAssetInspector.Models;
using HKBN.ProAssetInspector.Utilities;

namespace HKBN.ProAssetInspector.Services
{
    public class CsvExportService : ICsvExportService
    {
        private static readonly string[] Headers =
        {
            "Severity", "IssueCode", "LayerName", "ObjectID", "AssetID", "AssetType", "Message"
        };

        public OperationResult ExportValidationIssues(IEnumerable<ValidationIssue> issues, string path)
        {
            var list = issues?.ToList() ?? new List<ValidationIssue>();
            if (list.Count == 0)
                return OperationResult.Fail("No validation issues to export.");

            try
            {
                var rows = list.Select(i => (IReadOnlyList<string>)new[]
                {
                    i.Severity ?? string.Empty,
                    i.IssueCode ?? string.Empty,
                    i.LayerName ?? string.Empty,
                    i.ObjectID.ToString(),
                    i.AssetID ?? string.Empty,
                    i.AssetType ?? string.Empty,
                    i.Message ?? string.Empty
                });

                CsvWriter.WriteUtf8Bom(path, Headers, rows);
                return OperationResult.Ok(path);
            }
            catch (System.Exception ex)
            {
                return OperationResult.Fail(ExceptionHelper.ToFriendlyMessage(ex));
            }
        }
    }
}
