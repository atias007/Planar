using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;
using Quartz.Logging;
using System.Diagnostics;
using System.Runtime.Loader;
using System.Security;
using System.Text;

namespace Planar
{
    public abstract class ProcessJob : BaseCommonJob<ProcessJob, ProcessJobProperties>
    {
        protected ProcessJob(ILogger<ProcessJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            Process? process = null;

            try
            {
                await SetProperties(context);

                // TODO:  ValidatePlanarJob();

                var startInfo = GetProcessStartInfo();
                process = Process.Start(startInfo);

                if (process == null)
                {
                    var filename = Path.Combine(Properties.Path, Properties.Filename);
                    throw new PlanarJobException($"Could not start process {filename}");
                }

                if (Properties.WaitForExit)
                {
                    process.EnableRaisingEvents = true;
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.OutputDataReceived += (sender, eventArgs) =>
                    {
                        _logger.LogInformation(eventArgs.Data);
                    };

                    process.ErrorDataReceived += (sender, eventArgs) =>
                    {
                        _logger.LogError(eventArgs.Data);
                    };

                    if (Properties.Timeout.HasValue && Properties.Timeout != TimeSpan.Zero)
                    {
                        process.WaitForExit(Convert.ToInt32(Properties.Timeout.Value.TotalMilliseconds));
                    }
                    else
                    {
                        await process.WaitForExitAsync();
                    }
                }
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

        private ProcessStartInfo GetProcessStartInfo()
        {
            var startInfo = new ProcessStartInfo
            {
                Arguments = Properties.Arguments,
                CreateNoWindow = true,
                ErrorDialog = false,
                FileName = Properties.Filename,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, Properties.Path)
            };

            if (Properties.WaitForExit)
            {
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
            }

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