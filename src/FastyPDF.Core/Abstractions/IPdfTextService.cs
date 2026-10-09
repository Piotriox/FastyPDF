namespace FastyPDF.Core.Abstractions;

public interface IPdfTextService
{
    Task<string> GetPageTextAsync(string path, int pageIndex, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> SearchPagesAsync(string path, string query, CancellationToken cancellationToken = default);
}
