using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliLoginRequest
    {
        [ActionProperty(DefaultOrder = 1, LongName = "host", ShortName = "h")]
        public string Host { get; set; }

        [ActionProperty(DefaultOrder = 2, LongName = "port", ShortName = "p")]
        public int Port { get; set; }

        [ActionProperty(LongName = "ssl", ShortName = "s")]
        public bool SSL { get; set; }

        [ActionProperty(LongName = "remember", ShortName = "r")]
        public bool Remember { get; set; }

        [ActionProperty(LongName = "remember-days", ShortName = "rd")]
        public int RememberDays { get; set; }

        [ActionProperty(LongName = "user", ShortName = "u")]
        public string User { get; set; }

        [ActionProperty(LongName = "password", ShortName = "w")]
        public string Password { get; set; }

        [IterativeActionProperty]
        public bool Iterative { get; set; }

        public DateTimeOffset ConnectDate { get; set; }
    }
}