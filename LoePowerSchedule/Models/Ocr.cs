using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LoePowerSchedule.Models;

public class OcrDoc
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string ParsedDateString { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<string> ParsedHours { get; set; } = new List<string>();
    public List<string> ParsedGroups { get; set; } = new List<string>();
}