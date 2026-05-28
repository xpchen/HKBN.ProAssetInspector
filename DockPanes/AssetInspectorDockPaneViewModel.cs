using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using HKBN.ProAssetInspector.Models;
using HKBN.ProAssetInspector.Services;
using HKBN.ProAssetInspector.Utilities;
using Microsoft.Win32;
using RelayCommand = HKBN.ProAssetInspector.Utilities.RelayCommand;

namespace HKBN.ProAssetInspector.DockPanes
{
    internal class AssetInspectorDockPaneViewModel : DockPane
    {
        private const string DockPaneId = "HKBN_ProAssetInspector_AssetInspectorDockPane";

        private readonly IMapLayerService _mapLayerService = new MapLayerService();
        private readonly IAssetSelectionService _assetSelectionService = new AssetSelectionService();
        private readonly IAssetValidationService _assetValidationService = new AssetValidationService();
        private readonly IMapNavigationService _mapNavigationService = new MapNavigationService();
        private readonly ICsvExportService _csvExportService = new CsvExportService();

        private bool _isLoading;
        private string _statusMessage = "Ready.";
        private string _activeMapName = string.Empty;
        private bool _hasActiveMapView;
        private LayerInfo _selectedLayer;
        private AssetRecord _selectedAsset;
        private ValidationIssue _selectedIssue;
        private string _layerDetailText = string.Empty;
        private string _emptyAssetsMessage = string.Empty;
        private string _emptyIssuesMessage = string.Empty;
        private string _emptyLayersMessage = string.Empty;
        private string _noMapMessage = string.Empty;

        protected AssetInspectorDockPaneViewModel()
        {
            Layers = new ObservableCollection<LayerInfo>();
            Assets = new ObservableCollection<AssetRecord>();
            Issues = new ObservableCollection<ValidationIssue>();

            RefreshLayersCommand = new RelayCommand(async () => await RefreshLayersAsync(), () => !IsLoading);
            LoadSelectedAssetsCommand = new RelayCommand(async () => await LoadSelectedAssetsAsync(), CanRunMapCommand);
            ValidateAssetsCommand = new RelayCommand(() => ValidateAssets(), CanRunMapCommand);
            ZoomToSelectedAssetCommand = new RelayCommand(async () => await ZoomToSelectedAsync(), CanZoomOrSelect);
            SelectInMapCommand = new RelayCommand(async () => await SelectInMapAsync(), CanZoomOrSelect);
            ExportValidationReportCommand = new RelayCommand(ExportValidationReport, () => !IsLoading && Issues.Count > 0);

        }

        public ObservableCollection<LayerInfo> Layers { get; }
        public ObservableCollection<AssetRecord> Assets { get; }
        public ObservableCollection<ValidationIssue> Issues { get; }

        public RelayCommand RefreshLayersCommand { get; }
        public RelayCommand LoadSelectedAssetsCommand { get; }
        public RelayCommand ValidateAssetsCommand { get; }
        public RelayCommand ZoomToSelectedAssetCommand { get; }
        public RelayCommand SelectInMapCommand { get; }
        public RelayCommand ExportValidationReportCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                    RefreshCommandStates();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string ActiveMapName
        {
            get => _activeMapName;
            set => SetProperty(ref _activeMapName, value);
        }

        public bool HasActiveMapView
        {
            get => _hasActiveMapView;
            set
            {
                if (SetProperty(ref _hasActiveMapView, value))
                {
                    NoMapMessage = value ? string.Empty : "No active map view.";
                    RefreshCommandStates();
                }
            }
        }

        public string NoMapMessage
        {
            get => _noMapMessage;
            set => SetProperty(ref _noMapMessage, value);
        }

        public string EmptyLayersMessage
        {
            get => _emptyLayersMessage;
            set => SetProperty(ref _emptyLayersMessage, value);
        }

        public string EmptyAssetsMessage
        {
            get => _emptyAssetsMessage;
            set => SetProperty(ref _emptyAssetsMessage, value);
        }

        public string EmptyIssuesMessage
        {
            get => _emptyIssuesMessage;
            set => SetProperty(ref _emptyIssuesMessage, value);
        }

