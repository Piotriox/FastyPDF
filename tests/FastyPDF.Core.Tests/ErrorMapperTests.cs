using FastyPDF.Core.Exceptions;
using FastyPDF.Core.Services;
using Xunit;

namespace FastyPDF.Core.Tests;

public class ErrorMapperTests
{
    [Fact]
    public void Map_PdfOperationException_ReturnsSame()
    {
        var original = new PdfOperationException(PdfErrorKind.EncryptedDocument, "Encrypted");
        var result = ErrorMapper.Map(original, "Fallback");
        Assert.Same(original, result);
    }

    [Fact]
    public void Map_OperationCanceledException_ReturnsCanceled()
    {
        var ex = new OperationCanceledException();
        var result = ErrorMapper.Map(ex, "Fallback");
        Assert.Equal(PdfErrorKind.Canceled, result.Kind);
    }

    [Fact]
    public void Map_FileNotFoundException_ReturnsFileNotFound()
    {
        var ex = new FileNotFoundException();
        var result = ErrorMapper.Map(ex, "Fallback");
        Assert.Equal(PdfErrorKind.FileNotFound, result.Kind);
    }

    [Fact]
    public void Map_UnauthorizedAccessException_ReturnsAccessDenied()
    {
        var ex = new UnauthorizedAccessException();
        var result = ErrorMapper.Map(ex, "Fallback");
        Assert.Equal(PdfErrorKind.AccessDenied, result.Kind);
    }

    [Fact]
    public void Map_GenericException_ReturnsFallbackMessage()
    {
        var ex = new InvalidOperationException("Something bad");
        var result = ErrorMapper.Map(ex, "Genel hata oluştu.");
        Assert.Equal(PdfErrorKind.Unknown, result.Kind);
        Assert.Equal("Genel hata oluştu.", result.UserMessage);
    }
}
