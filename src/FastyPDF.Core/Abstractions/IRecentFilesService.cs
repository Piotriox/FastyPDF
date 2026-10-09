using FastyPDF.Core.Models;

namespace FastyPDF.Core.Abstractions;

public interface IRecentFilesService
{
    IReadOnlyList<RecentFileEntry> Items { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task AddAsync(string path, CancellationToken cancellationToken = default);

    Task UpdateLastPageAsync(string path, int pageIndex, CancellationToken cancellationToken = default);

    int GetLastPage(string path);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
