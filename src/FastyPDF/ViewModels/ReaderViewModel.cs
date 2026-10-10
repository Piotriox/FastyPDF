using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Models;
using FastyPDF.Services;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace FastyPDF.ViewModels;

public partial class RenderedPageViewModel : ObservableObject
{
    [ObservableProperty] private int _leftPageIndex;
    [ObservableProperty] private ImageSource? _leftImage;
    [ObservableProperty] private ImageSource? _rightImage;
    [ObservableProperty] private bool _hasRightPage;
    [ObservableProperty] private string _pageLabel = string.Empty;
    [ObservableProperty] private double _pageWidth = 595;
    [ObservableProperty] private double _pageHeight = 842;
    public bool IsRendered { get; set; }
}

public partial class ReaderViewModel : ToolViewModelBase
{
    private readonly IPdfDocumentService _documents;
    private readonly IPdfRenderService _render;
    private readonly IPdfTextService _text;
    private readonly IRecentFilesService _recent;
    private readonly PickerService _pickers;
    private readonly IAppLog _log;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _renderCts;
    private double _pageWidthPoints = 595;
    private double _pageHeightPoints = 842;
    private List<int> _searchMatchingPages = [];
    private const int RenderWindow = 3;

    public ReaderViewModel(
        IPdfDocumentService documents,
        IPdfRenderService render,
        IPdfTextService text,
        IRecentFilesService recent,
        PickerService pickers,
        IAppLog log)
    {
        _documents = documents;
        _render = render;
        _text = text;
        _recent = recent;
        _pickers = pickers;
        _log = log;
    }

