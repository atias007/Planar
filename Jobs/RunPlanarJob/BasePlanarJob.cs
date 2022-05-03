using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;
using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;
using System.Text.Json;
using Planar;
using System.Linq;

namespace RunPlanarJob
{
    public abstract class BasePlanarJob<T> : BaseCommonJob<T>
        where T : class, new()
    {
        public string FileName { get; set; }

        public string TypeName { get; set; }

        private MessageBroker _broker;

        public override async Task Execute(IJobExecutionContext context)
        {
            AssemblyLoadContext assemblyContext = null;

            try
            {
                _broker = new MessageBroker(context);

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

                var instance = assembly.CreateInstance(TypeName);

                // TODO: check that instance has execute function which accept 2 strings and return system.Task
                var method = type.GetMethod("Execute");
                MapJobInstanceProperties(context, type, instance);

                var mapContext = MapContext(context);
                var contextJson = JsonSerializer.Serialize(mapContext);

                var result = method.Invoke(instance, new object[] { contextJson, _broker }) as Task;
                await result;
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

                //// TODO: to be implement
                ////if (ex is PlanarJobAggragateException)
                ////{
                ////    throw new JobExecutionException(message);
                ////}

                throw new JobExecutionException(message, ex);
            }
            finally
            {
                FinalizeJob(context);
                assemblyContext?.Unload();
            }
        }

        private JobExecutionContext MapContext(IJobExecutionContext context)
        {
            var settings = LoadJobSettings();
            var mergeData = context.MergedJobDataMap.ToDictionary(k => k.Key, v => Convert.ToString(v.Value));
            var result = new JobExecutionContext
            {
                JobSettings = settings,
                FireInstanceId = context.FireInstanceId,
            };

            return result;
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