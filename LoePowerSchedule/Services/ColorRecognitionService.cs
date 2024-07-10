using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace LoePowerSchedule.Services;

public enum ColorType
{
    Unknown,
    Orange,
    Green
}

public class ColorRecognitionService
{
   public ColorType GeZoneColor(Image<Rgba32> image, int x, int y, int width, int height)
   {
       // Calculate the average color of the zone
        Rgba32 averageColor = GetAverageColor(image, x, y, width, height);

        // Classify the color
        ColorType colorCategory = ClassifyColor(averageColor);
        return colorCategory;
   }

    private static Rgba32 GetAverageColor(Image<Rgba32> image, int x, int y, int width, int height)
    {
        long totalR = 0;
        long totalG = 0;
        long totalB = 0;
        long totalA = 0;
        int pixelCount = 0;

        for (int i = x; i < x + width; i++)
        {
            for (int j = y; j < y + height; j++)
            {
                if (i < image.Width && j < image.Height) // Ensure we are within the image boundaries
                {
                    Rgba32 pixel = image[i, j];
                    totalR += pixel.R;
                    totalG += pixel.G;
                    totalB += pixel.B;
                    totalA += pixel.A;
                    pixelCount++;
                }
            }
        }

        return new Rgba32(
            (byte)(totalR / pixelCount),
            (byte)(totalG / pixelCount),
            (byte)(totalB / pixelCount),
            (byte)(totalA / pixelCount)
        );
    }

    private static ColorType ClassifyColor(Rgba32 color)
    {
        // Convert RGB to HSV
        (float hue, float saturation, float value) = RgbToHsv(color);

        // Classify based on hue
        if ((hue >= 0 && hue <= 60) || (hue >= 300 && hue <= 360))
        {
            return ColorType.Orange;
        }
        else if (hue >= 60 && hue <= 180)
        {
            return ColorType.Green;
        }
        else
        {
            return ColorType.Unknown;
        }
    }

    private static (float hue, float saturation, float value) RgbToHsv(Rgba32 color)
    {
        float r = color.R / 255f;
        float g = color.G / 255f;
        float b = color.B / 255f;

        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        float hue = 0;
        if (delta != 0)
        {
            if (max == r)
            {
                hue = 60 * (((g - b) / delta) % 6);
            }
            else if (max == g)
            {
                hue = 60 * (((b - r) / delta) + 2);
            }
            else
            {
                hue = 60 * (((r - g) / delta) + 4);
            }
        }

        float saturation = max == 0 ? 0 : delta / max;
        float value = max;

        if (hue < 0)
        {
            hue += 360;
        }

        return (hue, saturation, value);
    }
}