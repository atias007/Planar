using CommonJob;
using Microsoft.Extensions.Logging;
using Planner.Client;
using Planner.Common;
using Quartz;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RunPlannerJob
{
    public abstract class BasePlannerJob<T> : BaseCommonJob<T>
        where T : class, new()
    {
        public string FileName { get; set; }

        public string TypeName { get; set; }

        public override async Task Execute(IJobExecutionContext context)
        {
            try
            {
                MapProperties(context);

                Validate();

                var assemblyFilename = Path.Combine(JobPath, FileName);
                var assembly = AssemblyLoader.LoadFromAssemblyPath(assemblyFilename);
                var type = assembly.GetType(TypeName);

                if (type == null)
                {
                    throw new ApplicationException($"Type {TypeName} could not be found at assembly '{assemblyFilename}'");
                }

                if (Activator.CreateInstance(type) is not BaseJob instance)
                {
                    throw new ApplicationException($"Fail to create instance of job {type.FullName}");
                }

                LoadJobSettings(instance);
                MapJobInstanceProperties(context, type, instance);

                await instance.Execute(context);
            }
            catch (JobExecutionException ex)
            {
                SetJobRunningProperty("Fail", true);
                var message = $"FireInstanceId {context.FireInstanceId} throw JobExecutionException with message {ex.Message}";
                var jobException = new JobExecutionException(ex)
                {
                    RefireImmediately = ex.RefireImmediately,
                    Source = ex.Source,
                    UnscheduleAllTriggers = ex.UnscheduleAllTriggers,
                    UnscheduleFiringTrigger = ex.UnscheduleFiringTrigger,
                };

                throw jobException;
            }
            catch (Exception ex)
            {
                SetJobRunningProperty("Fail", true);
                var message = $"FireInstanceId {context.FireInstanceId} throw exception with message {ex.Message}";
                throw new JobExecutionException(message, ex);
            }
            finally
            {
                FinalizeJob(context);
            }
        }

        private new void Validate()
        {
            try
            {
                base.Validate();

                ValidateMandatoryString(FileName, nameof(FileName));
                ValidateMandatoryString(TypeName, nameof(TypeName));
                var assemblyFilename = Path.Combine(JobPath, FileName);

                if (File.Exists(assemblyFilename) == false)
                {
                    throw new ApplicationException($"Assembly file name '{assemblyFilename}' could not be found");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Fail at {nameof(BasePlannerJob<T>)}.{nameof(Validate)}", ex);
                throw;
            }
        }
    }
}