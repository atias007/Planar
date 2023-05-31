﻿using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUpdateEntityByIdRequest : CliUpdateEntityRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        [Required("id argument is required")]
        public int Id { get; set; }
    }
}