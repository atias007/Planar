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

        private JobMessageBroker _broker;

        private string AssemblyFilename { get; set; }

        public override async Task Execute(IJobExecutionContext context)
        {
            AssemblyLoadContext assemblyContext = null;

            try
            {
                MapProperties(context);
                AssemblyFilename = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, JobPath, FileName);

                Validate();

                assemblyContext = AssemblyLoader.CreateAssemblyLoadContext(context.FireInstanceId, true);
                var assembly = AssemblyLoader.LoadFromAssemblyPath(AssemblyFilename, assemblyContext);

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

        private new void Validate()
        {
            try
            {
                base.Validate();

                ValidateMandatoryString(FileName, nameof(FileName));
                ValidateMandatoryString(TypeName, nameof(TypeName));

                if (File.Exists(AssemblyFilename) == false)
                {
                    throw new ApplicationException($"Assembly filename '{AssemblyFilename}' could not be found");
                }
            }
            catch (Exception ex)
            {
                var source = nameof(Validate);
                Logger.Instance.LogError(ex, "Fail at {Source}", source);
                throw;
            }
        }

        private MethodInfo Validate(Type type)
        {
            //// ***** Attention: be aware for sync code with Validate on BaseJobTest *****

            if (type == null)
            {
                throw new ApplicationException($"Type '{TypeName}' could not be found at assembly '{AssemblyFilename}'");
            }

            var baseTypeName = type.BaseType?.FullName;
            if (baseTypeName != "Planar.BaseJob")
            {
                throw new ApplicationException($"Type '{TypeName}' from assembly '{AssemblyFilename}' not inherit 'Planar.Job.BaseJob' type");
            }

            var method = type.GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                throw new ApplicationException($"Type '{TypeName}' from assembly '{AssemblyFilename}' has no 'Execute' method");
            }

            if (method.ReturnType != typeof(Task))
            {
                throw new ApplicationException($"Method 'Execute' at type '{TypeName}' from assembly '{AssemblyFilename}' has no 'Task' return type (current return type is {method.ReturnType.FullName})");
            }

            var parameters = method.GetParameters();
            if (parameters?.Length != 1)
            {
                throw new ApplicationException($"Method 'Execute' at type '{TypeName}' from assembly '{AssemblyFilename}' must have only 1 parameters (current parameters count {parameters?.Length})");
            }

            if (parameters[0].ParameterType.ToString().StartsWith("System.Object") == false)
            {
                throw new ApplicationException($"Second parameter in method 'Execute' at type '{TypeName}' from assembly '{AssemblyFilename}' must be object. (current type '{parameters[1].ParameterType.Name}')");
            }

            return method;

            //// ***** Attention: be aware for sync code with Validate on BaseJobTest *****
        }
    }
}