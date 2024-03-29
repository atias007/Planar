﻿using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetAllJobsRequest : CliPagingRequest, IQuietResult
    {
        [ActionProperty("g", "group")]
        public string? JobGroup { get; set; }

        [QuietActionProperty]
        public bool Quiet { get; set; }

        [ActionProperty("a", "active")]
        public bool Active { get; set; }

        [ActionProperty("i", "inactive")]
        public bool Inactive { get; set; }

        [ActionProperty("s", "system")]
        public bool System { get; set; }

        [ActionProperty("t", "type")]
        public string? JobType { get; set; }

        [ActionProperty("f", "filter")]
        public string? Filter { get; set; }
    }
}