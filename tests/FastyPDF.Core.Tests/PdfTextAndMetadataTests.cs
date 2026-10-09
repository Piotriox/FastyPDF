using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Pdf;
using PdfSharp.Pdf;

namespace FastyPDF.Core.Tests;

public class PdfTextAndMetadataTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PdfiumRuntime _runtime;

    private class NullLog : IAppLog
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
    }

    public PdfTextAndMetadataTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "fastypdf-textmeta-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        _runtime = new PdfiumRuntime();
    }

    public void Dispose()
    {
        _runtime.Dispose();
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch { }
        }
    }

    [Fact]
    public async Task GetInfoAsync_ExtractsMetadataAndPageDimensions()
    {
        var path = Path.Combine(_tempDir, "meta_sample.pdf");
        using (var doc = new PdfDocument())
        {
            doc.Info.Title = "Test Document Title";
            doc.Info.Author = "FastyPDF Author";
            doc.Info.Subject = "Unit Testing";
            var page = doc.AddPage();
            page.Width = PdfSharp.Drawing.XUnit.FromPoint(600);
            page.Height = PdfSharp.Drawing.XUnit.FromPoint(800);
            doc.Save(path);
        }

        var docService = new PdfiumDocumentService(_runtime, new NullLog());
        var info = await docService.GetInfoAsync(path);

        Assert.Equal(1, info.PageCount);
        Assert.Equal("Test Document Title", info.Title);
        Assert.Equal("FastyPDF Author", info.Author);
        Assert.Equal("Unit Testing", info.Subject);
        Assert.True(info.PageWidthPoints > 0);
        Assert.True(info.PageHeightPoints > 0);
    }

    [Fact]
    public async Task TextService_EmptyPagesHandleCleanly()
    {
        var path = Path.Combine(_tempDir, "empty_sample.pdf");
        using (var doc = new PdfDocument())
        {
            doc.AddPage();
            doc.AddPage();
            doc.Save(path);
        }

        var textService = new PdfiumTextService(_runtime, new NullLog());
        var text = await textService.GetPageTextAsync(path, 0);
        Assert.NotNull(text);

        var searchResults = await textService.SearchPagesAsync(path, "nonexistent");
        Assert.Empty(searchResults);
    }
}
