using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Job.Http;
using System.Text;

namespace Planar.Job
{
    internal interface IPlanarJob { }

    public static partial class PlanarJob
    {
#if NETSTANDARD2_0
        internal static HttpJobStartProperties Properties { get; set; }
#else
        internal static HttpJobStartProperties Properties { get; set; } = null!;
#endif

        private static ILogger? _logger;

        public async static Task StartAsync(HttpJobStartProperties properties)
        {
            ArgumentNullException.ThrowIfNull(properties);
            InitWebApplication(properties);
            _logger = GetLogger(properties);

            try
            {
                FillProperties();
                if (await Debug(properties, _mainCancellationTokenSource.Token)) { return; }

                await SafeStartAsync(properties);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fail to start http planar hosted job");
            }
            finally
            {
                _mainCancellationTokenSource?.Dispose();
            }
        }

        private static void InitWebApplication(HttpJobStartProperties properties)
        {
            if (properties.Host != null) { return; }
            var builder = WebApplication.CreateBuilder();
            properties.Host = builder.Build();
        }

        private static ILogger GetLogger(HttpJobStartProperties properties)
        {
            ArgumentNullException.ThrowIfNull(properties.Host);
            var logger = properties.Host.Services.GetService<ILogger<IPlanarJob>>();
            if (logger == null)
            {
                return new CustomConsoleLogger(nameof(PlanarJob));
            }

            return logger;
        }

        private async static Task SafeStartAsync(HttpJobStartProperties properties)
        {
            Properties = properties;
            properties.Host ??= WebApplication.CreateBuilder().Build();

            var webApp = (WebApplication)properties.Host;
            webApp.MapPost("/planar/invoke/{route}",
                   (HttpContext httpContext, string route) => SafeRouteMessageAsync(httpContext, route));

            webApp.MapGet("/planar/health-check", () => Results.Text("Healthy"));
            ////webApp.MapGet("/planar/info", () => Results.Ok(properties));

            await webApp.RunAsync();
        }

        private static async Task<IResult> ProcessInvokeMessageAsync(HttpContext httpContext, string route)
        {
            _logger?.LogDebug("Received invoke message");
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
                await Execute(httpContext, jobDefinition.JobType, info);
                return Results.Accepted();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fail to execute {Name}", jobDefinition?.JobType.Name);

                if (ex is PlanarJobConflictException) { return Results.Conflict(ex.Message); }
                if (ex is PlanarJobNotFoundException) { return Results.NotFound(ex.Message); }
                if (ex is PlanarJobBadRequestException) { return Results.BadRequest(ex.Message); }

                return Results.Json(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<IResult> ProcessCancelAsync(HttpContext httpContext)
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
                if (!_jobInstances.TryGetValue(fid, out jobInstanceInfo)) { return Results.NotFound(); }

                jobInstanceInfo.Cancel();
                if(_logger?.IsEnabled(LogLevel.Information) == true)
                {
                    _logger?.LogInformation("Job with FireInstanceId {FireInstanceId} has been cancelled", fid);
                }

                return Results.Accepted();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fail to cancel job FireInstanceId {FireInstanceId}", fid);
                return Results.Json(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<IResult> SafeRouteMessageAsync(HttpContext httpContext, string route)
        {
            try
            {
                return await RouteMessageAsync(httpContext, route);
            }
            catch (Exception ex)
            {
                return Results.Json(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<IResult> RouteMessageAsync(HttpContext httpContext, string route)
        {
            var command = SafeGetHeader(httpContext, "Command");
            switch (command)
            {
                case "Invoke":
                    return await ProcessInvokeMessageAsync(httpContext, route);

                case "Cancel":
                    return await ProcessCancelAsync(httpContext);

                case "Ping":
                    _logger?.LogDebug("Received Ping");
                    return Results.Ok("Pong");

                default:
                    if (string.IsNullOrWhiteSpace(command))
                    {
                        _logger?.LogError("Missing or empty Command header.");
                        return Results.BadRequest("Missing or empty Command header.");
                    }
                    else
                    {
                        _logger?.LogError("Command header '{Command}' is not supported.", command);
                        return Results.BadRequest($"Command header '{command}' is not supported.");
                    }
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
                throw new PlanarJobBadRequestException($"{name} header is missing or empty in the http request headers");
            }

            return result;
        }

        private static async Task Execute(HttpContext httpContext, Type jobType, JobInstanceInfo jobInstanceInfo)
        {
            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
            var json = await reader.ReadToEndAsync(jobInstanceInfo.CancellationToken);
            if (jobType == null) { return; }

            _ = Execute(jobType, Properties, json, jobInstanceInfo.CancellationToken)
                .ContinueWith(async t =>
                {
                    await UntrackInstance(jobInstanceInfo.FireInstanceId);
                });
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