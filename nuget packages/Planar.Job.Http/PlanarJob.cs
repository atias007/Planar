using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Job.Http;
using System.Text;

namespace Planar.Job
{
    public static partial class PlanarJob
    {
#if NETSTANDARD2_0
        internal static HttpJobStartProperties Properties { get; set; }
#else
        internal static HttpJobStartProperties Properties { get; set; } = null!;
#endif

        public async static Task StartAsync(HttpJobStartProperties properties)
        {
            if (properties == null) { throw new ArgumentNullException(nameof(properties)); }

            try
            {
                FillProperties();
                if (await Debug(properties, _mainCancellationTokenSource.Token)) { return; }

                await SafeStartAsync(properties);
            }
            catch (Exception ex)
            {
                await ConsoleLogger.Log(LogLevel.Critical, "----------------");
                await ConsoleLogger.Log(LogLevel.Critical, " Fail to start");
                await ConsoleLogger.Log(LogLevel.Critical, "----------------");
                await ConsoleLogger.Log(LogLevel.Critical, ex.ToString());
            }
        }

        private async static Task SafeStartAsync(HttpJobStartProperties properties)
        {
            Properties = properties;
            properties.WebApplication ??= WebApplication.CreateBuilder().Build();

            properties.WebApplication.MapPost("/planar/invoke/{route}",
                   (HttpContext httpContext, string route) => RouteMessageAsync(httpContext, route));

            await properties.WebApplication.RunAsync();
        }

        private static async Task ProcessInvokeMessageAsync(HttpContext httpContext, string route)
        {
            await ConsoleLogger.Log(LogLevel.Debug, ">> Received invoke message <<");
            string fid = string.Empty;
#if NETSTANDARD2_0
            JobDefinition jobDefinition = null;
#else
            JobDefinition? jobDefinition = null;
#endif
            try
            {
                jobDefinition = Properties.GetJobDefinition(route);
                fid = GetFireInstanceIdHeader(httpContext);
                var info = TrackInstance(fid);
                await Execute(httpContext, jobDefinition.JobType, info.CancellationToken);
            }
            catch (Exception ex)
            {
                var log = new LogEntity { Level = LogLevel.Error, Message = $"Fail to execute {jobDefinition?.JobType.Name}".TrimEnd() };
                await Console.Error.WriteLineAsync(log.ToString());
                log.Message = ex.ToString();
                await Console.Error.WriteLineAsync(log.ToString());
            }
            finally
            {
                await UntrackInstance(fid);
            }
        }

        private static async Task ProcessCancelAsync(HttpContext httpContext)
        {
            string fid = string.Empty;
#if NETSTANDARD2_0
            JobInstanceInfo jobInstanceInfo = null;
#else
            JobInstanceInfo? jobInstanceInfo = null;
#endif

            try
            {
                fid = GetFireInstanceIdHeader(httpContext);
                if (!_jobInstances.TryGetValue(fid, out jobInstanceInfo)) { return; }

                jobInstanceInfo.Cancel();
                var log2 = new LogEntity { Level = LogLevel.Information, Message = $"Job with FireInstanceId {fid} has been cancelled" };
                await Console.Error.WriteLineAsync(log2.ToString());
            }
            catch (Exception ex)
            {
                var log = new LogEntity { Level = LogLevel.Error, Message = $"Fail to cancel job FireInstanceId {fid}".TrimEnd() };
                await Console.Error.WriteLineAsync(log.ToString());
                log.Message = ex.ToString();
                await Console.Error.WriteLineAsync(log.ToString());
            }
        }

        private static async Task RouteMessageAsync(HttpContext httpContext, string route)
        {
            var command = SafeGetHeader(httpContext, "Command");
            switch (command)
            {
                case "Invoke":
                    await ProcessInvokeMessageAsync(httpContext, route);
                    break;

                case "Cancel":
                    await ProcessCancelAsync(httpContext);
                    break;

                case "Ping":
                    break;

                default:
                    var message = string.IsNullOrWhiteSpace(command) ?
                        "Missing or empty Command header." :
                        $"Command header '{command}' is not supported.";

                    var log = new LogEntity { Level = LogLevel.Error, Message = message };
                    await Console.Error.WriteLineAsync(log.ToString());
                    break;
            }
        }

        private static string SafeGetHeader(HttpContext httpContext, string name)
        {
            try
            {
                return GetHeader(httpContext, name);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetFireInstanceIdHeader(HttpContext httpContext)
        {
            return GetHeader(httpContext, "FireInstanceId");
        }

        private static string GetHeader(HttpContext httpContext, string name)
        {
            httpContext.Request.Headers.TryGetValue(name, out var headerValueObj);
            var result = headerValueObj.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new PlanarJobException($"{name} header is missing or empty in the http request headers");
            }

            return result;
        }

        private static async Task Execute(HttpContext httpContext, Type jobType, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
            var json = await reader.ReadToEndAsync(cancellationToken);
            if (jobType == null) { return; }
            await Execute(jobType, Properties?.PlanarHostname, json, cancellationToken);
        }

        private static void FillProperties()
        {
            FillArguments();
            if (HasArgument("--debug"))
            {
                Mode = RunningMode.Debug;
            }
            else
            {
                Mode = RunningMode.Release;
            }

            Environment = GetArgument("--environment")?.Value ?? "Development";
        }
    }
}