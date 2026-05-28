using ArcGIS.Desktop.Framework.Contracts;
using HKBN.ProAssetInspector.DockPanes;

namespace HKBN.ProAssetInspector.Commands
{
    internal class ShowAssetInspectorButton : Button
    {
        protected override void OnClick()
        {
            AssetInspectorDockPaneViewModel.Show();
        }
    }
}
