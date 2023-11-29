using System.ComponentModel.DataAnnotations;

namespace Planar.API.Common.Entities
{
    public class SetJobPathRequest
    {
        public string? Folder { get; set; }

        public string? JobFileName { get; set; }
    }
}