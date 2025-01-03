﻿using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliUpdateTimeoutRequest : CliTriggerKey
    {
        [Required("timeout argument is required")]
        [ActionProperty(DefaultOrder = 1, Name = CliActionMetadata.TimeSpan)]
        public TimeSpan Timeout { get; set; }
    }
}