using FastyPDF.Core.Models;

namespace FastyPDF.Core.Abstractions;

public interface IRecentFilesService
{
    IReadOnlyList<RecentFileEntry> Items { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task AddAsync(string path, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
