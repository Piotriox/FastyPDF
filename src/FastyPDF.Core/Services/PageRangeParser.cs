using FastyPDF.Core.Exceptions;
using FastyPDF.Core.Models;

namespace FastyPDF.Core.Services;

public static class PageRangeParser
{
    public static IReadOnlyList<PageRange> Parse(string text, int pageCount)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new PdfOperationException(PdfErrorKind.InvalidPageRange, "Sayfa aralığı boş olamaz.");
        }

        if (pageCount <= 0)
        {
            throw new PdfOperationException(PdfErrorKind.EmptyInput, "PDF içinde sayfa bulunamadı.");
        }

        var ranges = new List<PageRange>();
        var parts = text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (part.Contains('-', StringComparison.Ordinal))
            {
                var bounds = part.Split('-', StringSplitOptions.TrimEntries);
                if (bounds.Length != 2 || !int.TryParse(bounds[0], out var start) || !int.TryParse(bounds[1], out var end))
                {
                    throw new PdfOperationException(PdfErrorKind.InvalidPageRange, $"Geçersiz sayfa aralığı: {part}");
                }

                if (start > end)
                {
                    (start, end) = (end, start);
                }

                Validate(start, end, pageCount, part);
                ranges.Add(new PageRange(start, end));
            }
            else
            {
                if (!int.TryParse(part, out var page))
                {
                    throw new PdfOperationException(PdfErrorKind.InvalidPageRange, $"Geçersiz sayfa numarası: {part}");
                }

                Validate(page, page, pageCount, part);
                ranges.Add(new PageRange(page, page));
            }
        }

        if (ranges.Count == 0)
        {
            throw new PdfOperationException(PdfErrorKind.InvalidPageRange, "Geçerli bir sayfa aralığı bulunamadı.");
        }

        return ranges;
    }

    public static IReadOnlyList<int> ToPageIndices(IEnumerable<PageRange> ranges)
    {
        return ranges
            .SelectMany(range => Enumerable.Range(range.StartPage, range.Length))
            .Select(page => page - 1)
            .Distinct()
            .OrderBy(index => index)
            .ToList();
    }

    private static void Validate(int start, int end, int pageCount, string raw)
    {
        if (start < 1 || end > pageCount)
        {
            throw new PdfOperationException(
                PdfErrorKind.InvalidPageRange,
                $"Sayfa aralığı belgenin dışında: {raw} (1–{pageCount}).");
        }
    }
}
