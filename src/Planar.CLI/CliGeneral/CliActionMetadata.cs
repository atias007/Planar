using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Planar.CLI
{
    public class CliActionMetadata
    {
        private Type? _requestType;

        public string Module { get; set; } = string.Empty;

        public List<string> Command { get; set; } = new();

        public MethodInfo? Method { get; set; }

        public bool AllowNullRequest { get; set; }

        public Type? GetRequestType()
        {
            if (Method == null) { return null; }

            if (_requestType == null)
            {
                var parameters = Method.GetParameters();
                if (parameters.Length == 0) { return null; }
                if (parameters.Length > 1)
                {
                    throw new CliException($"cli Error: Action {Method.Name} has more then 1 parameter");
                }

                _requestType = parameters[0].ParameterType;
            }

            return _requestType;
        }

        public List<RequestPropertyInfo> GetRequestPropertiesInfo()
        {
            var requestType = GetRequestType();
            var result = new List<RequestPropertyInfo>();
            if (requestType == null)
            {
                return result;
            }

            var props = requestType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var inheritKey =
                requestType.IsAssignableFrom(typeof(CliJobOrTriggerKey)) ||
                requestType.IsSubclassOf(typeof(CliJobOrTriggerKey));

            foreach (var item in props)
            {
                var att = item.GetCustomAttribute<ActionPropertyAttribute>();
                var req = item.GetCustomAttribute<RequiredAttribute>();
                var info = new RequestPropertyInfo
                {
                    PropertyInfo = item,
                    LongName = att?.LongName?.ToLower(),
                    ShortName = att?.ShortName?.ToLower(),
                    Default = (att?.Default).GetValueOrDefault(),
                    Required = req != null,
                    RequiredMissingMessage = req?.Message,
                    DefaultOrder = (att?.DefaultOrder).GetValueOrDefault(),
                    JobOrTriggerKey = inheritKey && item.Name == nameof(CliJobOrTriggerKey.Id)
                };
                result.Add(info);
            }

            return result;
        }
    }

    public class RequestPropertyInfo
    {
        public string? Name
        {
            get
            {
                return PropertyInfo?.Name?.ToLower();
            }
        }

        public string? ShortName { get; set; }

        public string? LongName { get; set; }

        public bool Default { get; set; }

        public int DefaultOrder { get; set; }

        public bool Required { get; set; }

        public string? RequiredMissingMessage { get; set; }

        public bool ValueSupplied { get; set; }

        public PropertyInfo? PropertyInfo { get; set; }

        public bool JobOrTriggerKey { get; set; }
    }
}