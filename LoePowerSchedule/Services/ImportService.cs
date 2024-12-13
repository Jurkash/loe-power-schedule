using LoePowerSchedule.DAL;
using LoePowerSchedule.Extensions;
using Microsoft.Extensions.Options;

namespace LoePowerSchedule.Services;

using System.Threading.Tasks;

public class ImportService(
    ImageScraperService scraperService,
    VisionService visionService,
    ScheduleParserService scheduleParserService,
    ScheduleRepository scheduleRepository,
    OcrRepository ocrRepository,
    IOptions<ImportOptions> importOptions)
{
    public async Task ImportAsync()
    {
        var images = await scraperService.GetImagesFromClass(
            importOptions.Value.ImportUrl,
            importOptions.Value.ImportClassName);

        // images = new List<string>() { "https://api.loe.lviv.ua/media/675ae7497a4db_desk12.jpeg" };
        
        foreach (var imageUrl in images)
        {
            var dbSchedule = await scheduleRepository.GetByImageUrlAsync(imageUrl);
            if (dbSchedule?.Groups.Count > 0) continue;

            var outageHours = await visionService.GetOutageHoursFromImage(imageUrl);
            var parsedSchedule = scheduleParserService.ParseFromHoursGroups(imageUrl, outageHours.date, outageHours.groups);
            
            await ocrRepository.SaveOcrResultAsync(
                outageHours.date.ToString("O"),
                outageHours.groups.Values.SelectMany(g => g).ToList(),
                outageHours.groups.Select(g => g.Key).ToList() 
            );
            
            var sameDateScheduleDb = await scheduleRepository.GetByDateAsync(parsedSchedule.Date);
            if (sameDateScheduleDb != null) await scheduleRepository.ArchiveAsync(sameDateScheduleDb.Id);
            await scheduleRepository.CreateAsync(parsedSchedule);
        }
    }
}
