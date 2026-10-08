using FastyPDF.Core.Models;

namespace FastyPDF.Core.Abstractions;

public interface IPdfRenderService
{
    Task<PageSize> GetPageSizeAsync(string path, int pageIndex, CancellationToken cancellationToken = default);

    Task<BgraImage> RenderPageAsync(
        string path,
        int pageIndex,
        float scale,
        CancellationToken cancellationToken = default);

    Task<BgraImage> RenderThumbnailAsync(
        string path,
        int pageIndex,
        int maxWidth,
        CancellationToken cancellationToken = default);
}
