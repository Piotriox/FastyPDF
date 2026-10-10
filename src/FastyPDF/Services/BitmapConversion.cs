using System.Runtime.InteropServices.WindowsRuntime;
using FastyPDF.Core.Models;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FastyPDF.Services;

public static class BitmapConversion
{
    public static WriteableBitmap ToWriteableBitmap(BgraImage image, bool invertColors = false)
    {
        var bitmap = new WriteableBitmap(image.Width, image.Height);
        var byteCount = image.PixelByteCount;
        using var stream = bitmap.PixelBuffer.AsStream();
        if (invertColors)
        {
            var pixels = image.Pixels;
            for (var i = 0; i < byteCount; i += 4)
            {
                pixels[i] = (byte)(255 - pixels[i]);         // B
                pixels[i + 1] = (byte)(255 - pixels[i + 1]); // G
                pixels[i + 2] = (byte)(255 - pixels[i + 2]); // R
                // Alpha stays unchanged
            }
        }
        stream.Write(image.Pixels, 0, byteCount);
        bitmap.Invalidate();
        return bitmap;
    }
}
