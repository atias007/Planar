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
using System.Reflection;

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

                // TODO: validate the type and the method variables
                var type = assembly.GetType(TypeName);
                Validate(type);

                // TODO: validate the type ang the method variables
                var method = type.GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance);
                var instance = assembly.CreateInstance(TypeName);

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

                if (ex.GetType().Name == "PlanarJobAggragateException")
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

        private JobExecutionContext MapContext(IJobExecutionContext context)
        {
            var settings = LoadJobSettings();
            var mergeData = context.MergedJobDataMap.ToDictionary(k => k.Key, v => Convert.ToString(v.Value));
            var result = new JobExecutionContext
            {
                JobSettings = settings,
                FireInstanceId = context.FireInstanceId,
                FireTime = context.FireTimeUtc,
                NextFireTime = context.NextFireTimeUtc,
                PreviousFireTime = context.PreviousFireTimeUtc,
                Recovering = context.Recovering,
                RefireCount = context.RefireCount,
                ScheduledFireTime = context.ScheduledFireTimeUtc
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

        private MethodInfo Validate(Type type)
        {
            if (type == null)
            {
                var assemblyFilename = Path.Combine(JobPath, FileName);
                throw new ApplicationException($"Type '{TypeName}' could not be found at assembly '{assemblyFilename}'");
            }

            var baseTypeName = type.BaseType?.FullName;
            if (baseTypeName != "Planar.Job.BaseJob")
            {
                var assemblyFilename = Path.Combine(JobPath, FileName);
                throw new ApplicationException($"Type '{TypeName}' from assembly '{assemblyFilename}' not inherit 'Planar.Job.BaseJob' type");
            }

            var method = type.GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                var assemblyFilename = Path.Combine(JobPath, FileName);
                throw new ApplicationException($"Type '{TypeName}' from assembly '{assemblyFilename}' has no 'Execute' method");
            }

            if (method.ReturnType != typeof(Task))
            {
                var assemblyFilename = Path.Combine(JobPath, FileName);
                throw new ApplicationException($"Method 'Execute' at type '{TypeName}' from assembly '{assemblyFilename}' has no 'Task' return type (current return type is {method.ReturnType.FullName})");
            }

            var parameters = method.GetParameters();
            if (parameters?.Length != 2)
            {
                var assemblyFilename = Path.Combine(JobPath, FileName);
                throw new ApplicationException($"Method 'Execute' at type '{TypeName}' from assembly '{assemblyFilename}' must have only 2 parameters (current parameters count {parameters?.Length})");
            }

            if (parameters[0].ParameterType != typeof(string))
            {
                var assemblyFilename = Path.Combine(JobPath, FileName);
                throw new ApplicationException($"First parameter in method 'Execute' at type '{TypeName}' from assembly '{assemblyFilename}' must be string. (current type '{parameters[0].ParameterType.Name}')");
            }

            if (parameters[1].ParameterType.ToString().StartsWith("System.Object") == false)
            {
                var assemblyFilename = Path.Combine(JobPath, FileName);
                throw new ApplicationException($"Second parameter in method 'Execute' at type '{TypeName}' from assembly '{assemblyFilename}' must be object. (current type '{parameters[1].ParameterType.Name}')");
            }

            return method;
        }
    }
}