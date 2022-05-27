using System.ComponentModel.DataAnnotations;

namespace Planar.API.Common.Entities
{
    public class JobOrTriggerKey
    {
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