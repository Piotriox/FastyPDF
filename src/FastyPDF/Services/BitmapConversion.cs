using System.Runtime.InteropServices.WindowsRuntime;
using FastyPDF.Core.Models;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FastyPDF.Services;

public static class BitmapConversion
{
    public static WriteableBitmap ToWriteableBitmap(BgraImage image)
    {
        var bitmap = new WriteableBitmap(image.Width, image.Height);
        using var stream = bitmap.PixelBuffer.AsStream();
        stream.Write(image.Pixels, 0, image.Pixels.Length);
        bitmap.Invalidate();
        return bitmap;
    }
}
