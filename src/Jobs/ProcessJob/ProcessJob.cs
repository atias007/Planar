using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
using ProcessJob;
using Quartz;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Planar
{
    public abstract class ProcessJob : BaseCommonJob<ProcessJob, ProcessJobProperties>
    {
        private static readonly string _seperator = string.Empty.PadLeft(80, '-');
        private readonly StringBuilder _output = new();
        private long _peakPagedMemorySize64;
        private long _peakVirtualMemorySize64;
        private long _peakWorkingSet64;
        private Process? _process;
        private bool _processKilled;

        protected ProcessJob(ILogger<ProcessJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }

        private string? _filename;

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
                await Initialize(context);

                ValidateProcessJob();
                context.CancellationToken.Register(() => OnCancel());

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
                var metadata = JobExecutionMetadata.GetInstance(context);
                metadata.UnhandleException = ex;
            }
            finally
            {
                FinalizeJob(context);
                FinalizeProcess();
            }
        }

        private void FinalizeProcess()
        {
            try { _process?.CancelErrorRead(); } catch { DoNothingMethod(); }
            try { _process?.CancelOutputRead(); } catch { DoNothingMethod(); }
            try { _process?.Close(); } catch { DoNothingMethod(); }
            try { _process?.Dispose(); } catch { DoNothingMethod(); }
            try { if (_process != null) { _process.EnableRaisingEvents = false; } } catch { DoNothingMethod(); }
            try { if (_process != null) { _process.OutputDataReceived -= ProcessOutputDataReceived; } } catch { DoNothingMethod(); }
            try { if (_process != null) { _process.ErrorDataReceived -= ProcessErrorDataReceived; } } catch { DoNothingMethod(); }
        }

        private void ValidateProcessJob()
        {
            try
            {
                ValidateMandatoryString(Properties.Path, nameof(Properties.Path));
                ValidateMandatoryString(Properties.Filename, nameof(Properties.Filename));

                if (!File.Exists(Filename))
                {
                    throw new ProcessJobException($"process filename '{Filename}' could not be found");
                }
            }
            catch (Exception ex)
            {
                var source = nameof(ValidateProcessJob);
                _logger.LogError(ex, "Fail at {Source}", source);
                MessageBroker.AppendLog(LogLevel.Error, $"Fail at {source}. {ex.Message}");
                throw;
            }
        }

        private void OnCancel()
        {
            Kill("request for cancel process");
        }

        private void OnTimeout()
        {
            Kill("timeout expire");
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
                throw new ProcessJobException($"process '{Filename}' was stopped at {DateTimeOffset.Now}");
            }

            if (_process == null) { return; }
            var exitCode = _process.ExitCode;
            if (Properties.FailExitCodes.Any())
            {
                if (Properties.FailExitCodes.Any(f => f == exitCode))
                {
                    var codes = string.Join(',', Properties.FailExitCodes);
                    throw new ProcessJobException($"process '{Filename}' ended with exit code {exitCode} which is one of fail exit codes ({codes})");
                }

                return;
            }

            if (Properties.SuccessExitCodes.Any())
            {
                if (!Properties.SuccessExitCodes.Any(s => s == exitCode))
                {
                    var codes = string.Join(',', Properties.SuccessExitCodes);
                    throw new ProcessJobException($"process '{Filename}' ended with exit code {exitCode} which is not one of success exit codes ({codes})");
                }

                return;
            }

            if (!string.IsNullOrEmpty(Properties.FailOutputRegex))
            {
                var output = _output.ToString();
                var regex = new Regex(Properties.FailOutputRegex, RegexOptions.None, TimeSpan.FromSeconds(5));
                if (regex.IsMatch(output))
                {
                    throw new ProcessJobException($"process '{Filename}' ended with an output that matched the fail output message '{Properties.FailOutputRegex}'");
                }

                return;
            }

            if (!string.IsNullOrEmpty(Properties.SuccessOutputRegex))
            {
                var output = _output.ToString();
                var regex = new Regex(Properties.SuccessOutputRegex, RegexOptions.None, TimeSpan.FromSeconds(5));
                if (!regex.IsMatch(output))
                {
                    throw new ProcessJobException($"process '{Filename}' ended with an output that not matched the success output message '{Properties.SuccessOutputRegex}'");
                }

                return;
            }

            if (exitCode != 0)
            {
                throw new ProcessJobException($"process '{Filename}' ended with exit code {exitCode} which is different from success exit code 0");
            }
        }

        private ProcessStartInfo GetProcessStartInfo()
        {
            var startInfo = new ProcessStartInfo
            {
                Arguments = Properties.Arguments,
                CreateNoWindow = true,
                ErrorDialog = false,
                FileName = Filename,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, Properties.Path),
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            if (!string.IsNullOrEmpty(Properties.OutputEncoding))
            {
                var encoding = Encoding.GetEncoding(Properties.OutputEncoding);
                startInfo.StandardErrorEncoding = encoding;
                startInfo.StandardOutputEncoding = encoding;
            }

            return startInfo;
        }

        private void LogProcessInformation()
        {
            if (_process == null) { return; }
            if (!Properties.LogProcessInformation) { return; }
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

        private bool StartProcess(ProcessStartInfo startInfo, TimeSpan timeout)
        {
            _process = Process.Start(startInfo);
            if (_process == null)
            {
                var filename = Path.Combine(Properties.Path, Properties.Filename);
                throw new ProcessJobException($"could not start process {filename}");
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
    }
}