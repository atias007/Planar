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

        private string AssemblyFilename { get; set; }

        public override async Task Execute(IJobExecutionContext context)
        {
            AssemblyLoadContext assemblyContext = null;

            try
            {
                await SetProperties(context);
                AssemblyFilename = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, Properties.Path, Properties.Filename);

                ValidatePlanarJob();

                assemblyContext = AssemblyLoader.CreateAssemblyLoadContext(context.FireInstanceId, true);
                var assembly = AssemblyLoader.LoadFromAssemblyPath(AssemblyFilename, assemblyContext);

                var type = assembly.GetType(Properties.ClassName);
                if (type == null)
                {
                    type = assembly.GetTypes().FirstOrDefault(t => t.FullName == Properties.ClassName);
                }

                var method = ValidateBaseJob(type);
                var instance = assembly.CreateInstance(Properties.ClassName);

                MapJobInstanceProperties(context, type, instance);

                var settings = LoadJobSettings(Properties.Path);
                var _broker = new JobMessageBroker(context, settings);
                await (method.Invoke(instance, new object[] { _broker }) as Task);

                MapJobInstancePropertiesBack(context, type, instance);
            }
            catch (Exception ex)
            {
                var metadata = JobExecutionMetadata.GetInstance(context);
                metadata.UnhandleException = ex;
            }
            finally
            {
                FinalizeJob(context);
                assemblyContext?.Unload();
            }
        }

        private void ValidatePlanarJob()
        {
            try
            {
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

            var baseTypeName = type.BaseType?.FullName;
            if (baseTypeName != $"{nameof(Planar)}.BaseJob")
            {
                throw new PlanarJobException($"type '{Properties.ClassName}' from assembly '{AssemblyFilename}' not inherit 'Planar.Job.BaseJob' type");
            }

            var method = type.GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                throw new PlanarJobException($"type '{Properties.ClassName}' from assembly '{AssemblyFilename}' has no 'Execute' method");
            }

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
                throw new PlanarJobException($"second parameter in method 'Execute' at type '{Properties.ClassName}' from assembly '{AssemblyFilename}' must be object. (current type '{parameters[1].ParameterType.Name}')");
            }

            return method;

            //// ***** Attention: be aware for sync code with Validate on BaseJobTest *****
        }
    }
}