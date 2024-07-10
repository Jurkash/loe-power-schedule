using LoePowerSchedule.Extensions;
using Microsoft.Extensions.Options;

namespace LoePowerSchedule.Services;

public class ImportHostedService(
    IServiceProvider serviceProvider, 
    IOptions<ScrapeOptions> scrapeOptions) : IHostedService, IDisposable
{
    private Timer _timer;
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(
            DoWork, 
            null, 
            TimeSpan.Zero, 
            TimeSpan.FromSeconds(scrapeOptions.Value.ImportPeriodSec));
        
        return Task.CompletedTask;
    }

    private async void DoWork(object state)
    {
        using var scope = serviceProvider.CreateScope();
        var scheduleParsingService = scope.ServiceProvider.GetRequiredService<ImportService>();
        await scheduleParsingService.ImportAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}