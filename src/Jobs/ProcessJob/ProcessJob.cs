using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.General;
using ProcessJob;
using Quartz;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Planar;

public abstract class ProcessJob : BaseProcessJob<ProcessJobProperties>
{
    protected ProcessJob(
        ILogger logger,
        IJobPropertyDataLayer dataLayer,
        JobMonitorUtil jobMonitorUtil,
        IClusterUtil clusterUtil) : base(logger, dataLayer, jobMonitorUtil, clusterUtil)
    {
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await Initialize(context);

            ValidateProcessJob();
            context.CancellationToken.Register(OnCancel);

            var timeout = TriggerHelper.GetTimeoutWithDefault(context.Trigger);
            var startInfo = GetProcessStartInfo();
            StartMonitorDuration(context);
            var success = StartProcess(startInfo, timeout);
            StopMonitorDuration();
            if (!success)
            {
                OnTimeout(context);
            }

            ExtractOutputData();
            LogProcessOutput();
            LogProcessInformation();
            CheckProcessExitCode();
        }
        catch (Exception ex)
        {
            HandleException(context, ex);
        }
        finally
        {
            await FinalizeJob(context);
            FinalizeProcess();
        }
    }

    protected void LogProcessOutput()
    {
        MessageBroker.AppendLog(LogLevel.Information, Seperator);
        MessageBroker.AppendLog(LogLevel.Information, " process output:");
        MessageBroker.AppendLog(LogLevel.Information, Seperator);
        if (Properties.LogOutput)
        {
            MessageBroker.AppendLogRaw(FinalOutputText);
        }
        else
        {
            MessageBroker.AppendLog(LogLevel.Information, "process output logging is disabled");
        }
    }

    private void ExtractOutputData()
    {
        var extractor = new OutputExtractor(FinalOutputText, _logger);
        SetProcessEffectedRows(extractor);
    }

    private void SetProcessEffectedRows(OutputExtractor extractor)
    {
        var rows = extractor.GetEffectedRows();
        if (rows.HasValue)
        {
            MessageBroker.SetEffectedRows(rows.Value);
        }
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
            var regex = new Regex(Properties.FailOutputRegex, RegexOptions.None, TimeSpan.FromSeconds(5));
            if (regex.IsMatch(FinalOutputText))
            {
                throw new ProcessJobException($"process '{Filename}' ended with an output that matched the fail output message '{Properties.FailOutputRegex}'");
            }

            return;
        }

        if (!string.IsNullOrEmpty(Properties.SuccessOutputRegex))
        {
            var regex = new Regex(Properties.SuccessOutputRegex, RegexOptions.None, TimeSpan.FromSeconds(5));
            if (!regex.IsMatch(FinalOutputText))
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

    protected override ProcessStartInfo GetProcessStartInfo()
    {
        var startInfo = base.GetProcessStartInfo();
        startInfo.Arguments = Properties.Arguments;

        if (!string.IsNullOrEmpty(Properties.OutputEncoding))
        {
            var encoding = Encoding.GetEncoding(Properties.OutputEncoding);
            startInfo.StandardErrorEncoding = encoding;
            startInfo.StandardOutputEncoding = encoding;
        }

        return startInfo;
    }
}