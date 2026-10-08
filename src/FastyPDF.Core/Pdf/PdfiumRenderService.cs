using System.Runtime.InteropServices;
using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Exceptions;
using FastyPDF.Core.Models;
using FastyPDF.Core.Services;
using PDFiumCore;

namespace FastyPDF.Core.Pdf;

public sealed class PdfiumRenderService : IPdfRenderService
{
    private const int AnnotFlag = 1;
    private const int LcdTextFlag = 2;
    private readonly PdfiumRuntime _runtime;
    private readonly IAppLog _log;

    public PdfiumRenderService(PdfiumRuntime runtime, IAppLog log)
    {
        _runtime = runtime;
        _log = log;
    }

    public async Task<PageSize> GetPageSizeAsync(string path, int pageIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _runtime.ExecuteAsync(() =>
            {
                var document = _runtime.LoadDocument(path);
                try
                {
                    double width = 0;
                    double height = 0;
                    if (fpdfview.FPDF_GetPageSizeByIndex(document, pageIndex, ref width, ref height) == 0)
                    {
                        throw new PdfOperationException(PdfErrorKind.CorruptDocument, "Sayfa boyutu okunamadı.");
                    }

                    return new PageSize { WidthPoints = width, HeightPoints = height };
                }
                finally
                {
                    fpdfview.FPDF_CloseDocument(document);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var mapped = ErrorMapper.Map(ex, "Sayfa boyutu alınamadı.");
            _log.Error($"Page size failed: {path} #{pageIndex}", mapped);
            throw mapped;
        }
    }

    public Task<BgraImage> RenderPageAsync(string path, int pageIndex, float scale, CancellationToken cancellationToken = default)
    {
        var safeScale = Math.Clamp(scale, 0.1f, 8f);
        return RenderInternalAsync(path, pageIndex, safeScale, cancellationToken);
    }

    public async Task<BgraImage> RenderThumbnailAsync(string path, int pageIndex, int maxWidth, CancellationToken cancellationToken = default)
    {
        var size = await GetPageSizeAsync(path, pageIndex, cancellationToken).ConfigureAwait(false);
        var scale = (float)(maxWidth / Math.Max(size.WidthPoints, 1));
        return await RenderInternalAsync(path, pageIndex, Math.Clamp(scale, 0.05f, 2f), cancellationToken).ConfigureAwait(false);
    }

    private async Task<BgraImage> RenderInternalAsync(string path, int pageIndex, float scale, CancellationToken cancellationToken)
    {
        try
        {
            return await _runtime.ExecuteAsync(() =>
            {
                var document = _runtime.LoadDocument(path);
                FpdfPageT? page = null;
                FpdfBitmapT? bitmap = null;
                try
                {
                    page = fpdfview.FPDF_LoadPage(document, pageIndex);
                    if (page is null)
                    {
                        throw new PdfOperationException(PdfErrorKind.CorruptDocument, "PDF sayfası yüklenemedi.");
                    }

                    var widthPts = fpdfview.FPDF_GetPageWidthF(page);
                    var heightPts = fpdfview.FPDF_GetPageHeightF(page);
                    var width = Math.Max(1, (int)Math.Round(widthPts * scale));
                    var height = Math.Max(1, (int)Math.Round(heightPts * scale));

                    bitmap = fpdfview.FPDFBitmapCreateEx(width, height, (int)FPDFBitmapFormat.BGRA, IntPtr.Zero, 0);
                    if (bitmap is null)
                    {
                        throw new PdfOperationException(PdfErrorKind.Unknown, "Sayfa görüntüsü oluşturulamadı.");
                    }

                    fpdfview.FPDFBitmapFillRect(bitmap, 0, 0, width, height, 0xFFFFFFFF);
                    fpdfview.FPDF_RenderPageBitmap(bitmap, page, 0, 0, width, height, 0, AnnotFlag | LcdTextFlag);

                    var stride = fpdfview.FPDFBitmapGetStride(bitmap);
                    var buffer = fpdfview.FPDFBitmapGetBuffer(bitmap);
                    var packedStride = width * 4;
                    var pixels = new byte[packedStride * height];
                    if (stride == packedStride)
                    {
                        Marshal.Copy(buffer, pixels, 0, pixels.Length);
                    }
                    else
                    {
                        for (var y = 0; y < height; y++)
                        {
                            Marshal.Copy(IntPtr.Add(buffer, y * stride), pixels, y * packedStride, packedStride);
                        }
                    }

                    return new BgraImage
                    {
                        Width = width,
                        Height = height,
                        Stride = packedStride,
                        Pixels = pixels
                    };
                }
                finally
                {
                    if (bitmap is not null)
                    {
                        fpdfview.FPDFBitmapDestroy(bitmap);
                    }

                    if (page is not null)
                    {
                        fpdfview.FPDF_ClosePage(page);
                    }

                    fpdfview.FPDF_CloseDocument(document);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var mapped = ErrorMapper.Map(ex, "PDF sayfası görüntülenemedi.");
            _log.Error($"Render failed: {path} #{pageIndex}", mapped);
            throw mapped;
        }
    }
}
