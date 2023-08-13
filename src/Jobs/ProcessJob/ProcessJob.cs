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
    public abstract class ProcessJob : BaseProcessJob<ProcessJob, ProcessJobProperties>
    {
        protected ProcessJob(ILogger<ProcessJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
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
}