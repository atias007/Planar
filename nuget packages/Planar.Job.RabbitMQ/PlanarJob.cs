using Microsoft.Extensions.Logging;
using Planar.Job.RabbitMq;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Job
{
    public static partial class PlanarJob
    {
#if NETSTANDARD2_0
        internal static RabbitMqJobStartProperties Properties { get; set; }
        private static RabbitMqFactory _rabbitMqFactory;
#else
        internal static RabbitMqJobStartProperties Properties { get; set; } = null!;
        private static RabbitMqFactory? _rabbitMqFactory;
#endif

        private static readonly ConcurrentDictionary<string, JobInstanceInfo> _jobInstances = new ConcurrentDictionary<string, JobInstanceInfo>();
        private static readonly CancellationTokenSource _mainCancellationTokenSource = new CancellationTokenSource();

        public async static Task StartAsync(RabbitMqJobStartProperties properties)
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

        static partial void GracefullShutdownSetup()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, a) => _mainCancellationTokenSource.Cancel();
            _mainCancellationTokenSource.Token.Register(async () =>
            {
                await ConsoleLogger.Log(LogLevel.Information, "Start gracefull shutdown");

                try
                {
                    foreach (var item in _jobInstances)
                    {
                        item.Value.Cancel();
                    }
                }
                catch { }

                for (int i = 0; i < 30; i++)
                {
                    if (_jobInstances.Count == 0) { break; }
                    await ConsoleLogger.Log(LogLevel.Information, $"Wait for {_jobInstances.Count} jobs to finish running");
                    await Task.Delay(1_000);
                }

                if (_jobInstances.Count > 0)
                {
                    await ConsoleLogger.Log(LogLevel.Error, $"{_jobInstances.Count} jobs to is running after waiting 30 seconds");
                }
            });

            Console.CancelKeyPress += (sender, args) =>
            {
                Console.WriteLine("\nCtrl+C detected! Performing cleanup...");

                // 2. Prevent the application from terminating immediately
                args.Cancel = true;

                _mainCancellationTokenSource.Cancel();
            };
        }

        private async static Task SafeStartAsync(RabbitMqJobStartProperties properties)
        {
            Properties = properties;
            _rabbitMqFactory = await RabbitMqFactory.GetInstance(properties, _mainCancellationTokenSource.Token);

            // Start consuming messages
            await _rabbitMqFactory.StartConsumeAsync(messageHandler: RouteMessageAsync);
        }

        /// <summary>
        /// Message handler - processes each received message
        /// </summary>
        /// <param name="messageBody">The message body as a string</param>
        /// <param name="eventArgs">Event arguments containing message metadata</param>
        /// <returns>True if message processed successfully, False to requeue</returns>
        private static async Task ProcessInvokeMessageAsync(BasicDeliverEventArgs eventArgs)
        {
            await ConsoleLogger.Log(LogLevel.Debug, ">> Received message <<");
            string fid = string.Empty;
#if NETSTANDARD2_0
            JobDefinition jobDefinition = null;
#else
            JobDefinition? jobDefinition = null;
#endif
            try
            {
                jobDefinition = Properties.GetJobDefinition(eventArgs.RoutingKey);
                fid = GetFireInstanceIdHeader(eventArgs);
                var info = TrackInstance(fid);
                await Execute(eventArgs, jobDefinition.JobType, info.CancellationToken);
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

        private static async Task ProcessCancelAsync(BasicDeliverEventArgs eventArgs)
        {
            await ConsoleLogger.Log(LogLevel.Debug, ">> Received message <<");
            string fid = string.Empty;
#if NETSTANDARD2_0
            JobInstanceInfo jobInstanceInfo = null;
#else
            JobInstanceInfo? jobInstanceInfo = null;
#endif

            try
            {
                fid = GetFireInstanceIdHeader(eventArgs);
                if (!_jobInstances.TryGetValue(fid, out jobInstanceInfo))
                {
                    var log = new LogEntity { Level = LogLevel.Information, Message = $"No running job found for FireInstanceId {fid}".TrimEnd() };
                    await Console.Error.WriteLineAsync(log.ToString());
                    return;
                }

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

        private static string GetHeader(BasicDeliverEventArgs eventArgs, string name)
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
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new PlanarJobException($"{name} header is missing or empty in the message headers");
            }

            return result;
        }

        private static JobInstanceInfo TrackInstance(string fireInstanceId)
        {
            var info = new JobInstanceInfo(fireInstanceId);

            if (!_jobInstances.TryAdd(fireInstanceId, info))
            {
                throw new PlanarJobException($"Duplicate FireInstanceId detected: {fireInstanceId}");
            }

            return info;
        }

        private static async Task UntrackInstance(string fireInstanceId)
        {
            if (_jobInstances.TryRemove(fireInstanceId, out var info))
            {
                try { info.Dispose(); } catch { }
            }

            if (_jobInstances.Count == 0)
            {
                try { await MqttClient.PublishAsync(MessageBrokerChannels.FinishInvokeJob); } catch { }
                await Task.Delay(50);
                try { await MqttClient.PublishAsync(MessageBrokerChannels.FinishInvokeJob); } catch { }
                await Task.Delay(50);
                try { await MqttClient.PublishAsync(MessageBrokerChannels.FinishInvokeJob); } catch { }
                await Task.Delay(50);
                try { await MqttClient.StopAsync(delaySeconds: 125); } catch { }
            }
        }

        private static async Task Execute(BasicDeliverEventArgs eventArgs, Type jobType, CancellationToken cancellationToken)
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
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