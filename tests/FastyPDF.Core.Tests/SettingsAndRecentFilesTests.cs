using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Models;
using FastyPDF.Core.Services;
using Xunit;

namespace FastyPDF.Core.Tests;

public class SettingsAndRecentFilesTests
{
    private class NullLog : IAppLog
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
    }

    private class StubSettingsService : ISettingsService
    {
        public AppSettings Current { get; } = new() { RecentFileLimit = 3 };
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task RecentFilesService_AddsAndTrimsToLimit()
    {
        var tempFile1 = Path.GetTempFileName();
        var tempFile2 = Path.GetTempFileName();
        var tempFile3 = Path.GetTempFileName();
        var tempFile4 = Path.GetTempFileName();

        try
        {
            var service = new RecentFilesService(new StubSettingsService(), new NullLog());
            await service.AddAsync(tempFile1);
            await service.AddAsync(tempFile2);
            await service.AddAsync(tempFile3);
            await service.AddAsync(tempFile4);

            Assert.Equal(3, service.Items.Count);
            // En son eklenen en başta olmalı
            Assert.Equal(Path.GetFullPath(tempFile4), service.Items[0].Path);
            Assert.Equal(Path.GetFullPath(tempFile3), service.Items[1].Path);
            Assert.Equal(Path.GetFullPath(tempFile2), service.Items[2].Path);

            // Tekrar tempFile2 eklenirse, en başa taşınmalı ve çift kayıt olmamalı
            await service.AddAsync(tempFile2);
            Assert.Equal(3, service.Items.Count);
            Assert.Equal(Path.GetFullPath(tempFile2), service.Items[0].Path);
        }
        finally
        {
            File.Delete(tempFile1);
            File.Delete(tempFile2);
            File.Delete(tempFile3);
            File.Delete(tempFile4);
        }
    }

    [Fact]
    public async Task RecentFilesService_ClearAsync_ClearsItems()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var service = new RecentFilesService(new StubSettingsService(), new NullLog());
            await service.AddAsync(tempFile);
            Assert.NotEmpty(service.Items);

            await service.ClearAsync();
            Assert.Empty(service.Items);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
