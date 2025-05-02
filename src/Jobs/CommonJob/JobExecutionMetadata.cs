using Microsoft.Extensions.Logging;
using Planar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using IJobExecutionContext = Quartz.IJobExecutionContext;

namespace CommonJob;

public class JobExecutionMetadata
{
    private readonly List<string> _log = [];
    private int _logSize;
    private bool _freezLog;
    private bool _freezException;

    public void AppendLog(string log)
    {
        if (log == null) { return; }
        if (_freezLog) { return; }
        _log.Add(log);
        _logSize += log.Length;

        if (_logSize > 20_000_000)
        {
            HasWarnings = true;
            _freezLog = true;

            var logEntity = new LogEntity { Level = LogLevel.Warning, Message = "log size exceeded 20mb. additional logs will not be recorded" };
            _log.Add(logEntity.ToString());
        }
    }

    public string GetLogText()
    {
        var sb = new StringBuilder(_log.Count);
        foreach (var text in _log)
        {
            sb.AppendLine(text);
        }

        return sb.ToString();
    }

    private readonly List<ExceptionDto> _exceptions = [];
    public IEnumerable<ExceptionDto> Exceptions => _exceptions;

    public void AddException(ExceptionDto exception)
    {
        if (_freezException) { return; }
        var length = _exceptions.Sum(e => e.Length);
        _exceptions.Add(exception);

        if (length > 20_000_000)
        {
            HasWarnings = true;
            _freezException = true;

            var logEntity = new LogEntity { Level = LogLevel.Warning, Message = "exception size exceeded 20mb. additional exceptions will not be recorded" };
            _log.Add(logEntity.ToString());
        }
    }

    public int? EffectedRows { get; set; }

    public byte Progress { get; set; }

    public bool HasWarnings { get; set; }

    private static readonly Lock Locker = new();

    public string GetExceptionsText()
    {
        var exceptions = Exceptions.ToList();
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