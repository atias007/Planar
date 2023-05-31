﻿using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetByNameRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("name argument is required")]
        public string Name { get; set; } = null!;
    }
}