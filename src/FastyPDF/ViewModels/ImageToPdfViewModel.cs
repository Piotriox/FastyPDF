using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Models;
using FastyPDF.Services;

namespace FastyPDF.ViewModels;

public partial class ImageToPdfViewModel : ToolViewModelBase
{
    private readonly IImageService _images;
    private readonly IPdfManipulationService _manipulation;
    private readonly IRecentFilesService _recent;
    private readonly PickerService _pickers;

    public ImageToPdfViewModel(
        IImageService images,
        IPdfManipulationService manipulation,
        IRecentFilesService recent,
        PickerService pickers)
    {
        _images = images;
        _manipulation = manipulation;
        _recent = recent;
        _pickers = pickers;
        Images.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasImages));
    }

    public ObservableCollection<FileItemViewModel> Images { get; } = [];

    public bool HasImages => Images.Count > 0;

    [RelayCommand]
    private async Task AddAsync()
    {
        var paths = await _pickers.PickImageFilesAsync();
        await AddPathsAsync(paths);
    }

    public async Task AddPathsAsync(IEnumerable<string> paths)
    {
        ClearMessages();
        foreach (var path in paths.Where(_images.IsSupported))
        {
            Images.Add(new FileItemViewModel
            {
                Path = path,
                Name = Path.GetFileName(path),
                Details = Path.GetExtension(path).Trim('.').ToUpperInvariant()
            });
        }

        OnPropertyChanged(nameof(HasImages));
    }

    [RelayCommand]
    private void Remove(FileItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        Images.Remove(item);
        OnPropertyChanged(nameof(HasImages));
    }

    [RelayCommand]
    private void MoveUp(FileItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var index = Images.IndexOf(item);
        if (index > 0)
        {
            Images.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveDown(FileItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var index = Images.IndexOf(item);
        if (index >= 0 && index < Images.Count - 1)
        {
            Images.Move(index, index + 1);
        }
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (Images.Count == 0)
        {
            ErrorMessage = "PDF oluşturmak için görseller ekleyin.";
            IsErrorOpen = true;
            return;
        }

        var output = await _pickers.PickSavePdfAsync("images.pdf");
        if (string.IsNullOrWhiteSpace(output))
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Görseller PDF'e dönüştürülüyor...";
        try
        {
            var prepared = new List<ImageInput>();
            foreach (var item in Images)
            {
                prepared.Add(await _images.PrepareForPdfAsync(item.Path));
            }

            await _manipulation.CreateFromImagesAsync(prepared, output, CreateProgress());
            ShowSuccess("PDF başarıyla oluşturuldu.");
            await _recent.AddAsync(output);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
