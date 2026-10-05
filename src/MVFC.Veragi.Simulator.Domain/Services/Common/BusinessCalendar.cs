using System.Globalization;
using MVFC.Veragi.Simulator.Shareable.Configuration;

namespace MVFC.Veragi.Simulator.Domain.Services.Common;

public sealed class BusinessCalendar(TimeProvider clock, SimulatorOptions options)
{
    private readonly TimeProvider _clock = clock;
    private readonly SimulatorOptions _options = options;

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone)).DateTime);

    public DateOnly AddBusinessDays(DateOnly date, int days)
    {
        for (var count = 0; count < days;)
        {
            date = date.AddDays(1);

            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday && !_options.Holidays.Contains(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
                count++;
        }

        return date;
    }
}
