using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Exceptions;
using FastyPDF.Core.Models;
using FastyPDF.Core.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Xunit;

namespace FastyPDF.Core.Tests;

public class PdfSharpManipulationServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PdfSharpManipulationService _service;

    private class NullLog : IAppLog
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
    }

    public PdfSharpManipulationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "fastypdf-pdfsharp-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        _service = new PdfSharpManipulationService(new NullLog());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch { }
        }
    }

    private string CreateSamplePdf(string name, int pageCount)
    {
        var path = Path.Combine(_tempDir, name);
        using var doc = new PdfDocument();
        for (var i = 1; i <= pageCount; i++)
        {
            doc.AddPage();
        }
        doc.Save(path);
        return path;
    }

    [Fact]
    public async Task MergeAsync_MergesMultiplePdfs()
    {
        var pdf1 = CreateSamplePdf("pdf1.pdf", 2);
        var pdf2 = CreateSamplePdf("pdf2.pdf", 3);
        var output = Path.Combine(_tempDir, "merged.pdf");

        await _service.MergeAsync(new[] { pdf1, pdf2 }, output, null);

        Assert.True(File.Exists(output));
        using var mergedDoc = PdfSharp.Pdf.IO.PdfReader.Open(output, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        Assert.Equal(5, mergedDoc.PageCount);
    }

    [Fact]
    public async Task MergeAsync_EmptyInput_ThrowsEmptyInput()
    {
        var output = Path.Combine(_tempDir, "empty.pdf");
        var ex = await Assert.ThrowsAsync<PdfOperationException>(() =>
            _service.MergeAsync(Array.Empty<string>(), output, null));

        Assert.Equal(PdfErrorKind.EmptyInput, ex.Kind);
    }

    [Fact]
    public async Task SplitAsync_SplitsIntoMultipleParts()
    {
        var pdf = CreateSamplePdf("to_split.pdf", 5);
        var ranges = new List<PageRange> { new(1, 2), new(3, 5) };
        var part1 = Path.Combine(_tempDir, "part1.pdf");
        var part2 = Path.Combine(_tempDir, "part2.pdf");

        await _service.SplitAsync(pdf, ranges, new[] { part1, part2 }, null);

        Assert.True(File.Exists(part1));
        Assert.True(File.Exists(part2));
        using var doc1 = PdfSharp.Pdf.IO.PdfReader.Open(part1, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        using var doc2 = PdfSharp.Pdf.IO.PdfReader.Open(part2, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        Assert.Equal(2, doc1.PageCount);
        Assert.Equal(3, doc2.PageCount);
    }

    [Fact]
    public async Task OrganizeAsync_ReordersAndRemovesPages()
    {
        var pdf = CreateSamplePdf("organize.pdf", 4);
        var output = Path.Combine(_tempDir, "organized.pdf");

        // Sayfa sırası: 3. sayfa (index 2), 1. sayfa (index 0)
        await _service.OrganizeAsync(pdf, new[] { 2, 0 }, output, null);

        Assert.True(File.Exists(output));
        using var doc = PdfSharp.Pdf.IO.PdfReader.Open(output, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        Assert.Equal(2, doc.PageCount);
    }

    [Fact]
    public async Task CreateFromImagesAsync_CreatesPdfFromImages()
    {
        // 1x1 piksel geçerli PNG byte'ları
        var pngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

        var images = new List<ImageInput>
        {
            new()
            {
                Path = "test1.png",
                PdfCompatibleBytes = pngBytes,
                Format = "png",
                PixelWidth = 100,
                PixelHeight = 100
            },
            new()
            {
                Path = "test2.png",
                PdfCompatibleBytes = pngBytes,
                Format = "png",
                PixelWidth = 100,
                PixelHeight = 100
            }
        };

        var output = Path.Combine(_tempDir, "images.pdf");
        await _service.CreateFromImagesAsync(images, output, null);

        Assert.True(File.Exists(output));
        using var doc = PdfSharp.Pdf.IO.PdfReader.Open(output, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        Assert.Equal(2, doc.PageCount);
    }
}
