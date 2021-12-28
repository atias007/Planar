using Planner.API.Common.Entities;
using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliJobOrTriggerKey
    {
        [ActionProperty(DefaultOrder = 0)]
        public string Id { get; set; }

        internal JobOrTriggerKey GetKey()
        {
            var result = new JobOrTriggerKey { Id = Id };

            return result;
        }
    }
}