using Microsoft.Extensions.Configuration;

namespace RedisStreamCheck;

internal class RedisKeyCheck(IConfigurationSection section) : IRedisKey
{
    public string Key { get; set; } = section.GetValue<string>("key") ?? string.Empty;
    public string? MaxMemoryUsage { get; set; } = section.GetValue<string>("max memory usage");
    public string? MinMemoryUsage { get; set; } = section.GetValue<string>("min memory usage");
    public int? MaxLength { get; set; } = section.GetValue<int?>("max length");
    public int? MinLength { get; set; } = section.GetValue<int?>("min length");
    public int? Database { get; set; } = section.GetValue<int?>("database");
    public int? RetryCount { get; set; } = section.GetValue<int?>("retry count");
    public int? MaximumFailsInRow { get; set; } = section.GetValue<int?>("retry interval");
    public TimeSpan? RetryInterval { get; set; } = section.GetValue<TimeSpan?>("maximum fails in row");

    //// --------------------------------------- ////

    public int? MaxMemoryUsageNumber { get; set; }
    public int? MinMemoryUsageNumber { get; set; }
    public bool IsValid => MaxMemoryUsageNumber > 0 || MinMemoryUsageNumber > 0 || MaxLength > 0 || MinLength > 0;

    //// --------------------------------------- ////

    public void SetSize()
    {
        MaxMemoryUsageNumber = GetSize(MaxMemoryUsage, "max memory usage");
        MinMemoryUsageNumber = GetSize(MinMemoryUsage, "min memory usage");
    }

    private int? GetSize(string? source, string fieldName)
    {
        var factor = 0;
        if (string.IsNullOrWhiteSpace(source)) { return null; }

        source = source.Trim().ToLower();
        var cleansource = string.Empty;
        if (source.EndsWith("bytes"))
        {
            factor = 0;
            cleansource = source.Replace("bytes", string.Empty);
        }
        else if (cleansource.EndsWith("kb"))
        {
            factor = 1;
            cleansource = source.Replace("kb", string.Empty);
        }
        else if (source.EndsWith("mb"))
        {
            factor = 2;
            cleansource = source.Replace("mb", string.Empty);
        }
        else if (source.EndsWith("gb"))
        {
            factor = 3;
            cleansource = source.Replace("gb", string.Empty);
        }
        else if (source.EndsWith("tb"))
        {
            factor = 4;
            cleansource = source.Replace("tb", string.Empty);
        }
        else if (source.EndsWith("pb"))
        {
            factor = 5;
            cleansource = source.Replace("pb", string.Empty);
        }

        if (!int.TryParse(cleansource, out var number))
        {
            throw new InvalidDataException($"{fieldName} for key '{Key}' is not a number");
        }

        var result = number * (int)Math.Pow(1024, factor);
        return result;
    }
}