using System.Text.Json;
using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Models;

namespace FastyPDF.Core.Services;

public sealed class RecentFilesService : IRecentFilesService
{
    private readonly ISettingsService _settings;
    private readonly IAppLog _log;
    private List<RecentFileEntry> _items = [];

    public RecentFilesService(ISettingsService settings, IAppLog log)
    {
        _settings = settings;
        _log = log;
    }

    public IReadOnlyList<RecentFileEntry> Items => _items;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();
        if (!File.Exists(AppPaths.RecentFilesFile))
        {
            _items = [];
            return;
        }

        try
        {
            await using var stream = File.OpenRead(AppPaths.RecentFilesFile);
            _items = await JsonSerializer.DeserializeAsync<List<RecentFileEntry>>(stream, cancellationToken: cancellationToken)
                         .ConfigureAwait(false)
                     ?? [];
            _items = _items
                .Where(item => File.Exists(item.Path))
                .Take(_settings.Current.RecentFileLimit)
                .ToList();
        }
        catch (Exception ex)
        {
            _log.Error("Recent files could not be loaded.", ex);
            _items = [];
        }
    }

    public async Task AddAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        var full = Path.GetFullPath(path);
        _items.RemoveAll(item => string.Equals(item.Path, full, StringComparison.OrdinalIgnoreCase));
        _items.Insert(0, new RecentFileEntry
        {
            Path = full,
            DisplayName = Path.GetFileName(full),
            LastUsedUtc = DateTimeOffset.UtcNow
        });

        var limit = Math.Max(1, _settings.Current.RecentFileLimit);
        if (_items.Count > limit)
        {
            _items = _items.Take(limit).ToList();
        }

        await PersistAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _items = [];
        await PersistAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        AppPaths.EnsureCreated();
        await using var stream = File.Create(AppPaths.RecentFilesFile);
        await JsonSerializer.SerializeAsync(stream, _items, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
