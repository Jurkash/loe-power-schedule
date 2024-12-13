using LoePowerSchedule.Models;
using MongoDB.Driver;
using TimeProvider = LoePowerSchedule.Services.TimeProvider;
namespace LoePowerSchedule.DAL;

public class OcrRepository(MongoDbContext context, TimeProvider timeProvider)
{
    private readonly IMongoCollection<OcrDoc> _hours = context.Hours;

    public async Task SaveOcrResultAsync(string date, List<string> hours, List<string> groups)
    {
        if (hours.Count == 0) return;
        await _hours.InsertOneAsync(new OcrDoc()
        {
            ParsedDateString = date,
            ParsedHours = hours,
            ParsedGroups = groups,
            Timestamp = timeProvider.UtcNow.DateTime
        });
    }
}