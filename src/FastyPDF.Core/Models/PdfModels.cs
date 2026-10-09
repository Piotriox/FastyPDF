namespace FastyPDF.Core.Models;

public sealed class PdfDocumentInfo
{
    public required string Path { get; init; }

    public required string FileName { get; init; }

    public required int PageCount { get; init; }

    public bool IsEncrypted { get; init; }

    public long FileSizeBytes { get; init; }

    public string? Title { get; init; }

    public string? Author { get; init; }

    public string? Subject { get; init; }

    public string? Creator { get; init; }

    public string? Producer { get; init; }

    public string? CreationDate { get; init; }

    public double PageWidthPoints { get; init; }

    public double PageHeightPoints { get; init; }
}

public sealed record PageRange(int StartPage, int EndPage)
{
    public int Length => EndPage - StartPage + 1;
}

public sealed class ProgressUpdate
{
    public string Message { get; init; } = string.Empty;

    public double? Percent { get; init; }
}

public sealed class BgraImage
{
    public required int Width { get; init; }

    public required int Height { get; init; }

    public required int Stride { get; init; }

    public required byte[] Pixels { get; init; }
}

public sealed class PageSize
{
    public required double WidthPoints { get; init; }

    public required double HeightPoints { get; init; }
}

public sealed class RecentFileEntry
{
    public required string Path { get; init; }

    public required string DisplayName { get; init; }

    public DateTimeOffset LastUsedUtc { get; init; }

    public int LastPageIndex { get; set; }
}

public sealed class ImageInput
{
    public required string Path { get; init; }

    public required byte[] PdfCompatibleBytes { get; init; }

    public required string Format { get; init; }

    public required int PixelWidth { get; init; }

    public required int PixelHeight { get; init; }
}
