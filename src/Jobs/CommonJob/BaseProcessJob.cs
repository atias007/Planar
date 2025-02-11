using Microsoft.Extensions.Logging;
using Planar;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Service.General;
using Quartz;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace CommonJob;

public abstract class BaseProcessJob<TProperties> : BaseCommonJob<TProperties>
    where TProperties : class, new()
{
    protected Process? _process;
    protected bool _processKilled;
    private readonly StringBuilder _output = new();
    private readonly Timer _processMetricsTimer = new(1000);
    private readonly Lock Locker = new();
    private string? _filename;
    private bool _listenOutput = true;
    private long _peakWorkingSet64;
    private long _peakVirtualMemorySize64;
    private string? _outputText;

    protected BaseProcessJob(
        ILogger logger,
        IJobPropertyDataLayer dataLayer,
        JobMonitorUtil jobMonitorUtil,
        IClusterUtil clusterUtil) : base(logger, dataLayer, jobMonitorUtil, clusterUtil)
    {
        if (Properties is not IFileJobProperties)
        {
            throw new PlanarException($"Job type '{Properties.GetType()}' does not implement '{nameof(IFileJobProperties)}'");
        }
    }

    protected string FinalOutputText
    {
        get
        {
            _outputText ??= _output.ToString();
            return _outputText ?? string.Empty;
        }
    }

    protected string Filename
    {
        get
        {
            _filename ??= FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, FileProperties.Path, FileProperties.Filename);

            return _filename;
        }
    }

    protected IFileJobProperties FileProperties =>
        Properties as IFileJobProperties ??
        throw new PlanarException($"Job type '{Properties.GetType()}' does not implement '{nameof(IFileJobProperties)}'");

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

    protected void FinalizeProcess()
    {
        SafeInvoke(() => _process?.CancelErrorRead());
        SafeInvoke(() => _process?.CancelOutputRead());
        SafeInvoke(() => _process?.Close());
        SafeInvoke(() => _process?.Dispose());
        SafeInvoke(() => { if (_process != null) { _process.EnableRaisingEvents = false; } });
        SafeUnsubscribeOutput();
        SafeInvoke(() => { if (_processMetricsTimer != null) { _processMetricsTimer.Elapsed -= MetricsTimerElapsed; } });
    }

    protected virtual ProcessStartInfo GetProcessStartInfo()
    {
        var startInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            ErrorDialog = false,
            FileName = Filename,
            UserName = FileProperties.UserName,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, FileProperties.Path),
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo.Domain = FileProperties.Domain;
            startInfo.Password = ToSecureString(FileProperties.Password);
        }

        return startInfo;
    }

    private static SecureString? ToSecureString(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return null;
        }

        var secureString = new SecureString();
        try
        {
            foreach (char c in plainText)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly(); // Make the string read-only for security
            return secureString;
        }
        finally
        {
            secureString.Dispose();
        }
    }

    protected void LogProcessInformation()
    {
        if (_process == null) { return; }
        if (!_process.HasExited) { return; }

        MessageBroker.AppendLog(LogLevel.Information, Seperator);
        MessageBroker.AppendLog(LogLevel.Information, " process information:");
        MessageBroker.AppendLog(LogLevel.Information, Seperator);
        MessageBroker.AppendLog(LogLevel.Information, $"exit Code: {_process.ExitCode}");
        MessageBroker.AppendLog(LogLevel.Information, $"peak Working Set Memory: {FormatBytes(_peakWorkingSet64)}");
        MessageBroker.AppendLog(LogLevel.Information, $"peak Virtual Memory: {FormatBytes(_peakVirtualMemorySize64)}");

        var username = string.IsNullOrWhiteSpace(FileProperties.UserName) ?
            GetUsername(Environment.UserDomainName, Environment.UserName) :
            GetUsername(FileProperties.Domain, FileProperties.UserName);

        MessageBroker.AppendLog(LogLevel.Information, $"username: {username}");
        MessageBroker.AppendLog(LogLevel.Information, Seperator);
        Thread.Sleep(500);
    }

    private static string GetUsername(string? domain, string username)
    {
        return string.IsNullOrWhiteSpace(domain) ? username : $"{domain}\\{username}";
    }

    protected void OnCancel()
    {
        Kill("request for cancel process");
    }

    protected void OnTimeout(IJobExecutionContext context)
    {
        SafeScan(MonitorEvents.ExecutionTimeout, context);
        Kill("timeout expire");
    }

    protected bool StartProcess(ProcessStartInfo startInfo, TimeSpan timeout)
    {
        _process = Process.Start(startInfo);
        if (_process == null)
        {
            var filename =
                string.IsNullOrWhiteSpace(FileProperties.Path) ?
                FileProperties.Filename :
                Path.Combine(FileProperties.Path, FileProperties.Filename);

            throw new PlanarException($"could not start process {filename}");
        }

        _process.EnableRaisingEvents = true;
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        if (_listenOutput) { _process.OutputDataReceived += ProcessOutputDataReceived; }
        if (_listenOutput) { _process.ErrorDataReceived += ProcessOutputDataReceived; }
        _processMetricsTimer.Elapsed += MetricsTimerElapsed;
        _processMetricsTimer.Start();

        _process.WaitForExit(Convert.ToInt32(timeout.TotalMilliseconds));
        if (!_process.HasExited)
        {
            MessageBroker.AppendLog(LogLevel.Error, $"process timeout expire. Timeout was {timeout:hh\\:mm\\:ss}");
            return false;
        }

        return true;
    }

    protected void SafeUnsubscribeOutput()
    {
        if (_process == null) { return; }
        if (!_listenOutput) { return; }
        SafeInvoke(() => _process.ErrorDataReceived -= ProcessOutputDataReceived);
        SafeInvoke(() => _process.OutputDataReceived -= ProcessOutputDataReceived);
        _listenOutput = false;
    }

    protected void ValidateProcessJob()
    {
        try
        {
            ValidateMandatoryString(FileProperties.Filename, nameof(FileProperties.Filename));

            if (!File.Exists(Filename))
            {
                throw new PlanarException($"process filename '{Filename}' could not be found");
            }
        }
        catch (Exception ex)
        {
            var source = nameof(ValidateProcessJob);
            _logger.LogError(ex, "fail at {Source}. Message: {Message}", source, ex.Message);
            MessageBroker.AppendLog(LogLevel.Error, $"fail at {source}. {ex.Message}");
            throw new CommonJobException($"fail at {source}", ex);
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
            MessageBroker.AppendLog(LogLevel.Warning, $"process was stopped. Reason: {reason}");
            _processKilled = true;
            _process.Kill(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to kill process job {Filename}", _process.StartInfo.FileName);
            MessageBroker.AppendLog(LogLevel.Error, $"fail to kill process job {_process.StartInfo.FileName}. {ex.Message}");
        }
    }

    private void MetricsTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        UpdatePeakVariables(_process);
    }

    private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        if (string.IsNullOrEmpty(eventArgs.Data)) { return; }
        _output.AppendLine(eventArgs.Data);
    }

    private void UpdatePeakVariables(Process? process)
    {
        if (process == null) { return; }
        if (process.HasExited) { return; }

        SafeInvoke(() =>
        {
            lock (Locker)
            {
                _peakWorkingSet64 = process.PeakWorkingSet64;
            }
        });

        SafeInvoke(() =>
        {
            lock (Locker)
            {
                _peakVirtualMemorySize64 = process.PeakVirtualMemorySize64;
            }
        });
    }
}