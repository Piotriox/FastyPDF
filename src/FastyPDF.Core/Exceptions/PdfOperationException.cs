namespace FastyPDF.Core.Exceptions;

public enum PdfErrorKind
{
    Unknown,
    FileNotFound,
    FileInUse,
    AccessDenied,
    DiskFull,
    CorruptDocument,
    EncryptedDocument,
    UnsupportedDocument,
    InvalidPageRange,
    InvalidImage,
    Canceled,
    EmptyInput
}

public sealed class PdfOperationException : Exception
{
    public PdfErrorKind Kind { get; }

    public string UserMessage { get; }

    public PdfOperationException(PdfErrorKind kind, string userMessage, Exception? inner = null)
        : base(userMessage, inner)
    {
        Kind = kind;
        UserMessage = userMessage;
    }
}
