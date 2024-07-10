using HtmlAgilityPack;
using LoePowerSchedule.Extensions;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace LoePowerSchedule.Services;

public class ImageScraperService(IOptions<BrowserOptions> browserOptions)
{
    public async Task<List<string>> GetImagesFromClass(string url, string className)
    {
        var content = await GetPageContentAsync(url, className);

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(content);
        var nodes = htmlDocument.DocumentNode.SelectNodes($"//*[contains(@class, '{className}')]");

        var images = new List<string>();

        if (nodes == null) return images;
        foreach (var node in nodes)
        {
            var imgNodes = node.SelectNodes(".//img");
            if (imgNodes == null) continue;
            foreach (var imgNode in imgNodes)
            {
                var src = imgNode.GetAttributeValue("src", null);
                if (src == null) continue;
                var imageUrl = src.StartsWith("http") ? src : $"{url}/{src.TrimStart('/')}";
                images.Add(imageUrl);
            }
        }

        return images;
    }

    private async Task<string> GetPageContentAsync(string url, string className)
    {
        // Connect to docker browserless/chrome container
        var browser = await Puppeteer.ConnectAsync(new ConnectOptions()
        {
            BrowserWSEndpoint = browserOptions.Value.BrowserUrl
        });
        await using var page = await browser.NewPageAsync();
        await page.GoToAsync(url);

        // Wait for the required component to render
        await page.WaitForSelectorAsync($"[class*='{className}']");

        // Extract the content
        var content = await page.GetContentAsync();

        return content;
    }
}