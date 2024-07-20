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
            Date = ConstructDateTimeOffset(date, 0),
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

    private List<IntervalDoc> ParseIntervals(DateTime date, List<string> header, List<string> values)
    {
        var result = new List<IntervalDoc>();
        var zip = header
            .Select(ParseTimeWindow)
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
                    StartTime = ConstructDateTimeOffset(date, lastIntervalDoc.StartHour),
                    EndTime = ConstructDateTimeOffset(date, lastIntervalDoc.EndHour),
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

        var lastStartTime = ConstructDateTimeOffset(date, lastIntervalDoc.StartHour);
        result.Add(new IntervalDoc
        {
            State = lastIntervalDoc.State,
            StartTime = lastStartTime,
            EndTime = lastStartTime.AddHours(lastIntervalDoc.EndHour - lastIntervalDoc.StartHour)
        });

        return result;
    }

    private (int from, int to) ParseTimeWindow(string timeString)
    {
        var fromTo = timeString.Split("-");
        var from = int.Parse(fromTo[0]);
        var to = int.Parse(fromTo[1]);
        from = from - to > 0 ? from - 24 : from;
        to = to - from < 0 ? to + 24 : to;
        return (from, to);
    }

    private DateTimeOffset ConstructDateTimeOffset(DateTime date, int hour)
    {
        if (hour == 24)
        {
            date = date.Date.AddDays(1);
            hour = 0;
        }

        return new DateTimeOffset(date.Year, date.Month, date.Day, hour, 0, 0, timeProvider.KyivOffset);
    }
}
