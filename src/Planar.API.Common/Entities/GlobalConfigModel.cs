using System;

namespace Planar.API.Common.Entities;

public class GlobalConfigModel : GlobalConfigModelAddRequest
{
    public DateTime? LastUpdate { get; set; }
}