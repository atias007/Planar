using System.ComponentModel.DataAnnotations;

namespace Planar.API.Common.Entities
{
    public class JobDataRequest : JobOrTriggerKey
    {
        public string DataKey { get; set; }

        public string DataValue { get; set; }
    }
}