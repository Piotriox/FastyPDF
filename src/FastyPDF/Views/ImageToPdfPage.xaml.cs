using FastyPDF.Services;
using FastyPDF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace FastyPDF.Views;

public sealed partial class ImageToPdfPage : Page
{
    public ImageToPdfViewModel ViewModel { get; }

    public ImageToPdfPage()
    {
        ViewModel = App.Services.GetRequiredService<ImageToPdfViewModel>();
        InitializeComponent();
        DataContext = this;
    }

    private void OnDragOver(object sender, DragEventArgs e) => e.AcceptedOperation = DataPackageOperation.Copy;

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        await ViewModel.AddPathsAsync(PickerService.FilterDroppedFiles(items, [".png", ".jpg", ".jpeg", ".bmp", ".webp", ".tif", ".tiff"]));
    }
}
