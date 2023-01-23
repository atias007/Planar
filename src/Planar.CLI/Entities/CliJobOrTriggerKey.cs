using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliJobOrTriggerKey
    {
        [Required("id argument is required")]
        [ActionProperty(DefaultOrder = 0)]
        public virtual string Id { get; set; }

        internal JobOrTriggerKey GetKey()
        {
            var result = new JobOrTriggerKey { Id = Id };

            return result;
        }
    }
}