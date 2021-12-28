using System;

namespace Planner.API.Common.Entities
{
    public class GetLastInstanceIdRequest : JobOrTriggerKey
    {
        public DateTime InvokeDate { get; set; }
    }
}