using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Planar.Common;
using Quartz;
using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace RunPlanarJob
{
    public abstract class BasePlanarJob<T> : BaseCommonJob<T>
        where T : class, new()
    {
        public string FileName { get; set; }

        public string TypeName { get; set; }

        public override async Task Execute(IJobExecutionContext context)
        {
            AssemblyLoadContext assemblyContext = null;

            try
            {
                MapProperties(context);

                Validate();

                var assemblyFilename = Path.Combine(JobPath, FileName);
                assemblyContext = AssemblyLoader.CreateAssemblyLoadContext(context.FireInstanceId, true);
                var assembly = AssemblyLoader.LoadFromAssemblyPath(assemblyFilename, assemblyContext);
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
                ThrowJobExecutingException(ex, context);
            }
            catch (Exception ex)
            {
                SetJobRunningProperty("Fail", true);
                var message = $"FireInstanceId {context.FireInstanceId} throw exception with message {ex.Message}";

                if (ex is PlanarJobAggragateException)
                {
                    throw new JobExecutionException(message);
                }

                throw new JobExecutionException(message, ex);
            }
            finally
            {
                FinalizeJob(context);
                assemblyContext?.Unload();
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
                    throw new ApplicationException($"Assembly filename '{assemblyFilename}' could not be found");
                }
            }
            catch (Exception ex)
            {
                var source = nameof(Validate);
                Logger.Instance.LogError(ex, "Fail at {@source}", source);
                throw;
            }
        }
    }
}