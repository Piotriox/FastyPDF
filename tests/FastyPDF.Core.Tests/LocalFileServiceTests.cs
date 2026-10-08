using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Models;
using FastyPDF.Core.Services;
using Xunit;

namespace FastyPDF.Core.Tests;

public class LocalFileServiceTests
{
    private class FakeSettingsService : ISettingsService
    {
        public AppSettings Current { get; } = new();
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public void BuildSplitFileName_FormatsWithPadding()
    {
        var service = new LocalFileService(new FakeSettingsService());
        var fileName = service.BuildSplitFileName("C:/docs/my-file.pdf", 3, 25);
        Assert.Equal("my-file-03.pdf", fileName);

        var fileNameBig = service.BuildSplitFileName("C:/docs/my-file.pdf", 7, 150);
        Assert.Equal("my-file-007.pdf", fileNameBig);
    }

    [Fact]
    public void BuildPageImageFileName_HandlesExtensionsWithAndWithoutDot()
    {
        var service = new LocalFileService(new FakeSettingsService());
        Assert.Equal("page-005.png", service.BuildPageImageFileName(5, "png"));
        Assert.Equal("page-012.jpg", service.BuildPageImageFileName(12, ".jpg"));
    }

    [Fact]
    public void GetUniqueFilePath_AppendsCounterWhenFileExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "fastypdf-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new LocalFileService(new FakeSettingsService());
            var file1 = Path.Combine(tempDir, "test.pdf");
            File.WriteAllText(file1, "dummy");

            var candidate1 = service.GetUniqueFilePath(tempDir, "test.pdf");
            Assert.Equal(Path.Combine(tempDir, "test-01.pdf"), candidate1);

            File.WriteAllText(candidate1, "dummy");
            var candidate2 = service.GetUniqueFilePath(tempDir, "test.pdf");
            Assert.Equal(Path.Combine(tempDir, "test-02.pdf"), candidate2);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
