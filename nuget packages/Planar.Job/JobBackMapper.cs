using Microsoft.Extensions.Logging;
using Planar.Common;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.Job
{
    internal class JobBackMapper
    {
#if NETSTANDARD2_0
        private readonly ILogger _logger;
        private readonly BaseJobFactory _baseJobFactory;
#else
        private readonly ILogger? _logger;
        private readonly BaseJobFactory? _baseJobFactory;
#endif

        public JobBackMapper(ILogger logger, BaseJobFactory baseJobFactory)
        {
            _logger = logger;
            _baseJobFactory = baseJobFactory;
        }

        public async Task MapJobInstancePropertiesBack(IJobExecutionContext context, object instance)
        {
            try
            {
                if (context == null) { return; }

                var propInfo = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                foreach (var prop in propInfo)
                {
                    if (prop.Name.StartsWith(Consts.ConstPrefix)) { continue; }
                    await SafePutData(context, prop, instance);
                }
            }
            catch (Exception ex)
            {
                var source = nameof(MapJobInstancePropertiesBack);
                _logger?.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetails.Key.Group, context.JobDetails.Key.Name);
                throw;
            }
        }

        private async Task SafePutData(IJobExecutionContext context, PropertyInfo prop, object instance)
        {
            var jobAttribute = prop.GetCustomAttribute<JobDataAttribute>();
            var triggerAttribute = prop.GetCustomAttribute<TriggerDataAttribute>();
            var ignoreAttribute = prop.GetCustomAttribute<IgnoreDataMapAttribute>();

            if (ignoreAttribute != null)
            {
                var jobKey = context.JobDetails.Key;

                _logger?.LogDebug("ATTENTION: Ignore map back property {PropertyName} of job '{JobGroup}.{JobName}' to data map",
                    prop.Name,
                    jobKey.Group,
                    jobKey.Name);

                return;
            }

            if (jobAttribute != null)
            {
                await SafePutJobDataMap(context, prop, instance);
            }

            if (triggerAttribute != null)
            {
                await SafePutTiggerDataMap(context, prop, instance);
            }

            if (jobAttribute == null && triggerAttribute == null)
            {
                if (context.JobDetails.JobDataMap.ContainsKey(prop.Name))
                {
                    await SafePutJobDataMap(context, prop, instance);
                }

                if (context.TriggerDetails.TriggerDataMap.ContainsKey(prop.Name))
                {
                    await SafePutTiggerDataMap(context, prop, instance);
                }
            }
        }

        private async Task SafePutJobDataMap(IJobExecutionContext context, PropertyInfo prop, object instance)
        {
#if NETSTANDARD2_0
            string value = null;
#else
            string? value = null;
#endif
            try
            {
                if (!Consts.IsDataKeyValid(prop.Name))
                {
                    throw new PlanarJobException($"the data key {prop.Name} in invalid");
                }

                value = PlanarConvert.ToString(prop.GetValue(instance));
                if (_baseJobFactory == null) { return; }
                await _baseJobFactory.PutJobDataAsync(prop.Name, value);
            }
            catch (Exception ex)
            {
                var jobKey = context.JobDetails.Key;
                _logger?.LogWarning(ex,
                    "Fail to save back value {Value} from property {Name} to JobDetails at job {JobGroup}.{JobName}",
                    value, prop.Name, jobKey.Group, jobKey.Name);
            }
        }

        private async Task SafePutTiggerDataMap(IJobExecutionContext context, PropertyInfo prop, object instance)
        {
#if NETSTANDARD2_0
            string value = null;
#else
            string? value = null;
#endif
            try
            {
                if (!Consts.IsDataKeyValid(prop.Name))
                {
                    throw new PlanarJobException($"the data key {prop.Name} in invalid");
                }

                value = PlanarConvert.ToString(prop.GetValue(instance));
                if (_baseJobFactory == null) { return; }
                await _baseJobFactory.PutTriggerDataAsync(prop.Name, value);
            }
            catch (Exception ex)
            {
                var jobKey = context.JobDetails.Key;
                _logger?.LogWarning(ex,
                    "Fail to save back value {Value} from property {Name} to TriggerDetails at job {JobGroup}.{JobName}",
                    value, prop.Name, jobKey.Group, jobKey.Name);
            }
        }
    }
}