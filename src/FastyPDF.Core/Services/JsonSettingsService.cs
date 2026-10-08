using System.Text.Json;
using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Models;

namespace FastyPDF.Core.Services;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public AppSettings Current { get; private set; } = new();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();
        if (!File.Exists(AppPaths.SettingsFile))
        {
            Current = new AppSettings();
            return;
        }

        await using var stream = File.OpenRead(AppPaths.SettingsFile);
        var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, Options, cancellationToken)
                     .ConfigureAwait(false);
        Current = loaded ?? new AppSettings();
        if (Current.RecentFileLimit <= 0)
        {
            Current.RecentFileLimit = 10;
        }

        if (Current.JpegQuality is < 40 or > 100)
        {
            Current.JpegQuality = 92;
        }

        if (Current.ThumbnailWidth is < 80 or > 400)
        {
            Current.ThumbnailWidth = 160;
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();
        await using var stream = File.Create(AppPaths.SettingsFile);
        await JsonSerializer.SerializeAsync(stream, Current, Options, cancellationToken).ConfigureAwait(false);
    }
}
