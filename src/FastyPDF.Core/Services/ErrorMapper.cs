using FastyPDF.Core.Exceptions;

namespace FastyPDF.Core.Services;

public static class ErrorMapper
{
    public static PdfOperationException Map(Exception exception, string fallback)
    {
        if (exception is PdfOperationException pdf)
        {
            return pdf;
        }

        if (exception is OperationCanceledException)
        {
            return new PdfOperationException(PdfErrorKind.Canceled, "İşlem iptal edildi.", exception);
        }

        if (exception is FileNotFoundException)
        {
            return new PdfOperationException(PdfErrorKind.FileNotFound, "Dosya bulunamadı.", exception);
        }

        if (exception is UnauthorizedAccessException)
        {
            return new PdfOperationException(PdfErrorKind.AccessDenied, "Dosyaya erişim izni yok.", exception);
        }

        if (exception is IOException io)
        {
            if (IsDiskFull(io))
            {
                return new PdfOperationException(PdfErrorKind.DiskFull, "Diskte yeterli alan yok.", exception);
            }

            if (IsFileLocked(io))
            {
                return new PdfOperationException(PdfErrorKind.FileInUse, "Dosya başka bir uygulama tarafından kullanılıyor.", exception);
            }

            return new PdfOperationException(PdfErrorKind.Unknown, "Dosya işlemi tamamlanamadı.", exception);
        }

        return new PdfOperationException(PdfErrorKind.Unknown, fallback, exception);
    }

    private static bool IsFileLocked(IOException exception)
    {
        const int sharingViolation = unchecked((int)0x80070020);
        const int lockViolation = unchecked((int)0x80070021);
        return exception.HResult is sharingViolation or lockViolation;
    }

    private static bool IsDiskFull(IOException exception)
    {
        const int diskFull = unchecked((int)0x80070070);
        return exception.HResult == diskFull;
    }
}
