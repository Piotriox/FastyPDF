using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Undo;
using FastyPDF.Services;

namespace FastyPDF.ViewModels;

public partial class OrganizeViewModel : ToolViewModelBase
{
    private readonly IPdfDocumentService _documents;
    private readonly IPdfManipulationService _manipulation;
    private readonly IPdfRenderService _render;
    private readonly ISettingsService _settings;
    private readonly IRecentFilesService _recent;
    private readonly PickerService _pickers;
    private readonly UndoRedoService<int[]> _undo = new();
    private readonly Dictionary<int, PageItemViewModel> _pagePool = new();
    private CancellationTokenSource? _loadCts;
    private bool _suspendHistory;

    public OrganizeViewModel(
        IPdfDocumentService documents,
        IPdfManipulationService manipulation,
        IPdfRenderService render,
        ISettingsService settings,
        IRecentFilesService recent,
        PickerService pickers)
    {
        _documents = documents;
        _manipulation = manipulation;
        _render = render;
        _settings = settings;
        _recent = recent;
        _pickers = pickers;
        Pages.CollectionChanged += OnPagesChanged;
    }

    public ObservableCollection<PageItemViewModel> Pages { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDocument))]
    private string? _sourcePath;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

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
        _undo.Clear();
        try
        {
            var info = await _documents.GetInfoAsync(path, _loadCts.Token);
            SourcePath = info.Path;
            _suspendHistory = true;
            Pages.Clear();
            _pagePool.Clear();
            for (var i = 0; i < info.PageCount; i++)
            {
                var pageItem = new PageItemViewModel { PageIndex = i };
                _pagePool[i] = pageItem;
                Pages.Add(pageItem);
            }

            _suspendHistory = false;
            RefreshUndoState();
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
    private void DeleteSelected(IList<object>? selectedItems)
    {
        var selected = selectedItems?.OfType<PageItemViewModel>().ToList() ?? [];
        if (selected.Count == 0)
        {
            return;
        }

        PushHistory();
        _suspendHistory = true;
        foreach (var item in selected)
        {
            Pages.Remove(item);
        }

        _suspendHistory = false;
        RefreshUndoState();
    }

    [RelayCommand]
    private void Undo()
    {
        var previous = _undo.Undo(Snapshot());
        if (previous is null)
        {
            return;
        }

        Restore(previous);
    }

    [RelayCommand]
    private void Redo()
    {
        var next = _undo.Redo(Snapshot());
        if (next is null)
        {
            return;
        }

        Restore(next);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(SourcePath) || Pages.Count == 0)
        {
            return;
        }

        var suggested = Path.GetFileNameWithoutExtension(SourcePath) + "-organized.pdf";
        var output = await _pickers.PickSavePdfAsync(suggested);
        if (string.IsNullOrWhiteSpace(output))
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Yeni PDF oluşturuluyor...";
        try
        {
            await _manipulation.OrganizeAsync(SourcePath, Pages.Select(page => page.PageIndex).ToList(), output, CreateProgress());
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

    public void CaptureBeforeReorder()
    {
        if (_suspendHistory)
        {
            return;
        }

        PushHistory();
    }

    private void OnPagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_suspendHistory || e.Action is NotifyCollectionChangedAction.Reset or NotifyCollectionChangedAction.Add)
        {
            return;
        }

        RefreshUndoState();
    }

    private void PushHistory()
    {
        _undo.Push(Snapshot());
        RefreshUndoState();
    }

    private int[] Snapshot() => Pages.Select(page => page.PageIndex).ToArray();

    private void Restore(int[] indices)
    {
        _suspendHistory = true;
        Pages.Clear();
        foreach (var index in indices)
        {
            if (_pagePool.TryGetValue(index, out var page))
            {
                Pages.Add(page);
            }
            else
            {
                var newPage = new PageItemViewModel { PageIndex = index };
                _pagePool[index] = newPage;
                Pages.Add(newPage);
            }
        }

        _suspendHistory = false;
        RefreshUndoState();
    }

    private void RefreshUndoState()
    {
        CanUndo = _undo.CanUndo;
        CanRedo = _undo.CanRedo;
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadThumbnailsAsync(CancellationToken cancellationToken)
    {
        var gate = new SemaphoreSlim(3);
        var pages = Pages.ToList();
        foreach (var page in pages)
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
