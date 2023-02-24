﻿using CommonJob;
using YamlDotNet.Serialization;

namespace Planar
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding.getencodings?view=net-7.0
    /// </summary>
    public class ProcessJobProperties : IFileJobProperties
    {
        public string Path { get; set; } = string.Empty;

        public string Filename { get; set; } = string.Empty;

        public string? Arguments { get; set; }

        [YamlMember(Alias = "output encoding")]
        public string? OutputEncoding { get; set; }

        public TimeSpan? Timeout { get; set; }

        [YamlMember(Alias = "success exit codes")]
        public IEnumerable<int> SuccessExitCodes { get; set; } = new List<int>();

        [YamlMember(Alias = "success output regex")]
        public string? SuccessOutputRegex { get; set; }

        [YamlMember(Alias = "fail exit codes")]
        public IEnumerable<int> FailExitCodes { get; set; } = new List<int>();

        [YamlMember(Alias = "fail output regex")]
        public string? FailOutputRegex { get; set; }

        [YamlMember(Alias = "log process information")]
        public bool LogProcessInformation { get; set; } = true;
    }
}