using System.Threading.Tasks;
using HKBN.ProAssetInspector.Models;

namespace HKBN.ProAssetInspector.Services
{
    public interface IMapNavigationService
    {
        Task<OperationResult> ZoomToAssetAsync(AssetRecord record);
        Task<OperationResult> SelectAssetAsync(AssetRecord record);
        Task<OperationResult> ZoomToIssueAsync(ValidationIssue issue);
        Task<OperationResult> SelectIssueAsync(ValidationIssue issue);
    }
}
