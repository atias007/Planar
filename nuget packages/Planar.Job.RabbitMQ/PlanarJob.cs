using Microsoft.Extensions.Logging;
using Planar.Job.RabbitMQ;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Job
{
    public static partial class PlanarJob
    {
#if NETSTANDARD2_0
        private static Type _jobType;
        internal static RabbitMQJobStartProperties Properties { get; set; }
        private static RabbitMQFactory _rabbitMQFactory;
#else
        internal static RabbitMQJobStartProperties Properties { get; set; } = null!;
        private static Type? _jobType;
        private static RabbitMQFactory? _rabbitMQFactory;
#endif

        private static readonly ConcurrentDictionary<string, JobInstanceInfo> _jobInstances = new ConcurrentDictionary<string, JobInstanceInfo>();
        private static readonly CancellationTokenSource _mainCancellationTokenSource = new CancellationTokenSource();

        public async static Task StartAsync<TJob>(RabbitMQJobStartProperties properties)
                    where TJob : BaseJob, new()
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            _jobType = typeof(TJob);
            AppDomain.CurrentDomain.ProcessExit += (s, a) => _mainCancellationTokenSource.Cancel();

            try
            {
                FillProperties();
                if (await Debug(_jobType, properties.PlanarHostName, _mainCancellationTokenSource.Token)) { return; }

                await SafeStartAsync<TJob>(properties);
            }
            catch (Exception ex)
            {
                await ConsoleLogger.Log(LogLevel.Critical, "----------------");
                await ConsoleLogger.Log(LogLevel.Critical, " Fail to start");
                await ConsoleLogger.Log(LogLevel.Critical, "----------------");
                await ConsoleLogger.Log(LogLevel.Critical, ex.ToString());
            }
        }

        private async static Task SafeStartAsync<TJob>(RabbitMQJobStartProperties properties)
                    where TJob : BaseJob, new()
        {
            Properties = properties;
            _rabbitMQFactory = RabbitMQFactory.GetInstance(properties, _mainCancellationTokenSource.Token);

            // set rabbitmq definition (exchange, queue, binding)
            await _rabbitMQFactory.EnsureDefinition();

            // Start consuming messages
            await _rabbitMQFactory.StartConsumeAsync(messageHandler: ProcessMessageAsync);
        }

        /// <summary>
        /// Message handler - processes each received message
        /// </summary>
        /// <param name="messageBody">The message body as a string</param>
        /// <param name="eventArgs">Event arguments containing message metadata</param>
        /// <returns>True if message processed successfully, False to requeue</returns>
        private static async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs)
        {
            Stopwatch.Start();
            await ConsoleLogger.Log(LogLevel.Debug, ">> Received message <<");
            string fid = string.Empty;
            try
            {
                fid = TrackInstance(eventArgs);
                await Execute(eventArgs, _mainCancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                var log = new LogEntity { Level = LogLevel.Critical, Message = $"Fail to execute {_jobType?.Name}" };
                await Console.Error.WriteLineAsync(log.ToString());
                log.Message = ex.ToString();
                await Console.Error.WriteLineAsync(log.ToString());
            }
            finally
            {
                await UntrackInstance(fid);
            }
        }

        private static string TrackInstance(BasicDeliverEventArgs eventArgs)
        {
#if NETSTANDARD2_0
            object fireInstanceIdObj = null;
#else
            object? fireInstanceIdObj = null;
#endif

            eventArgs.BasicProperties.Headers?.TryGetValue("FireInstanceId", out fireInstanceIdObj);
            if (fireInstanceIdObj is byte[] bytes)
            {
                fireInstanceIdObj = Encoding.UTF8.GetString(bytes);
            }

            var result = Convert.ToString(fireInstanceIdObj);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new PlanarJobException("FireInstanceId header is missing or empty in the message headers");
            }

            var info = new JobInstanceInfo(result, _mainCancellationTokenSource.Token);

            if (!_jobInstances.TryAdd(result, info))
            {
                throw new PlanarJobException($"Duplicate FireInstanceId detected: {result}");
            }

            return result;
        }

        private static async Task UntrackInstance(string fireInstanceId)
        {
            if (_jobInstances.TryRemove(fireInstanceId, out var info))
            {
                try { info.Dispose(); } catch { }
            }

            if (_jobInstances.Count == 0)
            {
                try { await MqttClient.StopAsync(delaySeconds: 125); } catch { }
            }
        }

        private static async Task Execute(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            if (_jobType == null) { return; }
            await Execute(_jobType, Properties?.PlanarHostName, json, cancellationToken);
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