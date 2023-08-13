using Microsoft.Extensions.Logging;
using Planar.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Planar.Common
{
    internal class JobMapper
    {
        private readonly ILogger? _logger;

        public JobMapper(ILogger logger)
        {
            _logger = logger;
        }

        public JobMapper()
        {
        }

        public void MapJobInstanceProperties(IJobExecutionContext context, object instance)
        {
            try
            {
                var allProperties = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                foreach (var item in context.MergedJobDataMap)
                {
                    if (item.Key.StartsWith(Consts.ConstPrefix)) { continue; }
                    var prop = allProperties.Find(p => string.Equals(p.Name, item.Key, StringComparison.OrdinalIgnoreCase));
                    MapProperty(context.JobDetails.Key, prop, item, instance);
                }
            }
            catch (Exception ex)
            {
                var source = nameof(MapJobInstanceProperties);
                _logger?.LogError(ex, "Fail at {Source} with job {Group}.{Name}", source, context.JobDetails.Key.Group, context.JobDetails.Key.Name);
                throw;
            }
        }

        private void MapProperty(IKey jobKey, PropertyInfo? prop, KeyValuePair<string, string?> data, object instance)
        {
            if (prop == null) { return; }

            try
            {
                var ignore = IsIgnoreProperty(prop, jobKey, data);
                if (ignore) { return; }

                var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                var finalType = underlyingType ?? prop.PropertyType;

                // nullable property with null value in data
                if (underlyingType != null && string.IsNullOrEmpty(PlanarConvert.ToString(data.Value))) { return; }

                var value = Convert.ChangeType(data.Value, finalType);
                prop.SetValue(instance, value);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex,
                    "Fail to map data key '{Key}' with value {Value} to property {Name} of job {JobGroup}.{JobName}",
                    data.Key, data.Value, prop.Name, jobKey.Group, jobKey.Name);
                throw;
            }
        }

        private bool IsIgnoreProperty(PropertyInfo property, IKey jobKey, KeyValuePair<string, string?> data)
        {
            var attributes = property.GetCustomAttributes();
            var ignore = attributes.Any(a => a.GetType().FullName == typeof(IgnoreDataMapAttribute).FullName);

            if (!ignore) { return false; }

            if (_logger == null)
            {
                Console.WriteLine($"Ignore map data key '{data.Key}' with value '{data.Value}' to property '{property.Name}' of job '{jobKey.Group}.{jobKey.Name}'");
            }
            else
            {
                _logger.LogDebug("Ignore map data key '{DataKey}' with value '{DataValue}' to property {PropertyName} of job '{JobGroup}.{JobName}'",
                        data.Key,
                        data.Value,
                        property.Name,
                        jobKey.Group,
                        jobKey.Name);
            }

            return true;
        }
    }
}