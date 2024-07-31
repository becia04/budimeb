using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.IO;

public static class ImageResizer
{
    public static void ResizeImage(Stream inputStream, Stream outputStream, int maxWidth, int maxHeight, int quality)
    {
        using (var image = SixLabors.ImageSharp.Image.Load(inputStream))
        {
            // Calculate new dimensions while preserving the aspect ratio
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxWidth, maxHeight)
            }));

            var encoder = new JpegEncoder { Quality = quality };

            image.Save(outputStream, encoder);
        }
    }
}
