using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Job.RabbitMQ;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
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

        private static readonly ConcurrentDictionary<string, object> _jobInstances = new ConcurrentDictionary<string, object>();
        private static readonly SemaphoreSlim _handleLock = new SemaphoreSlim(1, 1);

        public async static Task StartAsync<TJob>(RabbitMQJobStartProperties properties)
                    where TJob : BaseJob, new()
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            _jobType = typeof(TJob);

            try
            {
                Stopwatch.Start();
                FillProperties();
                if (await Debug(_jobType, properties.PlanarHostName)) { return; }

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
            var cancellationToken = GetMainCancellationToken();
            _rabbitMQFactory = RabbitMQFactory.GetInstance(properties, cancellationToken);

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
            await ConsoleLogger.Log(LogLevel.Debug, ">> Received message <<");
            string fid = string.Empty;
            try
            {
                fid = GetFireInstanceId(eventArgs);

                await Execute(eventArgs);
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
                _jobInstances.TryRemove(fid, out _);
                if (_jobInstances.Count == 0)
                {
                    await MqttClient.StopAsync(60);
                }
            }
        }

        private static string GetFireInstanceId(BasicDeliverEventArgs eventArgs)
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

            ////await _handleLock.WaitAsync();
            ////try
            ////{
            ////}
            ////finally
            ////{
            ////}

            if (!_jobInstances.TryAdd(result, result))
            {
                throw new PlanarJobException($"Duplicate FireInstanceId detected: {result}");
            }

            return result;
        }

        private static async Task Execute(BasicDeliverEventArgs eventArgs)
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            if (_jobType == null) { return; }
            await Execute(_jobType, Properties?.PlanarHostName, json);
        }

        private static CancellationToken GetMainCancellationToken()
        {
            var host = Host.CreateDefaultBuilder(System.Environment.GetCommandLineArgs()).Build();
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            var cancellationToken = lifetime.ApplicationStopping;
            return cancellationToken;
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