using System.ComponentModel.DataAnnotations.Schema;

namespace Planar.Service.Model
{
    public partial class MonitorMute
    {
        [NotMapped]
        public string? MonitorTitle { get; set; }
    }
}