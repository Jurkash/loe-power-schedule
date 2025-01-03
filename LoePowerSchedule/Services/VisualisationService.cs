using System.Globalization;
using LoePowerSchedule.DAL;
using LoePowerSchedule.Models;

namespace LoePowerSchedule.Services;

public class VisualisationService(
    ScheduleRepository scheduleRepository,
    TimeProvider timeProvider)
{
    private const string OutageIconColor = "gray";
    private const string ConnectedIconColor = "#fee440";

    private const string ConnectedIcon = $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 24 24\" fill=\"none\"><path d=\"M15.6976 17C15.915 16.7003 17.4283 16.0445 18.3029 14.9274C19.3662 13.5692 20 11.8586 20 10C20 5.58172 16.4183 2 12 2C7.58172 2 4 5.58172 4 10C4 11.8586 4.6338 13.5691 5.69706 14.9273C6.57163 16.0445 8.08503 16.7003 8.3024 17H15.6976Z\" fill=\"{ConnectedIconColor}\" fill-opacity=\"0.16\"/><path d=\"M9 18L15 18\" stroke=\"{ConnectedIconColor}\" stroke-width=\"2\" stroke-linecap=\"round\"/><mask id=\"path-3-inside-1_512_69\" fill=\"white\"><path fill-rule=\"evenodd\" clip-rule=\"evenodd\" d=\"M16.5 18.5892C16.5 17.8684 16.8991 17.2173 17.4756 16.7845C19.6185 15.176 21 12.6457 21 9.8C21 4.93989 16.9706 1 12 1C7.02944 1 3 4.93989 3 9.8C3 12.6457 4.38146 15.176 6.52442 16.7845C7.10094 17.2173 7.5 17.8684 7.5 18.5892V20C7.5 21.6569 8.84315 23 10.5 23H13.5C15.1569 23 16.5 21.6569 16.5 20V18.5892Z\"/></mask><path d=\"M17.4756 16.7845L18.6762 18.384L17.4756 16.7845ZM19 9.8C19 11.9794 17.9451 13.9313 16.2749 15.185L18.6762 18.384C21.292 16.4206 23 13.3121 23 9.8H19ZM12 3C15.9086 3 19 6.0866 19 9.8H23C23 3.79319 18.0325 -1 12 -1V3ZM5 9.8C5 6.0866 8.0914 3 12 3V-1C5.96748 -1 1 3.79319 1 9.8H5ZM7.72507 15.185C6.0549 13.9313 5 11.9794 5 9.8H1C1 13.3121 2.70802 16.4206 5.32378 18.384L7.72507 15.185ZM9.5 20V18.5892H5.5V20H9.5ZM10.5 21C9.94771 21 9.5 20.5523 9.5 20H5.5C5.5 22.7614 7.73858 25 10.5 25V21ZM13.5 21H10.5V25H13.5V21ZM14.5 20C14.5 20.5523 14.0523 21 13.5 21V25C16.2614 25 18.5 22.7614 18.5 20H14.5ZM14.5 18.5892V20H18.5V18.5892H14.5ZM5.32378 18.384C5.40813 18.4474 5.45867 18.5119 5.4832 18.5557C5.50541 18.5954 5.5 18.6049 5.5 18.5892H9.5C9.5 17.1061 8.68792 15.9078 7.72507 15.185L5.32378 18.384ZM16.2749 15.185C15.3121 15.9078 14.5 17.1061 14.5 18.5892H18.5C18.5 18.6049 18.4946 18.5954 18.5168 18.5557C18.5413 18.5119 18.5919 18.4474 18.6762 18.384L16.2749 15.185Z\" fill=\"{ConnectedIconColor}\" mask=\"url(#path-3-inside-1_512_69)\"/><path d=\"M8 10C8 7.79086 9.79086 6 12 6\" stroke=\"{ConnectedIconColor}\" stroke-width=\"2\" stroke-linecap=\"round\"/></svg>";
    private const string OutageIcon = $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 24 24\" fill=\"none\"><path d=\"M15.6976 17C15.915 16.7003 17.4283 16.0445 18.3029 14.9274C19.3662 13.5692 20 11.8586 20 10C20 5.58172 16.4183 2 12 2C7.58172 2 4 5.58172 4 10C4 11.8586 4.6338 13.5691 5.69706 14.9273C6.57163 16.0445 8.08503 16.7003 8.3024 17H15.6976Z\" fill=\"{OutageIconColor}\" fill-opacity=\"0.16\"/><path d=\"M9 18L15 18\" stroke=\"{OutageIconColor}\" stroke-width=\"2\" stroke-linecap=\"round\"/><mask id=\"path-3-inside-1_512_69\" fill=\"white\"><path fill-rule=\"evenodd\" clip-rule=\"evenodd\" d=\"M16.5 18.5892C16.5 17.8684 16.8991 17.2173 17.4756 16.7845C19.6185 15.176 21 12.6457 21 9.8C21 4.93989 16.9706 1 12 1C7.02944 1 3 4.93989 3 9.8C3 12.6457 4.38146 15.176 6.52442 16.7845C7.10094 17.2173 7.5 17.8684 7.5 18.5892V20C7.5 21.6569 8.84315 23 10.5 23H13.5C15.1569 23 16.5 21.6569 16.5 20V18.5892Z\"/></mask><path d=\"M17.4756 16.7845L18.6762 18.384L17.4756 16.7845ZM19 9.8C19 11.9794 17.9451 13.9313 16.2749 15.185L18.6762 18.384C21.292 16.4206 23 13.3121 23 9.8H19ZM12 3C15.9086 3 19 6.0866 19 9.8H23C23 3.79319 18.0325 -1 12 -1V3ZM5 9.8C5 6.0866 8.0914 3 12 3V-1C5.96748 -1 1 3.79319 1 9.8H5ZM7.72507 15.185C6.0549 13.9313 5 11.9794 5 9.8H1C1 13.3121 2.70802 16.4206 5.32378 18.384L7.72507 15.185ZM9.5 20V18.5892H5.5V20H9.5ZM10.5 21C9.94771 21 9.5 20.5523 9.5 20H5.5C5.5 22.7614 7.73858 25 10.5 25V21ZM13.5 21H10.5V25H13.5V21ZM14.5 20C14.5 20.5523 14.0523 21 13.5 21V25C16.2614 25 18.5 22.7614 18.5 20H14.5ZM14.5 18.5892V20H18.5V18.5892H14.5ZM5.32378 18.384C5.40813 18.4474 5.45867 18.5119 5.4832 18.5557C5.50541 18.5954 5.5 18.6049 5.5 18.5892H9.5C9.5 17.1061 8.68792 15.9078 7.72507 15.185L5.32378 18.384ZM16.2749 15.185C15.3121 15.9078 14.5 17.1061 14.5 18.5892H18.5C18.5 18.6049 18.4946 18.5954 18.5168 18.5557C18.5413 18.5119 18.5919 18.4474 18.6762 18.384L16.2749 15.185Z\" fill=\"{OutageIconColor}\" mask=\"url(#path-3-inside-1_512_69)\"/><path d=\"M8 10C8 7.79086 9.79086 6 12 6\" stroke=\"{OutageIconColor}\" stroke-width=\"2\" stroke-linecap=\"round\"/><line x1=\"2\" y1=\"2\" x2=\"22\" y2=\"22\" stroke=\"{OutageIconColor}\" stroke-width=\"2\" stroke-linecap=\"round\"/></svg>";
    public async Task<string> GenerateScheduleSvg(DateTimeOffset date, string groupId)
    {
        var schedule = await scheduleRepository.GetByDateAsync(date);
        if (schedule == null) return string.Empty;
         
        var group = schedule.Groups.FirstOrDefault(g => g.Id == groupId);
        return group == null 
            ? string.Empty 
            : GenerateScheduleSvgContent(schedule.Date, group);
    }
    
    private string GenerateScheduleSvgContent(DateTimeOffset date, GroupDoc group)
    {
        int topOffset = 100;
        // Parse the input date
        string dayOfWeek = date.ToString("ddd", new CultureInfo("uk-ua"));
        int day = date.Day;

        // SVG header and styling
        string svg = $@"<svg width=""320"" height=""1320"" xmlns=""http://www.w3.org/2000/svg"" style=""font-family: sans-serif"">
        <rect width=""100%"" height=""100%"" fill=""white""/>
        <text x=""200"" y=""30"" fill=""gray"" text-anchor=""middle"" alignment-baseline=""middle"" style=""font-size:34px;font-weight:bold; transform: rotate(270deg) translate(5px, -135px);
            transform-origin: 200px 50px;"">{group.Id}</text>
        <text x=""200"" y=""30"" fill=""gray"" text-anchor=""middle"" alignment-baseline=""middle"" style=""font-size:20px;font-weight:bold"">{dayOfWeek}</text>
        <text x=""200"" y=""60"" fill=""black"" text-anchor=""middle"" alignment-baseline=""middle"" style=""font-size:30px;font-weight:bold;"">{day}</text>
        ";

        // Time labels
        for (int hour = 0; hour <= 24; hour++)
        {
            string timeLabel = $"{hour:00}:00";
            int y = topOffset + hour * 50;
            svg += $"<text x=\"20\" y=\"{y}\" style=\"font-size:14px;\" fill=\"gray\">{timeLabel}</text>\n";
        }

        // Schedule blocks
        foreach (var interval in group.Intervals)
        {
            string state = interval.State == GridState.PowerOff ? "відключення" : "заживлено";
            string color = interval.State == GridState.PowerOn ? "#2ec4b6" : "#ff9f1c";
            
            // Calculate start and end positions
            int startDayOffset = (interval.StartTime.Date - date.Date).Days * 24;
            int endDayOffset = (interval.EndTime.Date - date.Date).Days * 24;
            double startHour = startDayOffset + interval.StartTime.Hour + interval.StartTime.Minute / 60.0;
            double endHour = endDayOffset + interval.EndTime.Hour + interval.EndTime.Minute / 60.0;
            double yStart = topOffset + startHour * 50;
            double yEnd = topOffset + endHour * 50;
            double height = yEnd - yStart;

            var eventTimeFrom = interval.StartTime.ToString("HH:mm");
            var eventTimeTo = interval.EndTime.ToString("HH:mm");
            svg += $"<rect rx=\"5\" ry=\"5\" x=\"100\" y=\"{yStart}\" width=\"200\" height=\"{height}\" fill=\"{color}\"/>\n";
            svg += interval.State == GridState.PowerOff
                ? $"<svg x=\"110\" y=\"{yStart + height / 2 - 10}\" height=\"300\" width=\"200\">{OutageIcon}</svg>"
                : $"<svg x=\"110\" y=\"{yStart + height / 2 - 10}\" height=\"300\" width=\"200\">{ConnectedIcon}</svg>";
            svg += $"<text x=\"200\" y=\"{yStart + height / 2 - 10}\" fill=\"white\" text-anchor=\"middle\" alignment-baseline=\"middle\" style=\"font-size:12px;font-weight:bold\">{state}</text>\n";
            svg += $"<text x=\"200\" y=\"{yStart + height / 2 + 10}\" fill=\"white\" text-anchor=\"middle\" alignment-baseline=\"middle\" style=\"font-size:16px;font-weight:bold\">{eventTimeFrom} – {eventTimeTo}</text>\n";
        }

        // Add current time line if today
        var now = timeProvider.KyivNow;
        if (date.Date == now.Date)
        {
            var currentHour = now.Hour + now.Minute / 60.0;
            var yNow = (topOffset + currentHour * 50).ToString(CultureInfo.InvariantCulture);
            svg += $"<circle cx=\"5\" cy=\"{yNow}\" r=\"5\" fill=\"#F06C9B\"/>\n";
            svg += $"<line x1=\"0\" y1=\"{yNow}\" x2=\"300\" y2=\"{yNow}\" stroke=\"#F06C9B\" stroke-width=\"2\"/>\n";
        }

        // Close SVG
        svg += "</svg>";

        return svg.ToString(CultureInfo.InvariantCulture);
    }
}