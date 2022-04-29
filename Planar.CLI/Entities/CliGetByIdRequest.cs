using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetByIdRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public int Id { get; set; }
    }
}