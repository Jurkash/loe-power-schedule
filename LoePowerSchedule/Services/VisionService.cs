using System.Globalization;
using System.Text.RegularExpressions;
using Azure.AI.Vision.ImageAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LoePowerSchedule.Services;

public class VisionService(
    ImageAnalysisClient visionClient,
    HttpClient httpClient,
    ColorRecognitionService colorService)
{
    private readonly ImageAnalysisClient _visionClient =
        visionClient ?? throw new ArgumentNullException(nameof(visionClient));

    private readonly HttpClient _httpClient =
        httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    private readonly ColorRecognitionService _colorService =
        colorService ?? throw new ArgumentNullException(nameof(colorService));

    public async Task<string[][]?> GetTableFromImage(string imageUrl)
    {
        var binaryData = await SharpenImageFromUrl(imageUrl);
        var result = await _visionClient.AnalyzeAsync(
            binaryData,
            VisualFeatures.Read);

        if (!result.HasValue) return null;

        var date = DateTime.Today;
        var dateBoundaries = new List<ImagePoint>();
        var i = 0;
        var j = 0;
        var schedule = new List<List<(string data, List<ImagePoint> boundaries)>> { new() };
        schedule[0].Add(("X", new List<ImagePoint>()));

        foreach (var block in result.Value.Read.Blocks)
        {
            foreach (var line in block.Lines)
            {
                foreach (var word in line.Words)
                {
                    var dateMatch = Regex.Match(word.Text, @"\d{1,2}\.\d{1,2}\.\d{4}");
                    if (dateMatch.Success)
                    {
                        var formats = new[] { "dd.MM.yyyy", "d.MM.yyyy", "dd.M.yyyy", "d.M.yyyy" };
                        bool isParsed = DateTime.TryParseExact(
                            dateMatch.Value,
                            formats,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var parsedDate
                        );
                        date = isParsed ? parsedDate : date;
                        dateBoundaries = word.BoundingPolygon.ToList();
                        continue;
                    }

                    var groupMatch = Regex.Match(word.Text, @"\b\d\.\d\b");
                    if (groupMatch.Success)
                    {
                        i++;
                        j = 1;
                        schedule.Add(new List<(string, List<ImagePoint>)>());
                        schedule[i].Add((groupMatch.Value, word.BoundingPolygon.ToList()));
                        continue;
                    }

                    var hoursMatch = Regex.Match(word.Text, @"\b\d{1,2}-\d{1,2}\b");
                    if (hoursMatch.Success)
                    {
                        j++;
                        schedule[i].Add((hoursMatch.Value, word.BoundingPolygon.ToList()));
                        continue;
                    }

                    // if (i > 0)
                    // {
                    //     j++;
                    //     var res = line.Words.Count > 1 ? "true" : "false";
                    //     var text = line.Text.ToLower();
                    //     if (line.Words.Count > 0
                    //         || text.Contains("є")
                    //         || text.Contains("р")
                    //         || text.Contains("г")
                    //         || text.Contains("і")
                    //         || text.Contains("я"))
                    //         res = "true";
                    //     if (text.Contains("в")
                    //         || text.Contains("и")
                    //         || text.Contains("м")
                    //         || text.Contains("к")
                    //         || text.Contains("о"))
                    //         res = "false";
                    //     schedule[i].Add(res);
                    // }
                }
            }
        }

        schedule[0][0] = (date.ToShortDateString(), dateBoundaries);
        return EnrichSchedule(schedule, binaryData);
    }

    private string[][] EnrichSchedule(List<List<(string data, List<ImagePoint> boundaries)>> grid,
        BinaryData binaryImage)
    {
        var byteData = new ReadOnlySpan<byte>(binaryImage.ToArray());
        using var rgbaImage = Image.Load<Rgba32>(byteData);
        var result = new string[grid.Count][];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new string [grid[0].Count];
        }
        result[0] = grid[0].Select(x => x.data).ToArray();

        var headers = grid[0];
        for (var i = 1; i < grid.Count; i++)
        {
            result[i][0] =grid[i][0].data;
            var groupId = grid[i][0];
            for (var j = 1; j < result[i].Length; j++)
            {
                var x = headers[j].boundaries.Min(b => b.X);
                var y = grid[i][0].boundaries.Min(b => b.Y);
                var width = headers[j].boundaries.Max(b => b.X) - x;
                var height = grid[i][0].boundaries.Max(b => b.Y) - y;
                var color = _colorService.GeZoneColor(rgbaImage, x, y, width, height);
                var cell = color switch
                {
                    ColorType.Orange => "false",
                    ColorType.Green => "true",
                    _ => "false"
                };
                result[i][j] = cell;
            }
        }

        return result;
    }

    private async Task<BinaryData> SharpenImageFromUrl(string imageUrl)
    {
        using var image = await DownloadImageAsync(imageUrl);
        // using var image = await OpenImageAsync("./wwwroot/2024-07-09 22.32.57.jpg");
        image.Mutate(x => x.GaussianSharpen());
        var customBase64 = image.ToBase64String(JpegFormat.Instance);
        var prefix = "data:image/jpeg;base64,";
        var base64 = customBase64.Substring(prefix.Length, customBase64.Length - prefix.Length);
        return Base64ToBinaryData(base64);
    }

    private async Task<Image<Rgba32>> DownloadImageAsync(string url)
    {
        using var client = new HttpClient();
        var imageBytes = await _httpClient.GetByteArrayAsync(url);
        return Image.Load<Rgba32>(imageBytes);
    }

    private async Task<Image<Rgba32>> OpenImageAsync(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("The specified file was not found.", imagePath);
        }

        await using FileStream stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
        return await Image.LoadAsync<Rgba32>(stream);
    }

    private static BinaryData Base64ToBinaryData(string base64String)
    {
        byte[] imageBytes = Convert.FromBase64String(base64String);
        return new BinaryData(imageBytes);
    }
}