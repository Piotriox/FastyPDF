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
                    double width = 0;
                    double height = 0;
                    fpdfview.FPDF_GetPageSizeByIndex(document, 0, ref width, ref height);

                    string? title = null, author = null, subject = null, creator = null, producer = null, creationDate = null;
                    try
                    {
                        using var pdfDoc = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                        title = string.IsNullOrWhiteSpace(pdfDoc.Info.Title) ? null : pdfDoc.Info.Title;
                        author = string.IsNullOrWhiteSpace(pdfDoc.Info.Author) ? null : pdfDoc.Info.Author;
                        subject = string.IsNullOrWhiteSpace(pdfDoc.Info.Subject) ? null : pdfDoc.Info.Subject;
                        creator = string.IsNullOrWhiteSpace(pdfDoc.Info.Creator) ? null : pdfDoc.Info.Creator;
                        producer = string.IsNullOrWhiteSpace(pdfDoc.Info.Producer) ? null : pdfDoc.Info.Producer;
                        if (pdfDoc.Info.CreationDate != DateTime.MinValue)
                        {
                            creationDate = pdfDoc.Info.CreationDate.ToString("yyyy-MM-dd HH:mm");
                        }
                    }
                    catch
                    {
                        // Metadata extraction failure is non-fatal
                    }

                    return new PdfDocumentInfo
                    {
                        Path = path,
                        FileName = file.Name,
                        PageCount = count,
                        FileSizeBytes = file.Length,
                        Title = title,
                        Author = author,
                        Subject = subject,
                        Creator = creator,
                        Producer = producer,
                        CreationDate = creationDate,
                        PageWidthPoints = width,
                        PageHeightPoints = height
                    };
                }
                finally
                {
                    _runtime.CloseDocument(document);
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
