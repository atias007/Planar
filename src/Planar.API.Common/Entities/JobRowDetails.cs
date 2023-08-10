namespace Planar.API.Common.Entities
{
    public class JobRowDetails
    {
        public string Id { get; set; } = string.Empty;

        public string Group { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string JobType { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}