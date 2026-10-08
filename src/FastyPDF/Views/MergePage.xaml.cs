using FastyPDF.Services;
using FastyPDF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace FastyPDF.Views;

public sealed partial class MergePage : Page
{
    public MergeViewModel ViewModel { get; }

    public MergePage()
    {
        ViewModel = App.Services.GetRequiredService<MergeViewModel>();
        InitializeComponent();
        DataContext = this;
        KeyboardAccelerators.Add(CreateAccelerator(VirtualKey.O, VirtualKeyModifiers.Control, () => ViewModel.AddFilesCommand.Execute(null)));
        KeyboardAccelerators.Add(CreateAccelerator(VirtualKey.S, VirtualKeyModifiers.Control, () => ViewModel.MergeCommand.Execute(null)));
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "PDF ekle";
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        await ViewModel.AddPathsAsync(PickerService.FilterDroppedFiles(items, [".pdf"]));
    }

    private static KeyboardAccelerator CreateAccelerator(VirtualKey key, VirtualKeyModifiers modifiers, Action action)
    {
        var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        accelerator.Invoked += (_, args) =>
        {
            action();
            args.Handled = true;
        };
        return accelerator;
    }
}
