using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
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

        protected ProcessJob(ILogger<ProcessJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }

        private string Filename
        {
            get
            {
                return FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, Properties.Path, Properties.Filename);
            }
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            Process? process = null;

            try
            {
                await Initialize(context);

                ValidateProcessJob();

                var startInfo = GetProcessStartInfo();
                process = await StartProcess(startInfo);
                LogProcessInformation(process);
                CheckProcessExitCode(process);
            }
            catch (Exception ex)
            {
                var metadata = JobExecutionMetadata.GetInstance(context);
                metadata.UnhandleException = ex;
            }
            finally
            {
                FinalizeJob(context);
                try { process?.CancelErrorRead(); } catch { }
                try { process?.CancelOutputRead(); } catch { }
                try { process?.Close(); } catch { }
                try { process?.Dispose(); } catch { }
            }
        }

        public void ValidateProcessJob()
        {
            try
            {
                ValidateMandatoryString(Properties.Path, nameof(Properties.Path));
                ValidateMandatoryString(Properties.Filename, nameof(Properties.Filename));

                if (!File.Exists(Filename))
                {
                    throw new PlanarJobException($"process filename '{Filename}' could not be found");
                }
            }
            catch (Exception ex)
            {
                var source = nameof(ValidateProcessJob);
                _logger.LogError(ex, "Fail at {Source}", source);
                throw;
            }
        }

        private static string FormatBytes(long bytes)
        {
            const int scale = 1024;
            string[] orders = new string[] { "gb", "mb", "kb", "bytes" };
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

        private void CheckProcessExitCode(Process process)
        {
            var exitCode = process.ExitCode;
            if (Properties.FailExitCodes.Any())
            {
                if (Properties.FailExitCodes.Any(f => f == exitCode))
                {
                    var codes = string.Join(',', Properties.FailExitCodes);
                    throw new ProcessJobException($"The process ended with exit code {exitCode} which is one of fail exit codes ({codes})");
                }

                return;
            }

            if (Properties.SuccessExitCodes.Any())
            {
                if (!Properties.SuccessExitCodes.Any(s => s == exitCode))
                {
                    var codes = string.Join(',', Properties.SuccessExitCodes);
                    throw new ProcessJobException($"The process ended with exit code {exitCode} which is not one of success exit codes ({codes})");
                }

                return;
            }

            if (!string.IsNullOrEmpty(Properties.FailOutputRegex))
            {
                var output = _output.ToString();
                var regex = new Regex(Properties.FailOutputRegex, RegexOptions.None, TimeSpan.FromSeconds(5));
                if (regex.IsMatch(output))
                {
                    throw new ProcessJobException($"The process ended with an output that matched the fail output message '{Properties.SuccessOutputRegex}'");
                }

                return;
            }

            if (!string.IsNullOrEmpty(Properties.SuccessOutputRegex))
            {
                var output = _output.ToString();
                var regex = new Regex(Properties.SuccessOutputRegex, RegexOptions.None, TimeSpan.FromSeconds(5));
                if (!regex.IsMatch(output))
                {
                    throw new ProcessJobException($"The process ended with an output that not matched the success output message '{Properties.SuccessOutputRegex}'");
                }

                return;
            }

            if (exitCode != 0)
            {
                throw new ProcessJobException($"The process ended with exit code {exitCode} which is different from success exit code 0");
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

        private void LogProcessInformation(Process process)
        {
            if (!Properties.LogProcessInformation) { return; }

            MessageBroker.AppendLog(LogLevel.Information, _seperator);
            MessageBroker.AppendLog(LogLevel.Information, " - Process information:");
            MessageBroker.AppendLog(LogLevel.Information, _seperator);
            MessageBroker.AppendLog(LogLevel.Information, $"ExitCode: {process.ExitCode}");
            MessageBroker.AppendLog(LogLevel.Information, $"StartTime: {process.StartTime}");
            MessageBroker.AppendLog(LogLevel.Information, $"ExitTime: {process.ExitTime}");
            MessageBroker.AppendLog(LogLevel.Information, $"Id: {process.Id}");
            MessageBroker.AppendLog(LogLevel.Information, $"PeakPagedMemorySize64: {FormatBytes(_peakPagedMemorySize64)}");
            MessageBroker.AppendLog(LogLevel.Information, $"PeakWorkingSet64: {FormatBytes(_peakWorkingSet64)}");
            MessageBroker.AppendLog(LogLevel.Information, $"PeakVirtualMemorySize64: {FormatBytes(_peakVirtualMemorySize64)}");
            MessageBroker.AppendLog(LogLevel.Information, _seperator);
        }

        private async Task<Process> StartProcess(ProcessStartInfo startInfo)
        {
            Process? process = Process.Start(startInfo);
            if (process == null)
            {
                var filename = Path.Combine(Properties.Path, Properties.Filename);
                throw new PlanarJobException($"Could not start process {filename}");
            }

            process.EnableRaisingEvents = true;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.OutputDataReceived += (sender, eventArgs) =>
            {
                if (string.IsNullOrEmpty(eventArgs.Data)) { return; }
                _output.AppendLine(eventArgs.Data);
                MessageBroker.AppendLog(LogLevel.Information, eventArgs.Data);
                UpdatePeakVariables(process);
            };

            process.ErrorDataReceived += (sender, eventArgs) =>
            {
                if (string.IsNullOrEmpty(eventArgs.Data)) { return; }
                _output.AppendLine(eventArgs.Data);
                MessageBroker.AppendLog(LogLevel.Error, eventArgs.Data);
                UpdatePeakVariables(process);
            };

            if (Properties.Timeout.HasValue && Properties.Timeout != TimeSpan.Zero)
            {
                process.WaitForExit(Convert.ToInt32(Properties.Timeout.Value.TotalMilliseconds));
            }
            else
            {
                await process.WaitForExitAsync();
            }

            return process;
        }

        private void UpdatePeakVariables(Process process)
        {
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