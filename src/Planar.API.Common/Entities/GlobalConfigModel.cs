using System;

namespace Planar.API.Common.Entities;

public class GlobalConfigModel : GlobalConfigModelRequest
{
    public DateTime? LastUpdate { get; set; }
}