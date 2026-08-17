using Microsoft.Extensions.Logging;
using Planar.Common;
using System;
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

                var allProperties = ReflectionHelper.GetProperties(instance);
                foreach (var prop in allProperties)
                {
                    if (prop.Name.StartsWith(Consts.ConstPrefix)) { continue; }
                    await SafePutData(prop, instance);
                }
            }
            catch (Exception ex)
            {
                var source = nameof(MapJobInstancePropertiesBack);
                _logger?.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetails.Key.Group, context.JobDetails.Key.Name);
                throw new PlanarJobException("Fail to map job instance properties back", ex);
            }
        }

        private async Task SafePutData(PropertyInfo prop, object instance)
        {
            var ignoreAttribute = prop.GetCustomAttribute<IgnoreDataMapAttribute>();

            if (ignoreAttribute != null)
            {
                if (_logger?.IsEnabled(LogLevel.Information) ?? false)
                {
                    _logger.LogInformation("ATTENTION: mapping property {PropertyName} is skipped due to 'IgnoreDataMap' attribute",
                        prop.Name);
                }

                return;
            }

            var jobAttribute = prop.GetCustomAttribute<JobDataAttribute>();
            if (jobAttribute != null)
            {
                await SafePutJobDataMap(jobAttribute, prop, instance);
            }

            var triggerAttribute = prop.GetCustomAttribute<TriggerDataAttribute>();
            if (triggerAttribute != null)
            {
                await SafePutTiggerDataMap(triggerAttribute, prop, instance);
            }
        }

        private async Task SafePutJobDataMap(DataAttribute attribute, PropertyInfo prop, object instance)
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
                    _logger?.LogWarning("the data key {Name} in invalid", prop.Name);
                }

                if (attribute.ReadOnly)
                {
                    _logger?.LogInformation("ATTENTION: mapping property {PropertyName} is skipped due to 'JobData' attribute with ReadOnly=true",
                        prop.Name);
                }

                value = PlanarConvert.ToString(prop.GetValue(instance));
                if (_baseJobFactory == null) { return; }

                await _baseJobFactory.PutJobDataAsync(prop.Name, value);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex,
                    "Fail to save value {Value} from property {Name} to job data",
                    value, prop.Name);
            }
        }

        private async Task SafePutTiggerDataMap(DataAttribute attribute, PropertyInfo prop, object instance)
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
                    _logger?.LogWarning("the data key {Name} in invalid", prop.Name);
                }

                if (attribute.ReadOnly)
                {
                    _logger?.LogInformation("ATTENTION: mapping property {PropertyName} is skipped due to 'TriggerData' attribute with ReadOnly=true",
                        prop.Name);
                }

                value = PlanarConvert.ToString(prop.GetValue(instance));
                if (_baseJobFactory == null) { return; }

                await _baseJobFactory.PutTriggerDataAsync(prop.Name, value);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex,
                    "Fail to save value {Value} from property {Name} to trigger data",
                    value, prop.Name);
            }
        }
    }
}