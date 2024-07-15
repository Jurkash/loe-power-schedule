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
        
        foreach (var imageUrl in images)
        {
            var dbSchedule = await scheduleRepository.GetByImageUrlAsync(imageUrl);
            if (dbSchedule?.Groups.Count > 0) continue;

            var ocrTable = await visionService.GetTableFromImage(imageUrl);
            await ocrRepository.SaveOcrResultAsync(
                ocrTable[0].ToList(), 
                ocrTable.Select(r => r[0]).ToList());
            var parsedSchedule = scheduleParserService.ParseScheduleFromList(imageUrl, ocrTable);

            var sameDateScheduleDb = await scheduleRepository.GetByDateAsync(parsedSchedule.Date);
            if (sameDateScheduleDb != null) await scheduleRepository.ArchiveAsync(sameDateScheduleDb.Id);
            await scheduleRepository.CreateAsync(parsedSchedule);
        }
    }
}
