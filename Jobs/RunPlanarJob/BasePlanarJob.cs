using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace RunPlanarJob
{
    public abstract class BasePlanarJob<T> : BaseCommonJob<T>
        where T : class, new()
    {
        public string FileName { get; set; }

        public string TypeName { get; set; }

        private JobMessageBroker _broker;

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
                    type = assembly.GetTypes().FirstOrDefault(t => t.FullName == TypeName);
                }
                Validate(type);

                var method = type.GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance);
                var instance = assembly.CreateInstance(TypeName);

                MapJobInstanceProperties(context, type, instance);

                var settings = LoadJobSettings();
                _broker = new JobMessageBroker(context, settings);
                var result = method.Invoke(instance, new object[] { _broker }) as Task;
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
            //// ***** Attention: be aware for sync code with Validate on BaseJobTest *****

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
            if (parameters?.Length != 1)
            {
                var assemblyFilename = Path.Combine(JobPath, FileName);
                throw new ApplicationException($"Method 'Execute' at type '{TypeName}' from assembly '{assemblyFilename}' must have only 1 parameters (current parameters count {parameters?.Length})");
            }

            if (parameters[0].ParameterType.ToString().StartsWith("System.Object") == false)
            {
                var assemblyFilename = Path.Combine(JobPath, FileName);
                throw new ApplicationException($"Second parameter in method 'Execute' at type '{TypeName}' from assembly '{assemblyFilename}' must be object. (current type '{parameters[1].ParameterType.Name}')");
            }

            return method;

            //// ***** Attention: be aware for sync code with Validate on BaseJobTest *****
        }
    }
}