    public ObservableCollection<PageItemViewModel> Thumbnails { get; } = [];
    public ObservableCollection<RenderedPageViewModel> RenderedPages { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDocument))]
    [NotifyPropertyChangedFor(nameof(PageLabel))]
    private string? _sourcePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageLabel))]
    private int _pageIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageLabel))]
    private int _pageCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZoomPercentText))]
    private double _zoom = 1.2;

    [ObservableProperty] private double _viewportWidth = 800;
    [ObservableProperty] private double _viewportHeight = 600;

    public string ZoomPercentText => $"{(int)Math.Round(Zoom * 100)}%";

    [ObservableProperty] private bool _isThumbnailsVisible = true;
    [ObservableProperty] private bool _isDarkMode;
    [ObservableProperty] private bool _isTwoPageMode;
    [ObservableProperty] private bool _isSearchOpen;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _searchStatusText = string.Empty;
    [ObservableProperty] private int _searchCurrentMatchNumber;
    [ObservableProperty] private int _searchTotalMatches;
    [ObservableProperty] private bool _isPropertiesOpen;
    [ObservableProperty] private PdfDocumentInfo? _documentProperties;

    public bool HasDocument => !string.IsNullOrEmpty(SourcePath);

    public string PageLabel
    {
        get
        {
            if (PageCount == 0) return string.Empty;
            if (IsTwoPageMode && PageIndex + 1 < PageCount)
                return $"{PageIndex + 1}-{PageIndex + 2} / {PageCount}";
            return $"{PageIndex + 1} / {PageCount}";
        }
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        var files = await _pickers.PickPdfFilesAsync(false);
        if (files.Count > 0)
            await LoadAsync(files[0]);
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
            PageCount = info.PageCount;
            DocumentProperties = info;

            var lastPage = _recent.GetLastPage(path);
            PageIndex = Math.Clamp(lastPage, 0, Math.Max(0, PageCount - 1));

            Thumbnails.Clear();
            for (var i = 0; i < info.PageCount; i++)
                Thumbnails.Add(new PageItemViewModel { PageIndex = i });

            await _recent.AddAsync(path);
            await RebuildRenderedPagesAsync(_loadCts.Token);
            _ = LoadThumbnailsAsync(_loadCts.Token);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    [RelayCommand] private Task FirstAsync() => ScrollToRowForPageAsync(0);
    [RelayCommand] private Task PreviousAsync() => ScrollToRowForPageAsync(IsTwoPageMode ? PageIndex - 2 : PageIndex - 1);
    [RelayCommand] private Task NextAsync() => ScrollToRowForPageAsync(IsTwoPageMode ? PageIndex + 2 : PageIndex + 1);
    [RelayCommand] private Task LastAsync() => ScrollToRowForPageAsync(PageCount - 1);

    [RelayCommand]
    private async Task GoToPageNumberAsync(string? text)
    {
        if (int.TryParse(text, out var number))
            await GoToAsync(number - 1);
    }

    public async Task GoToAsync(int index)
    {
        if (!HasDocument) return;
        PageIndex = Math.Clamp(index, 0, Math.Max(0, PageCount - 1));
        OnPropertyChanged(nameof(PageLabel));
        _ = _recent.UpdateLastPageAsync(SourcePath!, PageIndex);
        await RenderVisiblePagesAsync();
    }

    public async Task ScrollToRowForPageAsync(int pageIndex)
    {
        if (!HasDocument) return;
        pageIndex = Math.Clamp(pageIndex, 0, Math.Max(0, PageCount - 1));
        PageIndex = pageIndex;
        OnPropertyChanged(nameof(PageLabel));
        _ = _recent.UpdateLastPageAsync(SourcePath!, PageIndex);
        await RenderVisiblePagesAsync();
    }

    [RelayCommand]
    private Task ZoomInAsync()
    {
        Zoom = Math.Min(Zoom + 0.2, 4);
        return CancelAndRebuildAsync();
    }

    [RelayCommand]
    private Task ZoomOutAsync()
    {
        Zoom = Math.Max(Zoom - 0.2, 0.25);
        return CancelAndRebuildAsync();
    }

    [RelayCommand]
    private Task SetZoomAsync(double zoom)
    {
        Zoom = Math.Clamp(zoom, 0.25, 4.0);
        return CancelAndRebuildAsync();
    }

    [RelayCommand]
    private Task FitWidthAsync()
    {
        var targetWidth = IsTwoPageMode ? _pageWidthPoints * 2 + 20 : _pageWidthPoints;
        Zoom = Math.Clamp(ViewportWidth / Math.Max(targetWidth, 1), 0.25, 4);
        return CancelAndRebuildAsync();
    }

    [RelayCommand]
    private Task FitPageAsync()
    {
        var targetWidth = IsTwoPageMode ? _pageWidthPoints * 2 + 20 : _pageWidthPoints;
        var scaleX = ViewportWidth / Math.Max(targetWidth, 1);
        var scaleY = ViewportHeight / Math.Max(_pageHeightPoints, 1);
        Zoom = Math.Clamp(Math.Min(scaleX, scaleY), 0.25, 4);
        return CancelAndRebuildAsync();
    }

    [RelayCommand] private void ToggleThumbnails() => IsThumbnailsVisible = !IsThumbnailsVisible;

    public Task ApplyDarkModeAsync() => CancelAndRebuildAsync();

    public Task ApplyTwoPageModeAsync()
    {
        OnPropertyChanged(nameof(PageLabel));
        return CancelAndRebuildAsync();
    }

    public void AdjustZoomImmediate(double delta)
    {
        var newZoom = Math.Clamp(Zoom + delta, 0.25, 4.0);
        if (Math.Abs(newZoom - Zoom) < 0.001) return;
        Zoom = newZoom;
        UpdatePageDisplayDimensions();
    }

    public void UpdatePageDisplayDimensions()
    {
        var w = _pageWidthPoints * Zoom;
        var h = _pageHeightPoints * Zoom;
        foreach (var p in RenderedPages)
        {
            p.PageWidth = w;
            p.PageHeight = h;
        }
    }

    public Task ApplyZoomAsync() => CancelAndRebuildAsync();

    private Task CancelAndRebuildAsync()
    {
        _renderCts?.Cancel();
        _renderCts = new CancellationTokenSource();
        return RebuildRenderedPagesAsync(_renderCts.Token);
    }

    [RelayCommand]
    private void ToggleSearch()
    {
        IsSearchOpen = !IsSearchOpen;
        if (!IsSearchOpen)
        {
            SearchQuery = string.Empty;
            SearchStatusText = string.Empty;
            SearchTotalMatches = 0;
            SearchCurrentMatchNumber = 0;
            _searchMatchingPages.Clear();
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) || !HasDocument)
        {
            SearchStatusText = string.Empty;
            SearchTotalMatches = 0;
            SearchCurrentMatchNumber = 0;
            _searchMatchingPages.Clear();
            return;
        }

        SearchStatusText = "Aranıyor...";
        var matches = await _text.SearchPagesAsync(SourcePath!, SearchQuery);
        _searchMatchingPages = matches.ToList();
        SearchTotalMatches = _searchMatchingPages.Count;

        if (SearchTotalMatches == 0)
        {
            SearchStatusText = "Eşleşme bulunamadı";
            SearchCurrentMatchNumber = 0;
        }
        else
        {
            var currentIdx = _searchMatchingPages.FindIndex(p => p >= PageIndex);
            if (currentIdx < 0) currentIdx = 0;
            SearchCurrentMatchNumber = currentIdx + 1;
            SearchStatusText = $"{SearchCurrentMatchNumber} / {SearchTotalMatches} sayfa";
            await ScrollToRowForPageAsync(_searchMatchingPages[currentIdx]);
        }
    }

    [RelayCommand]
    private async Task SearchNextAsync()
    {
        if (_searchMatchingPages.Count == 0) return;
        var nextIdx = _searchMatchingPages.FindIndex(p => p > PageIndex);
        if (nextIdx < 0) nextIdx = 0;
        SearchCurrentMatchNumber = nextIdx + 1;
        SearchStatusText = $"{SearchCurrentMatchNumber} / {SearchTotalMatches} sayfa";
        await ScrollToRowForPageAsync(_searchMatchingPages[nextIdx]);
    }

    [RelayCommand]
    private async Task SearchPrevAsync()
    {
        if (_searchMatchingPages.Count == 0) return;
        var prevIdx = _searchMatchingPages.FindLastIndex(p => p < PageIndex);
        if (prevIdx < 0) prevIdx = _searchMatchingPages.Count - 1;
        SearchCurrentMatchNumber = prevIdx + 1;
        SearchStatusText = $"{SearchCurrentMatchNumber} / {SearchTotalMatches} sayfa";
        await ScrollToRowForPageAsync(_searchMatchingPages[prevIdx]);
    }

    [RelayCommand]
    private async Task CopyPageTextAsync()
    {
        if (!HasDocument) return;
        var pageText = await _text.GetPageTextAsync(SourcePath!, PageIndex);
        if (!string.IsNullOrWhiteSpace(pageText))
        {
            var dp = new DataPackage();
            dp.SetText(pageText);
            Clipboard.SetContent(dp);
            ShowSuccess("Sayfa metni panoya kopyalandı.");
        }
        else
        {
            ShowError(new Exception("Bu sayfada seçilebilir metin bulunamadı."));
        }
    }

    [RelayCommand]
    private async Task ShowPropertiesAsync()
    {
        if (!HasDocument) return;
        try
        {
            DocumentProperties = await _documents.GetInfoAsync(SourcePath!);
            IsPropertiesOpen = true;
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    [RelayCommand] private void CloseProperties() => IsPropertiesOpen = false;

    [RelayCommand]
    private void Print()
    {
        if (string.IsNullOrEmpty(SourcePath) || !File.Exists(SourcePath)) return;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = SourcePath,
                Verb = "print",
                UseShellExecute = true,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _log.Error("Print failed", ex);
            ShowError(new Exception("Yazdırma işlemi başlatılamadı: " + ex.Message));
        }
    }

    [RelayCommand]
    private void CloseDocument()
    {
        _loadCts?.Cancel();
        _renderCts?.Cancel();
        SourcePath = null;
        PageCount = 0;
        PageIndex = 0;
        RenderedPages.Clear();
        Thumbnails.Clear();
        IsSearchOpen = false;
        _searchMatchingPages.Clear();
    }

    public async Task RebuildRenderedPagesAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(SourcePath) || PageCount == 0) return;

        try
        {
            var size = await _render.GetPageSizeAsync(SourcePath, 0, cancellationToken);
            _pageWidthPoints = size.WidthPoints;
            _pageHeightPoints = size.HeightPoints;
        }
        catch { }

        var w = _pageWidthPoints * Zoom;
        var h = _pageHeightPoints * Zoom;
        int expectedCount = IsTwoPageMode ? (PageCount + 1) / 2 : PageCount;
        bool needsRebuild = RenderedPages.Count != expectedCount;

        if (needsRebuild)
        {
            App.DispatcherQueue.TryEnqueue(() =>
            {
                RenderedPages.Clear();
                if (IsTwoPageMode)
                {
                    for (var i = 0; i < PageCount; i += 2)
                    {
                        var hasRight = i + 1 < PageCount;
                        RenderedPages.Add(new RenderedPageViewModel
                        {
                            LeftPageIndex = i,
                            HasRightPage = hasRight,
                            PageLabel = hasRight ? $"{i + 1} – {i + 2}" : $"{i + 1}",
                            PageWidth = w,
                            PageHeight = h
                        });
                    }
                }
                else
                {
                    for (var i = 0; i < PageCount; i++)
                    {
                        RenderedPages.Add(new RenderedPageViewModel
                        {
                            LeftPageIndex = i,
                            HasRightPage = false,
                            PageLabel = $"{i + 1}",
                            PageWidth = w,
                            PageHeight = h
                        });
                    }
                }
            });

            await Task.Delay(50, cancellationToken).ContinueWith(_ => { });
        }
        else
        {
            UpdatePageDisplayDimensions();
            foreach (var p in RenderedPages)
            {
                p.IsRendered = false;
            }
        }

        await RenderVisiblePagesAsync(cancellationToken);
    }

    public Task RenderVisiblePagesAsync()
    {
        return RenderVisiblePagesAsync(_renderCts?.Token ?? _loadCts?.Token ?? CancellationToken.None);
    }

    private async Task RenderVisiblePagesAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(SourcePath) || RenderedPages.Count == 0) return;

        var rowList = RenderedPages.ToList();
        int currentRowIndex = 0;
        for (int i = 0; i < rowList.Count; i++)
        {
            var r = rowList[i];
            if (r.LeftPageIndex == PageIndex ||
                (r.HasRightPage && r.LeftPageIndex + 1 == PageIndex))
            {
                currentRowIndex = i;
                break;
            }
        }

        int effectiveWindow = Zoom > 1.4 ? 1 : RenderWindow;
        var startRow = Math.Max(0, currentRowIndex - effectiveWindow);
        var endRow = Math.Min(rowList.Count - 1, currentRowIndex + effectiveWindow);

        var gate = new SemaphoreSlim(2);
        var tasks = new List<Task>();
        var currentZoom = (float)Zoom;

        for (var ri = startRow; ri <= endRow; ri++)
        {
            var row = rowList[ri];
            if (row.IsRendered) continue;

            tasks.Add(Task.Run(async () =>
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var leftImg = await _render.RenderPageAsync(SourcePath, row.LeftPageIndex, currentZoom, cancellationToken);
                    BgraImage? rightImg = null;
                    if (row.HasRightPage)
                    {
                        rightImg = await _render.RenderPageAsync(SourcePath, row.LeftPageIndex + 1, currentZoom, cancellationToken);
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        leftImg.Dispose();
                        rightImg?.Dispose();
                        return;
                    }

                    var tcs = new TaskCompletionSource();
                    var enqueued = App.DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            row.LeftImage = BitmapConversion.ToWriteableBitmap(leftImg, IsDarkMode);
                            if (rightImg != null)
                            {
                                row.RightImage = BitmapConversion.ToWriteableBitmap(rightImg, IsDarkMode);
                            }
                            row.IsRendered = true;
                        }
                        catch (Exception ex)
                        {
                            _log.Error($"Failed setting bitmaps for row {row.LeftPageIndex}", ex);
                        }
                        finally
                        {
                            leftImg.Dispose();
                            rightImg?.Dispose();
                            tcs.TrySetResult();
                        }
                    });

                    if (!enqueued)
                    {
                        leftImg.Dispose();
                        rightImg?.Dispose();
                        tcs.TrySetResult();
                    }

                    await tcs.Task.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _log.Error($"Render row {row.LeftPageIndex} failed", ex);
                }
                finally
                {
                    gate.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task LoadThumbnailsAsync(CancellationToken cancellationToken)
    {
        var gate = new SemaphoreSlim(3);
        foreach (var page in Thumbnails.ToList())
        {
            await gate.WaitAsync(cancellationToken);
            _ = Task.Run(async () =>
            {
                try
                {
                    var image = await _render.RenderThumbnailAsync(SourcePath!, page.PageIndex, 100, cancellationToken);
                    App.DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            page.Preview = BitmapConversion.ToWriteableBitmap(image);
                        }
                        finally
                        {
                            image.Dispose();
                            page.IsLoading = false;
                        }
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
