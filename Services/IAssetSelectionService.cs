using System.Collections.Generic;
using System.Threading.Tasks;
using HKBN.ProAssetInspector.Models;

namespace HKBN.ProAssetInspector.Services
{
    public interface IAssetSelectionService
    {
        Task<IReadOnlyList<AssetRecord>> LoadSelectedAssetsAsync();
    }
}
