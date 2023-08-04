using Microsoft.Extensions.Logging;
using Planar;
using Planar.Common;
using Planar.Common.Exceptions;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Timers;

namespace CommonJob
{
    public abstract class BaseProcessJob<TInstance, TProperties> : BaseCommonJob<TInstance, TProperties>
    where TInstance : class
    where TProperties : class, new()
    {
        protected long _peakPagedMemorySize64;
        protected long _peakVirtualMemorySize64;
        protected long _peakWorkingSet64;
        protected Process? _process;
        protected bool _processKilled;
        protected readonly Timer _processMetricsTimer = new(1000);
        protected static readonly string _seperator = string.Empty.PadLeft(40, '-');
        private readonly object Locker = new();

        protected BaseProcessJob(ILogger<TInstance> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }

        private string? _filename;

        protected string Filename
        {
            get
            {
                _filename ??= FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, Properties.Path, Properties.Filename);

                return _filename;
            }
        }

        protected void FinalizeProcess()
        {
            try { _process?.CancelErrorRead(); } catch { DoNothingMethod(); }
            try { _process?.CancelOutputRead(); } catch { DoNothingMethod(); }
            try { _process?.Close(); } catch { DoNothingMethod(); }
            try { _process?.Dispose(); } catch { DoNothingMethod(); }
            try { if (_process != null) { _process.EnableRaisingEvents = false; } } catch { DoNothingMethod(); }
            try { if (_process != null) { _process.OutputDataReceived -= ProcessOutputDataReceived; } } catch { DoNothingMethod(); }
            try { if (_process != null) { _process.ErrorDataReceived -= ProcessErrorDataReceived; } } catch { DoNothingMethod(); }
            try { if (_processMetricsTimer != null) { _processMetricsTimer.Elapsed -= MetricsTimerElapsed; } } catch { DoNothingMethod(); }
        }

        protected void ValidateProcessJob(IFileJobProperties properties)
        {
            try
            {
                ValidateMandatoryString(properties.Path, nameof(properties.Path));
                ValidateMandatoryString(properties.Filename, nameof(properties.Filename));

                if (!File.Exists(Filename))
                {
                    throw new PlanarException($"process filename '{Filename}' could not be found");
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

        protected void OnCancel()
        {
            Kill("request for cancel process");
        }

        protected void OnTimeout()
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

        protected static string FormatBytes(long bytes)
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

        protected ProcessStartInfo GetProcessStartInfo(IPathJobProperties properties)
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                ErrorDialog = false,
                FileName = Filename,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, properties.Path),
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            return startInfo;
        }

        protected bool StartProcess(ProcessStartInfo startInfo, TimeSpan timeout)
        {
            _process = Process.Start(startInfo);
            if (_process == null)
            {
                var filename = Path.Combine(Properties.Path, Properties.Filename);
                throw new PlanarException($"could not start process {filename}");
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

        protected void UpdatePeakVariables(Process? process)
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
    }
}