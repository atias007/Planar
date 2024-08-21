using Microsoft.Extensions.DependencyInjection;
using Planar.Common;
using Planar.Service.Calendars;
using Planar.Service.Listeners;
using Quartz;
using Quartz.Simpl;
using System;

namespace Planar.Service
{
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
                q.AddTriggerListener<RetryTriggerListener>();
                q.AddSchedulerListener<SchedulerListener>();

                q.AddCalendar<IsraelCalendar>(IsraelCalendar.Name, replace: true, updateTriggers: true, a => { });
                q.AddCalendar<DefaultCalendar>(DefaultCalendar.Name, replace: true, updateTriggers: true, a => { });
                foreach (var item in CalendarInfo.Items)
                {
                    if (string.Equals(item.Key, IsraelCalendar.Name, StringComparison.OrdinalIgnoreCase)) { continue; }
                    if (string.Equals(item.Key, DefaultCalendar.Name, StringComparison.OrdinalIgnoreCase)) { continue; }
                    q.AddCalendar<GlobalCalendar>(item.Key, replace: true, updateTriggers: true, calendar =>
                    {
                        calendar.Name = item.Key;
                        calendar.Key = item.Value;
                    });
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

                    switch (AppSettings.Database.Provider)
                    {
                        case "SqlServer":
                            x.UseSqlServer(AppSettings.Database.ConnectionString ?? string.Empty);
                            break;

                        default:
                            throw new NotImplementedException($"Database provider {AppSettings.Database.Provider} is not supported");
                    }

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
}