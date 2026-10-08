using FastyPDF.Core.Models;

namespace FastyPDF.Core.Abstractions;

public interface IImageEncoder
{
    Task EncodeAsync(
        BgraImage image,
        string outputPath,
        string format,
        int jpegQuality,
        CancellationToken cancellationToken = default);
}
