using Microsoft.Extensions.DependencyInjection;
using Planar.Common;
using Planar.Service.Calendars;
using Planar.Service.Listeners;
using Quartz;
using Quartz.Simpl;
using System;

namespace Planar.Service;

public static class QuartzServiceConfiguration
{
    public static IServiceCollection AddQuartzService(this IServiceCollection services)
    {
        try
        {
            return AddQuartzServiceInner(services);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Initialize: Fail to InitializeScheduler");
            Console.WriteLine(string.Empty.PadLeft(80, '-'));
            Console.WriteLine(ex.ToString());
            throw;
        }
    }

    private static IServiceCollection AddQuartzServiceInner(IServiceCollection services)
    {
        JsonObjectSerializer.AddCalendarSerializer<DefaultCalendar>(new DefaultCalendarSerializer());
        JsonObjectSerializer.AddCalendarSerializer<IsraelCalendar>(new IsraelCalendarSerializer());
        JsonObjectSerializer.AddCalendarSerializer<GlobalCalendar>(new GlobalCalendarSerializer());

        services.AddQuartz(q =>
        {
            q.SchedulerName = AppSettings.General.ServiceName;
            q.SchedulerId = AppSettings.General.InstanceId;
            q.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 10; });

            // this also injects scoped services (like EF DbContext)
            // MicrosoftDependencyInjectionJobFactory is the default for DI configuration, this method will be removed later on
            //// DEPRECATED: q.UseMicrosoftDependencyInjectionJobFactory();

            // convert time zones using converter that can handle Windows/Linux differences
            q.UseTimeZoneConverter();
            q.AddJobListener<LogJobListener>();
            q.AddJobListener<CircuitBreakerJobListener>();
            q.AddTriggerListener<RetryTriggerListener>();
            q.AddSchedulerListener<SchedulerListener>();

            q.AddCalendar(IsraelCalendar.Name, new IsraelCalendar(), replace: true, updateTriggers: false);
            q.AddCalendar(DefaultCalendar.Name, new DefaultCalendar(), replace: true, updateTriggers: false);
            foreach (var item in CalendarInfo.Items)
            {
                if (string.Equals(item.Key, IsraelCalendar.Name, StringComparison.OrdinalIgnoreCase)) { continue; }
                if (string.Equals(item.Key, DefaultCalendar.Name, StringComparison.OrdinalIgnoreCase)) { continue; }
                var cal = new GlobalCalendar { Name = item.Key, Key = item.Value };
                q.AddCalendar(item.Key, cal, replace: true, updateTriggers: false);
            }

            q.UsePersistentStore(x =>
            {
                x.PerformSchemaValidation = true; // default

                x.RetryInterval = TimeSpan.FromSeconds(2);

                // force job data map values to be considered as strings
                // prevents nasty surprises if object is accidentally serialized and then
                // serialization format breaks, defaults to false
                x.UseProperties = true;

                if (AppSettings.Cluster.Clustering)
                {
                    x.UseClustering(x =>
                    {
                        x.CheckinInterval = AppSettings.Cluster.CheckinInterval;
                        x.CheckinMisfireThreshold = AppSettings.Cluster.CheckinMisfireThreshold;
                    });
                }

                DbFactory.QuartzUsePersistentStore(x);

                // this requires Quartz.Serialization.Json NuGet package
                x.UseNewtonsoftJsonSerializer();
            });
        });

        services.AddQuartzHostedService(options =>
        {
            // when shutting down we want jobs to complete gracefully
            options.WaitForJobsToComplete = true;
            options.AwaitApplicationStarted = true;
            var delaySeconds = AppSettings.General.SchedulerStartupDelay;
            options.StartDelay = delaySeconds;
        });

        return services;
    }
}