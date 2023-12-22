using System.ComponentModel.DataAnnotations.Schema;

namespace Planar.Service.Model
{
    public partial class MonitorCounter
    {
        [NotMapped]
        public string? MonitorTitle { get; set; }
    }
}