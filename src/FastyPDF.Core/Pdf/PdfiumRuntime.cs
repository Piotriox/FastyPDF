using FastyPDF.Core.Exceptions;
using PDFiumCore;

namespace FastyPDF.Core.Pdf;

public sealed class PdfiumRuntime : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public async Task<T> ExecuteAsync<T>(Func<T> action, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureInitialized();
            return action();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ExecuteAsync(Action action, CancellationToken cancellationToken)
    {
        await ExecuteAsync(() =>
        {
            action();
            return 0;
        }, cancellationToken).ConfigureAwait(false);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        fpdfview.FPDF_InitLibrary();
        _initialized = true;
    }

    public FpdfDocumentT LoadDocument(string path)
    {
        if (!File.Exists(path))
        {
            throw new PdfOperationException(PdfErrorKind.FileNotFound, "PDF dosyası bulunamadı.");
        }

        var document = fpdfview.FPDF_LoadDocument(path, null);
        if (document is null)
        {
            var error = fpdfview.FPDF_GetLastError();
            throw error switch
            {
                2 => new PdfOperationException(PdfErrorKind.FileNotFound, "PDF dosyası okunamadı."),
                3 => new PdfOperationException(PdfErrorKind.CorruptDocument, "PDF dosyası bozuk veya okunamıyor."),
                4 => new PdfOperationException(PdfErrorKind.EncryptedDocument, "Bu PDF şifreli. FastyPDF şifreli belgelerle işlem yapamaz."),
                5 => new PdfOperationException(PdfErrorKind.EncryptedDocument, "Bu PDF güvenlik kısıtlamaları nedeniyle açılamıyor."),
                _ => new PdfOperationException(PdfErrorKind.UnsupportedDocument, "PDF dosyası açılamadı.")
            };
        }

        return document;
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _gate.Wait();
        try
        {
            fpdfview.FPDF_DestroyLibrary();
            _initialized = false;
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }
}
