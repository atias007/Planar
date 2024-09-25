namespace InfluxDBCheck;

internal class InfluxDBServer
{
    public string Url { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;

    public bool IsEmpty => string.IsNullOrWhiteSpace(Url) && string.IsNullOrWhiteSpace(Token) && string.IsNullOrWhiteSpace(Organization);
}