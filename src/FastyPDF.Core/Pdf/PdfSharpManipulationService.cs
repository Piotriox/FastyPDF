using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Exceptions;
using FastyPDF.Core.Models;
using FastyPDF.Core.Services;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace FastyPDF.Core.Pdf;

public sealed class PdfSharpManipulationService : IPdfManipulationService
{
    private readonly IAppLog _log;

    public PdfSharpManipulationService(IAppLog log)
    {
        _log = log;
    }

    public Task MergeAsync(
        IReadOnlyList<string> inputPaths,
        string outputPath,
        IProgress<ProgressUpdate>? progress,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(() =>
        {
            if (inputPaths.Count == 0)
            {
                throw new PdfOperationException(PdfErrorKind.EmptyInput, "Birleştirmek için en az bir PDF ekleyin.");
            }

            using var output = new PdfDocument();
            for (var i = 0; i < inputPaths.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = inputPaths[i];
                progress?.Report(new ProgressUpdate
                {
                    Message = $"PDF birleştiriliyor... ({i + 1}/{inputPaths.Count})",
                    Percent = (i / (double)inputPaths.Count) * 100
                });

                using var input = OpenForImport(path);
                CopyPages(input, output, Enumerable.Range(0, input.PageCount), cancellationToken);
            }

            SaveNew(output, outputPath);
        }, "PDF birleştirilemedi.", cancellationToken);
    }

    public Task SplitAsync(
        string inputPath,
        IReadOnlyList<PageRange> ranges,
        IReadOnlyList<string> outputPaths,
        IProgress<ProgressUpdate>? progress,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(() =>
        {
            if (ranges.Count == 0)
            {
                throw new PdfOperationException(PdfErrorKind.InvalidPageRange, "Ayrılacak sayfa aralığı yok.");
            }

            if (ranges.Count != outputPaths.Count)
            {
                throw new PdfOperationException(PdfErrorKind.Unknown, "Çıktı dosya sayısı aralık sayısıyla eşleşmiyor.");
            }

            using var input = OpenForImport(inputPath);
            for (var i = 0; i < ranges.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var range = ranges[i];
                progress?.Report(new ProgressUpdate
                {
                    Message = $"PDF ayrılıyor... ({i + 1}/{ranges.Count})",
                    Percent = (i / (double)ranges.Count) * 100
                });

                using var part = new PdfDocument();
                var indices = Enumerable.Range(range.StartPage - 1, range.Length);
                CopyPages(input, part, indices, cancellationToken);
                SaveNew(part, outputPaths[i]);
            }
        }, "PDF ayrılamadı.", cancellationToken);
    }

    public Task OrganizeAsync(
        string inputPath,
        IReadOnlyList<int> pageIndicesInOrder,
        string outputPath,
        IProgress<ProgressUpdate>? progress,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(() =>
        {
            if (pageIndicesInOrder.Count == 0)
            {
                throw new PdfOperationException(PdfErrorKind.EmptyInput, "Kaydedilecek sayfa kalmadı.");
            }

            progress?.Report(new ProgressUpdate { Message = "Sayfa sırası kaydediliyor...", Percent = 10 });
            using var input = OpenForImport(inputPath);
            using var output = new PdfDocument();
            CopyPages(input, output, pageIndicesInOrder, cancellationToken);
            SaveNew(output, outputPath);
        }, "Sayfa sırası kaydedilemedi.", cancellationToken);
    }

    public Task CreateFromImagesAsync(
        IReadOnlyList<ImageInput> images,
        string outputPath,
        IProgress<ProgressUpdate>? progress,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(() =>
        {
            if (images.Count == 0)
            {
                throw new PdfOperationException(PdfErrorKind.EmptyInput, "PDF oluşturmak için en az bir görsel ekleyin.");
            }

            using var output = new PdfDocument();
            for (var i = 0; i < images.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new ProgressUpdate
                {
                    Message = $"Görseller PDF'e dönüştürülüyor... ({i + 1}/{images.Count})",
                    Percent = (i / (double)images.Count) * 100
                });

                var image = images[i];
                using var stream = new MemoryStream(image.PdfCompatibleBytes, writable: false);
                using var xImage = XImage.FromStream(stream);
                var page = output.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                using var gfx = XGraphics.FromPdfPage(page);
                DrawFitted(gfx, xImage, page.Width, page.Height);
            }

            SaveNew(output, outputPath);
        }, "Görseller PDF'e dönüştürülemedi.", cancellationToken);
    }

    private async Task RunAsync(Action work, string fallback, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(work, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var mapped = ErrorMapper.Map(ex, fallback);
            _log.Error(fallback, mapped);
            throw mapped;
        }
    }

    private static PdfDocument OpenForImport(string path)
    {
        try
        {
            return PdfReader.Open(path, PdfDocumentOpenMode.Import);
        }
        catch (PdfReaderException ex) when (ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            throw new PdfOperationException(PdfErrorKind.EncryptedDocument, "Bu PDF şifreli. FastyPDF şifreli belgelerle işlem yapamaz.", ex);
        }
        catch (Exception ex)
        {
            throw new PdfOperationException(PdfErrorKind.CorruptDocument, "PDF dosyası açılamadı. Dosya bozuk veya desteklenmiyor olabilir.", ex);
        }
    }

    private static void CopyPages(PdfDocument source, PdfDocument target, IEnumerable<int> indices, CancellationToken cancellationToken)
    {
        foreach (var index in indices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (index < 0 || index >= source.PageCount)
            {
                throw new PdfOperationException(PdfErrorKind.InvalidPageRange, "Sayfa numarası belgenin dışında.");
            }

            target.AddPage(source.Pages[index]);
        }
    }

    private static void SaveNew(PdfDocument document, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        document.Save(outputPath);
    }

    private static void DrawFitted(XGraphics gfx, XImage image, XUnit pageWidth, XUnit pageHeight)
    {
        const double margin = 18;
        var maxWidth = pageWidth.Point - (margin * 2);
        var maxHeight = pageHeight.Point - (margin * 2);
        var imageRatio = image.PixelWidth / (double)image.PixelHeight;
        var boxRatio = maxWidth / maxHeight;

        double drawWidth;
        double drawHeight;
        if (imageRatio > boxRatio)
        {
            drawWidth = maxWidth;
            drawHeight = maxWidth / imageRatio;
        }
        else
        {
            drawHeight = maxHeight;
            drawWidth = maxHeight * imageRatio;
        }

        var x = margin + ((maxWidth - drawWidth) / 2);
        var y = margin + ((maxHeight - drawHeight) / 2);
        gfx.DrawImage(image, x, y, drawWidth, drawHeight);
    }
}
