﻿using YamlDotNet.Serialization;

namespace Planner.API.Common.Entities
{
    public class CronTriggerDetails : TriggerDetails
    {
        [YamlMember(Order = 97)]
        public string CronExpression { get; set; }
    }
}