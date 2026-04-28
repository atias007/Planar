using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Job.RabbitMQ;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        internal static RabbitMQJobStartProperties Properties { get; set; }
#else
        internal static string Environment { get; private set; } = null!;
        internal static RabbitMQJobStartProperties Properties { get; set; } = null!;
        private static Type? _jobType;
#endif
        public static PlanarJobDebugger Debugger { get; } = new PlanarJobDebugger();
        private static List<Argument> Arguments { get; set; } = new List<Argument>();
        internal static RunningMode Mode { get; set; } = RunningMode.Release;
        internal static Stopwatch Stopwatch { get; set; } = new Stopwatch();

        public async static Task StartAsync<TJob>(RabbitMQJobStartProperties properties)
                    where TJob : BaseJob, new()
        {
            _jobType = typeof(TJob);

            try
            {
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

            var factory = properties.GetConnectionFactory();
            var cancellationToken = GetMainCancellationToken();

            // set rabbitmq definition (exchange, queue, binding)
            await RabbitMQFactory.EnsureDefinition(
                connectionFactory: factory,
                properties,
                cancellationToken: cancellationToken);

            // Start consuming messages
            await RabbitMQFactory.StartConsumeAsync(
                connectionFactory: factory,
                properties,
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
            await ConsoleLogger.Log(LogLevel.Debug, ">> Received message <<");

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
                if (_jobType == null) { return; }
                json = ShowDebugMenu(_jobType);
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

            var success = await instance.Execute(json, Properties.PlanarHostName);

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

#if NETSTANDARD2_0

        private static Argument GetArgument(string key)
#else
        private static Argument? GetArgument(string key)
#endif
        {
            return Arguments.Find(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsKeyArgument(Argument arg)
        {
            if (string.IsNullOrEmpty(arg.Key)) { return false; }
            const string template = "^--[a-z,A-Z]";
            return Regex.IsMatch(arg.Key, template, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }

        private static void FillArguments()
        {
            var source = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < source.Length; i++)
            {
                Arguments.Add(new Argument { Key = source[i] });
            }

            for (int i = 1; i < Arguments.Count; i++)
            {
                var item1 = Arguments[i - 1];
                var item2 = Arguments[i];

                if (IsKeyArgument(item1) && !IsKeyArgument(item2))
                {
                    item1.Value = item2.Key;
                    item2.Key = null;
                    i++;
                }
            }
        }

        private static bool HasArgument(string key)
        {
            return Arguments.Exists(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        ////private static string ShowDebugMenu<TJob>()
        ////     where TJob : BaseJob, new()
        ////{
        ////    return ShowDebugMenu(typeof(TJob));
        ////}

        private static string ShowDebugMenu(Type type)
        {
            int? selectedIndex;

            var typeName = type.Name;
            var hasProfiles = Debugger.Profiles.Any();
            if (hasProfiles)
            {
                Console.Write("type the profile code ");
                Console.Write("to start executing the ");
            }
            else
            {
                Console.Write("type [Enter] to start executing the ");
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{typeName} ");
            Console.ResetColor();
            Console.WriteLine("job");
            Console.WriteLine();

            var index = 1;
            foreach (var p in Debugger.Profiles)
            {
                PrintMenuItem(p.Key, index.ToString());
                index++;
            }

            if (hasProfiles)
            {
                Console.WriteLine("------------------");
                PrintMenuItem("<Default>", "Enter");
                Console.WriteLine();
            }

            selectedIndex = GetMenuItem(quiet: !hasProfiles);

            MockJobExecutionContext context;
            IExecuteJobProperties properties;
            if (selectedIndex == null)
            {
                properties = new ExecuteJobPropertiesBuilder().SetDevelopmentEnvironment().Build();
                context = new MockJobExecutionContext(properties);
            }
            else
            {
                properties = Debugger.Profiles.Values.ToList()[selectedIndex.Value - 1];
                context = new MockJobExecutionContext(properties);
            }

            Environment = properties.Environment;
            var json = JsonSerializer.Serialize(context);
            return json;
        }

        private static int? GetMenuItem(bool quiet)
        {
            int index = 0;
            var valid = false;
            while (!valid)
            {
                if (!quiet) { Console.Write("Code: "); }
                using (var timer = new Timer(60_000))
                {
                    timer.Elapsed += TimerElapsed;
                    timer.Start();
                    var selected = Console.ReadLine();
                    timer.Stop();
                    if (string.IsNullOrEmpty(selected))
                    {
                        if (!quiet) { Console.WriteLine("<Default>"); }
                        return null;
                    }

                    if (!int.TryParse(selected, out index))
                    {
                        ShowErrorMenu($"Selected value '{selected}' is not valid numeric value");
                    }
                    else if (index > Debugger.Profiles.Count || index <= 0)
                    {
                        ShowErrorMenu($"Selected value '{index}' is not exists");
                    }
                    else
                    {
                        valid = true;
                    }
                }
            }

            return index;
        }

        private static void ShowErrorMenu(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void PrintMenuItem(string text, string key)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"[{key}] ");
            Console.ResetColor();
            Console.WriteLine(text);
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