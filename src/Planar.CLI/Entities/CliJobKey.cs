﻿using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliJobKey
    {
        [Required("id argument is required")]
        [ActionProperty(DefaultOrder = 0)]
        public virtual string Id { get; set; } = string.Empty;

        internal JobOrTriggerKey GetKey()
        {
            var result = new JobOrTriggerKey { Id = Id };

            return result;
        }
    }
}