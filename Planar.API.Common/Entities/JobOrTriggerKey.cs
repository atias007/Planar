using Planar.API.Common.Validation;

namespace Planar.API.Common.Entities
{
    public class JobOrTriggerKey
    {
        [Trim]
        [Required]
        public string Id { get; set; }

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(Id);
            }
        }
    }
}