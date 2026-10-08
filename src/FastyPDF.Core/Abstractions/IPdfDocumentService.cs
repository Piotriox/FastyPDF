using FastyPDF.Core.Models;

namespace FastyPDF.Core.Abstractions;

public interface IPdfDocumentService
{
    Task<PdfDocumentInfo> GetInfoAsync(string path, CancellationToken cancellationToken = default);
}
