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
        var grayScaleBinaryData = await SharpenImageFromUrl(imageUrl);
        var colorfulBinaryData = await KeepRawImageFromUrl(imageUrl);
        var result = await _visionClient.AnalyzeAsync(
            grayScaleBinaryData,
            VisualFeatures.Read,
            new ImageAnalysisOptions()
            {
                Language = "en",
                GenderNeutralCaption = false,
                ModelVersion = "latest"
            });

        // File.WriteAllBytes("output.png", grayScaleBinaryData.ToArray());

        if (!result.HasValue) return null;

        var dates = new List<DateTime>() { DateTime.Today };
        var dateBoundaries = new List<ImagePoint>();
        var i = 0;
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
                        if (DateTime.TryParseExact(
                                dateMatch.Value,
                                formats,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out var parsedDate
                            ))
                        {
                            dates.Add(parsedDate);
                        }

                        dateBoundaries = word.BoundingPolygon.ToList();
                        continue;
                    }

                    var groupMatch = Regex.Match(word.Text, @"\b\d\.\d\b");
                    if (groupMatch.Success)
                    {
                        i++;
                        schedule.Add(new List<(string, List<ImagePoint>)>());
                        schedule[i].Add((groupMatch.Value, word.BoundingPolygon.ToList()));
                        continue;
                    }

                    var hoursMatch = Regex.Match(word.Text, @"\b\d{1,2}-\d{1,2}\b");
                    if (hoursMatch.Success)
                    {
                        schedule[i].Add((hoursMatch.Value, word.BoundingPolygon.ToList()));
                        continue;
                    }
                }
            }
        }

        schedule[0][0] = (dates.Max().ToShortDateString(), dateBoundaries);
        return EnrichSchedule(schedule, colorfulBinaryData);
    }


    public async Task<(DateTime date, Dictionary<string,List<string>> groups)> GetOutageHoursFromImage(string imageUrl)
    {
        var grayScaleBinaryData = await SharpenImageFromUrl(imageUrl);
        var result = await _visionClient.AnalyzeAsync(
            grayScaleBinaryData,
            VisualFeatures.Read,
            new ImageAnalysisOptions()
            {
                Language = "en",
                GenderNeutralCaption = false,
                ModelVersion = "latest"
            });
        if (!result.HasValue) return (DateTime.Today,null);

        var dates = new List<DateTime>() { DateTime.Today };
        var groups = new List<(string, List<ImagePoint>)>() { };
        var hours = new List<(string, List<ImagePoint>)>() { };

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
                        if (DateTime.TryParseExact(
                                dateMatch.Value,
                                formats,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out var parsedDate
                            ))
                        {
                            dates.Add(parsedDate);
                        }

                        continue;
                    }

                    var groupMatch = Regex.Match(word.Text, @"\b\d\.\d\b");
                    if (groupMatch.Success)
                    {
                        groups.Add((groupMatch.Value, word.BoundingPolygon.ToList()));
                        continue;
                    }

                    var hoursMatch = Regex.Match(word.Text, @"\b\d{1,2}:\d{1,2}\b");
                    if (hoursMatch.Success)
                    {
                        hours.Add((hoursMatch.Value, word.BoundingPolygon.ToList()));
                        continue;
                    }
                }
            }
        }

        return GroupOutageHours(dates, groups, hours);
    }

    private (DateTime date, Dictionary<string,List<string>> groups) GroupOutageHours(
        List<DateTime> dates,
        List<(string name, List<ImagePoint> boundaries)> groups,
        List<(string value, List<ImagePoint> boundaries)> hours)
    {
        var scheduleDate = dates.Max();
        var groupOutageHours = groups
            .Select(g => g.name)
            .ToDictionary(x => x, _ => new List<string>());
        var groupBoundaries = new List<(string name, int top, int right, int bottom, int left)>();

        foreach (var group in groups)
        {
            //var nameLeft = group.boundaries.Min(b => b.X);
            var nameRight = group.boundaries.Max(b => b.X);
            //var nameTop = group.boundaries.Min(b => b.Y);
            var nameBottom = group.boundaries.Max(b => b.Y);
            
            var otherGroupBoundaries = groups
                .Where(g => g.name != group.name)
                .SelectMany(g => g.boundaries)
                .ToList();

            var left = group.boundaries.Min(b => b.X) - 50;
            var top = group.boundaries.Min((b => b.Y)) - 50;

            var right = otherGroupBoundaries
                .Where(b => b.X > nameRight + 50)
                .Select(b => b.X)
                .DefaultIfEmpty(left + 1000)
                .Min() - 50;
            var bottom = otherGroupBoundaries
                .Where(b => b.Y > nameBottom + 50)
                .Select(b => b.Y)
                .DefaultIfEmpty(top + 1000)
                .Min() - 50;

            groupBoundaries.Add((group.name, top, right, bottom, left));
        }

        foreach (var hour in hours)
        {
            var left = hour.boundaries.Min(b => b.X);
            var right = hour.boundaries.Max(b => b.X);
            var top = hour.boundaries.Min(b => b.Y);
            var bottom = hour.boundaries.Max(b => b.Y);

            var relevantGroupName = groupBoundaries
                .FirstOrDefault(g => g.top < top && g.bottom > bottom && g.left < left && g.right > right)
                .name;

            if (relevantGroupName is not null) groupOutageHours[relevantGroupName].Add(hour.value);
        }

        return (scheduleDate, groupOutageHours);
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
            result[i][0] = grid[i][0].data;
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
        image.Mutate(imageProcessingContext => imageProcessingContext
            // .GaussianSharpen()
            .Grayscale()
            // .Contrast(2f)
            // .Saturate(2f)
            .Resize(image.Width * 2, image.Height * 2));
        var customBase64 = image.ToBase64String(JpegFormat.Instance);
        var prefix = "data:image/jpeg;base64,";
        var base64 = customBase64.Substring(prefix.Length, customBase64.Length - prefix.Length);
        return Base64ToBinaryData(base64);
    }

    private async Task<BinaryData> KeepRawImageFromUrl(string imageUrl)
    {
        using var image = await DownloadImageAsync(imageUrl);
        image.Mutate(imageProcessingContext => imageProcessingContext
            // .GaussianSharpen()
            // .Grayscale()
            // .Contrast(2f)
            // .Saturate(2f)
            .Resize(image.Width * 2, image.Height * 2));
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