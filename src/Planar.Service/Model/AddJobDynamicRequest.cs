﻿using Planar.API.Common.Entities;

namespace Planar.Service.Model
{
    internal class AddJobDynamicRequest : AddJobRequest
    {
        public virtual dynamic Properties { get; set; }
    }
}