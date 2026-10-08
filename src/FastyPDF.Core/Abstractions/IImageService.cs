using FastyPDF.Core.Models;

namespace FastyPDF.Core.Abstractions;

public interface IImageService
{
    IReadOnlyList<string> SupportedExtensions { get; }

    bool IsSupported(string path);

    Task<ImageInput> PrepareForPdfAsync(string path, CancellationToken cancellationToken = default);
}
