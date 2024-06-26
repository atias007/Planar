using CloudNative.CloudEvents;
using CommonJob;
using CommonJob.MessageBrokerEntities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using PlanarJobInner;
using Quartz;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Planar;

public abstract class PlanarJob : BaseProcessJob<PlanarJobProperties>
{
    private readonly object ConsoleLocker = new();
    private readonly bool _isDevelopment;
    private bool _isHealthCheck;
    private string? _contextFilename;

    private PlanarJobExecutionException? _executionException;

    protected PlanarJob(
        ILogger logger,
        IJobPropertyDataLayer dataLayer,
        JobMonitorUtil jobMonitorUtil) : base(logger, dataLayer, jobMonitorUtil)
    {
        MqttBrokerService.InterceptingPublishAsync += InterceptingPublishAsync;
        _isDevelopment = string.Equals(AppSettings.General.Environment, "development", StringComparison.OrdinalIgnoreCase);
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await Initialize(context);
            ValidateProcessJob();
            ValidateExeFile();
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
            CheckJobErrorReport();
        }
        catch (Exception ex)
        {
            HandleException(context, ex);
        }
        finally
        {
            FinalizeJob(context);
            FinalizeProcess();
            UnregisterMqttBrokerService();
            SafeDeleteContextFile();
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

    private void CheckJobErrorReport()
    {
        if (_executionException == null) { return; }
        throw _executionException;
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

        if (_output.Length > 0)
        {
            MessageBroker.AppendLogRaw(_output.ToString());
        }

        throw new PlanarJobException($"WARNING! Abnormal process exit code {_process.ExitCode}. this may cause by unwaited tasks\\threads");
    }

    private void ValidateHealthCheck()
    {
        try
        {
            if (_isHealthCheck) { return; }

            if (_output.Length > 0)
            {
                var outputText = _output.ToString();
                MessageBroker.AppendLogRaw(outputText);
                if (outputText.Contains(nameof(PlanarJobException))) { return; }
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

    private void UnregisterMqttBrokerService()
    {
        try { MqttBrokerService.InterceptingPublishAsync -= InterceptingPublishAsync; } catch { DoNothingMethod(); }
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

    private void InterceptingPublishAsync(object? sender, CloudEventArgs e)
    {
        try
        {
            if (e.ClientId == FireInstanceId)
            {
                InterceptingPublishAsyncInner(e);
            }
        }
        catch (PlanarJobException ex)
        {
            _logger.LogCritical(ex, "Fail intercepting published message on MQTT broker event. {Error}", ex.Message);
        }
        catch (JobMonitorException ex)
        {
            _logger.LogError(ex, "Fail to execute monitor event");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fail intercepting published message on MQTT broker event");
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
                UnsubscribeOutput();
                break;

            default:
                _logger.LogWarning("PlanarJob intercepting published message with unsupported channel {Channel}", channel);
                break;
        }
    }
}