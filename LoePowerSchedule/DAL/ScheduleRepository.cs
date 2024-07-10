using LoePowerSchedule.Models;
using TimeProvider = LoePowerSchedule.Services.TimeProvider;

namespace LoePowerSchedule.DAL;

using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;

public class ScheduleRepository(MongoDbContext context, TimeProvider timeProvider)
{
    private readonly IMongoCollection<ScheduleDoc> _schedules = context.Schedules;

    public async Task<List<ScheduleDoc>> GetAllAsync()
    {
        return await _schedules
            .Find(schedule => true)
            .SortByDescending(s => s.Date)
            .ToListAsync();
    }

    public async Task<ScheduleDoc> GetByDateAsync(DateTimeOffset date)
    {
        var from = date.Date;
        var to = from.AddDays(1);
        return await _schedules
            .Find(schedule => schedule.Date >= from && schedule.Date < to)
            .FirstOrDefaultAsync();
    }

    public async Task<ScheduleDoc?> GetByImageUrlAsync(string imageUrl)
    {
        return await _schedules
            .Find(schedule => schedule.ImageUrl == imageUrl)
            .FirstOrDefaultAsync();
    }

    public async Task<ScheduleDoc> GetLatestAsync()
    {
        return await _schedules
            .Find(schedule => true)
            .SortByDescending(s => s.Date)
            .FirstOrDefaultAsync();
    }

    public async Task<DateTimeOffset?> GetNextStateTime(string groupId, GridState state)
    {
        var now = timeProvider.UtcNow;
        var yesterday = now.AddDays(-1);
        var schedules = await _schedules
            .Find(schedule => schedule.Date >= yesterday)
            .ToListAsync();

        return schedules
            .SelectMany(s => s.Groups)
            .Where(g => g.Id == groupId)
            .SelectMany(g => g.Intervals)
            .Where(i => i.State == state)
            .OrderBy(i => i.StartTime)
            .Aggregate
            (
                new List<IntervalDoc>(),
                (acc, i) =>
                {
                    var shouldMerge =
                        acc.Count > 0
                        && acc.Last().EndTime == i.StartTime
                        && acc.Last().State == i.State;
                    
                    if (shouldMerge) 
                        acc.Last().EndTime = i.EndTime;
                    else 
                        acc.Add(i);
                    return acc;
                })
            .FirstOrDefault(i => i.StartTime > now)?.StartTime ?? null;
    }

    public async Task<ScheduleDoc> GetByIdAsync(string id)
    {
        return await _schedules
            .Find<ScheduleDoc>(schedule => schedule.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(ScheduleDoc schedule)
    {
        await _schedules.InsertOneAsync(schedule);
    }

    public async Task UpdateAsync(string id, ScheduleDoc scheduleIn)
    {
        await _schedules.ReplaceOneAsync(schedule => schedule.Id == id, scheduleIn);
    }

    public async Task RemoveAsync(string id)
    {
        await _schedules.DeleteOneAsync(schedule => schedule.Id == id);
    }
}