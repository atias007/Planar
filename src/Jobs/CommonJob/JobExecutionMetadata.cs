using Planar;
using System;
using System.Collections.Generic;
using System.Text;
using IJobExecutionContext = Quartz.IJobExecutionContext;

namespace CommonJob;

public class JobExecutionMetadata
{
    public StringBuilder Log { get; set; } = new StringBuilder();

    public List<ExceptionDto> Exceptions { get; set; } = new List<ExceptionDto>();

    public int? EffectedRows { get; set; }

    public byte Progress { get; set; }

    public bool HasWarnings { get; set; }

    private static readonly object Locker = new();

    public string GetLog()
    {
        return Log.ToString();
    }

    public string GetExceptionsText()
    {
        var exceptions = Exceptions;
        if (exceptions == null || exceptions.Count == 0)
        {
            return string.Empty;
        }

        if (exceptions.Count == 1)
        {
            return exceptions[0].ExceptionText ?? string.Empty;
        }

        var seperator = string.Empty.PadLeft(80, '-');
        var sb = new StringBuilder();
        sb.AppendLine($"There is {exceptions.Count} aggregate exception");
        exceptions.ForEach(e => sb.AppendLine($"  - {e.Message}"));
        sb.AppendLine(seperator);
        exceptions.ForEach(e =>
        {
            sb.AppendLine(e.ExceptionText);
            sb.AppendLine(seperator);
        });

        return sb.ToString();
    }

    public Exception? UnhandleException { get; set; }

    public bool IsRunningFail => !IsRunningSuccess;
    public bool IsRunningSuccess => UnhandleException == null;

    public static JobExecutionMetadata GetInstance(IJobExecutionContext context)
    {
        lock (Locker)
        {
            if (context.Result is not JobExecutionMetadata result)
            {
                result = new JobExecutionMetadata();
                context.Result = result;
            }

            return result;
        }
    }
}