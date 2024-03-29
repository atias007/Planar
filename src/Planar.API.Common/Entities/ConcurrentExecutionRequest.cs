﻿using System;

namespace Planar.API.Common.Entities
{
    public class ConcurrentExecutionRequest : PagingRequest, IDateScope
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Server { get; set; }
        public string? InstanceId { get; set; }
    }
}