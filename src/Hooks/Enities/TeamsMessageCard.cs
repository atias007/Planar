using System.Text.Json.Serialization;
using Twilio.Rest.Messaging.V1;

namespace Planar.Hooks.Enities;

#pragma warning disable CA1822 // Mark members as static

internal class TeamsMessageCard
{
    private const string SummaryText = "Planar monitor notification";

    [JsonPropertyName("@type")]
    public string Type => "MessageCard";

    [JsonPropertyName("@context")]
    public string Context => "https://schema.org/extensions";

#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
    public string Summary => SummaryText;
    public string ThemeColor => "0078D7";
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static


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