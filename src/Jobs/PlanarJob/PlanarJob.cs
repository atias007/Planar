using CommonJob;
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

namespace Planar
{
    public abstract class PlanarJob : BaseCommonJob<PlanarJob, PlanarJobProperties>
    {
        private static readonly string _seperator = string.Empty.PadLeft(80, '-');
        private readonly IMonitorUtil _monitorUtil;
        private readonly StringBuilder _output = new();
        private string? _filename;
        private long _peakPagedMemorySize64;
        private long _peakVirtualMemorySize64;
        private long _peakWorkingSet64;
        private Process? _process;
        private bool _processKilled;

        protected PlanarJob(ILogger<PlanarJob> logger, IJobPropertyDataLayer dataLayer, IMonitorUtil monitorUtil) : base(logger, dataLayer)
        {
            _monitorUtil = monitorUtil;
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
                var startInfo = GetProcessStartInfo(context);
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

        private void CheckProcessExitCode()
        {
            if (_processKilled)
            {
                throw new PlanarJobException($"process '{Filename}' was stopped at {DateTimeOffset.Now}");
            }
        }

        private ProcessStartInfo GetProcessStartInfo(IJobExecutionContext context)
        {
            var json = JsonConvert.SerializeObject(context);
            var bytes = Encoding.UTF8.GetBytes(json);
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
            MessageBroker.AppendLog(LogLevel.Information, $"StartTime: {_process.StartTime}");
            MessageBroker.AppendLog(LogLevel.Information, $"ExitTime: {_process.ExitTime}");
            MessageBroker.AppendLog(LogLevel.Information, $"Id: {_process.Id}");
            MessageBroker.AppendLog(LogLevel.Information, $"PeakPagedMemorySize64: {FormatBytes(_peakPagedMemorySize64)}");
            MessageBroker.AppendLog(LogLevel.Information, $"PeakWorkingSet64: {FormatBytes(_peakWorkingSet64)}");
            MessageBroker.AppendLog(LogLevel.Information, $"PeakVirtualMemorySize64: {FormatBytes(_peakVirtualMemorySize64)}");
            MessageBroker.AppendLog(LogLevel.Information, _seperator);
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
            _output.AppendLine(eventArgs.Data);
            MessageBroker.AppendLog(LogLevel.Error, eventArgs.Data);
            UpdatePeakVariables(_process);
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.Data)) { return; }
            _output.AppendLine(eventArgs.Data);
            MessageBroker.AppendLog(LogLevel.Information, eventArgs.Data);
            UpdatePeakVariables(_process);
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

            if (!process.HasExited)
            {
                try
                {
                    _peakPagedMemorySize64 = process.PeakPagedMemorySize64;
                    _peakVirtualMemorySize64 = process.PeakVirtualMemorySize64;
                    _peakWorkingSet64 = process.PeakWorkingSet64;
                }
                catch
                {
                    // *** DO NOTHING ***
                }
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