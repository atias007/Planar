using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace FolderCheck;

internal class Folder(IConfigurationSection section, string path) : IFolder
{
    public string? Name { get; set; } = section.GetValue<string?>("name");
    public string Path { get; private set; } = path;
    public Monitor Monitor { get; set; } = ParseMonitor(section.GetSection("monitor").Value);
    public string? MonitorArgument { get; set; } = section.GetValue<string>("monitor argument");
    public IEnumerable<string>? FilesPattern { get; set; } = section.GetValue<string>("files pattern")?.Split(',').ToList();
    public bool IncludeSubdirectories { get; set; } = section.GetValue<bool>("include subdirectories");
    public int? RetryCount { get; set; } = section.GetValue<int?>("retry count");
    public TimeSpan? RetryInterval { get; set; } = section.GetValue<TimeSpan?>("retry interval");
    public int? MaximumFailsInRow { get; set; } = section.GetValue<int?>("maximum fails in row");

    //// --------------------------------------- ////

    public int MonitorArgumentNumber { get; set; }
    public string? MonitorText { get; private set; } = section.GetSection("monitor").Value;

    public void SetMonitorArgumentAge()
    {
        if (string.IsNullOrWhiteSpace(MonitorArgument))
        {
            throw new InvalidDataException($"monitor argument for folder name '{Name}' is null or empty");
        }

        if (!TimeSpan.TryParse(MonitorArgument, CultureInfo.CurrentCulture, out var span))
        {
            throw new InvalidDataException($"monitor argument for folder name '{Name}' is not a valid time span");
        }

        MonitorArgumentNumber = Convert.ToInt32(span.TotalMicroseconds);
    }

    public void SetMonitorArgumentNumber()
    {
        if (!int.TryParse(MonitorArgument, out var number))
        {
            throw new InvalidDataException($"monitor argument for folder name '{Name}' is not a number");
        }

        MonitorArgumentNumber = number;
    }

    public void SetMonitorArgumentSize()
    {
        var factor = 0;
        if (string.IsNullOrWhiteSpace(MonitorArgument))
        {
            throw new InvalidDataException($"monitor argument for folder name '{Name}' is null or empty");
        }

        MonitorArgument = MonitorArgument.Trim().ToLower();
        var cleanMonitorArgument = string.Empty;
        if (MonitorArgument.EndsWith("bytes"))
        {
            factor = 0;
            cleanMonitorArgument = MonitorArgument.Replace("bytes", string.Empty);
        }
        else if (cleanMonitorArgument.EndsWith("kb"))
        {
            factor = 1;
            cleanMonitorArgument = MonitorArgument.Replace("kb", string.Empty);
        }
        else if (MonitorArgument.EndsWith("mb"))
        {
            factor = 2;
            cleanMonitorArgument = MonitorArgument.Replace("mb", string.Empty);
        }
        else if (MonitorArgument.EndsWith("gb"))
        {
            factor = 3;
            cleanMonitorArgument = MonitorArgument.Replace("gb", string.Empty);
        }
        else if (MonitorArgument.EndsWith("tb"))
        {
            factor = 4;
            cleanMonitorArgument = MonitorArgument.Replace("tb", string.Empty);
        }
        else if (MonitorArgument.EndsWith("pb"))
        {
            factor = 5;
            cleanMonitorArgument = MonitorArgument.Replace("pb", string.Empty);
        }

        if (!int.TryParse(cleanMonitorArgument, out var number))
        {
            throw new InvalidDataException($"monitor argument for folder name '{Name}' is not a number");
        }

        MonitorArgumentNumber = number * (int)Math.Pow(1024, factor);
    }

    private static Monitor ParseMonitor(string? monitor)
    {
        if (string.IsNullOrWhiteSpace(monitor)) { return Monitor.None; }
        var monitorEnum = monitor.Trim().Replace("-", string.Empty);

        if (Enum.TryParse<Monitor>(monitorEnum, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidDataException($"invalid monitor value: {monitor}");
    }
}