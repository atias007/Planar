using CloudNative.CloudEvents;
using CommonJob;
using CommonJob.MessageBrokerEntities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Common;
using Planar.Common.Helpers;
using Quartz;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Planar
{
    public abstract class PlanarJob : BaseCommonJob<PlanarJob, PlanarJobProperties>
    {
        private static readonly string _seperator = string.Empty.PadLeft(40, '-');
        private readonly StringBuilder _error = new();
        private readonly IMonitorUtil _monitorUtil;
        private readonly StringBuilder _output = new();
        private readonly Timer _processMetricsTimer = new(1000);
        private readonly object Locker = new();
        private readonly object ConsoleLocker = new();
        private string? _filename;
        private long _peakPagedMemorySize64;
        private long _peakWorkingSet64;
        private Process? _process;
        private bool _processKilled;
        private readonly bool _isDevelopment;

        protected PlanarJob(
            ILogger<PlanarJob> logger,
            IJobPropertyDataLayer dataLayer,
            IMonitorUtil monitorUtil) : base(logger, dataLayer)
        {
            _monitorUtil = monitorUtil;

            MqttBrokerService.InterceptingPublishAsync += InterceptingPublishAsync;
            _isDevelopment = string.Equals(AppSettings.Environment, "development", StringComparison.OrdinalIgnoreCase);
        }

        private string Filename
        {
            get
            {
                _filename ??= FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, Properties.Path, Properties.Filename);

                return _filename;
            }
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await Initialize(context, _monitorUtil);
                ValidatePlanarJob();
                context.CancellationToken.Register(OnCancel);

                var timeout = TriggerHelper.GetTimeoutWithDefault(context.Trigger);
                var startInfo = GetProcessStartInfo();
                var success = StartProcess(startInfo, timeout);
                if (!success)
                {
                    OnTimeout();
                }

                LogProcessInformation();
                CheckProcessExitCode();
            }
            catch (Exception ex)
            {
                HandleException(context, ex);
            }
            finally
            {
                FinalizeJob(context);
                FinalizeProcess();
            }
        }

        private static string FormatBytes(long bytes)
        {
            const int scale = 1024;
            var orders = new string[] { "gb", "mb", "kb", "bytes" };
            var max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                {
                    return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);
                }

                max /= scale;
            }

            return "0 bytes";
        }

        private static byte GetCloudEventByteValue(CloudEvent cloudEvent)
        {
            var data = ValidateCloudEventData(cloudEvent.Data);

            if (byte.TryParse(data, out var value))
            {
                return value;
            }
            else
            {
                throw new PlanarJobException($"Message broker channels '{cloudEvent.Type}' has invalid byte value '{cloudEvent.Data}'");
            }
        }

        private static T GetCloudEventEntityValue<T>(CloudEvent cloudEvent)
            where T : class, new()
        {
            var data = ValidateCloudEventData(cloudEvent.Data);
            var log = JsonConvert.DeserializeObject<T>(data);
            return log
                ?? throw new PlanarJobException($"Message broker channels '{cloudEvent.Type}' has invalid '{typeof(T).Name}' value '{data}'");
        }

        private static int GetCloudEventInt32Value(CloudEvent cloudEvent)
        {
            var data = ValidateCloudEventData(cloudEvent.Data);

            if (int.TryParse(data, out var value))
            {
                return value;
            }
            else
            {
                throw new PlanarJobException($"Message broker channels '{cloudEvent.Type}' has invalid integer value '{cloudEvent.Data}'");
            }
        }

        private static string ValidateCloudEventData(object? data)
        {
            if (data == null)
            {
                throw new PlanarJobException("Message broker has empty data");
            }

            var result = data.ToString();
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new PlanarJobException("Message broker has empty data");
            }

            return result;
        }

        private void CheckProcessExitCode()
        {
            if (_processKilled)
            {
                throw new PlanarJobException($"process '{Filename}' was stopped at {DateTimeOffset.Now}");
            }
        }

        private void FinalizeProcess()
        {
            try { _process?.CancelErrorRead(); } catch { DoNothingMethod(); }
            try { _process?.CancelOutputRead(); } catch { DoNothingMethod(); }
            try { _process?.Close(); } catch { DoNothingMethod(); }
            try { _process?.Dispose(); } catch { DoNothingMethod(); }
            try { MqttBrokerService.InterceptingPublishAsync -= InterceptingPublishAsync; } catch { DoNothingMethod(); }
            try { if (_process != null) { _process.EnableRaisingEvents = false; } } catch { DoNothingMethod(); }
            try { if (_process != null) { _process.OutputDataReceived -= ProcessOutputDataReceived; } } catch { DoNothingMethod(); }
            try { if (_process != null) { _process.ErrorDataReceived -= ProcessErrorDataReceived; } } catch { DoNothingMethod(); }
            try { if (_processMetricsTimer != null) { _processMetricsTimer.Elapsed -= MetricsTimerElapsed; } } catch { DoNothingMethod(); }
        }

        private ProcessStartInfo GetProcessStartInfo()
        {
            var bytes = Encoding.UTF8.GetBytes(MessageBroker.Details);
            var base64String = Convert.ToBase64String(bytes);

            var startInfo = new ProcessStartInfo
            {
                Arguments = base64String,
                CreateNoWindow = true,
                ErrorDialog = false,
                FileName = Filename,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, Properties.Path),
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
            };

            return startInfo;
        }

        private void InterceptingPublishAsync(object? sender, CloudEventArgs e)
        {
            try
            {
                InterceptingPublishAsyncInner(e);
            }
            catch (PlanarJobException ex)
            {
                _logger.LogCritical("Fail intercepting published message on MQTT broker event. {Error}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fail intercepting published message on MQTT broker event");
            }
        }

        private void WriteToConsole(LogEntity logEntity)
        {
            if (_isDevelopment)
            {
                lock (ConsoleLocker)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Out.WriteLine($" - {logEntity.Message}");
                    Console.ResetColor();
                }
            }
        }

        private void InterceptingPublishAsyncInner(CloudEventArgs e)
        {
            if (!Enum.TryParse<MessageBrokerChannels>(e.CloudEvent.Type, ignoreCase: true, out var channel))
            {
                _logger.LogError("Message broker channels '{Type}' is not valid", e.CloudEvent.Type);
                return;
            }

            int iValue;
            KeyValueObject kv;
            switch (channel)
            {
                case MessageBrokerChannels.AddAggregateException:
                    var ex = GetCloudEventEntityValue<ExceptionDto>(e.CloudEvent);
                    MessageBroker.AddAggregateException(ex);
                    break;

                case MessageBrokerChannels.AppendLog:
                    var log = GetCloudEventEntityValue<LogEntity>(e.CloudEvent);
                    MessageBroker.AppendLog(log);
                    WriteToConsole(log);
                    break;

                case MessageBrokerChannels.IncreaseEffectedRows:
                    iValue = GetCloudEventInt32Value(e.CloudEvent);
                    MessageBroker.IncreaseEffectedRows(iValue);
                    break;

                case MessageBrokerChannels.SetEffectedRows:
                    iValue = GetCloudEventInt32Value(e.CloudEvent);
                    MessageBroker.SetEffectedRows(iValue);
                    break;

                case MessageBrokerChannels.PutJobData:
                    kv = GetCloudEventEntityValue<KeyValueObject>(e.CloudEvent);
                    MessageBroker.PutJobDataAction(kv);
                    break;

                case MessageBrokerChannels.PutTriggerData:
                    kv = GetCloudEventEntityValue<KeyValueObject>(e.CloudEvent);
                    MessageBroker.PutTriggerData(kv);
                    break;

                case MessageBrokerChannels.UpdateProgress:
                    var progress = GetCloudEventByteValue(e.CloudEvent);
                    MessageBroker.UpdateProgress(progress);
                    break;

                case MessageBrokerChannels.ReportException:
                    break;

                default:
                    break;
            }
        }

        private void Kill(string reason)
        {
            if (_process == null)
            {
                return;
            }

            try
            {
                MessageBroker.AppendLog(LogLevel.Warning, $"Process was stopped. Reason: {reason}");
                _processKilled = true;
                _process.Kill(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to kill process job {Filename}", _process.StartInfo.FileName);
                MessageBroker.AppendLog(LogLevel.Error, $"Fail to kill process job {_process.StartInfo.FileName}. {ex.Message}");
            }
        }

        private void LogProcessInformation()
        {
            if (_process == null) { return; }
            if (!_process.HasExited) { return; }

            MessageBroker.AppendLog(LogLevel.Information, _seperator);
            MessageBroker.AppendLog(LogLevel.Information, " - Process information:");
            MessageBroker.AppendLog(LogLevel.Information, _seperator);
            MessageBroker.AppendLog(LogLevel.Information, $"ExitCode: {_process.ExitCode}");
            MessageBroker.AppendLog(LogLevel.Information, $"PeakPagedMemorySize64: {FormatBytes(_peakPagedMemorySize64)}");
            MessageBroker.AppendLog(LogLevel.Information, $"PeakWorkingSet64: {FormatBytes(_peakWorkingSet64)}");
            MessageBroker.AppendLog(LogLevel.Information, _seperator);
        }

        private void MetricsTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            UpdatePeakVariables(_process);
        }

        private void OnCancel()
        {
            Kill("request for cancel process");
        }

        private void OnTimeout()
        {
            Kill("timeout expire");
        }

        private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.Data)) { return; }
            _error.AppendLine(eventArgs.Data);
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.Data)) { return; }
            _output.AppendLine(eventArgs.Data);
        }

        private bool StartProcess(ProcessStartInfo startInfo, TimeSpan timeout)
        {
            _process = Process.Start(startInfo);
            if (_process == null)
            {
                var filename = Path.Combine(Properties.Path, Properties.Filename);
                throw new PlanarJobException($"could not start process {filename}");
            }

            _process.EnableRaisingEvents = true;
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _process.OutputDataReceived += ProcessOutputDataReceived;
            _process.ErrorDataReceived += ProcessErrorDataReceived;
            _processMetricsTimer.Elapsed += MetricsTimerElapsed;
            _processMetricsTimer.Start();

            _process.WaitForExit(Convert.ToInt32(timeout.TotalMilliseconds));
            if (!_process.HasExited)
            {
                MessageBroker.AppendLog(LogLevel.Error, $"Process timeout expire. Timeout was {timeout:hh\\:mm\\:ss}");
                return false;
            }

            return true;
        }

        private void UpdatePeakVariables(Process? process)
        {
            if (process == null) { return; }

            if (process.HasExited) { return; }

            try
            {
                lock (Locker)
                {
                    _peakPagedMemorySize64 = process.PeakPagedMemorySize64;
                    _peakWorkingSet64 = process.PeakWorkingSet64;
                }
            }
            catch
            {
                // *** DO NOTHING ***
            }
        }

        private void ValidatePlanarJob()
        {
            try
            {
                // Obsolete: Support old dll files
                if (!string.IsNullOrEmpty(Properties.Filename))
                {
                    var fi = new FileInfo(Properties.Filename);
                    if (fi.Extension == ".dll") { Properties.Filename = $"{Properties.Filename[0..^4]}.exe"; }
                }

                ValidateMandatoryString(Properties.Path, nameof(Properties.Path));
                ValidateMandatoryString(Properties.Filename, nameof(Properties.Filename));

                if (!File.Exists(Filename))
                {
                    throw new PlanarJobException($"process filename '{Filename}' could not be found");
                }
            }
            catch (Exception ex)
            {
                var source = nameof(ValidatePlanarJob);
                _logger.LogError(ex, "Fail at {Source}", source);
                MessageBroker.AppendLog(LogLevel.Error, $"Fail at {source}. {ex.Message}");
                throw;
            }
        }
    }
}