﻿using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities;

public class CliQueueInvokeJobRequest : CliInvokeJobRequest
{
    [ActionProperty(DefaultOrder = 1, Name = "due date")]
    [Required("due date argument is required")]
    public DateTime DueDate { get; set; }
}