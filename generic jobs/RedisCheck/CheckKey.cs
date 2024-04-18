using Common;
using Microsoft.Extensions.Configuration;
using RedisCheck;

namespace RedisStreamCheck;

internal class CheckKey(IConfigurationSection section) : BaseDefault(section), ICheckElemnt, IRedisDefaults
{
    public string Key { get; set; } = section.GetValue<string>("key") ?? string.Empty;
    public string? MemoryUsage { get; set; } = section.GetValue<string>("memory usage");
    public int? Length { get; set; } = section.GetValue<int?>("length");
    public int? Database { get; set; } = section.GetValue<int?>("database");
    public bool? Exists { get; set; } = section.GetValue<bool?>("exists");

    //// --------------------------------------- ////

    public int? MemoryUsageNumber { get; set; }
    public bool IsValid => MemoryUsageNumber > 0 || Length > 0;

    //// --------------------------------------- ////

    public void SetSize()
    {
        MemoryUsageNumber = GetSize(MemoryUsage, "max memory usage");
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