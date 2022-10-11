using CommonJob;
using Microsoft.Extensions.Logging;
using Planar;
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
    public class PlanarJob : BaseCommonJob<PlanarJob>
    {
        public string FileName { get; set; }

        public string TypeName { get; set; }

        private string AssemblyFilename { get; set; }

        public override async Task Execute(IJobExecutionContext context)
        {
            AssemblyLoadContext assemblyContext = null;

            try
            {
                MapProperties(context);
                AssemblyFilename = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, JobPath, FileName);

                ValidatePlanarJob();

                assemblyContext = AssemblyLoader.CreateAssemblyLoadContext(context.FireInstanceId, true);
                var assembly = AssemblyLoader.LoadFromAssemblyPath(AssemblyFilename, assemblyContext);

                var type = assembly.GetType(TypeName);
                if (type == null)
                {
                    type = assembly.GetTypes().FirstOrDefault(t => t.FullName == TypeName);
                }

                var method = ValidateBaseJob(type);
                var instance = assembly.CreateInstance(TypeName);

                MapJobInstanceProperties(context, type, instance);

                var settings = LoadJobSettings();
                var _broker = new JobMessageBroker(context, settings);
                await (method.Invoke(instance, new object[] { _broker }) as Task);
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
                base.Validate();

                ValidateMandatoryString(FileName, nameof(FileName));
                ValidateMandatoryString(TypeName, nameof(TypeName));

                if (!File.Exists(AssemblyFilename))
                {
                    throw new PlanarJobException($"Assembly filename '{AssemblyFilename}' could not be found");
                }
            }
            catch (Exception ex)
            {
                var source = nameof(ValidatePlanarJob);
                Logger.LogError(ex, "Fail at {Source}", source);
                throw;
            }
        }

        private MethodInfo ValidateBaseJob(Type type)
        {
            //// ***** Attention: be aware for sync code with Validate on BaseJobTest *****

            if (type == null)
            {
                throw new PlanarJobException($"Type '{TypeName}' could not be found at assembly '{AssemblyFilename}'");
            }

            var baseTypeName = type.BaseType?.FullName;
            if (baseTypeName != $"{nameof(Planar)}.BaseJob")
            {
                throw new PlanarJobException($"Type '{TypeName}' from assembly '{AssemblyFilename}' not inherit 'Planar.Job.BaseJob' type");
            }

            var method = type.GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                throw new PlanarJobException($"Type '{TypeName}' from assembly '{AssemblyFilename}' has no 'Execute' method");
            }

            if (method.ReturnType != typeof(Task))
            {
                throw new PlanarJobException($"Method 'Execute' at type '{TypeName}' from assembly '{AssemblyFilename}' has no 'Task' return type (current return type is {method.ReturnType.FullName})");
            }

            var parameters = method.GetParameters();
            if (parameters?.Length != 1)
            {
                throw new PlanarJobException($"Method 'Execute' at type '{TypeName}' from assembly '{AssemblyFilename}' must have only 1 parameters (current parameters count {parameters?.Length})");
            }

            if (parameters[0].ParameterType.ToString().StartsWith("System.Object") == false)
            {
                throw new PlanarJobException($"Second parameter in method 'Execute' at type '{TypeName}' from assembly '{AssemblyFilename}' must be object. (current type '{parameters[1].ParameterType.Name}')");
            }

            return method;

            //// ***** Attention: be aware for sync code with Validate on BaseJobTest *****
        }
    }
}