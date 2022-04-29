using Planar.CLI.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Planar.CLI
{
    public class CliActionMetadata
    {
        private Type _requestType;

        public string Module { get; set; }

        public List<string> Command { get; set; } = new();

        public MethodInfo Method { get; set; }

        public Type RequestType
        {
            get
            {
                if (Method == null) return null;

                if (_requestType == null)
                {
                    var parameters = Method.GetParameters();
                    if (parameters.Length == 0) return null;
                    if (parameters.Length > 1)
                    {
                        throw new ApplicationException($"Cli Error: Action {Method.Name} has more then 1 parameter");
                    }

                    _requestType = parameters[0].ParameterType;
                }

                return _requestType;
            }
        }

        public List<RequestPropertyInfo> GetRequestPropertiesInfo()
        {
            var result = new List<RequestPropertyInfo>();
            var props = RequestType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var item in props)
            {
                var att = item.GetCustomAttribute<ActionPropertyAttribute>();
                var info = new RequestPropertyInfo
                {
                    PropertyInfo = item,
                    LongName = att?.LongName?.ToLower(),
                    ShortName = att?.ShortName?.ToLower(),
                    Default = (att?.Default).GetValueOrDefault(),
                    DefaultOrder = (att?.DefaultOrder).GetValueOrDefault()
                };
                result.Add(info);
            }

            return result;
        }
    }

    public class RequestPropertyInfo
    {
        public string Name
        {
            get
            {
                return PropertyInfo?.Name?.ToLower();
            }
        }

        public string ShortName { get; set; }

        public string LongName { get; set; }

        public bool Default { get; set; }

        public int DefaultOrder { get; set; }

        public PropertyInfo PropertyInfo { get; set; }
    }
}