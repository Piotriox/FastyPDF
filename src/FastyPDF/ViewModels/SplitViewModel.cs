using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Models;
using FastyPDF.Core.Services;
using FastyPDF.Services;

namespace FastyPDF.ViewModels;

public partial class SplitViewModel : ToolViewModelBase
{
    private readonly IPdfDocumentService _documents;
    private readonly IPdfManipulationService _manipulation;
    private readonly IPdfRenderService _render;
    private readonly IFileService _files;
    private readonly ISettingsService _settings;
    private readonly IRecentFilesService _recent;
    private readonly PickerService _pickers;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _workCts;

    public SplitViewModel(
        IPdfDocumentService documents,
        IPdfManipulationService manipulation,
        IPdfRenderService render,
        IFileService files,
        ISettingsService settings,
        IRecentFilesService recent,
        PickerService pickers)
    {
        _documents = documents;
        _manipulation = manipulation;
        _render = render;
        _files = files;
        _settings = settings;
        _recent = recent;
        _pickers = pickers;
    }

    public ObservableCollection<PageItemViewModel> Pages { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDocument))]
    private string? _sourcePath;

    [ObservableProperty]
    private string _rangeText = string.Empty;

    [ObservableProperty]
    private bool _useRanges;

    public bool HasDocument => !string.IsNullOrEmpty(SourcePath);

    [RelayCommand]
    private async Task OpenAsync()
    {
        var files = await _pickers.PickPdfFilesAsync(false);
        if (files.Count > 0)
        {
            await LoadAsync(files[0]);
        }
    }

    public async Task LoadAsync(string path)
    {
        ClearMessages();
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        try
        {
            var info = await _documents.GetInfoAsync(path, _loadCts.Token);
            SourcePath = info.Path;
            Pages.Clear();
            for (var i = 0; i < info.PageCount; i++)
            {
                Pages.Add(new PageItemViewModel { PageIndex = i });
            }

            OnPropertyChanged(nameof(HasDocument));
            await _recent.AddAsync(path);
            _ = LoadThumbnailsAsync(_loadCts.Token);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    [RelayCommand]
    private async Task SplitAsync(IList<object>? selectedItems)
    {
        if (string.IsNullOrEmpty(SourcePath))
        {
            return;
        }

        IReadOnlyList<PageRange> ranges;
        try
        {
            if (UseRanges)
            {
                ranges = PageRangeParser.Parse(RangeText, Pages.Count);
            }
            else
            {
                var selected = selectedItems?
                    .OfType<PageItemViewModel>()
                    .OrderBy(page => page.PageIndex)
                    .Select(page => page.PageIndex + 1)
                    .ToList() ?? [];
                if (selected.Count == 0)
                {
                    ErrorMessage = "Sayfa seçin veya bir sayfa aralığı girin.";
                    IsErrorOpen = true;
                    return;
                }

                ranges = selected.Select(page => new PageRange(page, page)).ToList();
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
            return;
        }

        var folder = await _pickers.PickFolderAsync() ?? _files.GetDefaultOutputDirectory();
        _workCts = new CancellationTokenSource();
        IsBusy = true;
        StatusMessage = "PDF ayrılıyor...";
        try
        {
            var outputs = ranges
                .Select((_, index) => _files.GetUniqueFilePath(folder, _files.BuildSplitFileName(SourcePath, index + 1, ranges.Count)))
                .ToList();
            await _manipulation.SplitAsync(SourcePath, ranges, outputs, CreateProgress(), _workCts.Token);
            ShowSuccess("PDF başarıyla ayrıldı.");
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
    private void Cancel() => _workCts?.Cancel();

    private async Task LoadThumbnailsAsync(CancellationToken cancellationToken)
    {
        var gate = new SemaphoreSlim(3);
        var tasks = Pages.Select(async page =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var image = await _render.RenderThumbnailAsync(SourcePath!, page.PageIndex, _settings.Current.ThumbnailWidth, cancellationToken);
                App.DispatcherQueue.TryEnqueue(() =>
                {
                    page.Preview = BitmapConversion.ToWriteableBitmap(image);
                    page.IsLoading = false;
                });
            }
            catch
            {
                App.DispatcherQueue.TryEnqueue(() => page.IsLoading = false);
            }
            finally
            {
                gate.Release();
            }
        });
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }
}
