using FastyPDF.Core.Abstractions;
using PDFiumCore;

namespace FastyPDF.Core.Pdf;

public sealed class PdfiumTextService : IPdfTextService
{
    private readonly PdfiumRuntime _runtime;
    private readonly IAppLog _log;

    public PdfiumTextService(PdfiumRuntime runtime, IAppLog log)
    {
        _runtime = runtime;
        _log = log;
    }

    public async Task<string> GetPageTextAsync(string path, int pageIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _runtime.ExecuteAsync(() =>
            {
                var doc = _runtime.LoadDocument(path);
                try
                {
                    var page = fpdfview.FPDF_LoadPage(doc, pageIndex);
                    if (page is null)
                    {
                        return string.Empty;
                    }

                    try
                    {
                        var textPage = fpdf_text.FPDFTextLoadPage(page);
                        if (textPage is null)
                        {
                            return string.Empty;
                        }

                        try
                        {
                            var count = fpdf_text.FPDFTextCountChars(textPage);
                            if (count <= 0)
                            {
                                return string.Empty;
                            }

                            var buffer = new ushort[count + 1];
                            var written = fpdf_text.FPDFTextGetText(textPage, 0, count, ref buffer[0]);
                            var charLength = Math.Max(0, written - 1);
                            var chars = new char[charLength];
                            for (var i = 0; i < charLength; i++)
                            {
                                chars[i] = (char)buffer[i];
                            }

                            return new string(chars);
                        }
                        finally
                        {
                            fpdf_text.FPDFTextClosePage(textPage);
                        }
                    }
                    finally
                    {
                        fpdfview.FPDF_ClosePage(page);
                    }
                }
                finally
                {
                    fpdfview.FPDF_CloseDocument(doc);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to extract text from {path} page {pageIndex}", ex);
            return string.Empty;
        }
    }

    public async Task<IReadOnlyList<int>> SearchPagesAsync(string path, string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        try
        {
            return await _runtime.ExecuteAsync(() =>
            {
                var matches = new List<int>();
                var doc = _runtime.LoadDocument(path);
                try
                {
                    var pageCount = fpdfview.FPDF_GetPageCount(doc);
                    for (var i = 0; i < pageCount; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var page = fpdfview.FPDF_LoadPage(doc, i);
                        if (page is null)
                        {
                            continue;
                        }

                        try
                        {
                            var textPage = fpdf_text.FPDFTextLoadPage(page);
                            if (textPage is null)
                            {
                                continue;
                            }

                            try
                            {
                                var count = fpdf_text.FPDFTextCountChars(textPage);
                                if (count <= 0)
                                {
                                    continue;
                                }

                                var buffer = new ushort[count + 1];
                                var written = fpdf_text.FPDFTextGetText(textPage, 0, count, ref buffer[0]);
                                var charLength = Math.Max(0, written - 1);
                                var chars = new char[charLength];
                                for (var c = 0; c < charLength; c++)
                                {
                                    chars[c] = (char)buffer[c];
                                }

                                var text = new string(chars);
                                if (text.Contains(query, StringComparison.OrdinalIgnoreCase))
                                {
                                    matches.Add(i);
                                }
                            }
                            finally
                            {
                                fpdf_text.FPDFTextClosePage(textPage);
                            }
                        }
                        finally
                        {
                            fpdfview.FPDF_ClosePage(page);
                        }
                    }

                    return (IReadOnlyList<int>)matches;
                }
                finally
                {
                    fpdfview.FPDF_CloseDocument(doc);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to search text in {path}", ex);
            return [];
        }
    }
}
