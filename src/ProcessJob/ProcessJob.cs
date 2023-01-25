using CommonJob;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Common;
using Quartz;
using System.Diagnostics;
using System.Text;

namespace Planar
{
    public abstract class ProcessJob : BaseCommonJob<ProcessJob, ProcessJobProperties>
    {
        private long _peakPagedMemorySize64;
        private long _peakVirtualMemorySize64;
        private long _peakWorkingSet64;

        protected ProcessJob(ILogger<ProcessJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            Process? process = null;
            
            try
            {
                await Initialize(context);

                // TODO:  ValidatePlanarJob();

                var startInfo = GetProcessStartInfo();
                process = await StartProcess(startInfo);
                LogProcessInformation(process);
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
                process?.CancelErrorRead();
                process?.CancelOutputRead();
                process?.Close();
                process?.Dispose();
            }
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
                MessageBroker.AppendLog(LogLevel.Information, eventArgs.Data);
                UpdatePeakVariables(process);
            };

            process.ErrorDataReceived += (sender, eventArgs) =>
            {
                if (string.IsNullOrEmpty(eventArgs.Data)) { return; }
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

        private ProcessStartInfo GetProcessStartInfo()
        {
            var startInfo = new ProcessStartInfo
            {
                Arguments = Properties.Arguments,
                CreateNoWindow = true,
                ErrorDialog = false,
                FileName = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, Properties.Path, Properties.Filename),
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = FolderConsts.GetAbsoluteSpecialFilePath(PlanarSpecialFolder.Jobs, Properties.Path),
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

        private static readonly string _seperator = string.Empty.PadLeft(80, '-');

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
            MessageBroker.AppendLog(LogLevel.Information, $"PeakPagedMemorySize64: {_peakPagedMemorySize64}");
            MessageBroker.AppendLog(LogLevel.Information, $"PeakVirtualMemorySize64: {_peakVirtualMemorySize64}");
            MessageBroker.AppendLog(LogLevel.Information, $"PeakWorkingSet64: {_peakWorkingSet64}");
            MessageBroker.AppendLog(LogLevel.Information, _seperator);
        }

        private void CheckProcessExitCode()
        {
        }
    }
}