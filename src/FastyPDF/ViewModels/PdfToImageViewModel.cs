using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Services;
using FastyPDF.Navigation;
using FastyPDF.Services;

namespace FastyPDF.ViewModels;

public partial class PdfToImageViewModel : ToolViewModelBase
{
    private readonly IPdfDocumentService _documents;
    private readonly IPdfRenderService _render;
    private readonly IImageEncoder _encoder;
    private readonly IFileService _files;
    private readonly ISettingsService _settings;
    private readonly IRecentFilesService _recent;
    private readonly PickerService _pickers;
    private readonly INavigationService _navigation;
    private readonly IAppLog _log;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _workCts;

    public PdfToImageViewModel(
        IPdfDocumentService documents,
        IPdfRenderService render,
        IImageEncoder encoder,
        IFileService files,
        ISettingsService settings,
        IRecentFilesService recent,
        PickerService pickers,
        INavigationService navigation,
        IAppLog log)
    {
        _documents = documents;
        _render = render;
        _encoder = encoder;
        _files = files;
        _settings = settings;
        _recent = recent;
        _pickers = pickers;
        _navigation = navigation;
        _log = log;
        Formats = ["PNG", "JPEG"];
        SelectedFormat = "PNG";
    }

    public ObservableCollection<PageItemViewModel> Pages { get; } = [];

    public IReadOnlyList<string> Formats { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDocument))]
    private string? _sourcePath;

    [ObservableProperty]
    private string _rangeText = string.Empty;

    [ObservableProperty]
    private string _selectedFormat;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSpecifyRange))]
    private bool _exportAll = true;

    public bool CanSpecifyRange => !ExportAll;

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
    private async Task ExportAsync(IList<object>? selectedItems)
    {
        if (string.IsNullOrEmpty(SourcePath))
        {
            return;
        }

        List<int> indices;
        try
        {
            if (ExportAll)
            {
                indices = Enumerable.Range(0, Pages.Count).ToList();
            }
            else if (!string.IsNullOrWhiteSpace(RangeText))
            {
                var ranges = PageRangeParser.Parse(RangeText, Pages.Count);
                indices = PageRangeParser.ToPageIndices(ranges).ToList();
            }
            else
            {
                indices = selectedItems?.OfType<PageItemViewModel>().Select(page => page.PageIndex).OrderBy(i => i).ToList() ?? [];
            }

            if (indices.Count == 0)
            {
                ErrorMessage = "Dışa aktarılacak sayfa seçin.";
                IsErrorOpen = true;
                return;
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
            return;
        }

        var folder = await _pickers.PickFolderAsync();
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        _workCts = new CancellationTokenSource();
        IsBusy = true;
        var extension = string.Equals(SelectedFormat, "JPEG", StringComparison.OrdinalIgnoreCase) ? "jpg" : "png";
        try
        {
            for (var i = 0; i < indices.Count; i++)
            {
                _workCts.Token.ThrowIfCancellationRequested();
                StatusMessage = $"Sayfalar dışa aktarılıyor... ({i + 1}/{indices.Count})";
                ProgressValue = (i / (double)indices.Count) * 100;
                IsProgressIndeterminate = false;
                var pageIndex = indices[i];
                var image = await _render.RenderPageAsync(SourcePath, pageIndex, 2f, _workCts.Token);
                var name = _files.BuildPageImageFileName(pageIndex + 1, extension);
                var output = _files.GetUniqueFilePath(folder, name);
                await _encoder.EncodeAsync(image, output, extension, _settings.Current.JpegQuality, _workCts.Token);
            }

            ShowSuccess("Görseller başarıyla oluşturuldu.");
        }
        catch (OperationCanceledException)
        {
            ClearMessages();
        }
        catch (Exception ex)
        {
            _log.Error("PDF to Image export failed", ex);
            ShowError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _workCts?.Cancel();
        _loadCts?.Cancel();
        _navigation.Navigate(ToolIds.Home);
    }

    private async Task LoadThumbnailsAsync(CancellationToken cancellationToken)
    {
        var gate = new SemaphoreSlim(3);
        foreach (var page in Pages.ToList())
        {
            await gate.WaitAsync(cancellationToken);
            _ = Task.Run(async () =>
            {
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
            }, cancellationToken);
        }
    }
}
