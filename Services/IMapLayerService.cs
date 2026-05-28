using System.Collections.Generic;
using System.Threading.Tasks;
using HKBN.ProAssetInspector.Models;

namespace HKBN.ProAssetInspector.Services
{
    public interface IMapLayerService
    {
        Task<(bool HasActiveMap, string MapName)> GetMapStatusAsync();
        Task<IReadOnlyList<LayerInfo>> GetFeatureLayersAsync();
        Task<LayerInfo> GetLayerInfoAsync(string layerName);
    }
}
