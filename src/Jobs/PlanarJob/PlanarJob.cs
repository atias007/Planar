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

namespace Planar
{
    public abstract class PlanarJob : BaseCommonJob<PlanarJob, PlanarJobProperties>
    {
        protected PlanarJob(ILogger<PlanarJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }

        private string? AssemblyFilename { get; set; }

        public override async Task Execute(IJobExecutionContext context)
        {
            AssemblyLoadContext? assemblyContext = null;
            Type? type = null;
            object? instance = null;

            try
            {
                await Initialize(context);
                AssemblyFilename = FolderConsts.GetSpecialFilePath(
                    PlanarSpecialFolder.Jobs,
                    Properties.Path ?? string.Empty,
                    Properties.Filename ?? string.Empty);

                ValidatePlanarJob();
                if (Properties.ClassName == null) { return; }

                assemblyContext = AssemblyLoader.CreateAssemblyLoadContext(context.FireInstanceId, true);
                var assembly =
                    AssemblyLoader.LoadFromAssemblyPath(AssemblyFilename, assemblyContext) ??
                    throw new PlanarJobException($"could not load assembly {AssemblyFilename}");

                type = assembly.GetType(Properties.ClassName);
                if (type == null)
                {
                    type = Array.Find(assembly.GetTypes(), t => t.FullName == Properties.ClassName);
                }

                if (type == null)
                {
                    throw new PlanarJobException($"could not load type {Properties.ClassName} from assembly {AssemblyFilename}");
                }

                var method = ValidateBaseJob(type);
                instance = assembly.CreateInstance(Properties.ClassName);
                if (instance == null)
                {
                    throw new PlanarJobException($"could not create {Properties.ClassName} from assembly {AssemblyFilename}");
                }

                MapJobInstanceProperties(context, type, instance);

                if (method.Invoke(instance, new object[] { MessageBroker }) is not Task task)
                {
                    throw new PlanarJobException($"method {method.Name} invoked but not return System.Task type");
                }

                await WaitForJobTask(context, task);
            }
            catch (Exception ex)
            {
                var metadata = JobExecutionMetadata.GetInstance(context);
                metadata.UnhandleException = ex;
            }
            finally
            {
                MapJobInstancePropertiesBack(context, type, instance);
                FinalizeJob(context);
                assemblyContext?.Unload();
            }
        }

        private void ValidatePlanarJob()
        {
            try
            {
                ValidateMandatoryString(Properties.Path, nameof(Properties.Path));
                ValidateMandatoryString(Properties.Filename, nameof(Properties.Filename));
                ValidateMandatoryString(Properties.ClassName, nameof(Properties.ClassName));

                if (!File.Exists(AssemblyFilename))
                {
                    throw new PlanarJobException($"assembly filename '{AssemblyFilename}' could not be found");
                }
            }
            catch (Exception ex)
            {
                var source = nameof(ValidatePlanarJob);
                _logger.LogError(ex, "Fail at {Source}", source);
                throw;
            }
        }

        private MethodInfo ValidateBaseJob(Type type)
        {
            //// ***** Attention: be aware for sync code with Validate on BaseJobTest *****

            if (type == null)
            {
                throw new PlanarJobException($"type '{Properties.ClassName}' could not be found at assembly '{AssemblyFilename}'");
            }

            ////var baseTypeName = type.BaseType?.FullName;
            ////if (baseTypeName != $"{nameof(Planar)}.BaseJob")
            ////{
            ////    throw new PlanarJobException($"type '{Properties.ClassName}' from assembly '{AssemblyFilename}' not inherit 'Planar.Job.BaseJob' type");
            ////}

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            var method =
                type.GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance) ??
                throw new PlanarJobException($"type '{Properties.ClassName}' from assembly '{AssemblyFilename}' has no 'Execute' method");
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

            if (method.ReturnType != typeof(Task))
            {
                throw new PlanarJobException($"method 'Execute' at type '{Properties.ClassName}' from assembly '{AssemblyFilename}' has no 'Task' return type (current return type is {method.ReturnType.FullName})");
            }

            var parameters = method.GetParameters();
            if (parameters?.Length != 1)
            {
                throw new PlanarJobException($"method 'Execute' at type '{Properties.ClassName}' from assembly '{AssemblyFilename}' must have only 1 parameters (current parameters count {parameters?.Length})");
            }

            if (!parameters[0].ParameterType.ToString().StartsWith("System.Object"))
            {
                throw new PlanarJobException($"first parameter in method 'Execute' at type '{Properties.ClassName}' from assembly '{AssemblyFilename}' must be object. (current type '{parameters[0].ParameterType.Name}')");
            }

            return method;

            //// ***** Attention: be aware for sync code with Validate on BaseJobTest *****
        }
    }
}