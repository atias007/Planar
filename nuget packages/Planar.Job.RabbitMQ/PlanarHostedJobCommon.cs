using Microsoft.Extensions.Logging;
using Planar.Common;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Job
{
    public static partial class PlanarJob
    {
        private static readonly ConcurrentDictionary<string, JobInstanceInfo> _jobInstances = new ConcurrentDictionary<string, JobInstanceInfo>();
        private static readonly CancellationTokenSource _mainCancellationTokenSource = new CancellationTokenSource();

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

        private static JobInstanceInfo TrackInstance(string fireInstanceId)
        {
            var info = new JobInstanceInfo(fireInstanceId);

            if (!_jobInstances.TryAdd(fireInstanceId, info))
            {
                throw new PlanarJobConflictException($"Duplicate FireInstanceId detected: {fireInstanceId}");
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
                for (int i = 0; i < 3; i++)
                {
                    try { await MqttClient.PublishAsync(fireInstanceId, MessageBrokerChannels.FinishInvokeJob); } catch { }
                    await Task.Delay(50);
                    try { await MqttClient.PublishAsync(fireInstanceId, MessageBrokerChannels.FinishInvokeJob); } catch { }
                    await Task.Delay(50);
                    try { await MqttClient.PublishAsync(fireInstanceId, MessageBrokerChannels.FinishInvokeJob); } catch { }
                }
            }
        }
    }
}