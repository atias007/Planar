using System.Text.Json.Serialization;

namespace Planar.Hooks.Enities
{
#pragma warning disable CA1822 // Mark members as static

    internal class TeamsMessageCard
    {
        [JsonPropertyName("@type")]
        public string Type => "MessageCard";

        [JsonPropertyName("@context")]
        public string Context => "https://schema.org/extensions";

        public string Summary => "Planar monitor notification";
        public string ThemeColor => "0078D7";

        public string? Title { get; set; }

        public List<Section> Sections { get; set; } = new();
    }

#pragma warning restore CA1822 // Mark members as static

    internal class Section
    {
        public string? ActivityTitle { get; set; }
        public string? ActivitySubtitle { get; set; }
        public string? ActivityImage { get; set; }

        public List<Fact> Facts { get; set; } = new();
        public string? Text { get; set; }
    }

    public class Fact
    {
        public Fact(string name, string? value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public string? Value { get; set; }
    }
}