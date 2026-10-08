using FastyPDF.Services;
using FastyPDF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace FastyPDF.Views;

public sealed partial class OrganizePage : Page
{
    public OrganizeViewModel ViewModel { get; }

    public OrganizePage()
    {
        ViewModel = App.Services.GetRequiredService<OrganizeViewModel>();
        InitializeComponent();
        AddAccelerator(VirtualKey.O, VirtualKeyModifiers.Control, () => ViewModel.OpenCommand.Execute(null));
        AddAccelerator(VirtualKey.S, VirtualKeyModifiers.Control, () => ViewModel.SaveCommand.Execute(null));
        AddAccelerator(VirtualKey.Z, VirtualKeyModifiers.Control, () => ViewModel.UndoCommand.Execute(null));
        AddAccelerator(VirtualKey.Y, VirtualKeyModifiers.Control, () => ViewModel.RedoCommand.Execute(null));
        AddAccelerator(VirtualKey.Delete, VirtualKeyModifiers.None, () => ViewModel.DeleteSelectedCommand.Execute(PageGrid.SelectedItems.ToList()));
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

    private void PageGrid_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        ViewModel.CaptureBeforeReorder();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteSelectedCommand.Execute(PageGrid.SelectedItems.ToList());
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
