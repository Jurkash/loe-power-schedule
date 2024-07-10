using LoePowerSchedule.Models;
using MongoDB.Driver;

namespace LoePowerSchedule.DAL;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<ScheduleDoc> Schedules => _database.GetCollection<ScheduleDoc>("Schedules");
}