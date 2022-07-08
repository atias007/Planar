using CommonJob;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;

namespace RunPowerShellJob
{
    [DisallowConcurrentExecution]
    public class PowerShellJob : BaseCommonJob<PowerShellJob>
    {
        public List<string> ScriptFiles { get; set; } = new();
        private List<string> _scripts;

        public override async Task Execute(IJobExecutionContext context)
        {
            var counter = 0;
            try
            {
                var metadata = JobExecutionMetadata.GetInstance(context);

                foreach (var acript in _scripts)
                {
                    using PowerShell ps = PowerShell.Create();
                    ps.AddScript(acript);
                    var output = await ps.InvokeAsync();
                    foreach (PSObject obj in output)
                    {
                        if (obj != null)
                        {
                            metadata.Information.AppendLine(obj.ToString());
                        }
                    }

                    metadata.EffectedRows = metadata.EffectedRows.GetValueOrDefault() + 1;
                    counter++;
                    var value = Convert.ToByte(Math.Floor(100 * (counter * 1.0 / _scripts.Count)));

                    metadata.Progress = value;
                }
            }
            catch (JobExecutionException ex)
            {
                SetJobRunningProperty("Fail", true);
                var jobEx = GetJobExecutingException(ex, context);
                throw jobEx;
            }
            finally
            {
                SetJobRunningProperty("Fail", true);
                FinalizeJob(context);
            }
        }

        private new void Validate()
        {
            try
            {
                base.Validate();

                if (ScriptFiles == null || ScriptFiles.Count == 0)
                {
                    throw new ApplicationException($"Property '{nameof(ScriptFiles)}' is mandatory for job '{GetType().FullName}'");
                }

                _scripts = new List<string>(ScriptFiles.Count);

                foreach (var f in ScriptFiles)
                {
                    var filename = Path.Combine(JobPath, f);
                    if (File.Exists(filename) == false)
                    {
                        throw new ApplicationException($"Script filename '{filename}' could not be found");
                    }

                    var content = File.ReadAllText(filename);
                    _scripts.Add(content);
                }
            }
            catch (Exception ex)
            {
                _scripts.Clear();
                var source = nameof(Validate);
                Logger.Instance.LogError(ex, "Fail at {Source}", source);
                throw;
            }
        }
    }
}