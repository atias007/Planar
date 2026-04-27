using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Job.RabbitMQ;
using RabbitMQ.Client.Events;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Planar.Job
{
    public static partial class PlanarJob
    {
#if NETSTANDARD2_0
        internal static string Environment { get; private set; }
        private static Type _jobType;
#else
        internal static string Environment { get; private set; } = null!;
        private static Type? _jobType;
#endif

        internal static RunningMode Mode { get; set; } = RunningMode.Release;
        internal static PlanarJobStartProperties Properties { get; set; } = PlanarJobStartProperties.Default;
        internal static Stopwatch Stopwatch { get; set; } = new Stopwatch();

        public async static Task StartAsync<TJob>(RabbitMQConnectionInfo connectionInfo)
                    where TJob : BaseJob, new()
        {
            await StartAsync<TJob>(connectionInfo, PlanarJobStartProperties.Default);
        }

        public async static Task StartAsync<TJob>(RabbitMQConnectionInfo connectionInfo, PlanarJobStartProperties properties)
                    where TJob : BaseJob, new()
        {
            _jobType = typeof(TJob);

            try
            {
                await SafeStartAsync<TJob>(connectionInfo, properties);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync("----------------");
                await Console.Out.WriteLineAsync(" Fail to start");
                await Console.Out.WriteLineAsync("----------------");
                await Console.Out.WriteLineAsync(ex.ToString());
            }
        }

        private async static Task SafeStartAsync<TJob>(RabbitMQConnectionInfo connectionInfo, PlanarJobStartProperties properties)
                    where TJob : BaseJob, new()
        {
            Properties = properties;

            var factory = connectionInfo.GetConnectionFactory();
            var cancellationToken = GetMainCancellationToken();

            // set rabbitmq definition (exchange, queue, binding)
            await RabbitMQFactory.EnsureDefinition(
                connectionFactory: factory,
                connectionInfo,
                cancellationToken: cancellationToken);

            // Start consuming messages
            await RabbitMQFactory.StartConsumeAsync(
                connectionFactory: factory,
                connectionInfo,
                messageHandler: ProcessMessageAsync,
                cancellationToken: cancellationToken
            );
        }

        private static CancellationToken GetMainCancellationToken()
        {
            var host = Host.CreateDefaultBuilder(System.Environment.GetCommandLineArgs()).Build();
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            var cancellationToken = lifetime.ApplicationStopping;
            return cancellationToken;
        }

        /// <summary>
        /// Message handler - processes each received message
        /// </summary>
        /// <param name="messageBody">The message body as a string</param>
        /// <param name="eventArgs">Event arguments containing message metadata</param>
        /// <returns>True if message processed successfully, False to requeue</returns>
        private static async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs)
        {
            await Console.Out.WriteLineAsync(">> Received message");

            try
            {
                await Execute(eventArgs);
            }
            catch (Exception ex)
            {
                var log = new LogEntity { Level = LogLevel.Critical, Message = $"Fail to execute {_jobType?.Name}" };
                await Console.Error.WriteLineAsync(log.ToString());
                log.Message = ex.ToString();
                await Console.Error.WriteLineAsync(log.ToString());
            }
        }

        private static async Task Execute(BasicDeliverEventArgs eventArgs)
        {
            string json;
            if (Mode == RunningMode.Debug)
            {
                json = string.Empty;
                // TODO: json = ShowDebugMenu<TJob>();
            }
            else
            {
                json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            }

            if (Mode == RunningMode.Debug)
            {
                await Console.Out.WriteLineAsync("---------------------------------------");
                await Console.Out.WriteAsync(">> Environment: ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                await Console.Out.WriteLineAsync(Environment);
                Console.ResetColor();
                await Console.Out.WriteLineAsync("---------------------------------------");
            }

            var instance = Activator.CreateInstance(_jobType) as BaseJob ??
                throw new InvalidOperationException($"Failed to create an instance of {_jobType?.Name}");

            var success = await instance.Execute(json);

            if (Mode == RunningMode.Debug)
            {
                await instance.PrintDebugSummary(success);
                await Console.Out.WriteLineAsync("---------------------------------------");
                await Console.Out.WriteLineAsync(">> Press any key to exit");
                await Console.Out.WriteLineAsync("---------------------------------------");
                using (var timer = new Timer(60_000))
                {
                    timer.Elapsed += TimerElapsed;
                    timer.Start();
                    Console.ReadKey(true);
                    timer.Stop();
                }
            }
        }

        private static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine("User input timeout. Terminate application");
            Console.ResetColor();
            System.Environment.Exit(-1);
        }
    }
}