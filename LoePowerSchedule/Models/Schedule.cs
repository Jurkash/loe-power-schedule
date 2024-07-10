using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LoePowerSchedule.Models;

public class ScheduleDoc
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
    public string DateString { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    public List<GroupDoc> Groups { get; set; } = new List<GroupDoc>();
}

public class GroupDoc
{
    public string Id { get; set; }
    public List<IntervalDoc> Intervals { get; set; } = new List<IntervalDoc>();
}

public class IntervalDoc
{
    public GridState State { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; } 
}

public enum GridState
{
    PowerOn = 1,
    PowerOff = 0
}