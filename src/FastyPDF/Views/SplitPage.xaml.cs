using FastyPDF.Services;
using FastyPDF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace FastyPDF.Views;

public sealed partial class SplitPage : Page
{
    public SplitViewModel ViewModel { get; }

    public SplitPage()
    {
        ViewModel = App.Services.GetRequiredService<SplitViewModel>();
        InitializeComponent();
        var open = new KeyboardAccelerator { Key = VirtualKey.O, Modifiers = VirtualKeyModifiers.Control };
        open.Invoked += async (_, args) =>
        {
            await ViewModel.OpenCommand.ExecuteAsync(null);
            args.Handled = true;
        };
        KeyboardAccelerators.Add(open);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

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

    private async void Split_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SplitCommand.ExecuteAsync(PageGrid.SelectedItems.ToList());
    }
}
