using FastyPDF.Core.Models;

namespace FastyPDF.Core.Abstractions;

public interface IPdfManipulationService
{
    Task MergeAsync(
        IReadOnlyList<string> inputPaths,
        string outputPath,
        IProgress<ProgressUpdate>? progress,
        CancellationToken cancellationToken = default);

    Task SplitAsync(
        string inputPath,
        IReadOnlyList<PageRange> ranges,
        IReadOnlyList<string> outputPaths,
        IProgress<ProgressUpdate>? progress,
        CancellationToken cancellationToken = default);

    Task OrganizeAsync(
        string inputPath,
        IReadOnlyList<int> pageIndicesInOrder,
        string outputPath,
        IProgress<ProgressUpdate>? progress,
        CancellationToken cancellationToken = default);

    Task CreateFromImagesAsync(
        IReadOnlyList<ImageInput> images,
        string outputPath,
        IProgress<ProgressUpdate>? progress,
        CancellationToken cancellationToken = default);
}
