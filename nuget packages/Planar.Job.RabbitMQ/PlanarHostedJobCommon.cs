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
                _logger?.LogInformation("Start gracefull shutdown");

                try
                {
                    foreach (var item in _jobInstances)
                    {
                        item.Value.Cancel();
                    }
                }
                catch
                {
                    // *** DO NOTHING, we are shutting down anyway, just try best effort to cancel running jobs *** //
                }

                for (int i = 0; i < 30; i++)
                {
                    if (_jobInstances.IsEmpty) { break; }
                    if(_logger?.IsEnabled(LogLevel.Information) == true)
                    {
                        _logger.LogInformation("Wait for {Count} jobs to finish running", _jobInstances.Count);
                    }

                    await Task.Delay(1_000);
                }

                if (!_jobInstances.IsEmpty)
                {
                    _logger?.LogError("{Count} jobs to is running after waiting 30 seconds", _jobInstances.Count);
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
                try { info.Dispose(); } 
                catch 
                {
                    // *** DO NOTHING, just try best effort to dispose the instance info *** //
                }
            }

            if (_jobInstances.IsEmpty)
            {
                for (int i = 0; i < 3; i++)
                {
                    try { await MqttClient.PublishAsync(fireInstanceId, MessageBrokerChannels.FinishInvokeJob); } 
                    catch 
                    {
                        // *** DO NOTHING, just try best effort to publish the finish message, we will try 3 times with 50ms delay in between *** //
                    }
                    await Task.Delay(50);
                    try { await MqttClient.PublishAsync(fireInstanceId, MessageBrokerChannels.FinishInvokeJob); }
                    catch
                    {
                        // *** DO NOTHING, just try best effort to publish the finish message, we will try 3 times with 50ms delay in between *** //
                    }
                    await Task.Delay(50);
                    try { await MqttClient.PublishAsync(fireInstanceId, MessageBrokerChannels.FinishInvokeJob); }
                    catch
                    {
                        // *** DO NOTHING, just try best effort to publish the finish message, we will try 3 times with 50ms delay in between *** //
                    }
                }
            }
        }
    }
}