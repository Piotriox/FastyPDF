using FastyPDF.Services;
using FastyPDF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

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
        AddAccelerator(VirtualKey.Left, VirtualKeyModifiers.None, () => ViewModel.PreviousCommand.Execute(null));
        AddAccelerator(VirtualKey.Right, VirtualKeyModifiers.None, () => ViewModel.NextCommand.Execute(null));
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is string path && File.Exists(path))
        {
            await ViewModel.LoadAsync(path);
        }
    }

    private void OnDragOver(object sender, DragEventArgs e) => e.AcceptedOperation = DataPackageOperation.Copy;

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        var pdf = PickerService.FilterDroppedFiles(items, [".pdf"]).FirstOrDefault();
        if (pdf is not null)
        {
            await ViewModel.LoadAsync(pdf);
        }
    }

    private async void Thumbnail_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PageItemViewModel page)
        {
            await ViewModel.GoToAsync(page.PageIndex);
        }
    }

    private void Viewport_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewModel.ViewportWidth = Math.Max(1, e.NewSize.Width - 24);
        ViewModel.ViewportHeight = Math.Max(1, e.NewSize.Height - 24);
    }

    private void FullScreen_Click(object sender, RoutedEventArgs e)
    {
        var presenter = App.Window.AppWindow.Presenter;
        if (presenter.Kind == AppWindowPresenterKind.FullScreen)
        {
            App.Window.AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        }
        else
        {
            App.Window.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
    }

    private void AddAccelerator(VirtualKey key, VirtualKeyModifiers modifiers, Action action)
    {
        var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        accelerator.Invoked += (_, args) =>
        {
            action();
            args.Handled = true;
        };
        KeyboardAccelerators.Add(accelerator);
    }
}
