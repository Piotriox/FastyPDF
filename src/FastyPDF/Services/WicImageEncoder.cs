using System.Runtime.InteropServices.WindowsRuntime;
using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;

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
        var softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, image.Width, image.Height, BitmapAlphaMode.Premultiplied);
        using (var buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
        using (var reference = buffer.CreateReference())
        {
            unsafe
            {
                ((IMemoryBufferByteAccess)reference).GetBuffer(out var data, out var capacity);
                var length = Math.Min(image.Pixels.Length, (int)capacity);
                System.Runtime.InteropServices.Marshal.Copy(image.Pixels, 0, (nint)data, length);
            }
        }

        var folder = Path.GetDirectoryName(outputPath)!;
        var name = Path.GetFileName(outputPath);
        var storageFolder = await StorageFolder.GetFolderFromPathAsync(folder);
        var file = await storageFolder.CreateFileAsync(name, CreationCollisionOption.FailIfExists);
        using var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
        var isJpeg = format.Equals("jpg", StringComparison.OrdinalIgnoreCase)
                     || format.Equals("jpeg", StringComparison.OrdinalIgnoreCase);
        var encoderId = isJpeg ? BitmapEncoder.JpegEncoderId : BitmapEncoder.PngEncoderId;

        BitmapEncoder encoder;
        if (isJpeg)
        {
            var propertySet = new BitmapPropertySet();
            var qualityFloat = Math.Clamp(jpegQuality / 100f, 0.01f, 1.0f);
            var qualityValue = new BitmapTypedValue(qualityFloat, Windows.Foundation.PropertyType.Single);
            propertySet.Add("ImageQuality", qualityValue);
            encoder = await BitmapEncoder.CreateAsync(encoderId, stream, propertySet);
        }
        else
        {
            encoder = await BitmapEncoder.CreateAsync(encoderId, stream);
        }

        encoder.SetSoftwareBitmap(softwareBitmap);
        await encoder.FlushAsync();
    }
}

[System.Runtime.InteropServices.ComImport]
[System.Runtime.InteropServices.Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
[System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
internal unsafe interface IMemoryBufferByteAccess
{
    void GetBuffer(out byte* buffer, out uint capacity);
}
