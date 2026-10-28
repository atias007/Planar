using Microsoft.Extensions.Logging;
using Planar.Common;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Job
{
    internal static partial class PlanarHostedJobCommon
    {
        private static readonly ConcurrentDictionary<string, JobInstanceInfo> _jobInstances = new ConcurrentDictionary<string, JobInstanceInfo>();
        private static readonly CancellationTokenSource _mainCancellationTokenSource = new CancellationTokenSource();

        internal static void SafeDisposeCancellationTokenSource()
        {
            try
            {
                _mainCancellationTokenSource.Dispose();
            }
            catch
            {
                // *** DO NOTHING, we are shutting down anyway, just try best effort to cancel running jobs *** //
            }
        }

        internal static bool TryGetInstanceInfo(string fireInstanceId, out JobInstanceInfo info)
        {
            return _jobInstances.TryGetValue(fireInstanceId, out info);
        }

        internal static void SetupGracefulShutdown(ILogger logger)
        {
            AppDomain.CurrentDomain.ProcessExit += (s, a) =>
            {
                try
                {
                    _mainCancellationTokenSource.Cancel();
                }
                catch
                {
                    // *** DO NOTHING, we are shutting down anyway, just try best effort to cancel running jobs *** //
                }
            };

            _mainCancellationTokenSource.Token.Register(async () =>
            {
                logger?.LogInformation("Start graceful shutdown");

                foreach (var item in _jobInstances)
                {
                    try
                    {
                        item.Value.Cancel();
                    }
                    catch
                    {
                        // *** DO NOTHING, we are shutting down anyway, just try best effort to cancel running jobs *** //
                    }
                }

                for (int i = 0; i < 30; i++)
                {
                    if (_jobInstances.IsEmpty) { break; }
                    if (logger?.IsEnabled(LogLevel.Information) == true)
                    {
                        logger.LogInformation("Wait for {Count} jobs to finish running", _jobInstances.Count);
                    }

#pragma warning disable S8949 // The overload accepting a 'CancellationToken' should be used
                    await Task.Delay(1_000);
#pragma warning restore S8949 // The overload accepting a 'CancellationToken' should be used
                }

                if (!_jobInstances.IsEmpty)
                {
                    logger?.LogError("{Count} jobs to is running after waiting 30 seconds", _jobInstances.Count);
                }
            });

            Console.CancelKeyPress += (sender, args) =>
            {
                Console.WriteLine("\nCtrl+C detected! Performing cleanup...");

                // 2. Prevent the application from terminating immediately
                args.Cancel = true;

                try
                {
                    _mainCancellationTokenSource.Cancel();
                }
                catch
                {
                    // *** DO NOTHING, we are shutting down anyway, just try best effort to cancel running jobs *** //
                }
            };
        }

        internal static JobInstanceInfo TrackInstance(string fireInstanceId)
        {
            var info = new JobInstanceInfo(fireInstanceId);

            if (!_jobInstances.TryAdd(fireInstanceId, info))
            {
                throw new PlanarJobConflictException($"Duplicate FireInstanceId detected: {fireInstanceId}");
            }

            return info;
        }

        internal static async Task UntrackInstance(string fireInstanceId)
        {
            if (_jobInstances.TryRemove(fireInstanceId, out var info))
            {
                try { info.Dispose(); }
                catch
                {
                    // *** DO NOTHING, just try best effort to dispose the instance info *** //
                }
            }

            if (_jobInstances.IsEmpty)
            {
                await SafeSendFinishInvokeJobSignal(fireInstanceId);
            }
        }

        private static async Task SafeSendFinishInvokeJobSignal(string fireInstanceId)
        {
            for (int i = 0; i < 3; i++)
            {
                try { await MqttClient.PublishAsync(fireInstanceId, MessageBrokerChannels.FinishInvokeJob); }
                catch
                {
                    // *** DO NOTHING, just try best effort to publish the finish message, we will try 3 times with 50ms delay in between *** //
                }
                await Task.Delay(50, CancellationToken.None);
            }
        }

        internal static async Task PrintLogo(Assembly assembly)
        {
            const string logo1 = @"  ____  _                              _       _         ";
            const string logo2 = @" |  _ \| | __ _ _ __   __ _ _ __      | | ___ | |__  ___ ";
            const string logo3 = @" | |_) | |/ _` | '_ \ / _` | '__|  _  | |/ _ \| '_ \/ __|";
            const string logo4 = @" |  __/| | (_| | | | | (_| | |    | |_| | (_) | |_) \__ \";
            const string logo5 = @" |_|   |_|\__,_|_| |_|\__,_|_|     \___/ \___/|_.__/|___/";

            await Console.Out.WriteLineAsync(logo1);
            await Console.Out.WriteLineAsync(logo2);
            await Console.Out.WriteLineAsync(logo3);
            await Console.Out.WriteLineAsync(logo4);
            await Console.Out.WriteLineAsync(logo5);
            await Console.Out.WriteLineAsync();
            var version = SafeGetVersion(assembly);
            PrintBoxed($"RabbitMQ Hosted Job [version: {version}]");
            await Console.Out.WriteLineAsync();
        }

        private static string SafeGetVersion(Assembly assembly)
        {
            try
            {
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static void PrintBoxed(string text)
        {
            int width = text.Length + 2; // one space padding each side
            string horizontal = new string('-', width);

            Console.WriteLine($"+{horizontal}+");
            Console.WriteLine($"| {text} |");
            Console.WriteLine($"+{horizontal}+");
        }
    }
}