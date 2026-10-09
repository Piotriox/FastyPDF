using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Models;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace FastyPDF.Services;

public sealed class WicImageEncoder : IImageEncoder
{
    public async Task EncodeAsync(
        BgraImage image,
        string outputPath,
        string format,
        int jpegQuality,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var isJpeg = string.Equals(format, "jpg", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(format, "jpeg", StringComparison.OrdinalIgnoreCase);

        // JPEG does not support transparency/alpha channel; use Ignore. PNG supports Premultiplied.
        var alphaMode = isJpeg ? BitmapAlphaMode.Ignore : BitmapAlphaMode.Premultiplied;
        var softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, image.Width, image.Height, alphaMode);
        softwareBitmap.CopyFromBuffer(image.Pixels.AsBuffer());

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var encoderId = isJpeg ? BitmapEncoder.JpegEncoderId : BitmapEncoder.PngEncoderId;

        using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var randomAccessStream = fileStream.AsRandomAccessStream();

        BitmapEncoder encoder;
        if (isJpeg)
        {
            var propertySet = new BitmapPropertySet();
            var qualityFloat = Math.Clamp(jpegQuality / 100f, 0.01f, 1.0f);
            var qualityValue = new BitmapTypedValue(qualityFloat, Windows.Foundation.PropertyType.Single);
            propertySet.Add("ImageQuality", qualityValue);
            encoder = await BitmapEncoder.CreateAsync(encoderId, randomAccessStream, propertySet);
        }
        else
        {
            encoder = await BitmapEncoder.CreateAsync(encoderId, randomAccessStream);
        }

        encoder.SetSoftwareBitmap(softwareBitmap);
        await encoder.FlushAsync();
        await randomAccessStream.FlushAsync();
    }
}
