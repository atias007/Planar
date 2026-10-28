using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Job.RabbitMq;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Job
{
    internal interface IPlanarJob
    { }

    public static partial class PlanarJob
    {
#if NETSTANDARD2_0
        internal static RabbitMqJobStartProperties Properties { get; set; }
        private static RabbitMqFactory _rabbitMqFactory;
        private static ILogger _logger;

#else
        internal static RabbitMqJobStartProperties Properties { get; set; } = null!;
        private static RabbitMqFactory? _rabbitMqFactory;
        private static ILogger? _logger;

#endif
        public static PlanarJobDebugger Debugger => PlanarJobCommon.Debugger;

        public async static Task StartAsync(RabbitMqJobStartProperties properties)
        {
            if (properties == null) { throw new ArgumentNullException(nameof(properties)); }
            PlanarJobCommon.Properties = properties;

            Console.OutputEncoding = Encoding.UTF8;
            await PlanarHostedJobCommon.PrintLogo(typeof(PlanarJob).Assembly, nameof(RabbitMq));

            _logger = GetLogger(properties);
            _ = PlanarJobCommon.StartHealthCheck(properties, _logger);

            try
            {
                PlanarHostedJobCommon.SetupGracefulShutdown(_logger);
                FillProperties();
                if (await PlanarJobCommon.Debug(properties)) { return; }

                await SafeStartAsync(properties);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fail to start rabbitmq planar hosted job");
            }
            finally
            {
                PlanarHostedJobCommon.SafeDisposeCancellationTokenSource();
                PlanarJobCommon.StopHealthCheck();
            }
        }

        private static ILogger GetLogger(RabbitMqJobStartProperties properties)
        {
            var logger = properties.Host?.Services.GetService<ILogger<IPlanarJob>>();
            if (logger == null)
            {
                return new CustomConsoleLogger(nameof(PlanarJob));
            }

            return logger;
        }

        private async static Task SafeStartAsync(RabbitMqJobStartProperties properties)
        {
            if (_logger == null) { throw new InvalidDataException("_logger is null"); }
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));

            // create rabbitmq factory instance and ensure connection
            _rabbitMqFactory = await RabbitMqFactory.GetInstance(_logger, properties, PlanarJobCommon.MainCancellationTokenSource.Token);

            // ensure queues, exchanges and bindings are created
            await _rabbitMqFactory.EnsureDefinitions();

            // apply manifest to ensure job definitions are up to date
            await ManifestUtil.ApplyManifest(properties);

            // Start consuming messages
            await Task.WhenAll(
                _rabbitMqFactory.StartConsumeAsync(messageHandler: RouteMessageAsync),
                properties.Host.RunAsync(PlanarJobCommon.MainCancellationTokenSource.Token));
        }

        /// <summary>
        /// Message handler - processes each received message
        /// </summary>
        /// <param name="messageBody">The message body as a string</param>
        /// <param name="eventArgs">Event arguments containing message metadata</param>
        /// <returns>True if message processed successfully, False to requeue</returns>
        private static async Task ProcessInvokeMessageAsync(BasicDeliverEventArgs eventArgs)
        {
            _logger?.LogDebug("Received invoke message");
            string fid = string.Empty;

#if NETSTANDARD2_0
            JobDefinition jobDefinition = null;
            JobInstanceInfo info = null;
#else
            JobDefinition? jobDefinition = null;
            JobInstanceInfo? info = null;
#endif
            try
            {
                jobDefinition = Properties.GetJobDefinition(eventArgs.RoutingKey);
                fid = GetFireInstanceIdHeader(eventArgs);
                info = PlanarHostedJobCommon.TrackInstance(fid);
                await Execute(eventArgs, jobDefinition.JobType, info.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fail to execute {Name}", jobDefinition?.JobType.Name);
            }
            finally
            {
                if (info != null) { await PlanarHostedJobCommon.UntrackInstance(fid); }
            }
        }

        private static async Task ProcessCancelAsync(BasicDeliverEventArgs eventArgs)
        {
            string fid = string.Empty;
#if NETSTANDARD2_0
            JobInstanceInfo jobInstanceInfo = null;
#else
            JobInstanceInfo? jobInstanceInfo = null;
#endif

            try
            {
                fid = GetFireInstanceIdHeader(eventArgs);
                if (!PlanarHostedJobCommon.TryGetInstanceInfo(fid, out jobInstanceInfo)) { return; }

                jobInstanceInfo.Cancel();
                _logger?.LogInformation("Job with FireInstanceId {FireInstanceId} has been cancelled", fid);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fail to cancel job FireInstanceId {FireInstanceId}", fid);
            }
        }

        private static async Task RouteMessageAsync(BasicDeliverEventArgs eventArgs)
        {
            var command = SafeGetHeader(eventArgs, "Command");
            switch (command)
            {
                case "Invoke":
                    await ProcessInvokeMessageAsync(eventArgs);
                    break;

                case "Cancel":
                    await ProcessCancelAsync(eventArgs);
                    break;

                case "Ping":
                    _logger?.LogDebug("Received Ping");
                    break;

                default:
                    if (string.IsNullOrWhiteSpace(command))
                    {
                        _logger?.LogError("Missing or empty Command header.");
                    }
                    else
                    {
                        _logger?.LogError("Command header '{Command}' is not supported.", command);
                    }

                    break;
            }
        }

        private static string SafeGetHeader(BasicDeliverEventArgs eventArgs, string name)
        {
            try
            {
                return GetHeader(eventArgs, name);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetFireInstanceIdHeader(BasicDeliverEventArgs eventArgs)
        {
            return GetHeader(eventArgs, "FireInstanceId");
        }

        private static bool IsEncrypted(BasicDeliverEventArgs eventArgs)
        {
            return string.Equals(GetHeader(eventArgs, "Encrypted", mandatory: false), bool.TrueString, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetHeader(BasicDeliverEventArgs eventArgs, string name, bool mandatory = true)
        {
#if NETSTANDARD2_0
            object headerValueObj = null;
#else
            object? headerValueObj = null;
#endif
            eventArgs.BasicProperties.Headers?.TryGetValue(name, out headerValueObj);
            if (headerValueObj is byte[] bytes)
            {
                return Encoding.UTF8.GetString(bytes);
            }

            var result = Convert.ToString(headerValueObj);
            if (string.IsNullOrWhiteSpace(result) && mandatory)
            {
                throw new PlanarJobException($"{name} header is missing or empty in the message headers");
            }

            return result;
        }

        private static async Task Execute(BasicDeliverEventArgs eventArgs, Type jobType, CancellationToken cancellationToken)
        {
            if (jobType == null) { return; }
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            var encrypt = IsEncrypted(eventArgs);
            if (encrypt && Properties.EncryptionKeyBytes.Length == 0)
            {
                throw new PlanarJobBadRequestException("Encryption key is missing. Set the encryption key at startup properties.");
            }

            if (encrypt)
            {
                _logger?.LogDebug("Decrypting message body for job {JobType}", jobType.Name);
                json = AesGcmStringCipher.Decrypt(json, Properties.EncryptionKeyBytes);
            }

            await PlanarJobCommon.Execute(jobType, Properties, json, cancellationToken);
        }

        private static void FillProperties()
        {
            PlanarJobCommon.InitArguments();
            if (PlanarJobCommon.HasArgument("--debug"))
            {
                PlanarJobCommon.Mode = RunningMode.Debug;
            }
            else
            {
                PlanarJobCommon.Mode = RunningMode.Release;
            }

            PlanarJobCommon.Environment = PlanarJobCommon.GetArgument("--environment")?.Value ?? "Development";
        }
    }
}