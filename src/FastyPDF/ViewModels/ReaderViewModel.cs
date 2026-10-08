using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastyPDF.Core.Abstractions;
using FastyPDF.Services;
using Microsoft.UI.Xaml.Media;

namespace FastyPDF.ViewModels;

public partial class ReaderViewModel : ToolViewModelBase
{
    private readonly IPdfDocumentService _documents;
    private readonly IPdfRenderService _render;
    private readonly IRecentFilesService _recent;
    private readonly PickerService _pickers;
    private CancellationTokenSource? _loadCts;
    private double _pageWidthPoints = 595;
    private double _pageHeightPoints = 842;

    public ReaderViewModel(
        IPdfDocumentService documents,
        IPdfRenderService render,
        IRecentFilesService recent,
        PickerService pickers)
    {
        _documents = documents;
        _render = render;
        _recent = recent;
        _pickers = pickers;
    }

    public ObservableCollection<PageItemViewModel> Thumbnails { get; } = [];

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
    private ImageSource? _currentPageImage;

    [ObservableProperty]
    private double _zoom = 1.2;

    [ObservableProperty]
    private double _viewportWidth = 800;

    [ObservableProperty]
    private double _viewportHeight = 600;

    public bool HasDocument => !string.IsNullOrEmpty(SourcePath);

    public string PageLabel => PageCount == 0 ? string.Empty : $"{PageIndex + 1} / {PageCount}";

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
            PageCount = info.PageCount;
            PageIndex = 0;
            Thumbnails.Clear();
            for (var i = 0; i < info.PageCount; i++)
            {
                Thumbnails.Add(new PageItemViewModel { PageIndex = i });
            }

            await _recent.AddAsync(path);
            await RenderCurrentAsync(_loadCts.Token);
            _ = LoadThumbnailsAsync(_loadCts.Token);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    [RelayCommand]
    private Task FirstAsync() => GoToAsync(0);

    [RelayCommand]
    private Task PreviousAsync() => GoToAsync(PageIndex - 1);

    [RelayCommand]
    private Task NextAsync() => GoToAsync(PageIndex + 1);

    [RelayCommand]
    private Task LastAsync() => GoToAsync(PageCount - 1);

    [RelayCommand]
    private async Task GoToPageNumberAsync(string? text)
    {
        if (int.TryParse(text, out var number))
        {
            await GoToAsync(number - 1);
        }
    }

    public Task GoToAsync(int index)
    {
        if (!HasDocument)
        {
            return Task.CompletedTask;
        }

        PageIndex = Math.Clamp(index, 0, Math.Max(0, PageCount - 1));
        return RenderCurrentAsync(_loadCts?.Token ?? CancellationToken.None);
    }

    [RelayCommand]
    private Task ZoomInAsync()
    {
        Zoom = Math.Min(Zoom + 0.2, 4);
        return RenderCurrentAsync(_loadCts?.Token ?? CancellationToken.None);
    }

    [RelayCommand]
    private Task ZoomOutAsync()
    {
        Zoom = Math.Max(Zoom - 0.2, 0.25);
        return RenderCurrentAsync(_loadCts?.Token ?? CancellationToken.None);
    }

    [RelayCommand]
    private Task FitWidthAsync()
    {
        Zoom = Math.Clamp(ViewportWidth / Math.Max(_pageWidthPoints, 1), 0.25, 4);
        return RenderCurrentAsync(_loadCts?.Token ?? CancellationToken.None);
    }

    [RelayCommand]
    private Task FitPageAsync()
    {
        var scaleX = ViewportWidth / Math.Max(_pageWidthPoints, 1);
        var scaleY = ViewportHeight / Math.Max(_pageHeightPoints, 1);
        Zoom = Math.Clamp(Math.Min(scaleX, scaleY), 0.25, 4);
        return RenderCurrentAsync(_loadCts?.Token ?? CancellationToken.None);
    }

    [RelayCommand]
    private void CloseDocument()
    {
        _loadCts?.Cancel();
        SourcePath = null;
        PageCount = 0;
        PageIndex = 0;
        CurrentPageImage = null;
        Thumbnails.Clear();
    }

    private async Task RenderCurrentAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(SourcePath) || PageCount == 0)
        {
            return;
        }

        try
        {
            var size = await _render.GetPageSizeAsync(SourcePath, PageIndex, cancellationToken);
            _pageWidthPoints = size.WidthPoints;
            _pageHeightPoints = size.HeightPoints;
            var image = await _render.RenderPageAsync(SourcePath, PageIndex, (float)Zoom, cancellationToken);
            App.DispatcherQueue.TryEnqueue(() => CurrentPageImage = BitmapConversion.ToWriteableBitmap(image));
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
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
                    var image = await _render.RenderThumbnailAsync(SourcePath!, page.PageIndex, 120, cancellationToken);
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
