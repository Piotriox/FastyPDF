using FastyPDF.Core.Abstractions;

namespace FastyPDF.Core.Services;

public sealed class LocalFileService : IFileService
{
    private readonly ISettingsService _settings;

    public LocalFileService(ISettingsService settings)
    {
        _settings = settings;
    }

    public string GetAppDataDirectory()
    {
        AppPaths.EnsureCreated();
        return AppPaths.Root;
    }

    public string GetDefaultOutputDirectory()
    {
        var configured = _settings.Current.DefaultOutputFolder;
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
        {
            return configured;
        }

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var folder = Path.Combine(documents, "FastyPDF");
        Directory.CreateDirectory(folder);
        return folder;
    }

    public string GetUniqueFilePath(string directory, string fileName)
    {
        Directory.CreateDirectory(directory);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var candidate = Path.Combine(directory, fileName);
        var index = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{name}-{index:00}{ext}");
            index++;
        }

        return candidate;
    }

    public string BuildSplitFileName(string sourcePath, int partIndex, int partCount)
    {
        var name = Path.GetFileNameWithoutExtension(sourcePath);
        var width = Math.Max(2, partCount.ToString().Length);
        return $"{name}-{partIndex.ToString().PadLeft(width, '0')}.pdf";
    }

    public string BuildPageImageFileName(int pageNumber, string extension)
    {
        var ext = extension.StartsWith('.') ? extension : "." + extension;
        return $"page-{pageNumber:000}{ext}";
    }
}
