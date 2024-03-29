﻿using System;

namespace Planar.API.Common.Entities
{
    public class LogDetails
    {
        public int Id { get; set; }

        public DateTime TimeStamp { get; set; }

        public string? Message { get; set; }

        public string? Level { get; set; }
    }
}