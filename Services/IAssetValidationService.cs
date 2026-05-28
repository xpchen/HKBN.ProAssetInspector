using System.Collections.Generic;
using HKBN.ProAssetInspector.Models;

namespace HKBN.ProAssetInspector.Services
{
    public interface IAssetValidationService
    {
        IReadOnlyList<ValidationIssue> Validate(IEnumerable<AssetRecord> records);
    }
}
