using Microsoft.Extensions.DependencyInjection;
using Planar.Service.Calendars;
using Quartz;
using Quartz.Impl.Calendar;
using System;
using System.Threading.Tasks;

namespace Planar.Startup;

public static class CalendarsInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var scheduler = await serviceProvider.GetRequiredService<ISchedulerFactory>().GetScheduler();
        var calendars = scheduler.GetCalendarNames().Result;
        var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BaseCalendar>>();
        foreach (var name in calendars)
        {
            var cal = scheduler.GetCalendar(name).Result;
            if (cal is ICalendarWithLogger logCal)
            {
                logCal.Logger = logger;
            }
        }
    }
}