namespace LoePowerSchedule.Services;

public class TimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    // public DateTimeOffset UtcNow => new DateTimeOffset(2024,07,09,23,30,0,0, TimeSpan.FromHours(3));
}