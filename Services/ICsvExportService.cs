using System.Collections.Generic;
using HKBN.ProAssetInspector.Models;

namespace HKBN.ProAssetInspector.Services
{
    public interface ICsvExportService
    {
        OperationResult ExportValidationIssues(IEnumerable<ValidationIssue> issues, string path);
    }
}