        public LayerInfo SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                if (SetProperty(ref _selectedLayer, value))
                    _ = UpdateLayerDetailAsync();
            }
        }

        public AssetRecord SelectedAsset
        {
            get => _selectedAsset;
            set
            {
                if (SetProperty(ref _selectedAsset, value))
                    RefreshCommandStates();
            }
        }

        public ValidationIssue SelectedIssue
        {
            get => _selectedIssue;
            set
            {
                if (SetProperty(ref _selectedIssue, value))
                    RefreshCommandStates();
            }
        }

        public string LayerDetailText
        {
            get => _layerDetailText;
            set => SetProperty(ref _layerDetailText, value);
        }

        public int AssetCount => Assets.Count;
        public int IssueCount => Issues.Count;

        /// <summary>
        /// Shows this dock pane. DAML id must match Config.daml dockPane id exactly.
        /// See: https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic10118.html
        /// </summary>
        internal static void Show()
        {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId) as AssetInspectorDockPaneViewModel;
            pane?.Activate();
        }

        protected override Task InitializeAsync()
        {
            return RefreshLayersAsync();
        }

        private bool CanRunMapCommand() => !IsLoading && HasActiveMapView;

        private bool CanZoomOrSelect()
        {
            if (IsLoading || !HasActiveMapView)
                return false;

            return SelectedAsset != null || SelectedIssue != null;
        }

        private void RefreshCommandStates()
        {
            NotifyPropertyChanged(nameof(AssetCount));
            NotifyPropertyChanged(nameof(IssueCount));
            RefreshLayersCommand.RaiseCanExecuteChanged();
            LoadSelectedAssetsCommand.RaiseCanExecuteChanged();
            ValidateAssetsCommand.RaiseCanExecuteChanged();
            ZoomToSelectedAssetCommand.RaiseCanExecuteChanged();
            SelectInMapCommand.RaiseCanExecuteChanged();
            ExportValidationReportCommand.RaiseCanExecuteChanged();
        }

        private async Task RefreshLayersAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            var previousLayer = SelectedLayer?.LayerName;

            try
            {
                var status = await QueuedTask.Run(() => _mapLayerService.GetMapStatusAsync());
                HasActiveMapView = status.HasActiveMap;
                ActiveMapName = status.HasActiveMap ? status.MapName : string.Empty;

                if (!status.HasActiveMap)
                {
                    Layers.Clear();
                    LayerDetailText = string.Empty;
                    EmptyLayersMessage = string.Empty;
                    StatusMessage = "No active map view.";
                    return;
                }

                var layers = await QueuedTask.Run(() => _mapLayerService.GetFeatureLayersAsync());

                Layers.Clear();
                foreach (var layer in layers)
                    Layers.Add(layer);

                EmptyLayersMessage = Layers.Count == 0 ? "No feature layers found." : string.Empty;

                SelectedLayer = string.IsNullOrWhiteSpace(previousLayer)
                    ? Layers.FirstOrDefault()
                    : Layers.FirstOrDefault(l => l.LayerName == previousLayer) ?? Layers.FirstOrDefault();

                StatusMessage = $"Loaded {Layers.Count} feature layer(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionHelper.ToFriendlyMessage(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpdateLayerDetailAsync()
        {
            if (SelectedLayer == null || !HasActiveMapView)
            {
                LayerDetailText = string.Empty;
                return;
            }

            try
            {
                var info = await QueuedTask.Run(() => _mapLayerService.GetLayerInfoAsync(SelectedLayer.LayerName));
                if (info == null)
                {
                    LayerDetailText = string.Empty;
                    return;
                }

                LayerDetailText =
                    $"LayerName: {info.LayerName}\n" +
                    $"GeometryType: {info.GeometryType}\n" +
                    $"FeatureClass: {info.FeatureClassName}\n" +
                    $"Field count: {info.FieldCount}\n" +
                    $"Has AssetID field: {info.HasAssetIdField}\n" +
                    $"Has AssetType field: {info.HasAssetTypeField}\n" +
                    $"Has Status field: {info.HasStatusField}";
            }
            catch (Exception ex)
            {
                LayerDetailText = ExceptionHelper.ToFriendlyMessage(ex);
            }
        }

        private async Task LoadSelectedAssetsAsync()
        {
            if (!CanRunMapCommand())
                return;

            IsLoading = true;
            try
            {
                var records = await QueuedTask.Run(() => _assetSelectionService.LoadSelectedAssetsAsync());

                Assets.Clear();
                Issues.Clear();
                SelectedAsset = null;
                SelectedIssue = null;

                foreach (var record in records)
                    Assets.Add(record);

                EmptyAssetsMessage = Assets.Count == 0 ? "No selected features found." : string.Empty;
                StatusMessage = Assets.Count == 0
                    ? "No selected features found."
                    : $"Loaded {Assets.Count} selected asset(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionHelper.ToFriendlyMessage(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ValidateAssets()
        {
            if (!CanRunMapCommand())
                return;

            try
            {
                var issues = _assetValidationService.Validate(Assets.ToList());

                Issues.Clear();
                SelectedIssue = null;
                foreach (var issue in issues)
                    Issues.Add(issue);

                EmptyIssuesMessage = Issues.Count == 0 ? string.Empty : string.Empty;
                StatusMessage = Issues.Count == 0
                    ? "Validation completed. No issues found."
                    : $"Validation completed. {Issues.Count} issue(s) found.";
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionHelper.ToFriendlyMessage(ex);
            }
            finally
            {
                RefreshCommandStates();
            }
        }

        private async Task ZoomToSelectedAsync()
        {
            if (!CanZoomOrSelect())
                return;

            IsLoading = true;
            try
            {
                OperationResult result;
                if (SelectedAsset != null)
                    result = await QueuedTask.Run(() => _mapNavigationService.ZoomToAssetAsync(SelectedAsset));
                else
                    result = await QueuedTask.Run(() => _mapNavigationService.ZoomToIssueAsync(SelectedIssue));

                StatusMessage = result.Success ? "Zoomed to feature." : result.Message;
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionHelper.ToFriendlyMessage(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SelectInMapAsync()
        {
            if (!CanZoomOrSelect())
                return;

            IsLoading = true;
            try
            {
                OperationResult result;
                if (SelectedAsset != null)
                    result = await QueuedTask.Run(() => _mapNavigationService.SelectAssetAsync(SelectedAsset));
                else
                    result = await QueuedTask.Run(() => _mapNavigationService.SelectIssueAsync(SelectedIssue));

                StatusMessage = result.Success ? "Feature selected on map." : result.Message;
            }
            catch (Exception ex)
            {
                StatusMessage = ExceptionHelper.ToFriendlyMessage(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExportValidationReport()
        {
            if (Issues.Count == 0)
            {
                StatusMessage = "No validation issues to export.";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "ValidationReport.csv",
                Title = "Export Validation Report"
            };

            if (dialog.ShowDialog() != true)
                return;

            var result = _csvExportService.ExportValidationIssues(Issues, dialog.FileName);
            if (result.Success)
            {
                StatusMessage = $"Exported {Issues.Count} issue(s).";
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Validation report exported to:\n{dialog.FileName}", "Export Complete");
            }
            else
            {
                StatusMessage = result.Message;
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(result.Message, "Export Failed");
            }
        }
    }
}
