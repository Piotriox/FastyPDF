using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FastyPDF.Core.Abstractions;
using FastyPDF.Services;

namespace FastyPDF.ViewModels;

public partial class MergeViewModel : ToolViewModelBase
{
    private readonly IPdfDocumentService _documents;
    private readonly IPdfManipulationService _manipulation;
    private readonly IRecentFilesService _recent;
    private readonly PickerService _pickers;
    private CancellationTokenSource? _cts;

    public MergeViewModel(
        IPdfDocumentService documents,
        IPdfManipulationService manipulation,
        IRecentFilesService recent,
        PickerService pickers)
    {
        _documents = documents;
        _manipulation = manipulation;
        _recent = recent;
        _pickers = pickers;
        Files.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFiles));
    }

    public ObservableCollection<FileItemViewModel> Files { get; } = [];

    public bool HasFiles => Files.Count > 0;

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        var paths = await _pickers.PickPdfFilesAsync(true);
        await AddPathsAsync(paths);
    }

    public async Task AddPathsAsync(IEnumerable<string> paths)
    {
        ClearMessages();
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var info = await _documents.GetInfoAsync(path);
                Files.Add(new FileItemViewModel
                {
                    Path = info.Path,
                    Name = info.FileName,
                    Details = $"{info.PageCount} sayfa"
                });
                await _recent.AddAsync(path);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        OnPropertyChanged(nameof(HasFiles));
    }

    [RelayCommand]
    private void Remove(FileItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        Files.Remove(item);
        OnPropertyChanged(nameof(HasFiles));
    }

    [RelayCommand]
    private void MoveUp(FileItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var index = Files.IndexOf(item);
        if (index > 0)
        {
            Files.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveDown(FileItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var index = Files.IndexOf(item);
        if (index >= 0 && index < Files.Count - 1)
        {
            Files.Move(index, index + 1);
        }
    }

    [RelayCommand]
    private async Task MergeAsync()
    {
        if (Files.Count == 0)
        {
            ErrorMessage = "Birleştirmek istediğiniz PDF dosyalarını ekleyin.";
            IsErrorOpen = true;
            return;
        }

        var output = await _pickers.PickSavePdfAsync("merged.pdf");
        if (string.IsNullOrWhiteSpace(output))
        {
            return;
        }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        StatusMessage = "PDF birleştiriliyor...";
        try
        {
            await _manipulation.MergeAsync(Files.Select(file => file.Path).ToList(), output, CreateProgress(), _cts.Token);
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

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();
}
