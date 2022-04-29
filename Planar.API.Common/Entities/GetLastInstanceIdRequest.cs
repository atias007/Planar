using System;

namespace Planar.API.Common.Entities
{
    public class GetLastInstanceIdRequest : JobOrTriggerKey
    {
        public DateTime InvokeDate { get; set; }
    }
}