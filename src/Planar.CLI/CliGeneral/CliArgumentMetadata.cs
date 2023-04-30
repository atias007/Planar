using System;
using System.Reflection;

namespace Planar.CLI
{
    public class CliArgumentMetadata
    {
        public string? Name
        {
            get
            {
                return PropertyInfo?.Name?.ToLower();
            }
        }

        public Type? EnumType
        {
            get
            {
                var type = PropertyInfo?.PropertyType;
                if (type == null) { return null; }
                if (type.IsEnum) { return type; }

                var underType = Nullable.GetUnderlyingType(type);
                if (underType == null) { return null; }
                if (underType.IsEnum) { return underType; }

                return null;
            }
        }

        public string? ShortName { get; set; }

        public string? LongName { get; set; }

        public string? DisplayName { get; set; }

        public bool Default { get; set; }

        public int DefaultOrder { get; set; }

        public bool Required { get; set; }

        public string? RequiredMissingMessage { get; set; }

        public bool ValueSupplied { get; set; }

        public PropertyInfo? PropertyInfo { get; set; }

        public bool JobKey { get; set; }

        public bool TriggerKey { get; set; }

        public bool MissingRequired => Required && !ValueSupplied;

        public bool IsJobOrTriggerKey => JobKey || TriggerKey;
    }
}