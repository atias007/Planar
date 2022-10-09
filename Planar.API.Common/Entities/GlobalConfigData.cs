using System;

namespace Planar.API.Common.Entities
{
    public class GlobalConfigData : GlobalConfigKey
    {
        public string Value { get; set; }

        public string Type { get; set; } = $"{nameof(String)}".ToLower();
    }
}