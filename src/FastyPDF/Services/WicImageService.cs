using System.Runtime.InteropServices.WindowsRuntime;
using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Exceptions;
using FastyPDF.Core.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace FastyPDF.Services;

public sealed class WicImageService : IImageService
{
    public IReadOnlyList<string> SupportedExtensions { get; } =
        [".png", ".jpg", ".jpeg", ".bmp", ".webp", ".tif", ".tiff"];

    public bool IsSupported(string path)
    {
        var ext = Path.GetExtension(path);
        return SupportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ImageInput> PrepareForPdfAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!IsSupported(path))
        {
            throw new PdfOperationException(PdfErrorKind.InvalidImage, "Bu görüntü biçimi desteklenmiyor.");
        }

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            using var stream = await file.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied);

            var ext = Path.GetExtension(path);
            var useJpeg = ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                          || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
            var encoderId = useJpeg ? BitmapEncoder.JpegEncoderId : BitmapEncoder.PngEncoderId;

            using var output = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(encoderId, output);
            encoder.SetSoftwareBitmap(softwareBitmap);
            await encoder.FlushAsync();
            output.Seek(0);

            var bytes = new byte[output.Size];
            await output.ReadAsync(bytes.AsBuffer(), (uint)output.Size, InputStreamOptions.None);

            return new ImageInput
            {
                Path = path,
                PdfCompatibleBytes = bytes,
                Format = useJpeg ? "jpeg" : "png",
                PixelWidth = (int)decoder.PixelWidth,
                PixelHeight = (int)decoder.PixelHeight
            };
        }
        catch (PdfOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new PdfOperationException(PdfErrorKind.InvalidImage, "Görüntü dosyası okunamadı.", ex);
        }
    }
}
