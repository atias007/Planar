﻿namespace Planar.Client.Entities
{
    public class MaxConcurrentExecution
    {
        public int Value { get; set; }

        public int Maximum { get; set; }

        public string Status { get; set; } = null!;
    }
}