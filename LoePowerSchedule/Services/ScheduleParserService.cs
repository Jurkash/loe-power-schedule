using LoePowerSchedule.Models;

namespace LoePowerSchedule.Services;

public class ScheduleParserService(TimeProvider timeProvider, ILogger<ScheduleParserService> logger)
{
    public ScheduleDoc ParseScheduleFromList(string imageUrl, string[][] input)
    {
        var correctSize = input.ToList().GroupBy(l => l.Length).Count() == 1;

        var date = DateTime.Parse(input[0][0]);
        var schedule = new ScheduleDoc
        {
            Date = ConstructDateTimeOffset(date, 0, 0),
            DateString = date.ToString("O"),
            ImageUrl = imageUrl,
            Groups = input.Skip(1).Select(l => new GroupDoc
            {
                Id = l[0],
                Intervals = ParseIntervals(date, input[0].Skip(1).ToList(), l.Skip(1).ToList())
            }).ToList()
        };

        return schedule;
    }
    
    public ScheduleDoc ParseFromHoursGroups(string imageUrl, DateTime date, Dictionary<string, List<string>> hoursGroups)
    {
        var schedule = new ScheduleDoc
        {
            Date = ConstructDateTimeOffset(date, 0, 0),
            DateString = date.ToString("O"),
            ImageUrl = imageUrl,
            Groups = hoursGroups.Select(pair => new GroupDoc
            {
                Id = pair.Key,
                Intervals = ParseIntervalsFromOutageHours(date, pair.Value)
            }).ToList()
        };

        return schedule;
    }

    private List<IntervalDoc> ParseIntervalsFromOutageHours(DateTime date, List<string> outageHours)
   {
        var result = new List<IntervalDoc>();

        var sortedOutages = outageHours
            .Select(time =>
            {
                var parts = time.Split(':');
                if (parts.Length != 2) throw new FormatException($"Invalid time format: {time}");
        
                int hours = int.Parse(parts[0]);
                int minutes = int.Parse(parts[1]);
        
                return TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);
            })
            .OrderBy(time => time)
            .ToList();

        for (int i = 0; i < sortedOutages.Count; i += 2)
        {
            var from = sortedOutages[i];
            var to = i + 1 < sortedOutages.Count ? sortedOutages[i + 1] : TimeSpan.Zero;

            // Add PowerOn interval before the outage if applicable
            if (i == 0 && from > TimeSpan.Zero)
            {
                result.Add(new IntervalDoc
                {
                    State = GridState.PowerOn,
                    StartTime = ConstructDateTimeOffset(date, 0, 0),
                    EndTime = ConstructDateTimeOffset(date, from.Hours, from.Minutes)
                });
            }

            // Add PowerOff interval for the outage
            result.Add(new IntervalDoc
            {
                State = GridState.PowerOff,
                StartTime = ConstructDateTimeOffset(date, from.Hours, from.Minutes),
                EndTime = ConstructDateTimeOffset(date, to.Hours + to.Days * 24, to.Minutes)
            });

            // Add PowerOn interval after the outage if applicable
            if (i + 1 < sortedOutages.Count && to < TimeSpan.FromHours(24))
            {
                var nextFrom = i + 2 < sortedOutages.Count ? sortedOutages[i + 2] : TimeSpan.FromHours(24);

                result.Add(new IntervalDoc
                {
                    State = GridState.PowerOn,
                    StartTime = ConstructDateTimeOffset(date, to.Hours, to.Minutes),
                    EndTime = ConstructDateTimeOffset(date, nextFrom.Hours + nextFrom.Days * 24, nextFrom.Minutes)
                });
            }
        }

        // Add final PowerOn interval if necessary
        // var lastEnd = sortedOutages.LastOrDefault();
        // if (lastEnd < TimeSpan.FromHours(24))
        // {
        //     result.Add(new IntervalDoc
        //     {
        //         State = GridState.PowerOn,
        //         StartTime = ConstructDateTimeOffset(date, lastEnd.Hours),
        //         EndTime = ConstructDateTimeOffset(date,24)
        //     });
        // }

        return result;
    }
    
    private List<IntervalDoc> ParseIntervals(DateTime date, List<string> header, List<string> values)
    {
        var result = new List<IntervalDoc>();
        var zip = header
            .Select(x => ParseTimeWindow(x,"-"))
            .Zip(values)
            .OrderBy(z => z.First.from)
            .ToList();

        var lastTime = zip[0].First;
        var lastIntervalDoc = new
        {
            State = zip[0].Second == "true" ? GridState.PowerOn : GridState.PowerOff,
            StartHour = lastTime.from,
            EndHour = lastTime.to
        };

        foreach (var item in zip)
        {
            var fromTo = item.First;
            var gridState = item.Second == "true" ? GridState.PowerOn : GridState.PowerOff;
            if (gridState != lastIntervalDoc.State)
            {
                result.Add(new IntervalDoc
                {
                    State = lastIntervalDoc.State,
                    StartTime = ConstructDateTimeOffset(date, lastIntervalDoc.StartHour, 0),
                    EndTime = ConstructDateTimeOffset(date, lastIntervalDoc.EndHour, 0),
                });
                lastIntervalDoc = new
                {
                    State = gridState,
                    StartHour = fromTo.from,
                    EndHour = fromTo.to
                };
            }
            else
            {
                lastIntervalDoc = lastIntervalDoc with { EndHour = fromTo.to };
            }
        }

        var lastStartTime = ConstructDateTimeOffset(date, lastIntervalDoc.StartHour, 0);
        result.Add(new IntervalDoc
        {
            State = lastIntervalDoc.State,
            StartTime = lastStartTime,
            EndTime = lastStartTime.AddHours(lastIntervalDoc.EndHour - lastIntervalDoc.StartHour)
        });

        return result;
    }

    private (int from, int to) ParseTimeWindow(string timeString, string separator = "-")
    {
        var fromTo = timeString.Split(separator);
        var from = int.Parse(fromTo[0]);
        var to = int.Parse(fromTo[1]);
        from = from - to > 0 ? from - 24 : from;
        to = to - from < 0 ? to + 24 : to;
        return (from, to);
    }

    private DateTimeOffset ConstructDateTimeOffset(DateTime date, int hour, int minute)
    {
        if (hour == 24)
        {
            date = date.Date.AddDays(1);
            hour = 0;
        }

        return new DateTimeOffset(date.Year, date.Month, date.Day, hour, minute, 0, timeProvider.KyivOffset);
    }
}
