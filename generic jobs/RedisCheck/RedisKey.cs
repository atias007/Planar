using Common;
using Microsoft.Extensions.Configuration;
using Redis;

namespace RedisCheck;

internal class RedisKey(IConfigurationSection section, Defaults defaults) : BaseDefault(section, defaults), ICheckElement, IRedisDefaults, IRedisKey, IVetoEntity
{
    public string Key { get; } = section.GetValue<string>("key") ?? string.Empty;
    public string? MemoryUsage { get; } = section.GetValue<string>("memory usage");
    public int? Length { get; } = section.GetValue<int?>("length");
    public int? Database { get; } = section.GetValue<int?>("database");
    public bool? Exists { get; } = section.GetValue<bool?>("exists");

    //// --------------------------------------- ////

    public int? MemoryUsageNumber { get; } = GetSize(section.GetValue<string>("memory usage"), "max memory usage");
    public bool IsValid => MemoryUsageNumber > 0 || Length > 0;

    //// --------------------------------------- ////

    private static int? GetSize(string? source, string fieldName)
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
            throw new InvalidDataException($"value {source} at '{fieldName}' is not a number");
        }

        var result = number * (int)Math.Pow(1024, factor);
        return result;
    }
}