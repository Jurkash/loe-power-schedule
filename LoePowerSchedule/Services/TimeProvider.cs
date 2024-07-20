namespace LoePowerSchedule.Services;

public class TimeProvider
{
    private readonly TimeZoneInfo _tzKyivInfo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTimeOffset KyivNow => UtcNow.ToOffset(KyivOffset);

    public TimeSpan KyivOffset => _tzKyivInfo.GetUtcOffset(UtcNow);
    // public DateTimeOffset UtcNow => new DateTimeOffset(2024,07,09,23,30,0,0, TimeSpan.FromHours(3));
}