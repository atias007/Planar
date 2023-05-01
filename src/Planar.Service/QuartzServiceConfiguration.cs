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
                q.SchedulerName = AppSettings.ServiceName;
                q.SchedulerId = AppSettings.InstanceId;
                q.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 10; });

                // this also injects scoped services (like EF DbContext)
                q.UseMicrosoftDependencyInjectionJobFactory();

                // convert time zones using converter that can handle Windows/Linux differences
                q.UseTimeZoneConverter();
                ////q.UseJobAutoInterrupt(options =>
                ////{
                ////    options.DefaultMaxRunTime = TimeSpan.FromSeconds(30);
                ////});

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

                    if (AppSettings.Clustering)
                    {
                        x.UseClustering(x =>
                        {
                            x.CheckinInterval = AppSettings.ClusteringCheckinInterval;
                            x.CheckinMisfireThreshold = AppSettings.ClusteringCheckinMisfireThreshold;
                        });
                    }

                    switch (AppSettings.DatabaseProvider)
                    {
                        case "SqlServer":
                            x.UseSqlServer(AppSettings.DatabaseConnectionString ?? string.Empty);
                            break;

                        default:
                            throw new NotImplementedException($"Database provider {AppSettings.DatabaseProvider} is not supported");
                    }

                    // this requires Quartz.Serialization.Json NuGet package
                    x.UseJsonSerializer();
                });
            });

            services.AddQuartzHostedService(options =>
            {
                // when shutting down we want jobs to complete gracefully
                options.WaitForJobsToComplete = true;
                options.AwaitApplicationStarted = true;
                var delaySeconds = AppSettings.SchedulerStartupDelay;
                options.StartDelay = delaySeconds;
            });

            return services;
        }
    }
}