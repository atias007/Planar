using DbUp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.General;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Startup;

public interface IDatabaseMigrationInitializerLogger
{
}

public static class DatabaseMigrationInitializer
{
    private static readonly IExecuter _executer = DbFactory.CreateDbMigrationExecuter(AppSettings.Database.ProviderName);

    public static async Task FixPlanarJobPropertiesForV182(IServiceProvider provider)
    {
        var dal = provider.GetRequiredService<IJobData>();

        var items = await dal.GetAllPropertiesForFixVersion182();
        var logger = provider.GetRequiredService<ILogger<IDatabaseMigrationInitializerLogger>>();
        var errors = new List<Exception>();
        try
        {
            foreach (var item in items)
            {
                try
                {
                    if (item.Properties == null) { continue; }
                    var processProperties = YmlUtil.Deserialize<PlanarJobProcessProperties>(item.Properties);
                    if (string.IsNullOrWhiteSpace(processProperties.Filename)) { continue; }

                    var newProperties = new PlanarJobProperties
                    {
                        InvokeMethod = "process",
                        Process = processProperties,
                    };

                    item.Properties = YmlUtil.Serialize(newProperties);
                    await dal.UpdateJobProperty(item);
                    Console.WriteLine($">> Upgrade job {item.JobId} properties to V1.8.2");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "fail to fix job properties (job id: {Id}) for version 1.8.2", item.JobId);
                    errors.Add(ex);
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException(errors);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    public static async Task FixJobProperties(IServiceProvider provider)
    {
        try
        {
            var dal = provider.GetRequiredService<IJobData>();
            var ids = await dal.GetUnknownJobProperties();
            if (!ids.Any()) { return; }

            var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
            var helper = provider.GetRequiredService<JobKeyHelper>();
            foreach (var id in ids)
            {
                try
                {
                    var key = await helper.GetJobKey(id);
                    var job = await scheduler.GetJobDetail(key);
                    if (job == null) { continue; }
                    var jobType = SchedulerUtil.GetJobTypeName(job);
                    await dal.UpdatePropertiesJobType(id, jobType);
                }
                catch
                {
                    // *** DO NOTHING ***
                }
            }
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    public static void RunMigration()
    {
        if (!AppSettings.Database.RunMigration)
        {
            Console.WriteLine("[x] Skip database migration");
            var count = CountScriptsToExecute();
            if (count > 0)
            {
                throw new PlanarException($"there are {count} script in database migration to run. enable database migrations in settings file or run database migrations manually");
            }

            return;
        }

        try
        {
            Console.WriteLine("[x] Run database migration");

            _executer.EnsureDatabaseExists(AppSettings.Database.ConnectionString);

            var count = CountScriptsToExecute();
            Console.WriteLine($"    - Found {count} scripts to run");

            if (count == 0)
            {
                return;
            }

            Console.WriteLine(string.Empty.PadLeft(80, '-'));
            var result = _executer.Execute(AppSettings.Database.ConnectionString);

            if (result.Successful)
            {
                Console.WriteLine(string.Empty.PadLeft(80, '-'));
                Console.WriteLine("Migration success");
                Console.WriteLine(string.Empty.PadLeft(80, '-'));
            }
            else
            {
                HandleError(result.Error);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    private static int CountScriptsToExecute()
    {
        var list = _executer.GetScripts(AppSettings.Database.ConnectionString);
        return list.Count();
    }

    private static void HandleError(Exception ex)
    {
        Console.WriteLine(string.Empty.PadLeft(80, '-'));
        Console.WriteLine("Migration fail");
        Console.WriteLine(string.Empty.PadLeft(80, '-'));
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(ex);
        Console.ResetColor();

        Console.ReadLine();
        Environment.Exit(-1);
    }
}