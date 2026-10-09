using System.Runtime.InteropServices.WindowsRuntime;
using FastyPDF.Core.Models;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FastyPDF.Services;

public static class BitmapConversion
{
    public static WriteableBitmap ToWriteableBitmap(BgraImage image, bool invertColors = false)
    {
        var bitmap = new WriteableBitmap(image.Width, image.Height);
        using var stream = bitmap.PixelBuffer.AsStream();
        if (!invertColors)
        {
            stream.Write(image.Pixels, 0, image.Pixels.Length);
        }
        else
        {
            var inverted = new byte[image.Pixels.Length];
            for (var i = 0; i < image.Pixels.Length; i += 4)
            {
                inverted[i] = (byte)(255 - image.Pixels[i]);         // B
                inverted[i + 1] = (byte)(255 - image.Pixels[i + 1]); // G
                inverted[i + 2] = (byte)(255 - image.Pixels[i + 2]); // R
                inverted[i + 3] = image.Pixels[i + 3];               // A
            }
            stream.Write(inverted, 0, inverted.Length);
        }
        bitmap.Invalidate();
        return bitmap;
    }
}
