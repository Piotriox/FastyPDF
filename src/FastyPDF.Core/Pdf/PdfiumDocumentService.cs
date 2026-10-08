using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Exceptions;
using FastyPDF.Core.Models;
using FastyPDF.Core.Services;
using PDFiumCore;

namespace FastyPDF.Core.Pdf;

public sealed class PdfiumDocumentService : IPdfDocumentService
{
    private readonly PdfiumRuntime _runtime;
    private readonly IAppLog _log;

    public PdfiumDocumentService(PdfiumRuntime runtime, IAppLog log)
    {
        _runtime = runtime;
        _log = log;
    }

    public async Task<PdfDocumentInfo> GetInfoAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _runtime.ExecuteAsync(() =>
            {
                var document = _runtime.LoadDocument(path);
                try
                {
                    var count = fpdfview.FPDF_GetPageCount(document);
                    if (count <= 0)
                    {
                        throw new PdfOperationException(PdfErrorKind.EmptyInput, "PDF içinde sayfa bulunamadı.");
                    }

                    var file = new FileInfo(path);
                    return new PdfDocumentInfo
                    {
                        Path = path,
                        FileName = file.Name,
                        PageCount = count,
                        FileSizeBytes = file.Length
                    };
                }
                finally
                {
                    fpdfview.FPDF_CloseDocument(document);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var mapped = ErrorMapper.Map(ex, "PDF bilgileri okunamadı.");
            _log.Error($"Failed to read PDF info: {path}", mapped);
            throw mapped;
        }
    }
}
