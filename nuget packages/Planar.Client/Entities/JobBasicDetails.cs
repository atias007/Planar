namespace Planar.Client.Entities
{
    public class JobBasicDetails
    {
        public string Id { get; set; } = string.Empty;

        public string Group { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string JobType { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}