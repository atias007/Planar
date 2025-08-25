using CloudNative.CloudEvents;
using CommonJob;
using CommonJob.MessageBrokerEntities;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Planar.Service.General;
using PlanarJob;
using PlanarJobInner;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Planar;

public abstract class PlanarJob(
    ILogger logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil,
    IServiceProvider serviceProvider) : BaseProcessJob<PlanarJobProperties>(logger, dataLayer, jobMonitorUtil, clusterUtil)
{
    private readonly Lock ConsoleLocker = new();
    private readonly bool _isDevelopment = string.Equals(AppSettings.General.Environment, "development", StringComparison.OrdinalIgnoreCase);
    private bool _isHealthCheck;
    private string? _contextFilename;

    private PlanarJobExecutionException? _executionException;
    private readonly List<PlanarJobException> _jobActionExceptions = [];

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            MqttBrokerService.RegisterInterceptingPublish(InterceptingPublishAsync, context.FireInstanceId);

            await Initialize(context);
            ValidateProcessJob();
            ValidateExeFile();
            SafeLogInvokeJobDetails(context);
            context.CancellationToken.Register(OnCancel);
            var timeout = TriggerHelper.GetTimeoutWithDefault(context.Trigger);
            var startInfo = GetProcessStartInfo();
            StartMonitorDuration(context);
            var success = StartProcess(startInfo, timeout);
            StopMonitorDuration();
            if (!success)
            {
                OnTimeout(context);
            }

            ValidateExitCode();
            ValidateHealthCheck();
            LogProcessInformation();
            CheckProcessCancel();
            CheckJobError();
        }
        catch (Exception ex)
        {
            HandleException(context, ex);
        }
        finally
        {
            await FinalizeJob(context);
            FinalizeProcess();
            UnregisterMqttBrokerService(context.FireInstanceId);
            SafeDeleteContextFile();
        }
    }

    private void SafeLogInvokeJobDetails(IJobExecutionContext context)
    {
        try
        {
            if (context.MergedJobDataMap.TryGetValue(Consts.InvokeJobJobIdDataKey, out var jobIdObj) &&
                context.MergedJobDataMap.TryGetValue(Consts.InvokeJobInstanceIdDataKey, out var instanceIdObj))
            {
                var jobId = jobIdObj?.ToString() ?? "[no job id]";
                var instanceId = instanceIdObj?.ToString() ?? "[no instance id]";
                MessageBroker.AppendLog(LogLevel.Information, Seperator);
                MessageBroker.AppendLog(LogLevel.Information, "job was invoked from another planar job");
                MessageBroker.AppendLog(LogLevel.Information, $"invoker job key: {jobId}");
                MessageBroker.AppendLog(LogLevel.Information, $"invoker instance id: {instanceId}");
                MessageBroker.AppendLog(LogLevel.Information, Seperator);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to log invoke job details at {Method}", nameof(SafeLogInvokeJobDetails));
        }
    }

    private void SafeDeleteContextFile()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_contextFilename) && File.Exists(_contextFilename))
            {
                SafeDeleteFile(_contextFilename);
            }
        }
        catch
        {
            // *** DO NOTHING ***
        }

        try
        {
            var path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, FileProperties.Path, Consts.PlanarJobArgumentContextFolder);
            var files = Directory.GetFiles(path, "*.ctx").Select(f => new FileInfo(f));
            foreach (var file in files)
            {
                if (file.LastWriteTime < DateTimeOffset.Now.AddDays(-1))
                {
                    SafeDeleteFile(file);
                }
            }
        }
        catch
        {
            // *** DO NOTHING ***
        }
    }

    private static void SafeDeleteFile(string filename)
    {
        try
        {
            File.Delete(filename);
        }
        catch
        {
            // *** DO NOTHING ***
        }
    }

    private static void SafeDeleteFile(FileInfo file)
    {
        try
        {
            file.Delete();
        }
        catch
        {
            // *** DO NOTHING ***
        }
    }

    private void CheckJobError()
    {
        if (_executionException == null)
        {
            if (_jobActionExceptions.Count == 0) { return; }
            if (_jobActionExceptions.Count == 1) { throw _jobActionExceptions[0]; }
            if (_jobActionExceptions.Count > 1) { throw new AggregateException("there are multiple job invoke errors", _jobActionExceptions); }
        }
        else
        {
            if (_jobActionExceptions.Count == 0) { throw _executionException; }

            var list = new List<Exception> { _executionException };
            list.AddRange(_jobActionExceptions);
            throw new AggregateException("there are multiple errors", list);
        }
    }

    private void ValidateExeFile()
    {
        if (!FileExtentionIsExe(Filename))
        {
            _logger.LogError("process filename '{Filename}' must have 'exe' extention", Filename);
            MessageBroker.AppendLog(LogLevel.Error, $"process filename '{Filename}' must have 'exe' extention");
            throw new PlanarException($"process filename '{Filename}' must have 'exe' extention");
        }
    }

    private static bool FileExtentionIsExe(string filename)
    {
        const string exe = ".exe";
        var fi = new FileInfo(filename);
        return string.Equals(fi.Extension, exe);
    }

    private static byte GetCloudEventByteValue(CloudEvent cloudEvent)
    {
        var data = ValidateCloudEventData(cloudEvent.Data);

        if (byte.TryParse(data, out var value))
        {
            return value;
        }
        else
        {
            throw new PlanarJobException($"Message broker channels '{cloudEvent.Type}' has invalid byte value '{cloudEvent.Data}'");
        }
    }

    private static string GetCloudEventStringValue(CloudEvent cloudEvent)
    {
        var data = ValidateCloudEventData(cloudEvent.Data);
        return data;
    }

    private static T GetCloudEventEntityValue<T>(CloudEvent cloudEvent)
        where T : class, new()
    {
        var data = ValidateCloudEventData(cloudEvent.Data);
        var log = JsonConvert.DeserializeObject<T>(data);
        return log
            ?? throw new PlanarJobException($"Message broker channels '{cloudEvent.Type}' has invalid '{typeof(T).Name}' value '{data}'");
    }

    private static int GetCloudEventInt32Value(CloudEvent cloudEvent)
    {
        var data = ValidateCloudEventData(cloudEvent.Data);

        if (int.TryParse(data, out var value))
        {
            return value;
        }
        else
        {
            throw new PlanarJobException($"Message broker channels '{cloudEvent.Type}' has invalid integer value '{cloudEvent.Data}'");
        }
    }

    private static string ValidateCloudEventData(object? data)
    {
        if (data == null)
        {
            throw new PlanarJobException("message broker has empty data");
        }

        var result = data.ToString();
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new PlanarJobException("message broker has empty data");
        }

        return result;
    }

    private void CheckProcessCancel()
    {
        if (_processKilled)
        {
            throw new PlanarJobException($"process '{Filename}' was stopped at {DateTimeOffset.Now}");
        }
    }

    private void ValidateExitCode()
    {
        if (_process == null) { return; }
        if (!_process.HasExited) { return; }
        if (_process.ExitCode == 0) { return; }
        if (_processKilled) { return; }

        if (FinalOutputText.Length > 0)
        {
            MessageBroker.AppendLogRaw(FinalOutputText.ToString());
        }

        throw new PlanarJobException($"WARNING! Abnormal process exit code {_process.ExitCode}. this may cause by unwaited tasks\\threads");
    }

    private void ValidateHealthCheck()
    {
        try
        {
            if (_isHealthCheck) { return; }

            if (FinalOutputText.Length > 0)
            {
                var outputText = FinalOutputText.ToString();
                MessageBroker.AppendLogRaw(outputText);
                if (outputText.Contains(nameof(PlanarJobException)))
                {
                    throw new PlanarJobException("fail to execute job");
                }
            }

            var log = new LogEntity
            {
                Level = LogLevel.Warning,
                Message = "WARNING! No health check signal from job. Check if the following code: \"PlanarJob.Start<TJob>();\" exists in startup of your console project (Program.cs)"
            };

            MessageBroker.AppendLog(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to handle health check at {Method}", nameof(FinalizeProcess));
        }

        throw new PlanarJobException("no health check signal from job. See job log for more information");
    }

    private static void UnregisterMqttBrokerService(string fireInstanceId)
    {
        MqttBrokerService.UnRegisterInterceptingPublish(fireInstanceId);
    }

    protected override ProcessStartInfo GetProcessStartInfo()
    {
        var startInfo = base.GetProcessStartInfo();
        var base64String = GetContextArgument(MessageBroker.Details);
        startInfo.Arguments = $"--planar-service-mode --context {base64String}";
        startInfo.StandardErrorEncoding = Encoding.UTF8;
        startInfo.StandardOutputEncoding = Encoding.UTF8;
        SetProcessToLinuxOs(startInfo);
        return startInfo;
    }

    private string GetContextArgument(string details)
    {
        const int lengthLimit = 30_000;

        var bytes = Encoding.UTF8.GetBytes(details);
        var base64String = Convert.ToBase64String(bytes);
        if (base64String.Length <= lengthLimit) { return base64String; }

        try
        {
            var path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, FileProperties.Path, Consts.PlanarJobArgumentContextFolder);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var identifier = Guid.NewGuid().ToString("N");
            var filename = Path.Combine(path, $"{identifier}.ctx");
            File.WriteAllText(filename, base64String);
            _contextFilename = filename;
            var result = $"[{identifier}]";
            bytes = Encoding.UTF8.GetBytes(result);
            base64String = Convert.ToBase64String(bytes);
            return base64String;
        }
        catch (Exception ex)
        {
            throw new PlanarJobException("fail to create temporary argument context file", ex);
        }
    }

    private static void SetProcessToLinuxOs(ProcessStartInfo startInfo)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var filename = GetFilenameFoLinux(startInfo.FileName);
            startInfo.FileName = "dotnet";
            startInfo.Arguments = $"\"{filename}\" {startInfo.Arguments}";
        }
    }

    private static string GetFilenameFoLinux(string filename)
    {
        var fi = new FileInfo(filename);
        if (string.Equals(fi.Extension, ".exe", StringComparison.OrdinalIgnoreCase))
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            var newFileName = $"{fileNameWithoutExtension}.dll";
            return newFileName;
        }
        else
        {
            return filename;
        }
    }

    private void InterceptingPublishAsync(CloudEventArgs e)
    {
        try
        {
            InterceptingPublishAsyncInner(e);
        }
        catch (PlanarJobException ex)
        {
            _logger.LogCritical(ex, "fail intercepting published message on MQTT broker event. {Error}", ex.Message);
        }
        catch (JobMonitorException ex)
        {
            _logger.LogError(ex, "fail to execute monitor event");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "fail intercepting published message on MQTT broker event");
        }
    }

    private void WriteToConsole(LogEntity logEntity)
    {
        if (_isDevelopment)
        {
            lock (ConsoleLocker)
            {
                SetLogLineColor(logEntity);
                Console.WriteLine($" » {logEntity.Message}");
                Console.ResetColor();
            }
        }
    }

    private static void SetLogLineColor(LogEntity logEntity)
    {
        Console.ForegroundColor = logEntity.Level switch
        {
            LogLevel.Warning => ConsoleColor.DarkYellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Debug => ConsoleColor.DarkCyan,
            LogLevel.Trace => ConsoleColor.DarkGreen,
            LogLevel.Critical => ConsoleColor.Magenta,
            _ => ConsoleColor.White,
        };
    }

    protected void SafeScan(MonitorCustomEventInfo customEventInfo, IJobExecutionContext context)
    {
        try
        {
            var name = $"{nameof(MonitorEvents.CustomEvent1)[..^1]}{customEventInfo.Number}";
            if (!Enum.TryParse<MonitorEvents>(name, out var @event))
            {
                _logger.LogError("monitor event '{Name}' is not valid", name);
                return;
            }

            var exception = new PlanarJobCustomMonitorException(customEventInfo.Message ?? "[no message]");
            MonitorUtil.Scan(@event, context, exception);
        }
        catch (Exception ex)
        {
            var source = nameof(SafeScan);
            _logger.LogCritical(ex, "Error handle {Source}: {Message}", source, ex.Message);
        }
    }

    private void InterceptingPublishAsyncInner(CloudEventArgs e)
    {
        if (!Enum.TryParse<MessageBrokerChannels>(e.CloudEvent.Type, ignoreCase: true, out var channel))
        {
            _logger.LogError("message broker channels '{Type}' is not valid", e.CloudEvent.Type);
            return;
        }

        int iValue;
        KeyValueObject kv;
        switch (channel)
        {
            case MessageBrokerChannels.AddAggregateException:
                var ex = GetCloudEventEntityValue<ExceptionDto>(e.CloudEvent);
                MessageBroker.AddAggregateException(ex);
                break;

            case MessageBrokerChannels.AppendLog:
                var log = GetCloudEventEntityValue<LogEntity>(e.CloudEvent);
                MessageBroker.AppendLog(log);
                WriteToConsole(log);
                break;

            case MessageBrokerChannels.IncreaseEffectedRows:
                iValue = GetCloudEventInt32Value(e.CloudEvent);
                MessageBroker.IncreaseEffectedRows(iValue);
                break;

            case MessageBrokerChannels.SetEffectedRows:
                iValue = GetCloudEventInt32Value(e.CloudEvent);
                MessageBroker.SetEffectedRows(iValue);
                break;

            case MessageBrokerChannels.PutJobData:
                kv = GetCloudEventEntityValue<KeyValueObject>(e.CloudEvent);
                MessageBroker.PutJobDataAction(kv);
                break;

            case MessageBrokerChannels.RemoveJobData:
                kv = GetCloudEventEntityValue<KeyValueObject>(e.CloudEvent);
                MessageBroker.RemoveJobDataAction(kv);
                break;

            case MessageBrokerChannels.PutTriggerData:
                kv = GetCloudEventEntityValue<KeyValueObject>(e.CloudEvent);
                MessageBroker.PutTriggerDataAction(kv);
                break;

            case MessageBrokerChannels.RemoveTriggerData:
                kv = GetCloudEventEntityValue<KeyValueObject>(e.CloudEvent);
                MessageBroker.RemoveTriggerDataAction(kv);
                break;

            case MessageBrokerChannels.ClearTriggerData:
                MessageBroker.ClearTriggerDataAction();
                break;

            case MessageBrokerChannels.ClearJobData:
                MessageBroker.ClearJobDataAction();
                break;

            case MessageBrokerChannels.UpdateProgress:
                var progress = GetCloudEventByteValue(e.CloudEvent);
                MessageBroker.UpdateProgress(progress);
                break;

            case MessageBrokerChannels.ReportException:
                var value = GetCloudEventStringValue(e.CloudEvent);
                _executionException = new PlanarJobExecutionException(value);
                break;

            case MessageBrokerChannels.ReportExceptionV2:
                var exValue = GetCloudEventEntityValue<PlanarJobExecutionExceptionDto>(e.CloudEvent);
                _executionException = new PlanarJobExecutionException(exValue.Message ?? "[no message]")
                {
                    ExceptionText = exValue.ExceptionText,
                    MostInnerMessage = exValue.MostInnerMessage,
                    MostInnerExceptionText = exValue.MostInnerExceptionText
                };
                break;

            case MessageBrokerChannels.HealthCheck:
                _isHealthCheck = true;
                SafeUnsubscribeOutput();
                break;

            case MessageBrokerChannels.MonitorCustomEvent:
                var monitorValue = GetCloudEventEntityValue<MonitorCustomEventInfo>(e.CloudEvent);
                SafeScan(monitorValue, MessageBroker.Context);
                break;

            case MessageBrokerChannels.InvokeJob:
                var invokeValue = GetCloudEventEntityValue<InvokeJobModel>(e.CloudEvent);
                RunInvokeJob(invokeValue).Wait();
                break;

            case MessageBrokerChannels.QueueInvokeJob:
                var queueInvokeValue = GetCloudEventEntityValue<QueueInvokeJobModel>(e.CloudEvent);
                RunQueueInvokeJob(queueInvokeValue).Wait();
                break;

            default:
                _logger.LogWarning("PlanarJob intercepting published message with unsupported channel {Channel}", channel);
                break;
        }
    }

    private async Task RunInvokeJob(InvokeJobModel invokeJob)
    {
        var request = new QueueInvokeJobRequest
        {
            Id = invokeJob.Id,
            Data = invokeJob.Options?.Data,
            NowOverrideValue = invokeJob.Options?.NowOverrideValue,
            Timeout = invokeJob.Options?.Timeout,
            DueDate = DateTime.Now.AddSeconds(3), // Default due date for invoke job is 3 seconds from now
            MaxRetries = invokeJob.Options?.MaxRetries,
            RetrySpan = invokeJob.Options?.RetrySpan
        };

        await RunQueueInvokeJob(request);
    }

    private async Task RunQueueInvokeJob(QueueInvokeJobModel invokeJob)
    {
        var request = new QueueInvokeJobRequest
        {
            Id = invokeJob.Id,
            Data = invokeJob.Options?.Data,
            NowOverrideValue = invokeJob.Options?.NowOverrideValue,
            Timeout = invokeJob.Options?.Timeout,
            DueDate = invokeJob.DueDate,
            MaxRetries = invokeJob.Options?.MaxRetries,
            RetrySpan = invokeJob.Options?.RetrySpan
        };

        await RunQueueInvokeJob(request);
    }

    private async Task RunQueueInvokeJob(QueueInvokeJobRequest request)
    {
        var validator = serviceProvider.GetRequiredService<IValidator<QueueInvokeJobRequest>>();
        var jobActions = serviceProvider.GetRequiredService<IJobActions>();

        var validationResult = validator.Validate(request);
        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            Log(LogLevel.Error, $"validation failed for job invoke. job id: {request.Id}. error: {errorMessage}");
            _jobActionExceptions.Add(new PlanarJobException($"validation failed for job invoke. job id {request.Id}"));
            return;
        }

        try
        {
            var jobKey = await jobActions.InternalJobPrepareQueueInvoke(request);
            request.Data ??= [];
            request.Data.TryAdd(Consts.InvokeJobJobIdDataKey, MessageBroker.Context.JobDetail.Key.ToString());
            request.Data.TryAdd(Consts.InvokeJobInstanceIdDataKey, MessageBroker.Context.FireInstanceId);

            var trigger = await jobActions.InternalJobQueueInvoke(request, jobKey);
            Log(LogLevel.Information, $"job invoke queued successfully. job id: {request.Id}. trigger id: {trigger.Id}");
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, $"job invoke failed for. job id {request.Id}. error: {ex.Message}");
            _jobActionExceptions.Add(new PlanarJobException($"job invoke failed for. job id: {request.Id}. error: {ex.Message}"));
        }
    }

    private void Log(LogLevel level, string message)
    {
        var logEntity = new LogEntity(level, message);
        MessageBroker.AppendLog(logEntity);
        WriteToConsole(logEntity);
    }
}