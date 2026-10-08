using FastyPDF.Services;
using FastyPDF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace FastyPDF.Views;

public sealed partial class PdfToImagePage : Page
{
    public PdfToImageViewModel ViewModel { get; }

    public PdfToImagePage()
    {
        ViewModel = App.Services.GetRequiredService<PdfToImageViewModel>();
        InitializeComponent();
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

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ExportCommand.ExecuteAsync(PageGrid.SelectedItems.ToList());
    }
}
