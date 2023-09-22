using Microsoft.Extensions.DependencyInjection;
using Planar.Calendar;
using Planar.Calendar.Hebrew;
using Planar.Common;
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

        private static string GetCalendarName<T>()
            where T : PlanarBaseCalendar
        {
            const string replace = "Calendar";
            var name = typeof(T).Name;
            if (name.EndsWith(replace))
            {
                name = name[0..^replace.Length];
            }

            return name;
        }

        private static IServiceCollection AddQuartzServiceInner(IServiceCollection services)
        {
            JsonObjectSerializer.AddCalendarSerializer<HebrewCalendar>(new CustomCalendarSerializer());

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
                q.UseJobAutoInterrupt(options =>
                {
                    options.DefaultMaxRunTime = AppSettings.General.JobAutoStopSpan;
                });

                q.AddJobListener<LogJobListener>();
                q.AddTriggerListener<RetryTriggerListener>();
                q.AddSchedulerListener<SchedulerListener>();

                var calendarName = GetCalendarName<HebrewCalendar>();
                q.AddCalendar<HebrewCalendar>(calendarName, true, true, a => { });

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