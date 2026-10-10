using FastyPDF.Services;
using FastyPDF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;

namespace FastyPDF.Views;

public sealed partial class ReaderPage : Page
{
    public ReaderViewModel ViewModel { get; }

    public ReaderPage()
    {
        ViewModel = App.Services.GetRequiredService<ReaderViewModel>();
        InitializeComponent();

        AddAccelerator(VirtualKey.O, VirtualKeyModifiers.Control, () => ViewModel.OpenCommand.Execute(null));
        AddAccelerator(VirtualKey.W, VirtualKeyModifiers.Control, () => ViewModel.CloseDocumentCommand.Execute(null));
        AddAccelerator(VirtualKey.Left,     VirtualKeyModifiers.None, () => ViewModel.PreviousCommand.Execute(null));
        AddAccelerator(VirtualKey.Right,    VirtualKeyModifiers.None, () => ViewModel.NextCommand.Execute(null));
        AddAccelerator(VirtualKey.PageUp,   VirtualKeyModifiers.None, () => ViewModel.PreviousCommand.Execute(null));
        AddAccelerator(VirtualKey.PageDown, VirtualKeyModifiers.None, () => ViewModel.NextCommand.Execute(null));
        AddAccelerator(VirtualKey.Home,     VirtualKeyModifiers.None, () => ViewModel.FirstCommand.Execute(null));
        AddAccelerator(VirtualKey.End,      VirtualKeyModifiers.None, () => ViewModel.LastCommand.Execute(null));
        AddAccelerator(VirtualKey.B, VirtualKeyModifiers.Control, () => ViewModel.ToggleThumbnailsCommand.Execute(null));
        AddAccelerator(VirtualKey.F, VirtualKeyModifiers.Control, () =>
        {
            ViewModel.ToggleSearchCommand.Execute(null);
            if (ViewModel.IsSearchOpen)
            {
                SearchBox.Focus(FocusState.Programmatic);
                SearchBox.SelectAll();
            }
        });
        AddAccelerator(VirtualKey.C,        VirtualKeyModifiers.Control, () => ViewModel.CopyPageTextCommand.Execute(null));
        AddAccelerator(VirtualKey.D,        VirtualKeyModifiers.Control, () => ViewModel.ShowPropertiesCommand.Execute(null));
        AddAccelerator(VirtualKey.P,        VirtualKeyModifiers.Control, () => ViewModel.PrintCommand.Execute(null));
        AddAccelerator(VirtualKey.Add,      VirtualKeyModifiers.Control, () => ViewModel.ZoomInCommand.Execute(null));
        AddAccelerator(VirtualKey.Subtract, VirtualKeyModifiers.Control, () => ViewModel.ZoomOutCommand.Execute(null));
        AddAccelerator(VirtualKey.Number0,  VirtualKeyModifiers.Control, () => ViewModel.SetZoomCommand.Execute(1.0));
        AddAccelerator(VirtualKey.F11,      VirtualKeyModifiers.None,    ToggleFullScreen);

        ViewModel.PropertyChanged += async (_, args) =>
        {
            switch (args.PropertyName)
            {
                case nameof(ViewModel.PageIndex):
                    if (!_isSyncingFromScroll)
                        SyncScrollToCurrentPage();
                    if (ViewModel.PageIndex >= 0 && ViewModel.PageIndex < ThumbnailList.Items.Count)
                        ThumbnailList.ScrollIntoView(ThumbnailList.Items[ViewModel.PageIndex]);
                    break;

                case nameof(ViewModel.IsPropertiesOpen):
                    if (ViewModel.IsPropertiesOpen && XamlRoot != null)
                    {
                        PropertiesDialog.XamlRoot = XamlRoot;
                        await PropertiesDialog.ShowAsync();
                        ViewModel.ClosePropertiesCommand.Execute(null);
                    }
                    break;

                case nameof(ViewModel.IsSearchOpen) when ViewModel.IsSearchOpen:
                    SearchBox.Focus(FocusState.Programmatic);
                    SearchBox.SelectAll();
                    break;
            }
        };
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is string path && File.Exists(path))
            await ViewModel.LoadAsync(path);
    }

    private bool _isSyncingFromScroll;
    private bool _isProgrammaticScroll;
    private bool _isZooming;

    private void MainScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_isZooming) return;

        if (_isProgrammaticScroll)
        {
            if (!e.IsIntermediate)
                _isProgrammaticScroll = false;
            return;
        }

        if (e.IsIntermediate) return;

        var sv = MainScrollViewer;
        var containerH = sv.ExtentHeight;
        if (containerH <= 0 || ViewModel.RenderedPages.Count == 0) return;

        var rowH = containerH / ViewModel.RenderedPages.Count;
        var rowIndex = Math.Clamp((int)Math.Round(sv.VerticalOffset / Math.Max(rowH, 1)), 0, ViewModel.RenderedPages.Count - 1);

        var row = ViewModel.RenderedPages[rowIndex];
        if (row.LeftPageIndex != ViewModel.PageIndex)
        {
            _isSyncingFromScroll = true;
            try
            {
                _ = ViewModel.GoToAsync(row.LeftPageIndex);
            }
            finally
            {
                _isSyncingFromScroll = false;
            }
        }
    }

    private void SyncScrollToCurrentPage()
    {
        if (_isSyncingFromScroll || _isZooming || ViewModel.RenderedPages.Count == 0) return;

        int rowIndex = -1;
        for (int i = 0; i < ViewModel.RenderedPages.Count; i++)
        {
            var r = ViewModel.RenderedPages[i];
            if (r.LeftPageIndex == ViewModel.PageIndex ||
                (r.HasRightPage && r.LeftPageIndex + 1 == ViewModel.PageIndex))
            {
                rowIndex = i;
                break;
            }
        }

        if (rowIndex < 0) return;

        var containerH = MainScrollViewer.ExtentHeight;
        if (containerH <= 0) return;

        var rowH = containerH / ViewModel.RenderedPages.Count;
        _isProgrammaticScroll = true;
        MainScrollViewer.ChangeView(null, rowIndex * rowH, null, true);
    }

    private DispatcherTimer? _zoomDebounceTimer;

    private void MainScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(MainScrollViewer).Properties;
        var ctrlDown = InputKeyboardSource
            .GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(CoreVirtualKeyStates.Down);

        if (!ctrlDown) return;

        e.Handled = true;
        var delta = properties.MouseWheelDelta;
        if (delta == 0) return;

        _isZooming = true;
        double step = delta > 0 ? 0.15 : -0.15;
        ViewModel.AdjustZoomImmediate(step);

        if (_zoomDebounceTimer == null)
        {
            _zoomDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _zoomDebounceTimer.Tick += async (_, _) =>
            {
                _zoomDebounceTimer.Stop();
                try
                {
                    await ViewModel.ApplyZoomAsync();
                    SyncScrollToCurrentPage();
                }
                finally
                {
                    _isZooming = false;
                }
            };
        }

        _zoomDebounceTimer.Stop();
        _zoomDebounceTimer.Start();
    }

    private async void DarkModeToggle_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ApplyDarkModeAsync();
    }

    private async void TwoPageToggle_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ApplyTwoPageModeAsync();
    }

    private void OnDragOver(object sender, DragEventArgs e) => e.AcceptedOperation = DataPackageOperation.Copy;

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        var items = await e.DataView.GetStorageItemsAsync();
        var pdf = PickerService.FilterDroppedFiles(items, [".pdf"]).FirstOrDefault();
        if (pdf is not null)
            await ViewModel.LoadAsync(pdf);
    }

    private async void Thumbnail_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PageItemViewModel page)
            await ViewModel.ScrollToRowForPageAsync(page.PageIndex);
    }

    private async void PageNumberBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            await ViewModel.GoToPageNumberCommand.ExecuteAsync(PageNumberBox.Text);
            e.Handled = true;
        }
    }

    private async void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            await ViewModel.SearchCommand.ExecuteAsync(null);
            e.Handled = true;
        }
    }

    private void Viewport_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewModel.ViewportWidth  = Math.Max(1, e.NewSize.Width  - 32);
        ViewModel.ViewportHeight = Math.Max(1, e.NewSize.Height - 32);
    }

    private void FullScreen_Click(object sender, RoutedEventArgs e) => ToggleFullScreen();

    private void ToggleFullScreen()
    {
        var presenter = App.Window.AppWindow.Presenter;
        if (presenter.Kind == AppWindowPresenterKind.FullScreen)
            App.Window.AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        else
            App.Window.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
    }

    private void Zoom50_Click(object sender, RoutedEventArgs e)  => ViewModel.SetZoomCommand.Execute(0.50);
    private void Zoom75_Click(object sender, RoutedEventArgs e)  => ViewModel.SetZoomCommand.Execute(0.75);
    private void Zoom100_Click(object sender, RoutedEventArgs e) => ViewModel.SetZoomCommand.Execute(1.00);
    private void Zoom125_Click(object sender, RoutedEventArgs e) => ViewModel.SetZoomCommand.Execute(1.25);
    private void Zoom150_Click(object sender, RoutedEventArgs e) => ViewModel.SetZoomCommand.Execute(1.50);
    private void Zoom200_Click(object sender, RoutedEventArgs e) => ViewModel.SetZoomCommand.Execute(2.00);
    private void Zoom300_Click(object sender, RoutedEventArgs e) => ViewModel.SetZoomCommand.Execute(3.00);

    private void AddAccelerator(VirtualKey key, VirtualKeyModifiers modifiers, Action action)
    {
        var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        accelerator.Invoked += (_, args) => { action(); args.Handled = true; };
        KeyboardAccelerators.Add(accelerator);
    }
}
