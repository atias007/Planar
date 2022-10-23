using Microsoft.Extensions.DependencyInjection;
using Planar.Calendar.Hebrew;
using Planar.Service.List;
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
                ////    // this is the default
                ////    options.DefaultMaxRunTime = TimeSpan.FromMinutes(5);
                ////});

                q.AddJobListener<LogJobListener>();
                q.AddTriggerListener<RetryTriggerListener>();
                q.AddCalendar<HebrewCalendar>(nameof(HebrewCalendar), true, true, c => { });

                q.UsePersistentStore(x =>
                {
                    x.PerformSchemaValidation = true; // default

                    x.RetryInterval = TimeSpan.FromSeconds(10);

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
                            x.UseSqlServer(AppSettings.DatabaseConnectionString);
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

#if DEBUG
                var delaySeconds = 1;
#else
                var delaySeconds = 30;
#endif

                options.StartDelay = TimeSpan.FromSeconds(delaySeconds);
            });

            return services;
        }
    }
}