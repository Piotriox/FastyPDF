namespace FastyPDF.Core.Abstractions;

public interface IFileService
{
    string GetAppDataDirectory();

    string GetDefaultOutputDirectory();

    string GetUniqueFilePath(string directory, string fileName);

    string BuildSplitFileName(string sourcePath, int partIndex, int partCount);

    string BuildPageImageFileName(int pageNumber, string extension);
}
